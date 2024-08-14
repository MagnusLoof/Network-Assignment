public class NetworkTimer
{
    float timer;
    public float minTimeBetweenTicks { get; }
    public int currentTick { get; private set; }

    public NetworkTimer(float serverTickRate)
    {
        minTimeBetweenTicks = 1.0f / serverTickRate;
    }

    public void Update(float deltaTime)
    {
        timer += deltaTime;
    }

    public bool ShouldTick()
    {
        if(timer >= minTimeBetweenTicks)
        {
            timer -= minTimeBetweenTicks;
            currentTick++;
            return true;
        }

        return false;
    }
}
