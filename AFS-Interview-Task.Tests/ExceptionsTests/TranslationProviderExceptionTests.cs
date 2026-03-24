using System;
using AFS_Interview_Task.Exceptions;
using Xunit;

namespace AFS_Interview_Task.Tests.ExceptionsTests
{
    public class TranslationProviderExceptionTests
    {
        [Fact]
        public void Constructor_Sets_StatusCode()
        {
            var ex = new TranslationProviderException(502, "Bad gateway");
            Assert.Equal(502, ex.StatusCode);
            Assert.Equal("Bad gateway", ex.Message);
        }

        [Fact]
        public void Can_Be_Thrown_And_Caught()
        {
            void Thrower() => throw new TranslationProviderException(500, "Err");
            var ex = Record.Exception((Action)Thrower);
            Assert.IsType<TranslationProviderException>(ex);
        }
    }
}
