public class Venta
{
    public int Id { get; set; }
    public DateTime Fecha { get; set; }
    public ICollection<DetalleVenta> DetalleVentas { get; set; }
}