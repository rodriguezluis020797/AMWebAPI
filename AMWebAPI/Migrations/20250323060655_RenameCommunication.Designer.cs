﻿// <auto-generated />
using System;
using AMWebAPI.Services.DataServices;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

#nullable disable

namespace AMWebAPI.Migrations
{
    [DbContext(typeof(AMCoreData))]
    [Migration("20250323060655_RenameCommunication")]
    partial class RenameCommunication
    {
        /// <inheritdoc />
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "9.0.2")
                .HasAnnotation("Relational:MaxIdentifierLength", 128);

            SqlServerModelBuilderExtensions.UseIdentityColumns(modelBuilder);

            modelBuilder.Entity("AMWebAPI.Models.CoreModels.SessionModel", b =>
                {
                    b.Property<long>("SessionId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bigint");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<long>("SessionId"));

                    b.Property<DateTime>("CreateDate")
                        .HasColumnType("datetime2");

                    b.Property<long>("UserId")
                        .HasColumnType("bigint");

                    b.HasKey("SessionId");

                    b.HasIndex("UserId");

                    b.ToTable("Session");
                });

            modelBuilder.Entity("AMWebAPI.Models.CoreModels.UserCommunicationModel", b =>
                {
                    b.Property<long>("CommunicationId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bigint");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<long>("CommunicationId"));

                    b.Property<DateTime?>("AttemptOne")
                        .HasColumnType("datetime2");

                    b.Property<DateTime?>("AttemptThree")
                        .HasColumnType("datetime2");

                    b.Property<DateTime?>("AttemptTwo")
                        .HasColumnType("datetime2");

                    b.Property<DateTime?>("DeleteDate")
                        .HasColumnType("datetime2");

                    b.Property<string>("Message")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<DateTime>("SendAfter")
                        .HasColumnType("datetime2");

                    b.Property<long>("UserId")
                        .HasColumnType("bigint");

                    b.HasKey("CommunicationId");

                    b.HasIndex("UserId");

                    b.ToTable("UserCommunication");
                });

            modelBuilder.Entity("AMWebAPI.Models.CoreModels.UserModel", b =>
                {
                    b.Property<long>("UserId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bigint");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<long>("UserId"));

                    b.Property<bool>("AccessGranted")
                        .HasColumnType("bit");

                    b.Property<DateTime>("CreateDate")
                        .HasColumnType("datetime2");

                    b.Property<DateTime?>("DeleteDate")
                        .HasColumnType("datetime2");

                    b.Property<string>("EMail")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("FirstName")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("LastName")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("MiddleName")
                        .HasColumnType("nvarchar(max)");

                    b.Property<DateTime?>("UpdateDate")
                        .HasColumnType("datetime2");

                    b.HasKey("UserId");

                    b.ToTable("User");
                });

            modelBuilder.Entity("AMWebAPI.Models.CoreModels.SessionModel", b =>
                {
                    b.HasOne("AMWebAPI.Models.CoreModels.UserModel", "User")
                        .WithMany("Sessions")
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("User");
                });

            modelBuilder.Entity("AMWebAPI.Models.CoreModels.UserCommunicationModel", b =>
                {
                    b.HasOne("AMWebAPI.Models.CoreModels.UserModel", "User")
                        .WithMany("Communications")
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("User");
                });

            modelBuilder.Entity("AMWebAPI.Models.CoreModels.UserModel", b =>
                {
                    b.Navigation("Communications");

                    b.Navigation("Sessions");
                });
#pragma warning restore 612, 618
        }
    }
}
