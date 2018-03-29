﻿using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SKit.Common;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SKit
{
    /// <summary>
    /// 游戏服务器
    /// </summary>
    public class GameServer : IDisposable
    {
        public const string MainWorldThreadName = "MainWorldThread";
        public int Id { get; }
        public bool IsRunning { get; private set; }
        /// <summary>
        /// 监听端口
        /// </summary>
        public int Port => Config.Port;

        /// <summary>
        /// 在线人数
        /// </summary>
        public int ClientCount => _sessions.Count;

        /// <summary>
        /// 登录玩家数
        /// </summary>
        public int UserCount => _users.Count;

        private readonly ConcurrentQueue<GameTask> _workingQueue = new ConcurrentQueue<GameTask>();
        private Thread _workingTask;
        //private CancellationTokenSource _listenerTokenSource;
        //private CancellationTokenSource _sendingTaskTokenSource;
        private CancellationTokenSource _workingTaskTokenSource;

        private SocketAsyncEventArgs _acceptEventArg;

        //private Task _listenerTask;

        private readonly ConcurrentQueue<GameMessage> _sendingQueue = new ConcurrentQueue<GameMessage>();
        private Thread _sendingTask;


        //private readonly ElasticPool<SocketAsyncEventArgs> _socketRecvArgsPool;//输入缓冲池
        //private readonly ElasticPool<SocketAsyncEventArgs> _socketSendArgsPool;//输出缓冲池

        private readonly ElasticPool<byte[]> _socketRecvBufferPool;//输入缓冲池
        private readonly ElasticPool<byte[]> _socketSendBufferPool;//输出缓冲池
        private readonly Packager _packager;//拆包打包器
        private readonly Serializer _serializer;//正反序列化工具
        private SKitConfig Config { get; }//配置
        private readonly IServiceCollection _services;//DI容器

        #region 连接管理
        private readonly ConcurrentDictionary<string, GameSession> _sessions = new ConcurrentDictionary<string, GameSession>();//所有连接 Key:SessionId
        private readonly ConcurrentDictionary<string, GameSession> _users = new ConcurrentDictionary<string, GameSession>();//登录的连接 Key:UserName
        #endregion

        private readonly Dictionary<string, GameProtoHandlerInfo> _handlers = new Dictionary<string, GameProtoHandlerInfo>();
        private readonly Dictionary<Type, GameController> _controllers = new Dictionary<Type, GameController>();
        public IEnumerable<GameProtoHandlerInfo> Handlers
        {
            get
            {
                return _handlers.Values;
            }
        }
        /// <summary>
        /// 核心监听客户端TCP
        /// </summary>
        private Socket _listener;
        private readonly ILogger<GameServer> _logger;

        #region 事件
        public event EventHandler<GameTaskDoneEventArgs> GameTaskDone;
        private void OnGamePlayerTaskDone(GameTaskDoneEventArgs args)
        {
            GameTaskDone?.Invoke(this, args);
        }
        /// <summary>
        /// 连接断开
        /// </summary>
        public event EventHandler<SessionCloseEventArgs> SessionClosed;
        private void OnSessionClosed(GameSession session, ClientCloseReason reason)
        {
            SessionClosed?.Invoke(this, new SessionCloseEventArgs() { GameSession = session, Reason = reason});
        }
        /// <summary>
        /// 有新的连接建立
        /// </summary>
        /// <param name="session"></param>
        /// <remarks>这里是网络通信部分，与游戏逻辑处理不是同一线程</remarks>
        public event EventHandler<SessionEnterEventArgs> NewSessionEnter;
        private void OnNewSessionConnected(GameSession session)
        {
            _logger.LogDebug($"当前连接数: {ClientCount}");
            NewSessionEnter?.Invoke(this, new SessionEnterEventArgs() { GameSession = session });
        }
        #endregion

        #region 开放方法
        public GameServer(IServiceCollection services)
        {
            _services = services;
            var provicer = services.BuildServiceProvider();
            Config = provicer.GetService<IOptions<SKitConfig>>().Value;
            Debug.Assert(Config != null, "SKitConfig Can't be NULL!");
            _serializer = provicer.GetService<Serializer>();
            Debug.Assert(_serializer != null, "ISerializable Can't be NULL!");
            _logger = provicer.GetService<ILogger<GameServer>>();
            Debug.Assert(_logger != null, "ILogger Can't be NULL!");
            _packager = provicer.GetService<Packager>();
            Debug.Assert(_packager != null, "ISPackager Can't be NULL!");
            this.Id = Config.Id;

            _socketRecvBufferPool = new ElasticPool<byte[]>(() =>
            {
                var buff = new byte[Config.RecvBufferSize];
                return buff;
            }, Config.PresetUserCount);
            _socketSendBufferPool = new ElasticPool<byte[]>(() =>
            {
                var buff = new byte[Config.SendBufferSize];
                return buff;
            }, Config.PresetUserCount);
            //_listener.AllowNatTraversal(true);
        }

        /// <summary>
        /// 启动服务器
        /// </summary>
        public void Start()
        {
            //反射
            this.ReflectProtocols();

            if (!IsRunning)
            {
                _logger.LogInformation($"启动工作线程...");
                IsRunning = true;

                //启动任务工作线程
                _workingTaskTokenSource = new CancellationTokenSource();
                _workingTask = new Thread(LoopWorking);
                _workingTask.Name = MainWorldThreadName;
                _workingTask.Start();

                _logger.LogInformation($"启动序列化线程...");
                //启动发送线程
                //_sendingTaskTokenSource = new CancellationTokenSource();
                _sendingTask = new Thread(LoopSending);
                _sendingTask.Start();

                IPEndPoint endPoint = new IPEndPoint(IPAddress.Any, Config.Port);
                _listener = new Socket(endPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

                _logger.LogInformation($"绑定端口{Config.Port}");
                _listener.Bind(endPoint);
                _listener.Listen(Config.Backlog);

                _listener.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);
                //_listener.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                //_listener.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.DontLinger, true);

                SocketAsyncEventArgs acceptEventArg = new SocketAsyncEventArgs();
                _acceptEventArg = acceptEventArg;
                acceptEventArg.Completed += acceptEventArg_Completed;

                _logger.LogInformation($"开启监听");
                if (!_listener.AcceptAsync(acceptEventArg))
                    ProcessAccept(acceptEventArg);

                _logger.LogInformation($"服务器[{Id}]已启动");

            }
        }

        public void Stop()
        {
            if (IsRunning)
            {
                try
                {
                    _logger.LogInformation($"Game Server [{Id}] Closing...");
                    //_listenerTokenSource.Cancel();
                    //_sendingTaskTokenSource.Cancel();
                    if(_acceptEventArg != null)
                    {
                        _acceptEventArg.Completed -= acceptEventArg_Completed;
                        _acceptEventArg.Dispose();
                        _acceptEventArg = null;
                    }

                    try
                    {
                        _listener.Close();
                    }
                    catch (Exception)
                    {
                        // ignored
                    }

                    int i = 1;
                    foreach (var session in _sessions.Values)
                    {
                        _logger.LogDebug("关闭第[{0}]个连接", i++);
                        try
                        {
                            CloseClientSocket(session.SocketAsyncEventArgs, ClientCloseReason.ServerClose);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, ex.Message);
                        }
                    }

                    _workingTaskTokenSource.Cancel();
                    _sendingTask.Join(10000);
                    _workingTask.Join(10000);

                    _users.Clear();

                    IsRunning = false;
                    _logger.LogInformation($"Game Server [{Id}] Closed");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, ex.Message);
                }
                finally
                {
                    GC.Collect();
                }
            }
        }

        public void Dispose()
        {
            Stop();
        }

        internal void SetLogin(GameSession session)
        {
            _users.AddOrUpdate(session.UserId, session, (username, oldSession) =>
            {
                //把原来的玩家踢下线
                CloseClientSocket(oldSession.SocketAsyncEventArgs, ClientCloseReason.Displacement);
                oldSession.Logout();
                return session;
            });
        }

        public bool IsOnline(string username)
        {
            if (string.IsNullOrEmpty(username))
                return false;
            return _users.ContainsKey(username);
        }

        /// <summary>
        /// 群发给所有连接
        /// </summary>
        public void BroadcastAllSessionAsync(Object msg)
        {
            GameMessage gmsg = new GameMessage()
            {
                MessageType = MessageType.AllSession,
                Msg = msg,
            };
            _sendingQueue.Enqueue(gmsg);
        }
        /// <summary>
        /// 群发给所有登录用户
        /// </summary>
        public void BroadcastAllUserAsync(Object msg)
        {
            GameMessage gmsg = new GameMessage()
            {
                MessageType = MessageType.AllUser,
                Msg = msg,
            };
            _sendingQueue.Enqueue(gmsg);
        }

        public void MultiSendByUserNameAsync(IEnumerable<string> usernames, Object msg)
        {
            GameMessage gmsg = new GameMessage()
            {
                MessageType = MessageType.ToMultiUsers,
                Msg = msg,
                DestIds = usernames
            };
            _sendingQueue.Enqueue(gmsg);
        }

        public void SendToSession(GameSession session, Object msg)
        {
            var buff = _socketSendBufferPool.Pop();
            try
            {
                byte[] data = _serializer.Serialize(msg);
                ArraySegment<byte> encodedMessage = _packager.Pack(data, buff, 0, buff.Length);
                session.Socket.Send(encodedMessage.Array, encodedMessage.Offset, encodedMessage.Count, SocketFlags.None);
            }
            catch (Exception)
            {
                //
            }
            finally
            {
                _socketSendBufferPool.Push(buff);
            }
        }
        /// <summary>
        /// 发送给指定登录用户
        /// </summary>
        public void SendByUserNameAsync(string username, Object msg)
        {
            GameMessage gmsg = new GameMessage()
            {
                MessageType = MessageType.ToUser,
                Msg = msg,
                DestId = username
            };
            _sendingQueue.Enqueue(gmsg);
        }
        /// <summary>
        /// 发送给指定连接
        /// </summary>
        public void SendBySessionIdAsync(string sessionId, Object msg)
        {
            GameMessage gmsg = new GameMessage()
            {
                MessageType = MessageType.ToSession,
                Msg = msg,
                DestId = sessionId
            };
            _sendingQueue.Enqueue(gmsg);
        }
        #endregion
        
        /// <summary>
        /// 接收消息前，可用于重写Filter
        /// </summary>
        /// <returns>是否过滤掉</returns>
        protected virtual bool Filter(GameSession session, GameProtoHandlerInfo handler)
        {
            if (!handler.AllowAnonymous && !session.IsAuthorized)
            {
                return true;
            }
            return false;
        }

        #region 网络部分
        void acceptEventArg_Completed(object sender, SocketAsyncEventArgs e)
        {
            ProcessAccept(e);
        }

        void ProcessAccept(SocketAsyncEventArgs e)
        {
            Socket socket = null;

            if (e.SocketError != SocketError.Success)
            {
                var errorCode = (int)e.SocketError;

                //The listen socket was closed
                if (errorCode == 995 || errorCode == 10004 || errorCode == 10038)
                    return;

                _logger.LogError(new SocketException(errorCode), e.SocketError.ToString());
            }
            else
            {
                socket = e.AcceptSocket;
            }

            e.AcceptSocket = null;

            bool willRaiseEvent = false;

            try
            {
                willRaiseEvent = _listener.AcceptAsync(e);
            }
            catch (ObjectDisposedException)
            {
                //The listener was stopped
                //Do nothing
                //make sure ProcessAccept won't be executed in this thread
                willRaiseEvent = true;
            }
            catch (NullReferenceException)
            {
                //The listener was stopped
                //Do nothing
                //make sure ProcessAccept won't be executed in this thread
                willRaiseEvent = true;
            }
            catch (Exception exc)
            {
                _logger.LogError(exc, exc.Message);
                //make sure ProcessAccept won't be executed in this thread
                willRaiseEvent = true;
            }

            try
            {
                if (socket != null)
                {
                    var args = new SocketAsyncEventArgs();
                    var buff = _socketRecvBufferPool.Pop();
                    args.SetBuffer(buff, 0, buff.Length);
                    args.Completed += IO_Completed;

                    var session = new GameSession
                    {
                        Server = this,
                        Socket = socket,
                        SocketAsyncEventArgs = args
                    };
                    args.UserToken = session;
                    _sessions.TryAdd(session.Id, session);
                    this.OnNewSessionConnected(session);

                    _logger.LogDebug($"{session.Id}: Enter");
                    bool willRaiseEventRecv = socket.ReceiveAsync(args);
                    if (!willRaiseEventRecv)
                    {
                        ProcessReceive(args);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
            }

            if (!willRaiseEvent)
                    ProcessAccept(e);
        }

        /// <summary>
        /// 拆包
        /// </summary>
        private IEnumerable<ArraySegment<byte>> Resolve(GameSession session, SocketAsyncEventArgs e)
        {
            int from = 0;
            session.BufferReaderCursor += e.BytesTransferred;//剩余可读取字节

            while (session.BufferReaderCursor > 0)
            {
                var readlength = 0;
                ArraySegment<byte> data = _packager.UnPack(e.Buffer, from, session.BufferReaderCursor, ref readlength);
                if (readlength != 0)
                {
                    yield return data;
                    session.BufferReaderCursor -= readlength;
                    from += readlength;
                    continue;
                }
                break;
            }
            if (session.BufferReaderCursor > 0)
            {
                Buffer.BlockCopy(e.Buffer, from, e.Buffer, 0, session.BufferReaderCursor);
            }
            e.SetBuffer(session.BufferReaderCursor, e.Buffer.Length - session.BufferReaderCursor);
        }

        private void ProcessReceive(SocketAsyncEventArgs e)
        {
            // check if the remote host closed the connection
            if (e.BytesTransferred > 0 && e.SocketError == SocketError.Success)
            {
                GameSession session = (GameSession)e.UserToken;
                try
                {
                    var datas = Resolve(session, e);
                    foreach (ArraySegment<byte> data in datas)
                    {
                        //消息处理: 反序列化和筛选
                        if (!DigestRecevedData(session, data))
                        {
                            if (Config.KickoutWhenProtocolError)
                            {
                                CloseClientSocket(e, ClientCloseReason.ProtocolError);
                                return;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, ex.Message);
                    CloseClientSocket(e, ClientCloseReason.ReceiveDataError);
                    return;
                }

                //PostReceive(receiveEventArgs);    
                bool willRaiseEvent = session.Socket.ReceiveAsync(e);
                if (!willRaiseEvent)
                {
                    ProcessReceive(e);
                }
            }
            else
            {
                CloseClientSocket(e, ClientCloseReason.ClientClose);
            }
        }

        private void CloseClientSocket(SocketAsyncEventArgs e, ClientCloseReason reason)
        {
            GameSession token = e.UserToken as GameSession;
            // close the socket associated with the client
            if (token == null)
                return;

            try
            {
                this.OnSessionClosed(token, reason);
                if (token.Socket.Connected)
                    token.Socket.Shutdown(SocketShutdown.Both);
            }
            catch (Exception)
            {
                // ignored
            }

            try
            {
                token.Socket.Close();
            }
            catch (Exception)
            {
                // ignored
            }

            if(_sessions.TryRemove(token.Id, out _))
            {
                if (token.UserId != null && _users.TryRemove(token.UserId, out _))
                {
                    var task = new GamePlayerLeaveTask(token, reason, _controllers.Values);
                    if (reason == ClientCloseReason.Displacement)
                    {
                        try
                        {
                            task.DoAction();
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, ex.Message);
                        }
                    }
                    else
                    {
                        //任务队列加入玩家离线
                        _workingQueue.Enqueue(task);
                    }
                }
                _socketRecvBufferPool.Push(e.Buffer);
                token?.Dispose();
                // Free the SocketAsyncEventArg so they can be reused by another client
                _logger.LogDebug($"{token.Id}: LEAVE, reason: {reason}|当前连接数: {ClientCount}");
            }
        }

        private void ProcessSend(SocketAsyncEventArgs e)
        {
            _socketSendBufferPool.Push(e.Buffer);
            e.Completed -= IO_Completed;
            e.Dispose();
        }

        private void IO_Completed(object sender, SocketAsyncEventArgs e)
        {
            switch (e.LastOperation)
            {
                case SocketAsyncOperation.Receive:
                    ProcessReceive(e);
                    break;
                case SocketAsyncOperation.Send:
                    ProcessSend(e);
                    break;
                default:
                    throw new ArgumentException("The last operation completed on the socket was not a receive or send");
            }
        }
        #endregion

        #region 任务执行派发部分
        private void LoopSending()
        {
            while (!_workingTaskTokenSource.IsCancellationRequested)
            {
                try
                {
                    if (!_sendingQueue.IsEmpty)
                    {
                        while (_sendingQueue.TryDequeue(out var message))
                        {
                            try
                            {
                                switch (message.MessageType)
                                {
                                    case MessageType.AllSession:
                                        {
                                            foreach (var session in _sessions.Values)
                                            {
                                                var args = new SocketAsyncEventArgs();
                                                args.Completed += IO_Completed;
                                                var buff = _socketSendBufferPool.Pop();
                                                args.SetBuffer(buff, 0, buff.Length);
                                                byte[] data = _serializer.Serialize(message.Msg);
                                                ArraySegment<byte> encodedMessage = _packager.Pack(data, args.Buffer, 0, args.Buffer.Length);
                                                args.SetBuffer(0, encodedMessage.Count);

                                                if (!session.Socket.SendAsync(args))
                                                {
                                                    ProcessSend(args);
                                                }
                                            }
                                        }
                                        break;
                                    case MessageType.AllUser:
                                        {
                                            foreach (var session in _users.Values)
                                            {
                                                var args = new SocketAsyncEventArgs();
                                                args.Completed += IO_Completed;
                                                var buff = _socketSendBufferPool.Pop();
                                                args.SetBuffer(buff, 0, buff.Length);
                                                byte[] data = _serializer.Serialize(message.Msg);
                                                ArraySegment<byte> encodedMessage = _packager.Pack(data, args.Buffer, 0, args.Buffer.Length);
                                                args.SetBuffer(0, encodedMessage.Count);
                                                if (!session.Socket.SendAsync(args))
                                                {
                                                    ProcessSend(args);
                                                }
                                            }
                                        }
                                        break;
                                    case MessageType.ToSession:
                                        {
                                            if (_sessions.TryGetValue(message.DestId, out var session))
                                            {
                                                var args = new SocketAsyncEventArgs();
                                                args.Completed += IO_Completed;
                                                var buff = _socketSendBufferPool.Pop();
                                                args.SetBuffer(buff, 0, buff.Length);
                                                byte[] data = _serializer.Serialize(message.Msg);
                                                ArraySegment<byte> encodedMessage = _packager.Pack(data, args.Buffer, 0, args.Buffer.Length);
                                                args.SetBuffer(0, encodedMessage.Count);
                                                if (!session.Socket.SendAsync(args))
                                                {
                                                    ProcessSend(args);
                                                }
                                            }
                                        }
                                        break;
                                    case MessageType.ToUser:
                                        {
                                            if (_users.TryGetValue(message.DestId, out var session))
                                            {
                                                var args = new SocketAsyncEventArgs();
                                                args.Completed += IO_Completed;
                                                var buff = _socketSendBufferPool.Pop();
                                                args.SetBuffer(buff, 0, buff.Length);
                                                byte[] data = _serializer.Serialize(message.Msg);
                                                ArraySegment<byte> encodedMessage = _packager.Pack(data, args.Buffer, 0, args.Buffer.Length);
                                                args.SetBuffer(0, encodedMessage.Count);
                                                if (!session.Socket.SendAsync(args))
                                                {
                                                    ProcessSend(args);
                                                }
                                            }
                                        }
                                        break;
                                    case MessageType.ToMultiUsers:
                                        {
                                            if(message.DestIds != null)
                                            {
                                                var args = new SocketAsyncEventArgs();
                                                args.Completed += IO_Completed;
                                                var buff = _socketSendBufferPool.Pop();
                                                args.SetBuffer(buff, 0, buff.Length);
                                                byte[] data = _serializer.Serialize(message.Msg);
                                                ArraySegment<byte> encodedMessage = _packager.Pack(data, args.Buffer, 0, args.Buffer.Length);
                                                args.SetBuffer(0, encodedMessage.Count);
                                                foreach (var username in message.DestIds)
                                                {
                                                    if (_users.TryGetValue(username, out var session))
                                                    {
                                                        if (!session.Socket.SendAsync(args))
                                                        {
                                                            ProcessSend(args);
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                        break;
                                    case MessageType.ToMultiSessions:
                                        {
                                            if (message.DestIds != null)
                                            {
                                                var args = new SocketAsyncEventArgs();
                                                args.Completed += IO_Completed;
                                                var buff = _socketSendBufferPool.Pop();
                                                args.SetBuffer(buff, 0, buff.Length);
                                                byte[] data = _serializer.Serialize(message.Msg);
                                                ArraySegment<byte> encodedMessage = _packager.Pack(data, args.Buffer, 0, args.Buffer.Length);
                                                args.SetBuffer(0, encodedMessage.Count);
                                                foreach (var id in message.DestIds)
                                                {
                                                    if (_sessions.TryGetValue(id, out var session))
                                                    {
                                                        if (!session.Socket.SendAsync(args))
                                                        {
                                                            ProcessSend(args);
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                        break;
                                }
                            }
                            catch (Exception)
                            {
                                //ignore
                            }
                        }
                    }
                    Thread.Sleep(1);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, ex.Message);
                }
            }
        }

        internal GameSession CurrentWorkingSession;
        private void LoopWorking()
        {
            while (!_workingTaskTokenSource.IsCancellationRequested)
            {
                try
                {
                    if (!_workingQueue.IsEmpty)
                    {
                        while (_workingQueue.TryDequeue(out var task))
                        {
                            var t = task as GamePlayerTask;
                            if(t != null)
                            {
                                CurrentWorkingSession = t.Session;
                                int result = task.DoAction();

                                this.OnGamePlayerTaskDone(new GameTaskDoneEventArgs()
                                {
                                    GameSession = t.Session,
                                    ResultCode = result,
                                });
                            }
                        }
                    }
                }
                catch (System.Data.Common.DbException ex)
                {
                    //数据库异常，宕机吧
                    _logger.LogError(ex, ex.Message);
                    throw;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, ex.Message);
                }
                Thread.Sleep(1);
            }
        }

        /// <summary>
        /// 处理消息包
        /// </summary>
        /// <returns>是否执行成功</returns>
        protected bool DigestRecevedData(GameSession session, ArraySegment<byte> data)
        {
            string cmd = _serializer.DataToCmd(data.Array, data.Offset, data.Count);
            if (cmd == null || !this._handlers.ContainsKey(cmd))
            {
                //如果没有处理器，先刷掉一批
                return false;
            }

            var handler = this._handlers[cmd];
            var type = handler.RequestType;
            if (Filter(session, handler))
            {
                return false;
            }

            var request = _serializer.Deserialize(type, data.Array, data.Offset, data.Count);
            var task = new GameRequestTask(handler, session, request);
            if (handler.IsAsynchronous)
            {
                //当遇到allowanonymous的任务时，即表示此任务不需要其他游戏逻辑线程同步，只要session同步即可，那么可以不放入逻辑线程而直接执行
                task.DoAction();
            }
            else
            {
                this._workingQueue.Enqueue(task);
            }
            return true;
        }
        #endregion

        #region 通讯协议约定
        /// <summary>
        /// 获得其他控制器
        /// </summary>
        public T GetController<T>() where T : GameController
        {
            Type t = typeof(T);
            return _controllers[t] as T;
        }
        /// <summary>
        /// 消息驱动，通过消息实体名找处理函数
        /// </summary>
        private void ReflectProtocols()
        {
            _logger.LogInformation($"开始读取模块");
            foreach (var type in Assembly.GetEntryAssembly().ExportedTypes)
            {
                if (type.GetTypeInfo().BaseType == typeof(GameController))
                {
                    _services.AddTransient(typeof(GameController), type);
                }
            }

            var provider = _services.BuildServiceProvider();
            var controllers = provider.GetServices<GameController>();
            foreach (var controller in controllers)
            {
                Type type = controller.GetType();
                controller.Server = this;
                _controllers.Add(type, controller);

                MethodInfo[] methods = type.GetMethods(BindingFlags.Public | BindingFlags.DeclaredOnly | BindingFlags.Instance);
                foreach (var methodInfo in methods)
                {
                    if (methodInfo.IsSpecialName || !methodInfo.Name.StartsWith("Call_"))
                    {
                        continue;
                    }
                    if(methodInfo.ReturnType != typeof(int))
                    {
                        _logger.LogWarning($"Method handler [{controller.GetType().Name}.{methodInfo.Name}] has wrong return type!");
                        continue;
                    }
                    ParameterInfo[] parameterInfos = methodInfo.GetParameters();
                    bool allowanonymous = methodInfo.GetCustomAttribute<AllowAnonymousAttribute>() != null;
                    bool isAsynchronous = methodInfo.GetCustomAttribute<AsynchronousAttribute>() != null;
                    if (parameterInfos.Length > 2 || parameterInfos.Length < 1)
                    {
                        _logger.LogWarning($"Protocol Handler {controller.GetType().Name }.{methodInfo.Name} parameters count wrong!");
                    }
                    else
                    {
                        List<Type> parameterTypes = new List<Type>();
                        foreach (var p in parameterInfos)
                        {
                            parameterTypes.Add(p.ParameterType);
                        }
                        var requestType = parameterTypes.FirstOrDefault(x=>x != typeof(GameSession));
                        if (requestType == null)
                        {
                            _logger.LogWarning($"Protocol Handler {controller.GetType().Name }.{methodInfo.Name} parameters not contains request entity!");
                            continue;
                        }
                        String cmd = requestType.Name;
                        Type methodGenericType;
                        GameProtoHandlerParameters paramSeq;
                        if (parameterTypes.Count == 1)
                        {
                            methodGenericType = typeof(Func<,>);
                            paramSeq = GameProtoHandlerParameters.Request;
                        }
                        else
                        {
                            methodGenericType = typeof(Func<,,>);
                            if (parameterTypes[0] == typeof(GameSession))
                            {
                                paramSeq = GameProtoHandlerParameters.GameSessionAndRequest;
                            }
                            else
                            {
                                paramSeq = GameProtoHandlerParameters.RequestAndGameSession;
                            }
                        }
                        parameterTypes.Add(typeof(int));
                        Type methodType = methodGenericType.MakeGenericType(parameterTypes.ToArray());
                        Delegate actionMethod = Delegate.CreateDelegate(methodType, controller, methodInfo);
                        var handler = new GameProtoHandlerInfo()
                        {
                            CMD = cmd,
                            MethodInfo = methodInfo,
                            ProcessAction = actionMethod,
                            RequestType = requestType,
                            Controller = controller,
                            AllowAnonymous = allowanonymous,
                            IsAsynchronous = isAsynchronous,
                            ParameterTypes = paramSeq
                        };
                        GameProtoHandlerInfo oldhandler = null;
                        if (!_handlers.TryGetValue(handler.CMD, out oldhandler))
                        {
                            _handlers.Add(handler.CMD, handler);
                            _serializer.Register(requestType);
                        }
                        else
                        {
                            _logger.LogWarning($"Request CMD [{handler.CMD}] in [{handler.Controller.GetType().Name }.{handler.MethodInfo.Name}] already declared in method: [{oldhandler.Controller.GetType().Name }.{oldhandler.MethodInfo.Name}]");
                        }
                    }
                }
            }

            foreach (var controller in controllers)
            {
                _logger.LogInformation($"加载模块:{controller.GetType().Name}");
                controller.RegisterEvents();
            }
        }

        #endregion 
    }
}
