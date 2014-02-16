namespace jacket.Reporting
{
    public interface IReporter
    {
        void Success(ScenarioResult scenarioResult);
        void Fail(ScenarioResult scenarioResult);
        void RunUntilCompletion();
        void Finished();
    }
}