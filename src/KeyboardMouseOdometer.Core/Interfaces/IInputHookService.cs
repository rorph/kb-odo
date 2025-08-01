using KeyboardMouseOdometer.Core.Models;

namespace KeyboardMouseOdometer.Core.Interfaces;

public interface IInputHookService : IDisposable
{
    event EventHandler<InputEvent>? InputReceived;
    bool IsActive { get; }
    void Start();
    void Stop();
}