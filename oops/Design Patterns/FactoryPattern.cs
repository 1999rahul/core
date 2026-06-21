using System;
using System.Collections.Generic;
using System.Text;

namespace oops.Design_Patterns
{
    // Simple factory pattern

    // Product interface
    public interface INotification
    {
        void Send(string message);
    }

    // Concrete products
    public class EmailNotification : INotification
    {
        public void Send(string message) =>
            Console.WriteLine($"📧 Email: {message}");
    }

    public class SmsNotification : INotification
    {
        public void Send(string message) =>
            Console.WriteLine($"📱 SMS: {message}");
    }

    public static class NotificationFactory
    {
        public static INotification Create(string type) => type switch
        {
            "email" => new EmailNotification(),
            "sms" => new SmsNotification(),
            _ => throw new ArgumentException($"Unknown type: {type}")
        };
    }


    // Abstract factory pattern

    public interface IButton
    {
        void Render();
    }

    public interface ICheckbox
    {
        void Render();
    }

    // ── Windows family ────────────────────────────────────────────
    public class WindowsButton : IButton
    {
        public void Render() => Console.WriteLine("🪟 Rendering Windows Button");
    }

    public class WindowsCheckbox : ICheckbox
    {
        public void Render() => Console.WriteLine("🪟 Rendering Windows Checkbox");
    }

    // ── Mac family ────────────────────────────────────────────────
    public class MacButton : IButton
    {
        public void Render() => Console.WriteLine("🍎 Rendering Mac Button");
    }

    public class MacCheckbox : ICheckbox
    {
        public void Render() => Console.WriteLine("🍎 Rendering Mac Checkbox");
    }

    // ── Abstract Factory ──────────────────────────────────────────
    public interface IUIFactory
    {
        IButton CreateButton();
        ICheckbox CreateCheckbox();
    }

    // ── Concrete Factories ────────────────────────────────────────
    public class WindowsFactory : IUIFactory
    {
        public IButton CreateButton() => new WindowsButton();
        public ICheckbox CreateCheckbox() => new WindowsCheckbox();
    }

    public class MacFactory : IUIFactory
    {
        public IButton CreateButton() => new MacButton();
        public ICheckbox CreateCheckbox() => new MacCheckbox();
    }

    // ── Client code (completely decoupled from concrete types) ────
    public class Application
    {
        private readonly IButton _button;
        private readonly ICheckbox _checkbox;

        public Application(IUIFactory factory)
        {
            _button = factory.CreateButton();
            _checkbox = factory.CreateCheckbox();
        }

        public void RenderUI()
        {
            _button.Render();
            _checkbox.Render();
        }
    }


}
