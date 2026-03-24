using System;
using AFS_Interview_Task.Exceptions;
using Xunit;

namespace AFS_Interview_Task.Tests.ExceptionsTests
{
    public class RateLimitExceptionTests
    {
        [Fact]
        public void Constructor_Sets_RetryAfter()
        {
            var ts = TimeSpan.FromSeconds(30);
            var ex = new RateLimitException(ts);

            Assert.Equal(ts, ex.RetryAfter);
        }

        [Fact]
        public void Default_Message_Is_Set()
        {
            var ex = new RateLimitException(TimeSpan.Zero);
            Assert.Equal("Rate limit exceeded.", ex.Message);
        }
    }
}
