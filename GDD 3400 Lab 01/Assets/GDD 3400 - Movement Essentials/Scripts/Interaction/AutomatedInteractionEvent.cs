using UnityEngine;
using UnityEngine.Events;

namespace GDD3400.MovementEssentials
{
    public class AutomatedInteractionEvent : MonoBehaviour
    {
        [SerializeField] private bool _isActive = true;
        [SerializeField] private Vector2 _interactionRangeX = new Vector2(-10, 10);
        [SerializeField] private Vector2 _interactionRangeZ = new Vector2(-10, 10);
        [SerializeField] private Vector2 _interactionInterval = new Vector2(5, 10);

        [SerializeField] private UnityEvent<Vector3> _onInteraction;

        private float _nextInteractionTime;
        private float _timer;

        private void Start()
        {
            _nextInteractionTime = 1f;
        }

        private void Update()
        {
            if (!_isActive) return;

            _timer += Time.deltaTime;

            if (_timer >= _nextInteractionTime)
            {
                _onInteraction.Invoke(new Vector3(Random.Range(_interactionRangeX.x, _interactionRangeX.y), 0, Random.Range(_interactionRangeZ.x, _interactionRangeZ.y)));
                _nextInteractionTime = Random.Range(_interactionInterval.x, _interactionInterval.y);
                _timer = 0f;
            }
        }
    }
}
