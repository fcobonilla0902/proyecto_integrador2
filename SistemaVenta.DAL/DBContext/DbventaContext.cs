using Microsoft.EntityFrameworkCore;
using SistemaVenta.Entity.Models;

namespace SistemaVenta.DAL.DBContext;

public partial class DbventaContext : DbContext
{
    public DbventaContext() { }
    public DbventaContext(DbContextOptions<DbventaContext> options) : base(options) { }
    public DbSet<Venta> Venta => Set<Venta>();
    public DbSet<DetalleVenta> DetalleVenta => Set<DetalleVenta>();
    public DbSet<Producto> Productos => Set<Producto>();
    public DbSet<NumeroCorrelativo> NumeroCorrelativos => Set<NumeroCorrelativo>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Producto>().HasKey(x => x.IdProducto);
        modelBuilder.Entity<Categoria>().HasKey(x => x.IdCategoria);
        modelBuilder.Entity<Usuario>().HasKey(x => x.IdUsuario);
        modelBuilder.Entity<TipoDocumentoVenta>().HasKey(x => x.IdTipoDocumentoVenta);
        modelBuilder.Entity<Menu>().HasKey(x => x.IdMenu);
        modelBuilder.Entity<Rol>().HasKey(x => x.IdRol);
        modelBuilder.Entity<RolMenu>().HasKey(x => x.IdRolMenu);
        modelBuilder.Entity<Negocio>().HasKey(x => x.IdNegocio);
        modelBuilder.Entity<Cliente>().HasKey(x => x.IdCliente);
        modelBuilder.Entity<DetalleVenta>().HasKey(x => x.IdDetalleVenta);
        modelBuilder.Entity<Configuracion>().HasNoKey().ToTable("c_Configuracion");
        modelBuilder.Entity<NumeroCorrelativo>().HasKey(x => x.IdNumeroCorrelativo);
        modelBuilder.Entity<NumeroCorrelativo>().ToTable("c_NumeroCorrelativo");
        modelBuilder.Entity<Producto>().ToTable("c_Producto");
        modelBuilder.Entity<Categoria>().ToTable("c_Categoria");
        modelBuilder.Entity<Usuario>().ToTable("c_Usuario");
        modelBuilder.Entity<TipoDocumentoVenta>().ToTable("c_TipoDocumentoVenta");
        modelBuilder.Ignore<RegimenFiscal>();
        modelBuilder.Entity<Menu>().ToTable("Menu");
        modelBuilder.Entity<Rol>().ToTable("Rol");
        modelBuilder.Entity<RolMenu>().ToTable("RolMenu");
        modelBuilder.Entity<Negocio>(entity =>
        {
            entity.ToTable("Negocio");
            entity.Ignore(x => x.CodigoPostal);
            entity.Ignore(x => x.RegimenFiscal);
            entity.Ignore(x => x.UsuarioPac);
            entity.Ignore(x => x.PasswordPac);
            entity.Ignore(x => x.SerieFactura);
            entity.Ignore(x => x.FolioInicialFactura);
            entity.Ignore(x => x.SimboloMoneda);
        });
        modelBuilder.Entity<Producto>().HasOne(x => x.IdCategoriaNavigation).WithMany(x => x.Productos).HasForeignKey(x => x.IdCategoria);
        modelBuilder.Entity<Usuario>().HasOne(x => x.IdRolNavigation).WithMany(x => x.Usuarios).HasForeignKey(x => x.IdRol);
        modelBuilder.Entity<Menu>().HasOne(x => x.IdMenuPadreNavigation).WithMany(x => x.InverseIdMenuPadreNavigation).HasForeignKey(x => x.IdMenuPadre);
        modelBuilder.Entity<RolMenu>().HasOne(x => x.IdMenuNavigation).WithMany(x => x.RolMenus).HasForeignKey(x => x.IdMenu);
        modelBuilder.Entity<RolMenu>().HasOne(x => x.IdRolNavigation).WithMany(x => x.RolMenus).HasForeignKey(x => x.IdRol);
        modelBuilder.Entity<DetalleVenta>().ToTable("DetalleVenta");
        modelBuilder.Entity<DetalleVenta>().HasOne(x => x.IdVentaNavigation).WithMany(x => x.DetalleVenta).HasForeignKey(x => x.IdVenta);
        modelBuilder.Entity<Cliente>(entity =>
        {
            entity.ToTable("c_Cliente");
            entity.Property(x => x.Nombre)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("nombre");

entity.Property(e => e.DomicilioFiscalReceptor)
    .HasMaxLength(100)
    .IsUnicode(false)
    .HasColumnName("domicilioFiscalReceptor");

entity.Property(e => e.RegimenFiscalReceptor)
    .HasMaxLength(100)
    .IsUnicode(false)
    .HasColumnName("regimenFiscalReceptor");
        });

modelBuilder.Entity<Venta>(entity =>
{
    entity.HasKey(e => e.IdVenta);

    entity.ToTable("Venta");

    entity.Property(e => e.IdVenta).HasColumnName("idVenta");

    entity.Property(e => e.NumeroVenta)
        .HasMaxLength(6)
        .IsUnicode(false)
        .HasColumnName("numeroVenta");

    entity.Property(e => e.IdTipoDocumentoVenta)
        .HasColumnName("idTipoDocumentoVenta");

    entity.Property(e => e.IdUsuario)
        .HasColumnName("idUsuario");

    entity.Property(e => e.DocumentoCliente)
        .HasMaxLength(10)
        .IsUnicode(false)
        .HasColumnName("documentoCliente");

    entity.Property(e => e.NombreCliente)
        .HasMaxLength(20)
        .IsUnicode(false)
        .HasColumnName("nombreCliente");

    entity.Property(e => e.SubTotal)
        .HasColumnType("decimal(10, 2)")
        .HasColumnName("subTotal");

    entity.Property(e => e.ImpuestoTotal)
        .HasColumnType("decimal(10, 2)")
        .HasColumnName("impuestoTotal");

    entity.Property(e => e.Total)
        .HasColumnType("decimal(10, 2)");

    entity.Property(e => e.FechaRegistro)
        .HasColumnType("datetime")
        .HasColumnName("fechaRegistro")
        .HasDefaultValueSql("(getdate())");

    //  CAMPOS NUEVOS PARA FACTURACIÓN
    entity.Property(e => e.IdCliente)
        .HasColumnName("IdCliente");

    entity.Property(e => e.IdFormaPago)
        .HasColumnName("idFormaPago");

    entity.Property(e => e.IdMetodoPago)
        .HasColumnName("idMetodoPago");

    entity.Property(e => e.IdUsoCFDI)
        .HasColumnName("idUsoCFDI");

    entity.Property(e => e.RutaXML)
        .HasMaxLength(500)
        .HasColumnName("RutaXML");

    entity.Property(e => e.RutaPDF)
        .HasMaxLength(500)
        .HasColumnName("RutaPDF");

    entity.Property(e => e.UUID)
        .HasMaxLength(100)
        .HasColumnName("UUID");

    entity.Property(e => e.FechaTimbrado)
        .HasColumnType("datetime")
        .HasColumnName("FechaTimbrado");

    //  RELACIONES
    entity.HasOne(d => d.IdTipoDocumentoVentaNavigation)
        .WithMany(p => p.Venta)
        .HasForeignKey(d => d.IdTipoDocumentoVenta)
        .HasConstraintName("FK__Venta__idTipoDoc__5CA1C101");

    entity.HasOne(d => d.IdUsuarioNavigation)
        .WithMany(p => p.Venta)
        .HasForeignKey(d => d.IdUsuario)
        .HasConstraintName("FK__Venta__idUsuario__5D95E53A");

    //  RELACIÓN CON CLIENTE - CONFIGURACIÓN CORRECTA
    entity.HasOne(d => d.IdClienteNavigation)
        .WithMany()
        .HasForeignKey(d => d.IdCliente)
        .OnDelete(DeleteBehavior.ClientSetNull)
        .IsRequired(false); // ← IMPORTANTE: Opcional porque puede ser null
});


OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}

