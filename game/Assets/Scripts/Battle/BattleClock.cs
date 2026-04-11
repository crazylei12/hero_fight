namespace Fight.Battle
{
    public class BattleClock
    {
        public BattleClock(float regulationDurationSeconds)
        {
            RegulationDurationSeconds = regulationDurationSeconds;
        }

        public float RegulationDurationSeconds { get; }

        public float ElapsedTimeSeconds { get; private set; }

        public bool IsRunning { get; private set; }

        public bool IsOvertime { get; private set; }

        public void Start()
        {
            ElapsedTimeSeconds = 0f;
            IsRunning = true;
            IsOvertime = false;
        }

        public void Tick(float deltaTime)
        {
            if (!IsRunning)
            {
                return;
            }

            ElapsedTimeSeconds += deltaTime;
        }

        public void EnterOvertime()
        {
            IsOvertime = true;
        }

        public void Stop()
        {
            IsRunning = false;
        }

        public bool HasReachedRegulationTime()
        {
            return ElapsedTimeSeconds >= RegulationDurationSeconds;
        }
    }
}
