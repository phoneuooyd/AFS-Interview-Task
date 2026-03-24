using System;
using AFS_Interview_Task.Exceptions;
using Xunit;

namespace AFS_Interview_Task.Tests.ExceptionsTests
{
    public class TranslationTimeoutExceptionTests
    {
        [Fact]
        public void Default_Message_Is_Set()
        {
            var ex = new TranslationTimeoutException();
            Assert.Equal("The translation provider request timed out.", ex.Message);
        }

        [Fact]
        public void Can_Be_Thrown()
        {
            void Thrower() => throw new TranslationTimeoutException("custom");
            var ex = Record.Exception((Action)Thrower);
            Assert.IsType<TranslationTimeoutException>(ex);
        }
    }
}
