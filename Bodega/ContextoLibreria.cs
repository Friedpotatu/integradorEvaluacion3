using Microsoft.EntityFrameworkCore;

public class ContextoLibreria : DbContext
{
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        base.OnConfiguring(optionsBuilder);
        optionsBuilder.UseMySQL("server=localhost;database=libreriaSus;user=root;password=");
    }
    public DbSet<Libro> Libros { get; set; }
}