namespace CESMII.Marketplace.Data.Extensions
{
    using System;

    public static class ExpressionExtension
    {
        public static Func<T, bool> And<T>(this Func<T, bool> left, Func<T, bool> right)
            => a => left(a) && right(a);

        public static Func<T, bool> Or<T>(this Func<T, bool> left, Func<T, bool> right)
            => a => left(a) || right(a);
    }
}
