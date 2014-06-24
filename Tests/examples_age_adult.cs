using System;

namespace Tests
{
    public class example_of_calculating_age_from_year_2000 : contexts.dob
    {
        static object[] examples =
        {
            new { dateOfBirth = 14.December(2005), age=4, expectedYear = 2009 }
        };

        example_of_calculating_age_from_year_2000(DateTime dateOfBirth)
        {
            given_time_is(new DateTimeOffset(2010, 01, 01, 0, 0, 0, TimeSpan.Zero));
            given_date_of_birth(dateOfBirth);
            when_calculating_age();
        }

        public void age_is_correct(int age)
        {
            calculated_age.Is(age);
        }

        public void date_of_birth_plus_age_is_expected_year(int age, int expectedYear)
        {
            date_of_birth.AddYears(age).Year.Is(expectedYear);
        }

        public void acceptance_criteria_with_text_matching_parameter_is_highlighted()
        {
            scenario_details["then.age_is_correct.display.name"].Is("*age* is correct");
        }

        public void acceptance_criteria_with_unmatching_parameter_has_parameters_appendend_and_highlighted()
        {
            scenario_details["then.date_of_birth_plus_age_is_expected_year.display.name"].Is("date of birth plus *age* is *expected year*");
        }
    }
}