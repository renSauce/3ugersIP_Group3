using System;
using Avalonia.Threading;

namespace SystemLogin.Security;

public class InactivityLogoutTimer
{
    private readonly DispatcherTimer _timer;
    private readonly Action _onTimeout;

    public InactivityLogoutTimer(TimeSpan timeout, Action onTimeout)
    {
        _onTimeout = onTimeout;
        _timer = new DispatcherTimer { Interval = timeout };
        _timer.Tick += (_, _) => HandleTimeout();
    }

    public void Reset()
    {
        _timer.Stop();
        _timer.Start();
    }

    public void Stop() => _timer.Stop();

    private void HandleTimeout()
    {
        _timer.Stop();
        _onTimeout();
    }
}
