﻿using Frontline.Common;
using Frontline.Data;
using Frontline.Domain;
using Frontline.GameDesign;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using protocol;
using SKit;
using SKit.Common;
using SKit.Common.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace Frontline.GameControllers
{
    public class PlayerController : GameController
    {
        public const String DES_KEY = "";

        private DataContext _db;
        private GameConfig _config;
        private GameDesignContext _designDb;
        private ILogger<PlayerController> _logger;

        Dictionary<int, DLevel> _dlevels;
        Dictionary<string, Player> _players = new Dictionary<string, Player>();
        Dictionary<string, PlayerBaseInfo> _simplePlayers = new Dictionary<string, PlayerBaseInfo>();

        public Dictionary<int, VIPPrivilege> VIP { get; private set; }

        public PlayerController(DataContext db, GameDesignContext design, IOptions<GameConfig> config, ILogger<PlayerController> logger)
        {
            _db = db;
            _config = config.Value;
            _designDb = design;
            _logger = logger;
        }

        public PlayerController()
        {

        }

        protected override void OnReadGameDesignTables()
        {
            _dlevels = _designDb.DLevels.AsNoTracking().ToDictionary(x => x.level, x => x);
            VIP = _designDb.VIPPrivileges.AsNoTracking().ToDictionary(x=>x.lv, x=>x);
        }

        protected override void OnRegisterEvents()
        {
            base.OnRegisterEvents();
            var camp = Server.GetController<CampController>();
            camp.UnitLevelUp += (o, e) => UpdateMaxPower();
            camp.UnitGradeUp += (o, e) => UpdateMaxPower();
            camp.EquipLevelUp += (o, e) => UpdateMaxPower();
            camp.EquipGradeUp += (o, e) => UpdateMaxPower();
            camp.TeamSettingChanged += (o, e) => UpdateMaxPower();

            Server.GameTaskDone += Server_GameTaskDone;
            Server.SessionClosed += Server_SessionClosed;
        }

        private void Server_SessionClosed(object sender, SessionCloseEventArgs e)
        {
            if(e.Reason == ClientCloseReason.Displacement)
            {
                KickOutNotify notify = new KickOutNotify();
                notify.success = true;
                Server.SendToSession(e.GameSession, notify);
            }
        }

        private void Server_GameTaskDone(object sender, GameTaskDoneEventArgs e)
        {
            //错误码统一管理
            if(e.ResultCode != 0)
            {
                var code = (GameErrorCode)e.ResultCode;
                e.GameSession.SendAsync(new LevelupUnitResponse()
                {
                    success = false,
                    info = code.ToString()
                });
            }
        }

        private void UpdateMaxPower()
        {
            var player = CurrentSession.GetBindPlayer();
            var team = player.Teams.FirstOrDefault(x=>x.IsSelected);
            if(team != null)
            {
                int power = player.Units.Where(x =>team.Units.Object.Contains(x.Id)).Sum(x => x.Power);
                if (player.MaxPower < power)
                {
                    player.MaxPower = power;
                    _db.SaveChanges();
                    //最大战力改变了
                    _logger.LogDebug($"玩家{player.Id}:{player.NickName}最大战力改变{player.MaxPower}");

                    MaxPowerChangeNotify notify = new MaxPowerChangeNotify()
                    {
                        Power = (int)player.MaxPower,
                        success = true,
                    };
                    CurrentSession.SendAsync(notify);
                }
            }
        }

        public override void OnLeave(ClientCloseReason reason)
        {
            base.OnLeave(reason);

            var player = CurrentSession.GetBindPlayer();
            //player.OnlineTime += (DateTime.Now - player.LastLoginTime);

            _db.SaveChanges();
        }

        #region API
        public Player QueryPlayer(string pid)
        {
            if(!_players.TryGetValue(pid, out var player))
            {
                var queryPlayer = _db.Players
                     .Where(p => p.Id == pid);
                PlayerLoader loader = new PlayerLoader()
                {
                    Loader = queryPlayer
                };
                this.OnPlayerLoading(loader);
                player = loader.Loader.FirstOrDefault();
                if(player != null)
                {
                    OnPlayerLoaded(player);
                    //check new day refresh
                    _db.SaveChanges();

                    _players.Add(pid, player);
                }
            }

            //检查每日刷新
            if (player.LastDayRefreshTime.Date != DateTime.Today)
            {
                //需要刷新
                OnPlayerEverydayRefresh(player);

                player.LastDayRefreshTime = DateTime.Today;
                _db.SaveChanges();
            }
            return player;
        }

        public PlayerBaseInfo QueryPlayerBaseInfo(string pid)
        {
            if (_players.TryGetValue(pid, out var player))
            {
                return player;
            }
            if (!_simplePlayers.TryGetValue(pid, out var simple))
            {
                simple = _db.Players
                     .Where(p => p.Id == pid).AsNoTracking().FirstOrDefault();
                if(simple != null)
                {
                    _simplePlayers.Add(pid, simple);
                }
            }
            return simple;
        }
        #endregion

        #region 事件
        /// <summary>
        /// 创建角色的时候
        /// </summary>
        /// <remarks>与读取角色事件互斥</remarks>
        internal event EventHandler<Player> PlayerCreating;
        private void OnPlayerCreating(Player player)
        {
            PlayerCreating?.Invoke(this, player);
        }
        /// <summary>
        /// 读取角色的时候
        /// </summary>
        /// <remarks>与创建角色事件互斥</remarks>
        internal event EventHandler<PlayerLoader> PlayerLoading;
        private void OnPlayerLoading(PlayerLoader loader)
        {
            loader.Loader = loader.Loader.Include(p => p.Wallet);
            PlayerLoading?.Invoke(this, loader);            
        }

        internal event EventHandler<Player> PlayerLoaded;
        private void OnPlayerLoaded(Player player)
        {
            PlayerLoaded?.Invoke(this, player);
        }

        internal event EventHandler<Player> PlayerEntered;
        private void OnPlayerEntered(Player player)
        {
            PlayerEntered?.Invoke(this, player);
        }

        internal event EventHandler<Player> PlayerEverydayRefresh;
        private void OnPlayerEverydayRefresh(Player player)
        {
            PlayerEverydayRefresh?.Invoke(this, player);
        }
        #endregion

        #region 辅助方法
        public int GetCurrencyValue(Player player, int type)
        {
            return player.Wallet.GetCurrency(type);
        }

        public bool IsCurrencyEnough(Player player, int type, int value)
        {
            if (value == 0)
            {
                return true;
            }
            var currency = player.Wallet.GetCurrency(type);
            return currency >= value;
        }

        public void AddCurrencies(Player player, int[] types, int[] values, string reason, double addex = 0)
        {
            ResourceAmountChangedNotify notify = new ResourceAmountChangedNotify();
            notify.success = true;
            notify.items = new List<ResourceInfo>();
            for (int i = 0; i < types.Length; i++)
            {
                int type = types[i];
                int value = (int)(values[i] * (1 + addex));
                if (value == 0)
                {
                    continue;
                }
                var currency = player.Wallet.AddCurrency(type, value);
                notify.items.Add(new ResourceInfo()
                {
                    type = 1,
                    id = type,
                    count = currency
                });
            }
            Server.SendByUserNameAsync(player.Id, notify);
        }

        /// <summary>
        /// 添加资源
        /// </summary>
        public int AddCurrency(Player player, int type, int value, string reason)
        {
            if (value == 0)
            {
                return player.Wallet.GetCurrency(type);
            }
            var currency = player.Wallet.AddCurrency(type, value);

            ResourceAmountChangedNotify notify = new ResourceAmountChangedNotify();
            notify.success = true;
            notify.items = new List<ResourceInfo>()
            {
                new ResourceInfo()
                {
                    type = 1,
                    id = type,
                    count = currency
                }
            };
            Server.SendByUserNameAsync(player.Id, notify);
            return currency;
        }
        /// <summary>
        /// 添加经验
        /// </summary>
        public void AddExp(Player player, int exp, string reason)
        {
            player.Exp += exp;
            if (player.Level == _dlevels.Count)
            {
                var dl = _dlevels[_dlevels.Count];
                if(player.Exp >= dl.exp)
                {
                    player.Exp = dl.exp;
                }
            }
            else
            {
                int old = player.Level;
                while (true)
                {
                    if(player.Level == _dlevels.Count)
                    {
                        break;
                    }
                    DLevel dl;
                    if (_dlevels.TryGetValue(player.Level, out dl))
                    {
                        if (player.Exp >= dl.exp)
                        {
                            player.Level += 1;
                        }
                        else
                        {
                            break;
                        }
                    }
                    else
                    {
                        break;
                    }
                }
                if (old != player.Level)
                {
                    //任务
                    //通知
                    LevelupNotify notify = new LevelupNotify();
                    notify.exp = player.Exp;
                    notify.level = player.Level;
                    notify.success = true;
                    Server.SendByUserNameAsync(player.Id, notify);//发送给当前player 
                }
            }            
        }
        #endregion

        #region 客户端接口

        /// <summary>
        /// 登录
        /// </summary>
        [AllowAnonymous]
        public int Call_Login(GameSession session, AuthRequest au)
        {
            if (session.IsAuthorized)
            {
                return (int)GameErrorCode.重复登录;
            }
            var json = JObject.Parse(DES.DesDecrypt(au.loginid, _config.DESKey));
            var ucenterId = json.Value<long>("id");
            var usercode = json.Value<String>("usercode");
            var bind = json.Value<bool>("bind");
            Player player = null;
            if (!_db.Players.Any(p => p.UserCenterId == ucenterId))
            {
                //创建角色信息
                player = new Player();
                player.Id = $"S{session.Server.Id}P{ucenterId}";
                int numb = _db.Players.Count();
                player.NickName = "No." + numb;
                player.UserCenterId = ucenterId;
                player.UserCode = usercode;
                player.Camp = 1;
                player.Icon = "touxiang6";
                player.Level = 1;
                player.Version = au.ver;
                player.VIP = 0;
                player.LastVipUpTime = player.LastLvUpTime = player.LastLoginTime = player.CreateTime = DateTime.Now;
                player.IsBind = false;
                player.IP = (session.Socket.RemoteEndPoint as IPEndPoint)?.Address.ToString();
                player.Wallet = new Wallet()
                {
                    PlayerId = player.Id,
                };
                //初始化资源
                player.Wallet.AddCurrency(CurrencyType.GOLD, 300000);
                player.Wallet.AddCurrency(CurrencyType.DIAMOND, 12888);
                player.Wallet.AddCurrency(CurrencyType.IRON, 10000);
                player.Wallet.AddCurrency(CurrencyType.SUPPLY, 10000);
                player.Wallet.AddCurrency(CurrencyType.OIL, 300);
         
                _db.Players.Add(player);
                this.OnPlayerCreating(player);
                player.MaxPower = player.Units.Sum(x => x.Power);
                _db.SaveChanges();
            }
            else
            {
                //var queryPlayer = _db.Players
                //    .Where(p => p.UserCode == usercode);
                //PlayerLoader loader = new PlayerLoader()
                //{
                //    Loader = queryPlayer
                //};
                //this.OnPlayerLoading(loader);
                //player = loader.Loader.First();
                //_db.SaveChanges();
                //OnPlayerLoaded(player);
                player = this.QueryPlayer(_db.Players.Where(p=>p.UserCode == usercode).Select(p=>p.Id).First());
                player.LastLoginTime = DateTime.Now;
                _db.SaveChanges();
            }
            session.Login(player.Id);
            session.SetBind(player);
            if (!_players.ContainsKey(player.Id))
            {
                _players.Add(player.Id, player);
            }
            this.OnPlayerEntered(player);
            AuthResponse response = new AuthResponse()
            {
                success = true,
                pid = player.Id
            };
            session.SendAsync(response);

            return 0;
        }

        //[AllowAnonymous]
        //public void CreatePlayer(CreatePlayerRequest au)
        //{
        //    if (CurrentSession.IsAuthorized)
        //    {
        //        return;
        //    }
        //    CreatePlayerResponse response = new CreatePlayerResponse();
        //    CurrentSession.SendAsync(response);
        //}

        [AllowAnonymous]
        [Asynchronous]
        public int Call_Ping(GameSession session, Ping ping)
        {
            session.SendAsync(new Pong() { success = true, time = DateTime.Now.ToUnixTime() });
            return 0;
        }

        public int Call_GetPlayerRes(ResRequest request)
        {
            var player = CurrentSession.GetBind<Player>();
            ResResponse response = new ResResponse();
            response.success = true;
            response.pid = player.Id;
            response.level = player.Level;
            response.nickyName = player.NickName;
            response.icon = player.Icon;
            response.exp = player.Exp;
            response.renameCnt = player.RenameNumb;
            response.vip = player.VIP;

            //检查每日刷新
            if (player.LastDayRefreshTime.Date != DateTime.Today)
            {
                //需要刷新
                OnPlayerEverydayRefresh(player);

                player.LastDayRefreshTime = DateTime.Today;
                _db.SaveChanges();
            }
            response.resInfos = new List<ResInfo>();
            for (int ct = 1; ct <= CurrencyType.MAX_TYPE; ct++)
            {
                response.resInfos.Add(new ResInfo()
                {
                    type = ct,
                    count = player.Wallet.GetCurrency(ct)
                });
            }
            
            //一些配置表的内容
            response.nextExp = _dlevels[player.Level].exp;
            response.resistMaxWave = 1;
            response.preExp = player.Level == 1 ? 0:  _dlevels[player.Level - 1].exp;
            CurrentSession.SendAsync(response);
            return 0;
        }

        public int Call_GetGuide(GuideInfoRequest request)
        {
            var player = CurrentSession.GetBind<Player>();
            GuideInfoResponse response = new GuideInfoResponse();
            response.success = true;
            response.id = player.Id;
            response.guide = 1000;// player.Guide;
            CurrentSession.SendAsync(response);
            return 0;
        }

        public int Call_SetGuide(GuideDoneRequest request)
        {
            var player = CurrentSession.GetBind<Player>();
            player.Guide = request.gindex;

            _db.SaveChanges();

            GuideDoneResponse response = new GuideDoneResponse();
            response.success = true;
            response.gindex = player.Guide;
            CurrentSession.SendAsync(response);
            return 0;
        }

        /// <summary>
        /// 充值详情
        /// </summary>
        /// <param name="session"></param>
        /// <param name="request"></param>
        public int Call_RechargeInfo(RechargeInfoRequest request)
        {
            RechargeInfoResponse response = JsonConvert.DeserializeObject<RechargeInfoResponse>("{ \"rechargeDiamond\":0,\"rechargeInfos\":[],\"diamondConsume\":0,\"success\":true}");
            CurrentSession.SendAsync(response);
            return 0;
        }
        

        public int Call_ShowPlayer(GameSession session, ShowPlayerRequest request)
        {
            string pid = request.pid;
            var player = session.GetBindPlayer();
            Player other = this.QueryPlayer(pid);
            if (other == null)
            {
                return (int)GameErrorCode.查无此人;
            }
            ShowPlayerResponse response = new ShowPlayerResponse();
            response.success = true;
            response.pid = pid;
            response.power = other.MaxPower;
            response.vip = other.VIP;
            response.isfriend = player.FriendList.Friends.Any(f => f.PlayerId == other.Id);
            response.icon = other.Icon;
            response.name = other.NickName;
            response.level = other.Level;

            session.SendAsync(response);
            return 0;
        }

        public int Call_RenameAndIcon(RenameAndIconRequest request)
        {
            RenameAndIconResponse r = new RenameAndIconResponse();
            r.success = true;
            r.nickyName = request.nickyName;
            r.icon = request.icon;
            //todo: 判断和谐字
            if(request.nickyName == null || request.nickyName.Length <= 2 || request.nickyName.Length >= 20)
            {
                return (int)GameErrorCode.名字长度不符合规则;
            }
            bool dup = _db.Players.Any(p => p.NickName == request.nickyName);
            if (dup)
            {
                return (int)GameErrorCode.名字已被使用;
            }
            var player = this.CurrentSession.GetBindPlayer();

            bool change = false;
            if (request.nickyName != player.NickName)
            {
                int[] costs = GameConfig.RenameCostDiamond;
                int cost = costs[costs.Length - 1];
                if (player.RenameNumb < costs.Length)
                {
                    cost = costs[player.RenameNumb];
                }

                if(player.Wallet.DIAMOND < cost)
                {
                    return (int)GameErrorCode.资源不足;
                }
                player.NickName = request.nickyName;
                player.RenameNumb++;
                change = true;
            }
            if(request.icon != player.Icon)
            {
                player.Icon = request.icon;
            }
            if (change)
            {
                _db.SaveChanges();
            }
            CurrentSession.SendAsync(r);
            return 0;
        }
        #endregion
    }
}
