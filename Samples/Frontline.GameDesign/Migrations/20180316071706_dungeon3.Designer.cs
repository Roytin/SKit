﻿// <auto-generated />
using Frontline.GameDesign;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.EntityFrameworkCore.Storage.Internal;
using System;

namespace Frontline.GameDesign.Migrations
{
    [DbContext(typeof(GameDesignContext))]
    [Migration("20180316071706_dungeon3")]
    partial class dungeon3
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn)
                .HasAnnotation("ProductVersion", "2.0.1-rtm-125");

            modelBuilder.Entity("Frontline.GameDesign.DDungeon", b =>
                {
                    b.Property<int>("id")
                        .ValueGeneratedOnAdd();

                    b.Property<string>("desc")
                        .HasMaxLength(32);

                    b.Property<string>("drop_items")
                        .HasMaxLength(128);

                    b.Property<int>("exp");

                    b.Property<int>("exp_element");

                    b.Property<int>("fight_times");

                    b.Property<int>("gold");

                    b.Property<string>("icon")
                        .HasMaxLength(32);

                    b.Property<int>("level_limit");

                    b.Property<string>("map_fighting")
                        .HasMaxLength(32);

                    b.Property<string>("map_file_name")
                        .HasMaxLength(32);

                    b.Property<string>("map_res_name")
                        .HasMaxLength(32);

                    b.Property<int>("mission");

                    b.Property<string>("name")
                        .HasMaxLength(32);

                    b.Property<int>("next");

                    b.Property<int>("oil_cost");

                    b.Property<int>("power");

                    b.Property<int>("random_id");

                    b.Property<string>("screen_id")
                        .HasMaxLength(32);

                    b.Property<int>("section");

                    b.Property<string>("section_name")
                        .HasMaxLength(32);

                    b.Property<int>("time_limit_1");

                    b.Property<int>("time_limit_2");

                    b.Property<int>("time_limit_3");

                    b.Property<int>("type")
                        .HasMaxLength(32);

                    b.Property<string>("type_name")
                        .HasMaxLength(32);

                    b.Property<int>("wipe_cost");

                    b.HasKey("id");

                    b.HasIndex("section", "type");

                    b.ToTable("DDungeons");
                });

            modelBuilder.Entity("Frontline.GameDesign.DItem", b =>
                {
                    b.Property<int>("tid")
                        .ValueGeneratedOnAdd();

                    b.Property<int>("bag_type");

                    b.Property<int>("breakCount");

                    b.Property<int>("breakRandomId");

                    b.Property<int>("breakUnitId");

                    b.Property<TimeSpan?>("cd");

                    b.Property<int>("diamond");

                    b.Property<int>("icon");

                    b.Property<string>("name")
                        .HasMaxLength(32);

                    b.Property<int>("overlap");

                    b.Property<int>("price");

                    b.Property<int>("quality");

                    b.Property<int>("type");

                    b.Property<bool>("useable");

                    b.Property<int>("worth");

                    b.HasKey("tid");

                    b.ToTable("DItems");
                });

            modelBuilder.Entity("Frontline.GameDesign.DLevel", b =>
                {
                    b.Property<int>("level")
                        .ValueGeneratedOnAdd();

                    b.Property<int>("buy_gold");

                    b.Property<int>("exp");

                    b.HasKey("level");

                    b.ToTable("DLevels");
                });

            modelBuilder.Entity("Frontline.GameDesign.DMonster", b =>
                {
                    b.Property<int>("id")
                        .ValueGeneratedOnAdd();

                    b.Property<int>("armor");

                    b.Property<int>("att");

                    b.Property<string>("att_effect")
                        .HasMaxLength(32);

                    b.Property<int>("bullet_count");

                    b.Property<float>("cd");

                    b.Property<int>("count");

                    b.Property<float>("crit");

                    b.Property<float>("crit_hurt");

                    b.Property<int>("defence");

                    b.Property<string>("desc")
                        .HasMaxLength(32);

                    b.Property<string>("die_model")
                        .HasMaxLength(32);

                    b.Property<float>("distance");

                    b.Property<int>("energy");

                    b.Property<int>("hp");

                    b.Property<float>("hurt_add");

                    b.Property<float>("hurt_multiple");

                    b.Property<float>("hurt_sub");

                    b.Property<float>("last_time");

                    b.Property<int>("lv");

                    b.Property<string>("model")
                        .HasMaxLength(32);

                    b.Property<string>("move_effect")
                        .HasMaxLength(32);

                    b.Property<string>("name")
                        .HasMaxLength(32);

                    b.Property<int>("nation");

                    b.Property<float>("off");

                    b.Property<float>("r");

                    b.Property<float>("rev");

                    b.Property<float>("rev_body");

                    b.Property<float>("scale");

                    b.Property<float>("speed");

                    b.Property<int>("type");

                    b.Property<int>("type_detail");

                    b.HasKey("id");

                    b.ToTable("DMonsters");
                });

            modelBuilder.Entity("Frontline.GameDesign.DMonsterAbility", b =>
                {
                    b.Property<int>("level")
                        .ValueGeneratedOnAdd();

                    b.Property<float>("s_atk");

                    b.Property<float>("s_def");

                    b.Property<float>("s_hp");

                    b.Property<float>("t_atk");

                    b.Property<float>("t_def");

                    b.Property<float>("t_hp");

                    b.HasKey("level");

                    b.ToTable("DMonsterAbilities");
                });

            modelBuilder.Entity("Frontline.GameDesign.DMonsterInDungeon", b =>
                {
                    b.Property<int>("dungeon_id");

                    b.Property<int>("mid");

                    b.Property<int>("level");

                    b.HasKey("dungeon_id", "mid");

                    b.ToTable("DMonsterInDungeons");
                });
#pragma warning restore 612, 618
        }
    }
}
