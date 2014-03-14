using System;

namespace Tests
{
    public static class DateExtensions
    {
        public static DateTime December(this int day, int year)
        {
            return new DateTime(year, 12, day);
        } 
    }
}