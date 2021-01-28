﻿// <auto-generated />
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using SheetMusic.Api.Database;

namespace SheetMusic.Api.Migrations
{
    [DbContext(typeof(SheetMusicContext))]
    [Migration("20190404194358_Changed part name prop")]
    partial class Changedpartnameprop
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "2.2.3-servicing-35854")
                .HasAnnotation("Relational:MaxIdentifierLength", 128)
                .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

            modelBuilder.Entity("SheetMusic.Api.Database.Entities.Category", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<bool>("Inactive");

                    b.Property<string>("Name");

                    b.HasKey("Id");

                    b.ToTable("SheetMusicCategories");
                });

            modelBuilder.Entity("SheetMusic.Api.Database.Entities.MusicPart", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<string>("Aliases");

                    b.Property<string>("Name");

                    b.HasKey("Id");

                    b.ToTable("MusicParts");
                });

            modelBuilder.Entity("SheetMusic.Api.Database.Entities.Musician", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<string>("Email");

                    b.Property<bool>("Inactive");

                    b.Property<string>("Name");

                    b.Property<string>("Password");

                    b.Property<Guid>("UserGroupId");

                    b.Property<string>("UserName");

                    b.HasKey("Id");

                    b.HasIndex("UserGroupId");

                    b.ToTable("Musicians");
                });

            modelBuilder.Entity("SheetMusic.Api.Database.Entities.MusicianMusicPart", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<Guid>("MusicPartId");

                    b.Property<Guid>("MusicianId");

                    b.HasKey("Id");

                    b.HasIndex("MusicPartId");

                    b.HasIndex("MusicianId");

                    b.ToTable("MusicianMusicPart");
                });

            modelBuilder.Entity("SheetMusic.Api.Database.Entities.SheetMusicCategory", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<Guid>("CategoryId");

                    b.Property<Guid>("SheetMusicId");

                    b.Property<Guid?>("SheetMusicSetId");

                    b.HasKey("Id");

                    b.HasIndex("CategoryId");

                    b.HasIndex("SheetMusicSetId");

                    b.ToTable("SheetMusicCategory");
                });

            modelBuilder.Entity("SheetMusic.Api.Database.Entities.SheetMusicPart", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<Guid>("MusicPartId");

                    b.Property<Guid>("SheetMusicId");

                    b.HasKey("Id");

                    b.HasIndex("MusicPartId");

                    b.HasIndex("SheetMusicId");

                    b.ToTable("SheetMusicParts");
                });

            modelBuilder.Entity("SheetMusic.Api.Database.Entities.SheetMusicSet", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<string>("ArchiveFileName");

                    b.Property<int>("ArchiveNumber");

                    b.Property<string>("Arranger");

                    b.Property<string>("Composer");

                    b.Property<bool>("HasBeenScanned");

                    b.Property<string>("MissingParts");

                    b.Property<string>("SoleSellingAgent");

                    b.Property<string>("Title");

                    b.HasKey("Id");

                    b.ToTable("SheetMusicSets");
                });

            modelBuilder.Entity("SheetMusic.Api.Database.Entities.UserGroup", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<string>("Name");

                    b.HasKey("Id");

                    b.ToTable("UserGroups");
                });

            modelBuilder.Entity("SheetMusic.Api.Database.Entities.Musician", b =>
                {
                    b.HasOne("SheetMusic.Api.Database.Entities.UserGroup", "UserGroup")
                        .WithMany("Musicians")
                        .HasForeignKey("UserGroupId")
                        .OnDelete(DeleteBehavior.Cascade);
                });

            modelBuilder.Entity("SheetMusic.Api.Database.Entities.MusicianMusicPart", b =>
                {
                    b.HasOne("SheetMusic.Api.Database.Entities.MusicPart", "MusicPart")
                        .WithMany("MusicianMusicParts")
                        .HasForeignKey("MusicPartId")
                        .OnDelete(DeleteBehavior.Cascade);

                    b.HasOne("SheetMusic.Api.Database.Entities.Musician", "Musician")
                        .WithMany("MusicianMusicParts")
                        .HasForeignKey("MusicianId")
                        .OnDelete(DeleteBehavior.Cascade);
                });

            modelBuilder.Entity("SheetMusic.Api.Database.Entities.SheetMusicCategory", b =>
                {
                    b.HasOne("SheetMusic.Api.Database.Entities.Category", "Category")
                        .WithMany("SheetMusicCategories")
                        .HasForeignKey("CategoryId")
                        .OnDelete(DeleteBehavior.Cascade);

                    b.HasOne("SheetMusic.Api.Database.Entities.SheetMusicSet", "SheetMusicSet")
                        .WithMany("Categories")
                        .HasForeignKey("SheetMusicSetId");
                });

            modelBuilder.Entity("SheetMusic.Api.Database.Entities.SheetMusicPart", b =>
                {
                    b.HasOne("SheetMusic.Api.Database.Entities.MusicPart", "Part")
                        .WithMany("Parts")
                        .HasForeignKey("MusicPartId")
                        .OnDelete(DeleteBehavior.Cascade);

                    b.HasOne("SheetMusic.Api.Database.Entities.SheetMusicSet", "SheetMusic")
                        .WithMany("Parts")
                        .HasForeignKey("SheetMusicId")
                        .OnDelete(DeleteBehavior.Cascade);
                });
#pragma warning restore 612, 618
        }
    }
}
