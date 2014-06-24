using System;
using System.Collections.Generic;
using Tests.Annotations;

namespace Tests.contexts
{
    public abstract class dob
    {
        protected int calculated_age;
        protected DateTime date_of_birth;
        protected DateTimeOffset current_time;
        [UsedImplicitly] protected IDictionary<string, object> scenario_details;


        protected void when_calculating_age()
        {
            // this is of course completely wrong.
            calculated_age = (int)((current_time - date_of_birth).TotalDays / 365);
        }

        protected void given_date_of_birth(DateTime dateOfBirth)
        {
            date_of_birth = dateOfBirth;
        }

        protected void given_time_is(DateTimeOffset now)
        {
            current_time = now;
        }
    }
}