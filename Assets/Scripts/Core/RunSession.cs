using System;

public enum RunState
{
    Ready,
    Countdown,
    Running,
    Paused,
    GameOver
}

/// <summary>Single source of truth for run state. UI animations do not control gameplay time.</summary>
public sealed class RunSession
{
    public RunState State { get; private set; }
    private double elapsedSeconds;
    public float Elapsed { get { return (float)elapsedSeconds; } }
    public int Stars { get; private set; }
    public int Score { get { return (int)Math.Min(int.MaxValue, Math.Floor(elapsedSeconds)); } }

    public bool StartCountdown()
    {
        if (State != RunState.Ready && State != RunState.Paused) return false;
        State = RunState.Countdown;
        return true;
    }

    public bool Begin()
    {
        if (State != RunState.Countdown) return false;
        State = RunState.Running;
        return true;
    }

    public bool Pause()
    {
        if (State != RunState.Running && State != RunState.Countdown) return false;
        State = RunState.Paused;
        return true;
    }

    public void Advance(float deltaSeconds)
    {
        if (State != RunState.Running || deltaSeconds <= 0f || float.IsNaN(deltaSeconds) || float.IsInfinity(deltaSeconds))
            return;
        elapsedSeconds = Math.Min((double)int.MaxValue, elapsedSeconds + deltaSeconds);
    }

    public bool CollectStar()
    {
        if (State != RunState.Running) return false;
        Stars = RunnerRules.SaturatingAdd(Stars, 1);
        return true;
    }

    public bool Finish()
    {
        if (State == RunState.GameOver) return false;
        State = RunState.GameOver;
        return true;
    }
}
