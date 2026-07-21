using System;
using System.Collections.Generic;
using System.Text;
using UnitTesting.Entities;

namespace UnitTesting.Services
{
    public interface IOrderService
    {
        public Task<Order> CreateOrder(int productId, int quantity, string customerEmail);
    }
}
