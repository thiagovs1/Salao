
using Microsoft.EntityFrameworkCore;
using SalaoBeleza.Models;

namespace SalaoBeleza.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<Cliente> Clientes { get; set; }
    public DbSet<Servico> Servicos { get; set; }
    public DbSet<Agendamento> Agendamentos { get; set; }
    public DbSet<AgendamentoServico> AgendamentoServicos { get; set; }
    public DbSet<Usuario> Usuarios { get; set; }
    public DbSet<Combo> Combos { get; set; }
    public DbSet<ComboServico> ComboServicos { get; set; }
    public DbSet<Profissional> Profissionais { get; set; }
    public DbSet<HorarioProfissional> HorariosProfissionais { get; set; }
    public DbSet<ServicoProfissional> ServicoProfissionais { get; set; }
    public DbSet<Avaliacao> Avaliacoes { get; set; }
    public DbSet<AvaliacaoCurtida> AvaliacaoCurtidas { get; set; }
    public DbSet<AvaliacaoResposta> AvaliacaoRespostas { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Cliente>().ToTable("Clientes");
        modelBuilder.Entity<Servico>().ToTable("Servicos");
        modelBuilder.Entity<Agendamento>().ToTable("Agendamentos");
        modelBuilder.Entity<Profissional>().ToTable("Profissionais");
        modelBuilder.Entity<HorarioProfissional>().ToTable("HorariosProfissionais");
        modelBuilder.Entity<Usuario>().ToTable("Usuario");
        modelBuilder.Entity<Combo>().ToTable("Combo");
        modelBuilder.Entity<ComboServico>().ToTable("ComboServico");

        modelBuilder.Entity<Agendamento>()
            .HasOne(a => a.Cliente)
            .WithMany(c => c.Agendamentos)
            .HasForeignKey(a => a.ClienteId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Agendamento>()
            .HasOne(a => a.Servico)
            .WithMany(s => s.Agendamentos)
            .HasForeignKey(a => a.ServicoId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<Agendamento>()
            .HasOne(a => a.Combo)
            .WithMany()
            .HasForeignKey(a => a.ComboId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<Agendamento>()
            .HasOne(a => a.Profissional)
            .WithMany(p => p.Agendamentos)
            .HasForeignKey(a => a.ProfissionalId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<AgendamentoServico>(entity =>
        {
            entity.ToTable("AgendamentoServicos");

            entity.HasKey(x => new
            {
                x.AgendamentoId,
                x.ServicoId
            });

            entity.HasOne(x => x.Agendamento)
                .WithMany(a => a.AgendamentoServicos)
                .HasForeignKey(x => x.AgendamentoId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(x => x.Servico)
                .WithMany(s => s.AgendamentoServicos)
                .HasForeignKey(x => x.ServicoId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ComboServico>(entity =>
        {
            entity.ToTable("ComboServico");

            entity.HasKey(cs => new
            {
                cs.ComboId,
                cs.ServicoId
            });

            entity.HasOne(cs => cs.Combo)
                .WithMany(c => c.ComboServicos)
                .HasForeignKey(cs => cs.ComboId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(cs => cs.Servico)
                .WithMany(s => s.ComboServicos)
                .HasForeignKey(cs => cs.ServicoId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<HorarioProfissional>()
            .HasOne(h => h.Profissional)
            .WithMany(p => p.HorariosProfissionais)
            .HasForeignKey(h => h.ProfissionalId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Usuario>()
            .HasOne(u => u.Profissional)
            .WithMany()
            .HasForeignKey(u => u.ProfissionalId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<Avaliacao>(entity =>
        {
            entity.ToTable("Avaliacoes");
            entity.HasKey(a => a.Id);

            entity.Property(a => a.Comentario)
                .HasMaxLength(1000)
                .IsRequired();

            entity.Property(a => a.DataCriacao)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasOne(a => a.Cliente)
                .WithMany()
                .HasForeignKey(a => a.ClienteId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<AvaliacaoCurtida>(entity =>
        {
            entity.ToTable("AvaliacaoCurtidas");
            entity.HasKey(c => c.Id);

            entity.HasIndex(c => new
            {
                c.AvaliacaoId,
                c.ClienteId
            }).IsUnique();

            entity.Property(c => c.DataCriacao)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasOne(c => c.Avaliacao)
                .WithMany(a => a.Curtidas)
                .HasForeignKey(c => c.AvaliacaoId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(c => c.Cliente)
                .WithMany()
                .HasForeignKey(c => c.ClienteId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<AvaliacaoResposta>(entity =>
        {
            entity.ToTable("AvaliacaoRespostas");
            entity.HasKey(r => r.Id);

            entity.Property(r => r.Resposta)
                .HasMaxLength(1000)
                .IsRequired();

            entity.Property(r => r.DataCriacao)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasOne(r => r.Avaliacao)
                .WithMany(a => a.Respostas)
                .HasForeignKey(r => r.AvaliacaoId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(r => r.Cliente)
                .WithMany()
                .HasForeignKey(r => r.ClienteId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ServicoProfissional>(entity =>
        {
            entity.ToTable("ServicoProfissional");

            entity.HasKey(sp => new
            {
                sp.ServicoId,
                sp.ProfissionalId
            });

            entity.HasOne(sp => sp.Servico)
                .WithMany(s => s.Profissionais)
                .HasForeignKey(sp => sp.ServicoId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(sp => sp.Profissional)
                .WithMany(p => p.ServicosProfissionais)
                .HasForeignKey(sp => sp.ProfissionalId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
