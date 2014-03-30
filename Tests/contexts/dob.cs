using System;

namespace Tests.contexts
{
    public abstract class dob
    {
        protected int calculated_age;
        DateTime date_of_birth;
        DateTimeOffset current_time;

        protected void when_calculating_age()
        {
            // this is of course completely wrong.
            calculated_age = (int)((current_time - date_of_birth).TotalDays / 365);
        }

        protected void given_date_of_birth(DateTime dateOfBirth)
        {
            date_of_birth = dateOfBirth;
        }

        protected void given_time_is(DateTimeOffset time)
        {
            current_time = time;
        }
    }
}