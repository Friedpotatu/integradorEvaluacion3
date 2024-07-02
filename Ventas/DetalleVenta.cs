public class DetalleVenta
{
    public int Id { get; set; }
    public int VentaId { get; set; }
    public Venta Venta { get; set; }
    public int LibroId { get; set; }
    public int Cantidad { get; set; }
    public int Precio { get; set; }
}