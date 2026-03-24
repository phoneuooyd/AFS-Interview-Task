using System;
using AFS_Interview_Task.Exceptions;
using Xunit;

namespace AFS_Interview_Task.Tests.ExceptionsTests
{
    public class UnsupportedTranslatorExceptionTests
    {
        [Fact]
        public void Message_Contains_Translator_Name()
        {
            var ex = new UnsupportedTranslatorException("foo");
            Assert.Contains("foo", ex.Message);
        }

        [Fact]
        public void Can_Be_Thrown()
        {
            void Thrower() => throw new UnsupportedTranslatorException("bar");
            var ex = Record.Exception((Action)Thrower);
            Assert.IsType<UnsupportedTranslatorException>(ex);
        }
    }
}
