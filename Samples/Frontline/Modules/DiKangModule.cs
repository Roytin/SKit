﻿using Frontline.Data;
using Newtonsoft.Json;
using protocol;
using SKit;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Frontline.Domain;
using Frontline.GameDesign;
using System.Linq;
using SKit.Common.Utils;
using Frontline.Common;

namespace Frontline.Modules
{
    public class DiKangModule : GameModule
    {

        private DataContext _db;

        public Dictionary<int, DDiKangQianXian> DDiKangQianXians { get; private set; }
        public Dictionary<int, DDiKangQianXianBuilding> DDiKangQianXianBuildings { get; private set; }
        public Dictionary<int, DDiKangQianXianBox> DDiKangQianXianBoxs { get; private set; }

        private DungeonController _dungeonModule;
        public DiKangModule(DataContext db, GameDesignContext design)
        {
            _db = db;
        }

        protected override void OnConfiguringModules()
        {
            //事件注册
            var playerModule = this.Server.GetModule<PlayerModule>();
            _dungeonModule = this.Server.GetModule<DungeonController>();

            var design = Server.GetModule<DesignDataModule>();
            design.Register(this, designDb =>
            {
                DDiKangQianXians = designDb.DDiKangQianXians.AsNoTracking().ToDictionary(x => x.wid, x => x);
                DDiKangQianXianBuildings = designDb.DDiKangQianXianBuildings.AsNoTracking().ToDictionary(x => x.id, x => x);
                DDiKangQianXianBoxs = designDb.DDiKangQianXianBoxs.AsNoTracking().ToDictionary(x=>x.id, x=>x);
            });
        }


        #region 事件

        #endregion

        #region 辅助方法
        private Dictionary<string, DiKang> _dikangs = new Dictionary<string, DiKang>();
        public DiKang QueryDiKang(string pid)
        {
            if (!_dikangs.TryGetValue(pid, out var dikang))
            {
                dikang = _db.DiKangs.FirstOrDefault(d => d.PlayerId == pid);
                if (dikang == null)
                {
                    dikang = new DiKang();
                    dikang.PlayerId = pid;
                    dikang.LastRefreshTime = DateTime.Now;
                    dikang.RecvBox = string.Empty;
                    dikang.ResetNumb = GameConfig.MaxDiKangNumbOneDay;
                    dikang.Current = 0;
                    dikang.Best = 0;
                    _db.DiKangs.Add(dikang);
                    _db.SaveChanges();
                }
                _dikangs.Add(pid, dikang);
            }

            if (dikang.LastRefreshTime != DateTime.Today)
            {
                dikang.RecvBox = string.Empty;
                dikang.ResetNumb = GameConfig.MaxDiKangNumbOneDay;
                _db.SaveChanges();
            }
            return dikang;
        }

        public ResistWaveInfo ToInfo(DDiKangQianXian w)
        {
            ResistWaveInfo info = new ResistWaveInfo();
            info.command = w.command;
            DDiKangQianXianBuilding du = DDiKangQianXianBuildings[w.command];
            info.defence = du.defence;
            info.hp = du.hp;
            info.token = w.token;
            info.wid = w.wid;
            List<MonsterInfo> ms = new List<MonsterInfo>();
            info.monsters = new List<MonsterInfo>();

            foreach (int mid in w.monsters.Object)
            {
                var dm = _dungeonModule.DMonsters[mid];
                MonsterInfo mi = _dungeonModule.ToMonsterInfo(mid, 1);
                info.monsters.Add(mi);
            }
            return info;
        }
        #endregion
    }
}