using System;
using Microsoft.EntityFrameworkCore;
using RabbitMQ.Client;
using System.Text;
using System.Linq;

class VentasApp
{
    private static void ListBooks()
    {
        var factory = new ConnectionFactory() { HostName = "localhost" };
        using (var connection = factory.CreateConnection())
        using (var channel = connection.CreateModel())
        {
            channel.QueueDeclare(queue: "bodega_queue", durable: false, exclusive: false, autoDelete: false, arguments: null);
            var body = Encoding.UTF8.GetBytes("listado");
            channel.BasicPublish(exchange: "", routingKey: "bodega_queue", basicProperties: null, body: body);
            Console.WriteLine(" [x] Sent 'listado'");
        }
    }

    private static void RegisterSale()
    {
        ListBooks();
        // Aquí deberías implementar la lógica para esperar la respuesta de la lista de libros y mostrarla

        Console.WriteLine("Ingrese el ID del libro a vender:");
        int idLibro = int.Parse(Console.ReadLine());
        Console.WriteLine("Ingrese la cantidad vendida:");
        int cantidad = int.Parse(Console.ReadLine());
        Console.WriteLine("Ingrese el precio del libro:");
        int precio = int.Parse(Console.ReadLine());

        using (var context = new ContextoVentas())
        {
            var venta = new Venta
            {
                Fecha = DateTime.Now
            };
            context.Ventas.Add(venta);
            context.SaveChanges();

            var detalleVenta = new DetalleVenta
            {
                VentaId = venta.Id,
                LibroId = idLibro,
                Cantidad = cantidad,
                Precio = precio
            };
            context.DetalleVentas.Add(detalleVenta);
            context.SaveChanges();
        }

        // Enviar mensaje para actualizar la cantidad de libros en la bodega
        var factory = new ConnectionFactory() { HostName = "localhost" };
        using (var connection = factory.CreateConnection())
        using (var channel = connection.CreateModel())
        {
            channel.QueueDeclare(queue: "bodega_queue", durable: false, exclusive: false, autoDelete: false, arguments: null);
            var message = $"{idLibro},{cantidad}";
            var body = Encoding.UTF8.GetBytes(message);
            channel.BasicPublish(exchange: "", routingKey: "bodega_queue", basicProperties: null, body: body);
            Console.WriteLine(" [x] Sent '{0}'", message);
        }
    }

    private static void ListSales()
    {
        using (var context = new ContextoVentas())
        {
            var ventas = context.Ventas.Include(v => v.DetalleVentas).ToList();
            foreach (var venta in ventas)
            {
                Console.WriteLine($"ID: {venta.Id}, Fecha: {venta.Fecha}");
                foreach (var detalle in venta.DetalleVentas)
                {
                    Console.WriteLine($"\tID Libro: {detalle.LibroId}, Cantidad: {detalle.Cantidad}, Precio: {detalle.Precio}");
                }
            }
        }
    }

    static void Main(string[] args)
    {
        bool running = true;
        while (running)
        {
            Console.WriteLine("Seleccione una opción:");
            Console.WriteLine("1. Registrar Venta");
            Console.WriteLine("2. Listar Ventas");
            Console.WriteLine("3. Salir");
            var option = Console.ReadLine();

            switch (option)
            {
                case "1":
                    RegisterSale();
                    break;
                case "2":
                    ListSales();
                    break;
                case "3":
                    running = false;
                    break;
                default:
                    Console.WriteLine("Opción no válida.");
                    break;
            }
        }
    }
}
