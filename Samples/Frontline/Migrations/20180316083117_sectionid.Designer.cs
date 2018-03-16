﻿// <auto-generated />
using Frontline.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.EntityFrameworkCore.Storage.Internal;
using System;

namespace Frontline.Migrations
{
    [DbContext(typeof(DataContext))]
    [Migration("20180316083117_sectionid")]
    partial class sectionid
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn)
                .HasAnnotation("ProductVersion", "2.0.1-rtm-125");

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

                    b.Property<DateTime>("LastLoginTime");

                    b.Property<DateTime>("LastLvUpTime");

                    b.Property<DateTime>("LastVipUpTime");

                    b.Property<int>("Level");

                    b.Property<long>("MaxPower");

                    b.Property<string>("NickName")
                        .IsRequired()
                        .HasMaxLength(20);

                    b.Property<bool>("OldPlayer");

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

            modelBuilder.Entity("Frontline.Domain.PlayerCurrency", b =>
                {
                    b.Property<string>("PlayerId");

                    b.Property<int>("Type");

                    b.Property<int>("Value");

                    b.HasKey("PlayerId", "Type");

                    b.ToTable("PlayerCurrency");
                });

            modelBuilder.Entity("Frontline.Domain.PlayerDungeon", b =>
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

                    b.Property<string>("PlayerSectionId");

                    b.Property<int>("ResetNumb");

                    b.Property<int>("Section");

                    b.Property<int>("Star");

                    b.Property<int>("Tid");

                    b.Property<int>("Type");

                    b.HasKey("Id");

                    b.HasIndex("PlayerId");

                    b.HasIndex("PlayerSectionId");

                    b.HasIndex("Tid", "PlayerId");

                    b.ToTable("PlayerDungeon");
                });

            modelBuilder.Entity("Frontline.Domain.PlayerSection", b =>
                {
                    b.Property<string>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<string>("PlayerId");

                    b.Property<int>("RecvdStarReward");

                    b.Property<int>("Section");

                    b.Property<int>("Type");

                    b.HasKey("Id");

                    b.ToTable("Sections");
                });

            modelBuilder.Entity("Frontline.Domain.PlayerCurrency", b =>
                {
                    b.HasOne("Frontline.Domain.Player")
                        .WithMany("Currencies")
                        .HasForeignKey("PlayerId")
                        .OnDelete(DeleteBehavior.Cascade);
                });

            modelBuilder.Entity("Frontline.Domain.PlayerDungeon", b =>
                {
                    b.HasOne("Frontline.Domain.PlayerSection")
                        .WithMany("Dungeons")
                        .HasForeignKey("PlayerSectionId");
                });
#pragma warning restore 612, 618
        }
    }
}
