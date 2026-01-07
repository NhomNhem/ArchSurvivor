using R3;
using UnityEngine;

namespace _ArchSurvivor.Core.Interfaces {
    public interface IInputReader {
        ReadOnlyReactiveProperty<Vector2> MoveDirection { get; }        
        ReadOnlyReactiveProperty<bool> IsMoving { get; }
    }
}