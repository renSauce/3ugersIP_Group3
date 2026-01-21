using System;
using System.Linq;
using Avalonia.Threading;

namespace SystemLogin.Security;

public class LoginSecurityService
{
    private readonly TimeSpan _lockDuration;
    private readonly Action<string> _log;
    private readonly DispatcherTimer _lockTimer;
    private DateTimeOffset? _lockedUntil;
    private int _consecutiveFailures;

    public LoginSecurityService(TimeSpan lockDuration, Action<string> logCallback)
    {
        _lockDuration = lockDuration;
        _log = logCallback;
        _lockTimer = new DispatcherTimer { Interval = _lockDuration };
        _lockTimer.Tick += (_, _) => Unlock();
    }

    public event Action? LockStateChanged;

    public bool IsLoginLocked => _lockedUntil.HasValue && _lockedUntil > DateTimeOffset.UtcNow;

    public static bool IsCredentialValid(string? value) =>
        !string.IsNullOrWhiteSpace(value) && value.All(char.IsLetterOrDigit);

    public static bool IsPasswordValid(string? value) =>
        IsCredentialValid(value) && value!.Length > 8;

    public void RecordFailedAttempt()
    {
        if (IsLoginLocked)
            return;

        _consecutiveFailures++;
        if (_consecutiveFailures < 3)
            return;

        _lockedUntil = DateTimeOffset.UtcNow.Add(_lockDuration);
        _log($"Login disabled for {(int)_lockDuration.TotalMinutes} minutes due to repeated failures.");
        _lockTimer.Stop();
        _lockTimer.Start();
        LockStateChanged?.Invoke();
    }

    public void ResetFailures()
    {
        _consecutiveFailures = 0;
        _lockedUntil = null;
        _lockTimer.Stop();
        LockStateChanged?.Invoke();
    }

    private void Unlock()
    {
        _lockTimer.Stop();
        _lockedUntil = null;
        _consecutiveFailures = 0;
        _log("Login re-enabled. You can try again.");
        LockStateChanged?.Invoke();
    }
}
