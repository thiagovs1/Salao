using Microsoft.EntityFrameworkCore;
using SalaoBeleza.Models;

namespace SalaoBeleza.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<ComboServico>()
            .HasKey(cs => new
            {
                cs.ComboId,
                cs.ServicoId
            });

        modelBuilder.Entity<ComboServico>()
            .HasOne(cs => cs.Combo)
            .WithMany(c => c.ComboServicos)
            .HasForeignKey(cs => cs.ComboId);

        modelBuilder.Entity<ComboServico>()
            .HasOne(cs => cs.Servico)
            .WithMany()
            .HasForeignKey(cs => cs.ServicoId);

        // Relação entre Agendamento e Serviço.
        modelBuilder.Entity<Agendamento>()
            .HasOne(a => a.Servico)
            .WithMany(s => s.Agendamentos)
            .HasForeignKey(a => a.ServicoId)
            .OnDelete(DeleteBehavior.Cascade);

        // Relação entre Agendamento e Combo.
        modelBuilder.Entity<Agendamento>()
            .HasOne(a => a.Combo)
            .WithMany()
            .HasForeignKey(a => a.ComboId)
            .OnDelete(DeleteBehavior.Cascade);

        // Serviço e Combo são opcionais porque um agendamento
        // pode representar um serviço OU um combo.
        modelBuilder.Entity<Agendamento>()
            .Property(a => a.ServicoId)
            .IsRequired(false);

        modelBuilder.Entity<Agendamento>()
            .Property(a => a.ComboId)
            .IsRequired(false);
    }

    public DbSet<Cliente> Clientes { get; set; }

    public DbSet<Servico> Servicos { get; set; }

    public DbSet<Agendamento> Agendamentos { get; set; }

    public DbSet<Usuario> Usuarios { get; set; }

    public DbSet<Combo> Combos { get; set; }

    public DbSet<ComboServico> ComboServicos { get; set; }

    public DbSet<Profissional> Profissionais { get; set; }

    public DbSet<HorarioProfissional> HorariosProfissionais { get; set; }
}