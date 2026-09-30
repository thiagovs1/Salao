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
    public DbSet<Profissional> Profissionais { get; set; }
    public DbSet<HorarioProfissional> HorariosProfissionais { get; set; }
    public DbSet<Combo> Combos { get; set; }
    public DbSet<ComboServico> ComboServicos { get; set; }
    public DbSet<Pedido> Pedidos { get; set; }
    public DbSet<PedidoServico> PedidoServicos { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Cliente>(entity =>
        {
            entity.ToTable("Cliente");

            entity.HasKey(c => c.Id);

            entity.Property(c => c.Id)
                .HasColumnName("id");

            entity.Property(c => c.Nome)
                .HasColumnName("nome")
                .HasMaxLength(30)
                .IsRequired();

            entity.Property(c => c.Telefone)
                .HasColumnName("telefone")
                .HasMaxLength(20)
                .IsRequired();

            entity.Property(c => c.Email)
                .HasColumnName("email")
                .HasMaxLength(30)
                .IsRequired();

            entity.Property(c => c.Senha)
                .HasColumnName("senha")
                .HasMaxLength(10)
                .IsRequired();

            entity.Property(c => c.Cpf)
                .HasColumnName("cpf")
                .HasMaxLength(14);
        });

        modelBuilder.Entity<Servico>(entity =>
        {
            entity.ToTable("Servico");

            entity.HasKey(s => s.Id);

            entity.Property(s => s.Id)
                .HasColumnName("id");

            entity.Property(s => s.Nome)
                .HasColumnName("nome")
                .HasMaxLength(30)
                .IsRequired();

            entity.Property(s => s.Categoria)
                .HasColumnName("categoria")
                .HasMaxLength(30)
                .IsRequired();

            entity.Property(s => s.Preco)
                .HasColumnName("preco")
                .HasPrecision(7, 2)
                .IsRequired();

            entity.Property(s => s.DuracaoMinutos)
                .HasColumnName("duracaominutos")
                .IsRequired();
        });

        modelBuilder.Entity<Profissional>(entity =>
        {
            entity.ToTable("Profissional");

            entity.HasKey(p => p.Id);

            entity.Property(p => p.Id)
                .HasColumnName("id");

            entity.Property(p => p.Nome)
                .HasColumnName("nome")
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(p => p.Especialidade)
                .HasColumnName("especialidade")
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(p => p.Avaliacao)
                .HasColumnName("avaliacao")
                .HasPrecision(2, 1);
        });

        modelBuilder.Entity<HorarioProfissional>(entity =>
        {
            entity.ToTable("HorarioProfissional");

            entity.HasKey(h => h.Id);

            entity.Property(h => h.Id)
                .HasColumnName("id");

            entity.Property(h => h.ProfissionalId)
                .HasColumnName("idProfissional")
                .IsRequired();

            entity.Property(h => h.DiaSemana)
                .HasColumnName("dia_semana")
                .HasMaxLength(20)
                .IsRequired();

            entity.Property(h => h.HoraInicio)
                .HasColumnName("hora_inicio")
                .IsRequired();

            entity.Property(h => h.HoraFim)
                .HasColumnName("hora_fim")
                .IsRequired();

            entity.HasOne(h => h.Profissional)
                .WithMany(p => p.Horarios)
                .HasForeignKey(h => h.ProfissionalId);
        });

        modelBuilder.Entity<Agendamento>(entity =>
        {
            entity.ToTable("Agendamento");

            entity.HasKey(a => a.Id);

            entity.Property(a => a.Id)
                .HasColumnName("id");

            entity.Property(a => a.ClienteId)
                .HasColumnName("idCliente")
                .IsRequired();

            entity.Property(a => a.ProfissionalId)
                .HasColumnName("idProfissional")
                .IsRequired();

            entity.Property(a => a.DataHora)
                .HasColumnName("datahora")
                .IsRequired();

            entity.Property(a => a.Status)
                .HasColumnName("status_tipo")
                .HasMaxLength(20)
                .HasDefaultValue("Pendente");

            entity.HasOne(a => a.Cliente)
                .WithMany(c => c.Agendamentos)
                .HasForeignKey(a => a.ClienteId);

            entity.HasOne(a => a.Profissional)
                .WithMany(p => p.Agendamentos)
                .HasForeignKey(a => a.ProfissionalId);
        });

        modelBuilder.Entity<AgendamentoServico>(entity =>
        {
            entity.ToTable("AgendamentoServico");

            entity.HasKey(x => new
            {
                x.AgendamentoId,
                x.ServicoId
            });

            entity.Property(x => x.AgendamentoId)
                .HasColumnName("idAgendamento");

            entity.Property(x => x.ServicoId)
                .HasColumnName("idServico");

            entity.HasOne(x => x.Agendamento)
                .WithMany(a => a.AgendamentoServicos)
                .HasForeignKey(x => x.AgendamentoId);

            entity.HasOne(x => x.Servico)
                .WithMany()
                .HasForeignKey(x => x.ServicoId);
        });

        modelBuilder.Entity<Combo>(entity =>
        {
            entity.ToTable("Combo");

            entity.HasKey(c => c.Id);

            entity.Property(c => c.Id)
                .HasColumnName("id");

            entity.Property(c => c.Nome)
                .HasColumnName("nome")
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(c => c.Preco)
                .HasColumnName("preco")
                .HasPrecision(7, 2)
                .IsRequired();
        });

        modelBuilder.Entity<ComboServico>(entity =>
        {
            entity.ToTable("ComboServico");

            entity.HasKey(cs => new
            {
                cs.ComboId,
                cs.ServicoId
            });

            entity.Property(cs => cs.ComboId)
                .HasColumnName("idCombo");

            entity.Property(cs => cs.ServicoId)
                .HasColumnName("idServico");

            entity.HasOne(cs => cs.Combo)
                .WithMany(c => c.ComboServicos)
                .HasForeignKey(cs => cs.ComboId);

            entity.HasOne(cs => cs.Servico)
                .WithMany()
                .HasForeignKey(cs => cs.ServicoId);
        });

        modelBuilder.Entity<Pedido>(entity =>
        {
            entity.ToTable("Pedido");

            entity.HasKey(p => p.Id);

            entity.Property(p => p.Id)
                .HasColumnName("id");

            entity.Property(p => p.ClienteId)
                .HasColumnName("idCliente");

            entity.Property(p => p.ComboId)
                .HasColumnName("idCombo");

            entity.Property(p => p.Status)
                .HasColumnName("status")
                .HasMaxLength(20)
                .IsRequired();

            entity.Property(p => p.DataCriacao)
                .HasColumnName("data_criacao");

            entity.HasOne(p => p.Cliente)
                .WithMany()
                .HasForeignKey(p => p.ClienteId);

            entity.HasOne(p => p.Combo)
                .WithMany()
                .HasForeignKey(p => p.ComboId);
        });

        modelBuilder.Entity<PedidoServico>(entity =>
        {
            entity.ToTable("PedidoServico");

            entity.HasKey(ps => new
            {
                ps.PedidoId,
                ps.ServicoId
            });

            entity.Property(ps => ps.PedidoId)
                .HasColumnName("idPedido");

            entity.Property(ps => ps.ServicoId)
                .HasColumnName("idServico");

            entity.HasOne(ps => ps.Pedido)
                .WithMany(p => p.PedidoServicos)
                .HasForeignKey(ps => ps.PedidoId);

            entity.HasOne(ps => ps.Servico)
                .WithMany()
                .HasForeignKey(ps => ps.ServicoId);
        });
    }
}