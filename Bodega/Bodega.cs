// Bodega.cs
using System;
using Microsoft.EntityFrameworkCore;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Linq;

class Bodega
{
    private static void ListBooks()
    {
        using (var context = new ContextoLibreria())
        {
            var libros = context.Libros.ToList();
            foreach (var libro in libros)
            {
                Console.WriteLine($"ID: {libro.Id}, Título: {libro.Titulo}, Cantidad: {libro.Cantidad}, Precio: {libro.Precio}");
            }
        }
    }

    private static void AddBook()
    {
        using (var context = new ContextoLibreria())
        {
            Console.WriteLine("Ingrese el título del libro:");
            string titulo = Console.ReadLine();
            Console.WriteLine("Ingrese la cantidad de libros:");
            int cantidad = int.Parse(Console.ReadLine());
            Console.WriteLine("Ingrese el precio del libro:");
            int precio = int.Parse(Console.ReadLine());

            var libro = new Libro
            {
                Titulo = titulo,
                Cantidad = cantidad,
                Precio = precio
            };
            context.Libros.Add(libro);
            context.SaveChanges();
        }
    }

    private static void UpdateBook()
    {
        using (var context = new ContextoLibreria())
        {
            Console.WriteLine("Ingrese el ID del libro a actualizar:");
            int id = int.Parse(Console.ReadLine());
            Console.WriteLine("Ingrese el nuevo precio del libro:");
            int precio = int.Parse(Console.ReadLine());
            Console.WriteLine("Ingrese la nueva cantidad del libro:");
            int cantidad = int.Parse(Console.ReadLine());

            var libro = context.Libros.Find(id);
            if (libro != null)
            {
                libro.Precio = precio;
                libro.Cantidad = cantidad;
                context.SaveChanges();
            }
        }
    }

    private static void DeleteBook()
    {
        using (var context = new ContextoLibreria())
        {
            Console.WriteLine("Ingrese el ID del libro a eliminar:");
            int id = int.Parse(Console.ReadLine());

            var libro = context.Libros.Find(id);
            if (libro != null)
            {
                context.Libros.Remove(libro);
                context.SaveChanges();
            }
        }
    }

    private static void ReceiveMessages()
    {
        var factory = new ConnectionFactory() { HostName = "localhost" };
        using (var connection = factory.CreateConnection())
        using (var channel = connection.CreateModel())
        {
            channel.QueueDeclare(queue: "bodega_queue", durable: false, exclusive: false, autoDelete: false, arguments: null);

            var consumer = new EventingBasicConsumer(channel);
            consumer.Received += (model, ea) =>
            {
                var body = ea.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);
                Console.WriteLine(" [x] Received {0}", message);

                if (message == "listado")
                {
                    ListBooks();
                }
                else
                {
                    // Assuming message format is "id,cantidad"
                    var parts = message.Split(',');
                    int id = int.Parse(parts[0]);
                    int cantidad = int.Parse(parts[1]);

                    using (var context = new ContextoLibreria())
                    {
                        var libro = context.Libros.Find(id);
                        if (libro != null)
                        {
                            libro.Cantidad -= cantidad;
                            context.SaveChanges();
                        }
                    }
                }
            };
            channel.BasicConsume(queue: "bodega_queue", autoAck: true, consumer: consumer);

            Console.WriteLine(" Press [enter] to exit.");
            Console.ReadLine();
        }
    }

    static void Main(string[] args)
    {
        bool running = true;
        while (running)
        {
            Console.WriteLine("Seleccione una opción:");
            Console.WriteLine("1. Listar Libros");
            Console.WriteLine("2. Agregar Libro");
            Console.WriteLine("3. Actualizar Libro");
            Console.WriteLine("4. Eliminar Libro");
            Console.WriteLine("5. Escuchar mensajes");
            Console.WriteLine("6. Salir");
            var option = Console.ReadLine();

            switch (option)
            {
                case "1":
                    ListBooks();
                    break;
                case "2":
                    AddBook();
                    break;
                case "3":
                    UpdateBook();
                    break;
                case "4":
                    DeleteBook();
                    break;
                case "5":
                    ReceiveMessages();
                    break;
                case "6":
                    running = false;
                    break;
                default:
                    Console.WriteLine("Opción no válida.");
                    break;
            }
        }
    }
}
