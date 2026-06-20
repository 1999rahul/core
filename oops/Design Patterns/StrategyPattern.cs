// ============================================================
// STRATEGY PATTERN IN C#
// ------------------------------------------------------------
// Key distinction from plain interface usage:
//   1. Context HOLDS the strategy as a field (delegation)
//   2. Strategy is SWAPPABLE at runtime
//   3. Context is OBLIVIOUS to which concrete strategy it has
// ============================================================

using System;
using System.Collections.Generic;
using System.Linq;

namespace StrategyPattern
{
    // --------------------------------------------------------
    // STEP 1: THE STRATEGY INTERFACE
    // This is just a contract — on its own, not a pattern yet.
    // --------------------------------------------------------
    public interface IPaymentStrategy
    {
        void Pay(decimal amount);
    }


    // --------------------------------------------------------
    // STEP 2: CONCRETE STRATEGIES
    // Each encapsulates ONE algorithm / behavior.
    // Adding a new payment method = add a new class here.
    // ShoppingCart (the context) never needs to change.
    // --------------------------------------------------------

    public class CreditCardPayment : IPaymentStrategy
    {
        private readonly string _cardNumber;

        public CreditCardPayment(string cardNumber)
        {
            _cardNumber = cardNumber;
        }

        public void Pay(decimal amount)
        {
            Console.WriteLine($"[CreditCard] Charged ${amount:F2} to card ending in {_cardNumber[^4..]}");
        }
    }

    public class PayPalPayment : IPaymentStrategy
    {
        private readonly string _email;

        public PayPalPayment(string email)
        {
            _email = email;
        }

        public void Pay(decimal amount)
        {
            Console.WriteLine($"[PayPal] Sent ${amount:F2} to {_email}");
        }
    }

    public class CryptoPayment : IPaymentStrategy
    {
        private readonly string _walletAddress;

        public CryptoPayment(string walletAddress)
        {
            _walletAddress = walletAddress;
        }

        public void Pay(decimal amount)
        {
            Console.WriteLine($"[Crypto] Transferred ${amount:F2} BTC to wallet {_walletAddress[..6]}...");
        }
    }

    // --------------------------------------------------------
    // STEP 3: THE CONTEXT
    // This is what makes it the Strategy *pattern*, not just
    // "multiple interface implementations":
    //
    //   - _strategy is a FIELD (not a local variable)
    //   - Checkout() DELEGATES to it without knowing what it is
    //   - SetPaymentStrategy() allows RUNTIME swapping
    // --------------------------------------------------------
    public class ShoppingCart
    {
        // KEY TRAIT #1 — context holds the strategy as a field
        private IPaymentStrategy _strategy;

        private readonly List<(string Name, decimal Price)> _items = new();

        // Inject via constructor — context is blind to the concrete type
        public ShoppingCart(IPaymentStrategy strategy)
        {
            _strategy = strategy;
        }

        // KEY TRAIT #2 — strategy is swappable at runtime
        // e.g. user changes payment method mid-checkout
        public void SetPaymentStrategy(IPaymentStrategy strategy)
        {
            _strategy = strategy;
        }

        public void AddItem(string name, decimal price)
        {
            _items.Add((name, price));
            Console.WriteLine($"  Added: {name} (${price:F2})");
        }

        public void Checkout()
        {
            decimal total = _items.Sum(i => i.Price);
            Console.WriteLine($"  Order total: ${total:F2}");

            // KEY TRAIT #3 — context is oblivious to WHICH strategy it holds.
            // It just calls .Pay(). The algorithm runs, context doesn't care how.
            _strategy.Pay(total);
        }
    }


    // --------------------------------------------------------
    // COUNTER-EXAMPLE: What Strategy is NOT
    // This is plain interface usage — no pattern here.
    // The caller picks and calls the implementation directly.
    // There's no context, no delegation, no runtime swapping.
    // --------------------------------------------------------
    public static class NotStrategyPattern
    {
        public static void Demo()
        {
            Console.WriteLine("\n--- NOT Strategy Pattern (plain interface usage) ---");

            // Caller knows exactly what it's calling.
            // No context. No delegation. Not swappable.
            IPaymentStrategy payment = new CreditCardPayment("4111111111111234");
            payment.Pay(99.99m);
        }
    }


    // --------------------------------------------------------
    // FACTORY (optional but common in production)
    // Keeps the strategy-selection logic in one place.
    // --------------------------------------------------------
    public static class PaymentStrategyFactory
    {
        public static IPaymentStrategy Create(string method) => method.ToLower() switch
        {
            "creditcard" => new CreditCardPayment("4111111111111234"),
            "paypal" => new PayPalPayment("user@example.com"),
            "crypto" => new CryptoPayment("1A2b3C4d5E6f7G8h"),
            _ => throw new ArgumentException($"Unknown payment method: {method}")
        };
    }


    // --------------------------------------------------------
    // DEMO
    // --------------------------------------------------------
    class Program
    {
        static void Main()
        {
            Console.WriteLine("=== Strategy Pattern Demo ===\n");


            // --- Demo 1: Constructor injection ---
            Console.WriteLine("--- Demo 1: Pay with Credit Card ---");
            var cart = new ShoppingCart(new CreditCardPayment("4111111111111234"));
            cart.AddItem("Laptop", 999.99m);
            cart.AddItem("Mouse", 29.99m);
            cart.Checkout();


            // --- Demo 2: Swap strategy at runtime ---
            // Same cart, user changes mind at checkout. Context doesn't care.
            Console.WriteLine("\n--- Demo 2: User switches to PayPal mid-checkout ---");
            cart.SetPaymentStrategy(new PayPalPayment("user@example.com"));
            cart.Checkout();


            // --- Demo 3: New strategy without touching ShoppingCart ---
            Console.WriteLine("\n--- Demo 3: New payment method (Crypto) — ShoppingCart unchanged ---");
            cart.SetPaymentStrategy(new CryptoPayment("1A2b3C4d5E6f7G8h"));
            cart.Checkout();


            // --- Demo 4: Via factory (production-style) ---
            Console.WriteLine("\n--- Demo 4: Strategy resolved from factory ---");
            string userChoice = "paypal"; // comes from UI / API request
            var cart2 = new ShoppingCart(PaymentStrategyFactory.Create(userChoice));
            cart2.AddItem("Keyboard", 79.99m);
            cart2.Checkout();


            // --- Counter-example ---
            NotStrategyPattern.Demo();


            // --- Summary ---
            Console.WriteLine("\n=== What makes it Strategy (not just interface usage) ===");
            Console.WriteLine("  1. Context holds strategy as a FIELD     -> _strategy");
            Console.WriteLine("  2. Context DELEGATES to it               -> _strategy.Pay()");
            Console.WriteLine("  3. Strategy is swappable at RUNTIME      -> SetPaymentStrategy()");
            Console.WriteLine("  4. Context is OBLIVIOUS to concrete type -> knows only IPaymentStrategy");
        }
    }
}