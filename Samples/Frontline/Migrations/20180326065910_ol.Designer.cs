﻿// <auto-generated />
using Frontline.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.EntityFrameworkCore.Storage.Internal;
using System;
using System.Collections.Generic;

namespace Frontline.Migrations
{
    [DbContext(typeof(DataContext))]
    [Migration("20180326065910_ol")]
    partial class ol
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn)
                .HasAnnotation("ProductVersion", "2.0.1-rtm-125");

            modelBuilder.Entity("Frontline.Domain.Dungeon", b =>
                {
                    b.Property<string>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<int>("FightTimes");

                    b.Property<bool>("IsLast");

                    b.Property<bool>("IsOpen");

                    b.Property<DateTime>("LastRefreshTime");

                    b.Property<int>("Mission");

                    b.Property<int>("Next")
                        .HasMaxLength(32);

                    b.Property<string>("PlayerId")
                        .HasMaxLength(20);

                    b.Property<int>("ResetNumb");

                    b.Property<int>("Section");

                    b.Property<string>("SectionId");

                    b.Property<int>("Star");

                    b.Property<int>("Tid");

                    b.Property<int>("Type");

                    b.HasKey("Id");

                    b.HasIndex("SectionId");

                    b.HasIndex("Tid", "PlayerId");

                    b.ToTable("Dungeon");
                });

            modelBuilder.Entity("Frontline.Domain.Equip", b =>
                {
                    b.Property<string>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<int>("GradeId");

                    b.Property<int>("Level");

                    b.Property<string>("PlayerId");

                    b.Property<int>("Pos");

                    b.Property<int>("Tid");

                    b.Property<string>("UnitId");

                    b.HasKey("Id");

                    b.HasIndex("UnitId");

                    b.ToTable("Equip");
                });

            modelBuilder.Entity("Frontline.Domain.FriendApplication", b =>
                {
                    b.Property<string>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<DateTime>("CreateTime");

                    b.Property<string>("FriendListId");

                    b.Property<string>("PlayerId");

                    b.HasKey("Id");

                    b.HasIndex("FriendListId");

                    b.ToTable("FriendApplication");
                });

            modelBuilder.Entity("Frontline.Domain.FriendList", b =>
                {
                    b.Property<string>("PlayerId");

                    b.Property<int>("RecvTimes");

                    b.HasKey("PlayerId");

                    b.ToTable("FriendList");
                });

            modelBuilder.Entity("Frontline.Domain.Friendship", b =>
                {
                    b.Property<string>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<string>("FriendListId");

                    b.Property<DateTime>("FromTime");

                    b.Property<bool>("IsRecvOil");

                    b.Property<bool>("IsSendOil");

                    b.Property<string>("PlayerId");

                    b.HasKey("Id");

                    b.HasIndex("FriendListId");

                    b.ToTable("Friendship");
                });

            modelBuilder.Entity("Frontline.Domain.Lottery", b =>
                {
                    b.Property<string>("PlayerId");

                    b.Property<int>("Dmd10UsedNumb");

                    b.Property<int>("DmdBaseNumb");

                    b.Property<DateTime>("DmdFreeNextTime");

                    b.Property<int>("DmdFreeNumb");

                    b.Property<DateTime>("DmdLastTime");

                    b.Property<int>("DmdUsedNumb");

                    b.Property<int>("Gold10UsedNumb");

                    b.Property<int>("GoldBaseNumb");

                    b.Property<DateTime>("GoldFreeNextTime");

                    b.Property<int>("GoldFreeNumb");

                    b.Property<DateTime>("GoldLastTime");

                    b.Property<int>("GoldUsedNumb");

                    b.HasKey("PlayerId");

                    b.ToTable("Lottery");
                });

            modelBuilder.Entity("Frontline.Domain.Player", b =>
                {
                    b.Property<string>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<int>("BuySkillNumb");

                    b.Property<int>("Camp");

                    b.Property<DateTime>("CreateTime");

                    b.Property<int>("Exp");

                    b.Property<float>("Guide");

                    b.Property<string>("IP")
                        .HasMaxLength(32);

                    b.Property<string>("Icon")
                        .HasMaxLength(20);

                    b.Property<bool>("IsBind");

                    b.Property<bool>("IsDeleted");

                    b.Property<string>("Language")
                        .HasMaxLength(32);

                    b.Property<DateTime>("LastDayRefreshTime");

                    b.Property<DateTime>("LastLoginTime");

                    b.Property<DateTime>("LastLvUpTime");

                    b.Property<DateTime>("LastVipUpTime");

                    b.Property<int>("Level");

                    b.Property<long>("MaxPower");

                    b.Property<string>("NickName")
                        .IsRequired()
                        .HasMaxLength(20);

                    b.Property<bool>("OldPlayer");

                    b.Property<TimeSpan>("OnlineTime");

                    b.Property<int>("RenameNumb");

                    b.Property<int>("ScienceNumb");

                    b.Property<int>("State");

                    b.Property<long>("StateTime");

                    b.Property<long>("UserCenterId");

                    b.Property<string>("UserCode")
                        .HasMaxLength(20);

                    b.Property<int>("VIP");

                    b.Property<int>("VIPExp");

                    b.Property<string>("Version")
                        .HasMaxLength(32);

                    b.HasKey("Id");

                    b.HasAlternateKey("NickName");

                    b.HasIndex("UserCenterId");

                    b.HasIndex("UserCode");

                    b.ToTable("Players");
                });

            modelBuilder.Entity("Frontline.Domain.PlayerItem", b =>
                {
                    b.Property<string>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<int>("Count");

                    b.Property<string>("PlayerId");

                    b.Property<int>("Tid");

                    b.HasKey("Id");

                    b.HasIndex("PlayerId");

                    b.ToTable("Items");
                });

            modelBuilder.Entity("Frontline.Domain.PlayerOlReward", b =>
                {
                    b.Property<string>("PlayerId");

                    b.Property<DateTime>("NextTime");

                    b.Property<int>("RewardIndex");

                    b.Property<int>("Round");

                    b.HasKey("PlayerId");

                    b.ToTable("PlayerOlReward");
                });

            modelBuilder.Entity("Frontline.Domain.PVPFormation", b =>
                {
                    b.Property<string>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<int>("Index");

                    b.Property<bool>("IsSelected");

                    b.Property<string>("PlayerId");

                    b.Property<JsonObject<List<string>>>("Units");

                    b.HasKey("Id");

                    b.HasIndex("PlayerId");

                    b.ToTable("Formations");
                });

            modelBuilder.Entity("Frontline.Domain.Section", b =>
                {
                    b.Property<string>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<int>("Index");

                    b.Property<string>("PlayerId");

                    b.Property<int>("RecvdStarReward");

                    b.Property<int>("Type");

                    b.HasKey("Id");

                    b.HasIndex("PlayerId");

                    b.ToTable("Sections");
                });

            modelBuilder.Entity("Frontline.Domain.Team", b =>
                {
                    b.Property<string>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<int>("Index");

                    b.Property<bool>("IsSelected");

                    b.Property<string>("PlayerId");

                    b.Property<JsonObject<List<string>>>("Units");

                    b.HasKey("Id");

                    b.HasIndex("PlayerId");

                    b.ToTable("Teams");
                });

            modelBuilder.Entity("Frontline.Domain.Unit", b =>
                {
                    b.Property<string>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<int>("Exp");

                    b.Property<int>("Grade");

                    b.Property<bool>("IsResting");

                    b.Property<int>("Level");

                    b.Property<int>("Number");

                    b.Property<string>("PlayerId");

                    b.Property<int>("Power");

                    b.Property<DateTime>("RestEndTime");

                    b.Property<int>("Tid");

                    b.HasKey("Id");

                    b.HasIndex("PlayerId");

                    b.ToTable("Units");
                });

            modelBuilder.Entity("Frontline.Domain.Wallet", b =>
                {
                    b.Property<string>("PlayerId");

                    b.Property<int>("DIAMOND");

                    b.Property<int>("GOLD");

                    b.Property<int>("HORN");

                    b.Property<int>("IRON");

                    b.Property<int>("LEGIONCOIN");

                    b.Property<int>("OIL");

                    b.Property<int>("SMOKE");

                    b.Property<int>("SUPPLY");

                    b.Property<int>("TEC");

                    b.Property<int>("TOKEN");

                    b.Property<int>("WIPES");

                    b.HasKey("PlayerId");

                    b.ToTable("Wallets");
                });

            modelBuilder.Entity("Frontline.Domain.Dungeon", b =>
                {
                    b.HasOne("Frontline.Domain.Section")
                        .WithMany("Dungeons")
                        .HasForeignKey("SectionId");
                });

            modelBuilder.Entity("Frontline.Domain.Equip", b =>
                {
                    b.HasOne("Frontline.Domain.Unit")
                        .WithMany("Equips")
                        .HasForeignKey("UnitId");
                });

            modelBuilder.Entity("Frontline.Domain.FriendApplication", b =>
                {
                    b.HasOne("Frontline.Domain.FriendList")
                        .WithMany("FriendApplications")
                        .HasForeignKey("FriendListId");
                });

            modelBuilder.Entity("Frontline.Domain.FriendList", b =>
                {
                    b.HasOne("Frontline.Domain.Player")
                        .WithOne("FriendList")
                        .HasForeignKey("Frontline.Domain.FriendList", "PlayerId")
                        .OnDelete(DeleteBehavior.Cascade);
                });

            modelBuilder.Entity("Frontline.Domain.Friendship", b =>
                {
                    b.HasOne("Frontline.Domain.FriendList")
                        .WithMany("Friends")
                        .HasForeignKey("FriendListId");
                });

            modelBuilder.Entity("Frontline.Domain.Lottery", b =>
                {
                    b.HasOne("Frontline.Domain.Player")
                        .WithOne("Lottery")
                        .HasForeignKey("Frontline.Domain.Lottery", "PlayerId")
                        .OnDelete(DeleteBehavior.Cascade);
                });

            modelBuilder.Entity("Frontline.Domain.PlayerItem", b =>
                {
                    b.HasOne("Frontline.Domain.Player")
                        .WithMany("Items")
                        .HasForeignKey("PlayerId");
                });

            modelBuilder.Entity("Frontline.Domain.PlayerOlReward", b =>
                {
                    b.HasOne("Frontline.Domain.Player")
                        .WithOne("OlReward")
                        .HasForeignKey("Frontline.Domain.PlayerOlReward", "PlayerId")
                        .OnDelete(DeleteBehavior.Cascade);
                });

            modelBuilder.Entity("Frontline.Domain.PVPFormation", b =>
                {
                    b.HasOne("Frontline.Domain.Player")
                        .WithMany("Formations")
                        .HasForeignKey("PlayerId");
                });

            modelBuilder.Entity("Frontline.Domain.Section", b =>
                {
                    b.HasOne("Frontline.Domain.Player")
                        .WithMany("Sections")
                        .HasForeignKey("PlayerId");
                });

            modelBuilder.Entity("Frontline.Domain.Team", b =>
                {
                    b.HasOne("Frontline.Domain.Player")
                        .WithMany("Teams")
                        .HasForeignKey("PlayerId");
                });

            modelBuilder.Entity("Frontline.Domain.Unit", b =>
                {
                    b.HasOne("Frontline.Domain.Player")
                        .WithMany("Units")
                        .HasForeignKey("PlayerId");
                });

            modelBuilder.Entity("Frontline.Domain.Wallet", b =>
                {
                    b.HasOne("Frontline.Domain.Player")
                        .WithOne("Wallet")
                        .HasForeignKey("Frontline.Domain.Wallet", "PlayerId")
                        .OnDelete(DeleteBehavior.Cascade);
                });
#pragma warning restore 612, 618
        }
    }
}
