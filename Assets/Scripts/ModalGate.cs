using UnityEngine;
using UnityEngine.EventSystems;

namespace CatGame
{
    /// <summary>One owner per HUD. Only that owner may restore the captured simulation speed.</summary>
    public sealed class ModalGate : MonoBehaviour
    {
        object _owner;
        float _previous;
        int _blockedFrame=-1;
        public bool InputBlocked { get { return IsBusy || Time.frameCount==_blockedFrame; } }
        public void SuppressGameplayThisFrame() { _blockedFrame=Time.frameCount; }
        public bool IsBusy { get { return _owner != null; } }
        public bool TryAcquire(object owner)
        {
            if (owner == null || IsBusy || Time.timeScale <= 0) return false;
            _owner = owner; _previous = Time.timeScale; Time.timeScale = 0;
            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
            return true;
        }
        public void Release(object owner)
        {
            if (!ReferenceEquals(_owner,owner)) return;
            _owner = null; Time.timeScale = _previous;
        }
        void OnDestroy() { if (IsBusy) { _owner=null; Time.timeScale=_previous; } }
    }
}
