﻿// <auto-generated />
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Typedown.Universal.Services;

namespace DatabaseMigration.Migrations
{
    [DbContext(typeof(Database))]
    partial class DatabaseModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "3.1.30");

            modelBuilder.Entity("Typedown.Universal.Models.ExportConfig", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<string>("Config")
                        .HasColumnType("TEXT");

                    b.Property<string>("Name")
                        .HasColumnType("TEXT");

                    b.Property<string>("Notes")
                        .HasColumnType("TEXT");

                    b.Property<int>("Type")
                        .HasColumnType("INTEGER");

                    b.HasKey("Id");

                    b.ToTable("ExportConfig");
                });

            modelBuilder.Entity("Typedown.Universal.Models.FileAccessHistory", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<DateTime>("AccessTime")
                        .HasColumnType("TEXT");

                    b.Property<string>("FilePath")
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.ToTable("FileAccessHistory");
                });

            modelBuilder.Entity("Typedown.Universal.Models.FolderAccessHistory", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<DateTime>("AccessTime")
                        .HasColumnType("TEXT");

                    b.Property<string>("FolderPath")
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.ToTable("FolderAccessHistory");
                });

            modelBuilder.Entity("Typedown.Universal.Models.ImageUploadConfig", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<string>("Config")
                        .HasColumnType("TEXT");

                    b.Property<bool>("IsEnable")
                        .HasColumnType("INTEGER");

                    b.Property<int>("Method")
                        .HasColumnType("INTEGER");

                    b.Property<string>("Name")
                        .HasColumnType("TEXT");

                    b.Property<string>("Notes")
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.ToTable("ImageUploadConfig");
                });
#pragma warning restore 612, 618
        }
    }
}
