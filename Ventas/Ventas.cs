using System;
using Microsoft.EntityFrameworkCore;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;
using System.Linq;
using System.Collections.Generic;

class VentasApp
{
    // Método para solicitar la lista de libros a la aplicación de Bodega
    private static List<Libro> ListBooks()
    {
        var factory = new ConnectionFactory() { HostName = "localhost" };
        using (var connection = factory.CreateConnection())
        using (var channel = connection.CreateModel())
        {
            // Declarar una cola temporal para recibir la respuesta
            var replyQueueName = channel.QueueDeclare().QueueName;
            var consumer = new EventingBasicConsumer(channel);
            var correlationId = Guid.NewGuid().ToString();

            var booksList = new List<Libro>();
            var props = channel.CreateBasicProperties();
            props.ReplyTo = replyQueueName;
            props.CorrelationId = correlationId;

            // Enviar un mensaje para solicitar la lista de libros
            var messageBytes = Encoding.UTF8.GetBytes("listado");
            channel.BasicPublish(
                exchange: "",
                routingKey: "bodega_queue",
                basicProperties: props,
                body: messageBytes);

            // Configurar el consumidor para recibir la respuesta
            consumer.Received += (model, ea) =>
            {
                if (ea.BasicProperties.CorrelationId == correlationId)
                {
                    var response = Encoding.UTF8.GetString(ea.Body.ToArray());
                    booksList = JsonSerializer.Deserialize<List<Libro>>(response);
                }
            };

            // Consumir la cola de respuestas
            channel.BasicConsume(
                consumer: consumer,
                queue: replyQueueName,
                autoAck: true);

            // Espera hasta recibir la respuesta
            while (booksList.Count == 0)
            {
                System.Threading.Thread.Sleep(100);
            }

            return booksList;
        }
    }

    // Método para registrar una venta
    private static void RegisterSale()
    {
        // Obtener y mostrar la lista de libros
        var libros = ListBooks();
        foreach (var libro in libros)
        {
            Console.WriteLine($"ID: {libro.Id}, Título: {libro.Titulo}, Cantidad: {libro.Cantidad}, Precio: {libro.Precio}");
        }

        // Solicitar los detalles de la venta al usuario
        Console.WriteLine("Ingrese el ID del libro a vender:");
        int idLibro = int.Parse(Console.ReadLine());
        Console.WriteLine("Ingrese la cantidad vendida:");
        int cantidad = int.Parse(Console.ReadLine());
        Console.WriteLine("Ingrese el precio del libro:");
        int precio = int.Parse(Console.ReadLine());

        // Registrar la venta en la base de datos
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

        // Enviar un mensaje para actualizar la cantidad de libros en la bodega
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

    // Método para listar todas las ventas
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

    // Método principal que ejecuta la aplicación
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