using AMWebAPI.CoreMigrations;
using Microsoft.EntityFrameworkCore;

namespace AMWebAPI.Context;

public partial class MyDbContext : DbContext
{
    public MyDbContext()
    {
    }

    public MyDbContext(DbContextOptions<MyDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Appointment> Appointments { get; set; }

    public virtual DbSet<Client> Clients { get; set; }

    public virtual DbSet<ClientCommunication> ClientCommunications { get; set; }

    public virtual DbSet<ClientNote> ClientNotes { get; set; }

    public virtual DbSet<Provider> Providers { get; set; }

    public virtual DbSet<ProviderAlert> ProviderAlerts { get; set; }

    public virtual DbSet<ProviderBilling> ProviderBillings { get; set; }

    public virtual DbSet<ProviderCommunication> ProviderCommunications { get; set; }

    public virtual DbSet<ProviderReview> ProviderReviews { get; set; }

    public virtual DbSet<ResetPasswordRequest> ResetPasswordRequests { get; set; }

    public virtual DbSet<Service> Services { get; set; }

    public virtual DbSet<Session> Sessions { get; set; }

    public virtual DbSet<SessionAction> SessionActions { get; set; }

    public virtual DbSet<UpdateProviderEmailRequest> UpdateProviderEmailRequests { get; set; }

    public virtual DbSet<VerifyProviderEmailRequest> VerifyProviderEmailRequests { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https: //go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseSqlServer(
            "Server=localhost,1433;Initial Catalog=am.core;Persist Security Info=False;User ID=sa;Password=YourStrong!Passw0rd;MultipleActiveResultSets=False;Encrypt=False;TrustServerCertificate=True;Connection Timeout=30;");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Appointment>(entity =>
        {
            entity.ToTable("Appointment");

            entity.HasIndex(e => e.ClientId, "IX_Appointment_ClientId");

            entity.HasIndex(e => e.ProviderId, "IX_Appointment_ProviderId");

            entity.HasIndex(e => e.ServiceId, "IX_Appointment_ServiceId");

            entity.Property(e => e.Price).HasColumnType("decimal(18, 2)");

            entity.HasOne(d => d.Client).WithMany(p => p.Appointments)
                .HasForeignKey(d => d.ClientId)
                .OnDelete(DeleteBehavior.ClientSetNull);

            entity.HasOne(d => d.Provider).WithMany(p => p.Appointments)
                .HasForeignKey(d => d.ProviderId)
                .OnDelete(DeleteBehavior.ClientSetNull);

            entity.HasOne(d => d.Service).WithMany(p => p.Appointments)
                .HasForeignKey(d => d.ServiceId)
                .OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<Client>(entity =>
        {
            entity.ToTable("Client");

            entity.HasIndex(e => e.ProviderId, "IX_Client_ProviderId");

            entity.HasOne(d => d.Provider).WithMany(p => p.Clients)
                .HasForeignKey(d => d.ProviderId)
                .OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<ClientCommunication>(entity =>
        {
            entity.ToTable("ClientCommunication");

            entity.HasIndex(e => e.ClientId, "IX_ClientCommunication_ClientId");

            entity.HasOne(d => d.Client).WithMany(p => p.ClientCommunications)
                .HasForeignKey(d => d.ClientId)
                .OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<ClientNote>(entity =>
        {
            entity.ToTable("ClientNote");

            entity.HasIndex(e => e.ClientId, "IX_ClientNote_ClientId");

            entity.HasOne(d => d.Client).WithMany(p => p.ClientNotes)
                .HasForeignKey(d => d.ClientId)
                .OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<Provider>(entity =>
        {
            entity.ToTable("Provider");

            entity.HasIndex(e => e.PayEngineId, "IX_Provider_PayEngineId")
                .IsUnique()
                .HasFilter("([PayEngineId] IS NOT NULL)");

            entity.Property(e => e.AddressLine1).HasDefaultValue("");
            entity.Property(e => e.BusinessName).HasDefaultValue("");
            entity.Property(e => e.City).HasDefaultValue("");
            entity.Property(e => e.Email).HasColumnName("EMail");
            entity.Property(e => e.EmailVerified).HasColumnName("EMailVerified");
            entity.Property(e => e.ZipCode).HasDefaultValue("");
        });

        modelBuilder.Entity<ProviderAlert>(entity =>
        {
            entity.ToTable("ProviderAlert");

            entity.HasIndex(e => e.ProviderId, "IX_ProviderAlert_ProviderId");

            entity.HasOne(d => d.Provider).WithMany(p => p.ProviderAlerts)
                .HasForeignKey(d => d.ProviderId)
                .OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<ProviderBilling>(entity =>
        {
            entity.ToTable("ProviderBilling");

            entity.HasIndex(e => e.ProviderId, "IX_ProviderBilling_ProviderId");

            entity.HasOne(d => d.Provider).WithMany(p => p.ProviderBillings)
                .HasForeignKey(d => d.ProviderId)
                .OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<ProviderCommunication>(entity =>
        {
            entity.ToTable("ProviderCommunication");

            entity.HasIndex(e => e.ProviderId, "IX_ProviderCommunication_ProviderId");

            entity.HasOne(d => d.Provider).WithMany(p => p.ProviderCommunications)
                .HasForeignKey(d => d.ProviderId)
                .OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<ProviderReview>(entity =>
        {
            entity.ToTable("ProviderReview");

            entity.HasIndex(e => e.ClientId, "IX_ProviderReview_ClientId");

            entity.HasIndex(e => e.GuidQuery, "IX_ProviderReview_GuidQuery").IsUnique();

            entity.HasIndex(e => e.ProviderId, "IX_ProviderReview_ProviderId");

            entity.Property(e => e.Rating).HasColumnType("decimal(18, 2)");

            entity.HasOne(d => d.Client).WithMany(p => p.ProviderReviews)
                .HasForeignKey(d => d.ClientId)
                .OnDelete(DeleteBehavior.ClientSetNull);

            entity.HasOne(d => d.Provider).WithMany(p => p.ProviderReviews)
                .HasForeignKey(d => d.ProviderId)
                .OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<ResetPasswordRequest>(entity =>
        {
            entity.HasKey(e => e.ResetPasswordId);

            entity.ToTable("ResetPasswordRequest");

            entity.HasIndex(e => e.ProviderId, "IX_ResetPasswordRequest_ProviderId");

            entity.HasOne(d => d.Provider).WithMany(p => p.ResetPasswordRequests)
                .HasForeignKey(d => d.ProviderId)
                .OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<Service>(entity =>
        {
            entity.ToTable("Service");

            entity.HasIndex(e => e.ProviderId, "IX_Service_ProviderId");

            entity.Property(e => e.Price).HasColumnType("decimal(18, 2)");

            entity.HasOne(d => d.Provider).WithMany(p => p.Services)
                .HasForeignKey(d => d.ProviderId)
                .OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<Session>(entity =>
        {
            entity.ToTable("Session");

            entity.HasIndex(e => e.ProviderId, "IX_Session_ProviderId");

            entity.HasOne(d => d.Provider).WithMany(p => p.Sessions)
                .HasForeignKey(d => d.ProviderId)
                .OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<SessionAction>(entity =>
        {
            entity.ToTable("SessionAction");

            entity.HasIndex(e => e.SessionId, "IX_SessionAction_SessionId");

            entity.Property(e => e.SessionAction1).HasColumnName("SessionAction");

            entity.HasOne(d => d.Session).WithMany(p => p.SessionActions)
                .HasForeignKey(d => d.SessionId)
                .OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<UpdateProviderEmailRequest>(entity =>
        {
            entity.ToTable("UpdateProviderEMailRequest");

            entity.HasIndex(e => e.ProviderId, "IX_UpdateProviderEMailRequest_ProviderId");

            entity.Property(e => e.UpdateProviderEmailRequestId).HasColumnName("UpdateProviderEMailRequestId");
            entity.Property(e => e.NewEmail).HasColumnName("NewEMail");

            entity.HasOne(d => d.Provider).WithMany(p => p.UpdateProviderEmailRequests)
                .HasForeignKey(d => d.ProviderId)
                .OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<VerifyProviderEmailRequest>(entity =>
        {
            entity.ToTable("VerifyProviderEMailRequest");

            entity.HasIndex(e => e.ProviderId, "IX_VerifyProviderEMailRequest_ProviderId").IsUnique();

            entity.Property(e => e.VerifyProviderEmailRequestId).HasColumnName("VerifyProviderEMailRequestId");

            entity.HasOne(d => d.Provider).WithOne(p => p.VerifyProviderEmailRequest)
                .HasForeignKey<VerifyProviderEmailRequest>(d => d.ProviderId)
                .OnDelete(DeleteBehavior.ClientSetNull);
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}