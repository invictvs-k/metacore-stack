using System;
using System.Threading.Tasks;
using Xunit;
using FluentAssertions;
using RoomServer.Models;

namespace RoomServer.Tests;

public class RoomContextTests
{
    [Fact]
    public void GetOrCreate_CreatesNewContext_WithInitState()
    {
        var store = new RoomContextStore();
        var context = store.GetOrCreate("room-test123");
        
        context.Should().NotBeNull();
        context.RoomId.Should().Be("room-test123");
        context.State.Should().Be(RoomState.Init);
        context.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void GetOrCreate_ReturnsSameContext_ForSameRoom()
    {
        var store = new RoomContextStore();
        var context1 = store.GetOrCreate("room-test123");
        var context2 = store.GetOrCreate("room-test123");
        
        context1.Should().BeSameAs(context2);
    }

    [Fact]
    public void UpdateState_ChangesStateAndTimestamp()
    {
        var store = new RoomContextStore();
        var context = store.GetOrCreate("room-test123");
        var originalCreatedAt = context.CreatedAt;
        
        context.LastStateChange.Should().BeNull();
        
        store.UpdateState("room-test123", RoomState.Active);
        
        context.State.Should().Be(RoomState.Active);
        context.LastStateChange.Should().NotBeNull();
        context.LastStateChange.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        context.CreatedAt.Should().Be(originalCreatedAt); // CreatedAt should not change
    }

    [Fact]
    public void Get_ReturnsNull_ForNonExistentRoom()
    {
        var store = new RoomContextStore();
        var context = store.Get("room-nonexistent");
        
        context.Should().BeNull();
    }

    [Fact]
    public void Remove_RemovesContext()
    {
        var store = new RoomContextStore();
        store.GetOrCreate("room-test123");
        
        var removed = store.Remove("room-test123");
        removed.Should().BeTrue();
        
        var context = store.Get("room-test123");
        context.Should().BeNull();
    }

    [Fact]
    public void RoomState_Enum_HasExpectedValues()
    {
        RoomState.Init.Should().BeDefined();
        RoomState.Active.Should().BeDefined();
        RoomState.Paused.Should().BeDefined();
        RoomState.Ended.Should().BeDefined();
    }

    [Fact]
    public void RoomState_ToString_ReturnsCorrectValues()
    {
        RoomState.Init.ToString().Should().Be("Init");
        RoomState.Active.ToString().Should().Be("Active");
        RoomState.Paused.ToString().Should().Be("Paused");
        RoomState.Ended.ToString().Should().Be("Ended");
    }
}
