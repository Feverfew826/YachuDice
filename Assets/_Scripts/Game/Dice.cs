using System.Collections.Generic;
using System.Threading;

using Cysharp.Threading.Tasks;

using UnityEngine;

public class Dice : MonoBehaviour
{
    [SerializeField] private AudioSource _colliderAudio;
    [SerializeField] private Rigidbody _rigidbody;
    [SerializeField] private Transform[] _positions;
    [SerializeField] private Vector3[] _rotations;
    [SerializeField] private float _stopVelocityTolerance;
    [SerializeField] private float _stopAngularVelocityTolerance;
    [SerializeField] private float _stopTimeout;

    private void OnCollisionEnter(Collision collision)
    {
        _colliderAudio.Play();
    }

    public async UniTask RollAsync(float force, float torque, float minDuration, CancellationToken cancellationToken)
    {
        _rigidbody.isKinematic = false;

        var random = Random.insideUnitCircle;
        var direction = Vector3.up + new Vector3(random.x, 0, random.y);
        direction.Normalize();
        _rigidbody.AddForce(direction * force, ForceMode.Impulse);
        _rigidbody.AddTorque(Random.insideUnitSphere * torque, ForceMode.Impulse);

        await UniTask.Delay(System.TimeSpan.FromSeconds(minDuration), cancellationToken: cancellationToken);
        await UniTask.WaitUntil(() => _rigidbody.velocity.magnitude < _stopVelocityTolerance && _rigidbody.angularVelocity.magnitude < _stopAngularVelocityTolerance, cancellationToken: cancellationToken).Timeout(System.TimeSpan.FromSeconds(_stopTimeout));
    }

    public void Stop()
    {
        _rigidbody.isKinematic = true;
    }

    public async UniTask RotateToNumberAsync(float duration, CancellationToken cancellationToken)
    {
        var result = GetResult();
        var index = result - 1;
        var destinationRotation = Quaternion.Euler(_rotations[index]);

        var targetTime = Time.fixedTime + duration;
        var delta = Quaternion.Angle(destinationRotation, transform.rotation) / duration * Time.fixedDeltaTime;
        while (Time.fixedTime < targetTime)
        {
            transform.rotation = Quaternion.RotateTowards(transform.rotation, destinationRotation, delta);
            await UniTask.NextFrame(PlayerLoopTiming.FixedUpdate, cancellationToken: cancellationToken);
        }

        transform.rotation = destinationRotation;
    }

    public int GetResult()
    {
        var positions = new List<Transform>();
        positions.AddRange(_positions);

        for (var i = 0; i < _positions.Length; i++)
        {
            var isTop = true;
            for (var j = 0; j < _positions.Length; j++)
            {
                if (_positions[i].position.y < _positions[j].position.y)
                {
                    isTop = false;
                    break;
                }
            }
            if (isTop)
                return i + 1;
        }

        throw new System.Exception();
    }
}
