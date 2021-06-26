/* This interface defines the method required for an object to be 'Resettable'
in the ObjectPool class. Parameters required for the Resetting are found inside
resetParameters. */
public interface IResettable {
    void Initialise(params object[] resetParameters);
    void AllocateMemory(params object[] memoryAllocationParameters);
    void OnDeath();
}
