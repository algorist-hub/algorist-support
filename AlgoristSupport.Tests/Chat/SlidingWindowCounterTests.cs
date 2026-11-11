using AlgoristSupport.Chat;
using Microsoft.Extensions.Time.Testing;

namespace AlgoristSupport.Tests.Chat;

public class SlidingWindowCounterTests
{
    private readonly FakeTimeProvider time = new();

    private SlidingWindowCounter Create (int limit) => new(limit, TimeSpan.FromSeconds(60), time);

    [Fact]
    public void Allows_up_to_limit_then_rejects ()
    {
        var counter = Create(3);

        Assert.True(counter.TryAcquire(out _));
        Assert.True(counter.TryAcquire(out _));
        Assert.True(counter.TryAcquire(out var none));
        Assert.Equal(TimeSpan.Zero, none);
        Assert.False(counter.TryAcquire(out _));
    }

    [Fact]
    public void Retry_after_is_measured_from_the_oldest_acquisition ()
    {
        var counter = Create(2);
        counter.TryAcquire(out _);
        time.Advance(TimeSpan.FromSeconds(20));
        counter.TryAcquire(out _);
        time.Advance(TimeSpan.FromSeconds(15));

        Assert.False(counter.TryAcquire(out var retryAfter));
        Assert.Equal(TimeSpan.FromSeconds(25), retryAfter);
    }

    [Fact]
    public void Window_slides ()
    {
        var counter = Create(2);
        counter.TryAcquire(out _);
        time.Advance(TimeSpan.FromSeconds(30));
        counter.TryAcquire(out _);

        time.Advance(TimeSpan.FromSeconds(29.9));
        Assert.False(counter.TryAcquire(out _));

        time.Advance(TimeSpan.FromSeconds(0.1));
        Assert.True(counter.TryAcquire(out _));
        Assert.False(counter.TryAcquire(out _));
    }

    [Fact]
    public void Rejected_attempts_are_not_counted ()
    {
        var counter = Create(1);
        counter.TryAcquire(out _);
        for (var i = 0; i < 5; i++)
        {
            Assert.False(counter.TryAcquire(out _));
        }

        time.Advance(TimeSpan.FromSeconds(60));
        Assert.True(counter.TryAcquire(out _));
    }

    [Fact]
    public void Limit_below_one_is_rejected () =>
        Assert.Throws<ArgumentOutOfRangeException>(() => Create(0));
}
