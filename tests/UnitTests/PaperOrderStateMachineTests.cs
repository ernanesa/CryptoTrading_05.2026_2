using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CryptoTrading.Contracts.Interfaces;
using CryptoTrading.Domain.Entities;
using CryptoTrading.Domain.Enums;
using CryptoTrading.Domain.Services;
using Xunit;

namespace CryptoTrading.UnitTests;

public class PaperOrderStateMachineTests
{
    [Fact]
    public void Created_ShouldInitializeToNew()
    {
        var order = new PaperOrder
        {
            Id = 1,
            Symbol = "BTCUSDT",
            ClientOrderId = "test-1",
            Quantity = 1.0m,
            Price = 50000m
        };

        var orderEvent = PaperOrderStateMachine.Created(order, DateTime.UtcNow);

        Assert.Equal(OrderStatus.New, order.Status);
        Assert.Null(orderEvent.FromStatus);
        Assert.Equal(OrderStatus.New, orderEvent.ToStatus);
        Assert.Equal("Created", orderEvent.EventType);
    }

    [Fact]
    public void Activate_FromNew_ShouldSetToOpen()
    {
        var order = new PaperOrder
        {
            Id = 1,
            Symbol = "BTCUSDT",
            Status = OrderStatus.New
        };

        var orderEvent = PaperOrderStateMachine.Activate(order, DateTime.UtcNow);

        Assert.Equal(OrderStatus.Open, order.Status);
        Assert.Equal(OrderStatus.New, orderEvent.FromStatus);
        Assert.Equal(OrderStatus.Open, orderEvent.ToStatus);
    }

    [Fact]
    public void ApplyFill_FullFill_ShouldSetToFilled()
    {
        var order = new PaperOrder
        {
            Id = 1,
            Symbol = "BTCUSDT",
            Status = OrderStatus.Open,
            Quantity = 1.0m,
            FilledQuantity = 0m
        };

        var orderEvent = PaperOrderStateMachine.ApplyFill(order, 1.0m, 50000m, 50m, DateTime.UtcNow);

        Assert.Equal(OrderStatus.Filled, order.Status);
        Assert.Equal(1.0m, order.FilledQuantity);
        Assert.Equal(50000m, order.AverageFillPrice);
        Assert.Equal(50m, order.FeePaid);
    }

    [Fact]
    public void InvalidTransition_ShouldThrow()
    {
        var order = new PaperOrder
        {
            Id = 1,
            Symbol = "BTCUSDT",
            Status = OrderStatus.Filled
        };

        Assert.Throws<InvalidOperationException>(() => PaperOrderStateMachine.Activate(order, DateTime.UtcNow));
    }
}
