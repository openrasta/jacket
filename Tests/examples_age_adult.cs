using System;

namespace Tests
{
    public class example_of_calculating_age : contexts.dob
    {
        static object[] examples =
        {
            new { dateOfBirth = 14.December(2005), age=8 }
        };

        example_of_calculating_age(DateTime dateOfBirth)
        {
            given_time_is(DateTimeOffset.UtcNow);
            given_date_of_birth(dateOfBirth);
            when_calculating_age();
        }

        void age_is_correct(int age)
        {
            calculated_age.Is(age);
        }
    }
}