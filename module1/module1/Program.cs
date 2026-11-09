using System;
using System.Collections.Generic;

namespace DeliveryServiceApp
{
    public interface IPayment
    {
        void Pay(double amount);
    }

    public class CardPayment : IPayment
    {
        public void Pay(double amount) => Console.WriteLine($"[Оплата] З картки знято {amount} грн.");
    }

    public class CashPayment : IPayment
    {
        public void Pay(double amount) => Console.WriteLine($"[Оплата] Отримано готівкою {amount} грн.");
    }

    public class Order
    {
        public int Number { get; set; }
        public string Description { get; set; }
        public double Weight { get; set; } // кг
        public double Distance { get; set; } // км
    }

    public abstract class Transport
    {
        public string Name { get; protected set; }
        public bool IsBusy { get; set; }
        public double MaxWeightLimit { get; protected set; }

        public Transport(string name, double maxWeightLimit)
        {
            Name = name;
            MaxWeightLimit = maxWeightLimit;
            IsBusy = false;
        }

        public abstract double CalculateCost(Order order);

        public virtual bool CanTake(Order order)
        {
            return order.Weight <= MaxWeightLimit;
        }
    }

    public class Bike : Transport
    {
        public Bike() : base("Велосипед", 15) { }

        public override double CalculateCost(Order order)
        {
            return 20 + (5 * order.Distance);
        }

        public override bool CanTake(Order order)
        {
            return base.CanTake(order) && order.Distance <= 10;
        }
    }

    public class Car : Transport
    {
        public Car() : base("Автомобіль", 500) { }

        public override double CalculateCost(Order order)
        {
            return 50 + (8 * order.Distance) + (1 * order.Weight);
        }
    }

    public class Truck : Transport
    {
        public Truck() : base("Вантажівка", 5000) { }

        public override double CalculateCost(Order order)
        {
            return 200 + (15 * order.Distance) + (0.5 * order.Weight);
        }
    }

    public class DeliveryService
    {
        private List<Transport> _transports = new List<Transport>();
        private Dictionary<Order, Transport> _activeDeliveries = new Dictionary<Order, Transport>();
        private List<Order> _history = new List<Order>();

        public void AddTransport(Transport transport)
        {
            _transports.Add(transport);
        }

        public void ShowAvailableCosts(Order order)
        {
            Console.WriteLine($"\n--- Доступний транспорт для замовлення #{order.Number} ({order.Description}) ---");
            bool canBeDelivered = false;

            foreach (var transport in _transports)
            {
                if (transport.CanTake(order))
                {
                    Console.WriteLine($"- {transport.Name}: {transport.CalculateCost(order)} грн.");
                    canBeDelivered = true;
                }
            }

            if (!canBeDelivered)
            {
                Console.WriteLine("Жоден транспорт не може взяти це замовлення.");
            }
        }

        public void AssignOrder(Order order, Transport transport)
        {
            if (transport.IsBusy)
            {
                Console.WriteLine($"[Помилка] {transport.Name} зараз зайнятий.");
                return;
            }

            if (!transport.CanTake(order))
            {
                Console.WriteLine($"[Помилка] {transport.Name} не підходить через ліміти.");
                return;
            }

            transport.IsBusy = true;
            _activeDeliveries.Add(order, transport);
            Console.WriteLine($"[Успіх] Замовлення #{order.Number} призначено на '{transport.Name}'.");
        }

        public void DeliverOrder(Order order, IPayment paymentMethod)
        {
            if (_activeDeliveries.ContainsKey(order))
            {
                var transport = _activeDeliveries[order];
                double cost = transport.CalculateCost(order);

                Console.WriteLine($"\n--- Виконання доставки замовлення #{order.Number} ---");
                paymentMethod.Pay(cost);

                transport.IsBusy = false;
                _activeDeliveries.Remove(order);
                _history.Add(order);

                Console.WriteLine($"[Успіх] Доставка завершена. {transport.Name} вільний.");
            }
            else
            {
                Console.WriteLine($"[Помилка] Замовлення #{order.Number} не є активним.");
            }
        }

        public void PrintStatus()
        {
            Console.WriteLine("\n========== СТАТУС ==========");
            Console.WriteLine("Активні замовлення:");
            if (_activeDeliveries.Count == 0) Console.WriteLine(" - Немає");
            foreach (var kvp in _activeDeliveries)
            {
                Console.WriteLine($" - Замовлення #{kvp.Key.Number} доставляється ({kvp.Value.Name})");
            }

            Console.WriteLine("\nІсторія:");
            if (_history.Count == 0) Console.WriteLine(" - Порожня");
            foreach (var order in _history)
            {
                Console.WriteLine($" - Замовлення #{order.Number} ({order.Description})");
            }
            Console.WriteLine("============================\n");
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;

            DeliveryService service = new DeliveryService();

            var bike = new Bike();
            var car = new Car();
            var truck = new Truck();

            service.AddTransport(bike);
            service.AddTransport(car);
            service.AddTransport(truck);

            Order pizzaOrder = new Order { Number = 1, Description = "Піца", Weight = 2, Distance = 5 };
            Order tvOrder = new Order { Number = 2, Description = "Телевізор", Weight = 30, Distance = 15 };
            Order bricksOrder = new Order { Number = 3, Description = "Цегла", Weight = 3000, Distance = 20 };

            service.ShowAvailableCosts(pizzaOrder);
            service.ShowAvailableCosts(tvOrder);
            service.ShowAvailableCosts(bricksOrder);

            Console.WriteLine("\n--- Призначення замовлень ---");
            service.AssignOrder(pizzaOrder, bike);
            service.AssignOrder(tvOrder, car);

            Order sushiOrder = new Order { Number = 4, Description = "Суші", Weight = 1, Distance = 3 };
            service.AssignOrder(sushiOrder, bike);

            service.PrintStatus();

            IPayment card = new CardPayment();
            IPayment cash = new CashPayment();

            service.DeliverOrder(pizzaOrder, card);
            service.DeliverOrder(tvOrder, cash);

            service.PrintStatus();

            Console.WriteLine("--- Повторна спроба ---");
            service.AssignOrder(sushiOrder, bike);
            service.DeliverOrder(sushiOrder, cash);

            service.PrintStatus();
        }
    }
}