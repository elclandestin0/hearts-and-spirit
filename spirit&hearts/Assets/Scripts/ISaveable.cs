using System;

public interface ISaveable
{
    /// <summary>
    /// Return an arbitrary serializable state object (struct or simple class) 
    /// that represents this component’s state.
    /// </summary>
    object CaptureState();

    /// <summary>
    /// Receive a state object (previously returned by CaptureState) 
    /// and restore internal fields.
    /// </summary>
    void RestoreState(object state);
}
