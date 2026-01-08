using System;
using System.Collections.Generic;
using KH.Framework2D.Pool;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Random = UnityEngine.Random;

namespace KH.Framework2D.Utils
{
    /// <summary>
    /// Spawn point marker with gizmo visualization.
    /// </summary>
    public class SpawnPoint : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private Color _gizmoColor = Color.green;
        [SerializeField] private float _gizmoRadius = 0.5f;
        [SerializeField] private SpawnPointType _type = SpawnPointType.Default;
        [SerializeField] private int _teamId = 0;
        
        public SpawnPointType Type => _type;
        public int TeamId => _teamId;
        public Vector3 Position => transform.position;
        public Quaternion Rotation => transform.rotation;
        
        private void OnDrawGizmos()
        {
            Gizmos.color = _gizmoColor;
            Gizmos.DrawWireSphere(transform.position, _gizmoRadius);
            Gizmos.DrawLine(transform.position, transform.position + transform.forward * _gizmoRadius * 2);
        }
        
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = _gizmoColor;
            Gizmos.DrawSphere(transform.position, _gizmoRadius);
        }
    }
    
    public enum SpawnPointType
    {
        Default,
        Player,
        Enemy,
        Item,
        Boss
    }
    
    /// <summary>
    /// Area-based spawner for random spawn positions.
    /// </summary>
    public class SpawnArea : MonoBehaviour
    {
        [Header("Area")]
        [SerializeField] private Vector2 _size = new Vector2(10f, 10f);
        [SerializeField] private bool _useCircle = false;
        [SerializeField] private float _radius = 5f;
        
        [Header("Settings")]
        [SerializeField] private Color _gizmoColor = Color.cyan;
        
        /// <summary>
        /// Get a random position within the spawn area.
        /// </summary>
        public Vector3 GetRandomPosition()
        {
            Vector3 offset;
            
            if (_useCircle)
            {
                Vector2 randomCircle = Random.insideUnitCircle * _radius;
                offset = new Vector3(randomCircle.x, randomCircle.y, 0);
            }
            else
            {
                offset = new Vector3(
                    Random.Range(-_size.x / 2f, _size.x / 2f),
                    Random.Range(-_size.y / 2f, _size.y / 2f),
                    0
                );
            }
            
            return transform.position + transform.TransformDirection(offset);
        }
        
        /// <summary>
        /// Get multiple random positions.
        /// </summary>
        public Vector3[] GetRandomPositions(int count, float minDistance = 0f)
        {
            var positions = new List<Vector3>();
            int maxAttempts = count * 10;
            int attempts = 0;
            
            while (positions.Count < count && attempts < maxAttempts)
            {
                Vector3 pos = GetRandomPosition();
                
                // Check minimum distance from other positions
                bool valid = true;
                if (minDistance > 0)
                {
                    foreach (var existingPos in positions)
                    {
                        if (Vector3.Distance(pos, existingPos) < minDistance)
                        {
                            valid = false;
                            break;
                        }
                    }
                }
                
                if (valid)
                {
                    positions.Add(pos);
                }
                
                attempts++;
            }
            
            return positions.ToArray();
        }
        
        private void OnDrawGizmos()
        {
            Gizmos.color = _gizmoColor;
            
            if (_useCircle)
            {
                DrawGizmoCircle(transform.position, _radius, 32);
            }
            else
            {
                Gizmos.matrix = transform.localToWorldMatrix;
                Gizmos.DrawWireCube(Vector3.zero, new Vector3(_size.x, _size.y, 0.1f));
            }
        }
        
        private void DrawGizmoCircle(Vector3 center, float radius, int segments)
        {
            float angleStep = 360f / segments;
            Vector3 prevPoint = center + new Vector3(radius, 0, 0);
            
            for (int i = 1; i <= segments; i++)
            {
                float angle = i * angleStep * Mathf.Deg2Rad;
                Vector3 newPoint = center + new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, 0);
                Gizmos.DrawLine(prevPoint, newPoint);
                prevPoint = newPoint;
            }
        }
    }
    
    /// <summary>
    /// Wave-based spawner for enemies or items.
    /// </summary>
    public class WaveSpawner : MonoBehaviour
    {
        [Header("Spawn Points")]
        [SerializeField] private SpawnPoint[] _spawnPoints;
        [SerializeField] private SpawnArea _spawnArea;
        
        [Header("Wave Settings")]
        [SerializeField] private WaveData[] _waves;
        [SerializeField] private float _timeBetweenWaves = 5f;
        [SerializeField] private float _timeBetweenSpawns = 0.5f;
        [SerializeField] private bool _autoStart = false;
        
        private int _currentWave;
        private int _aliveCount;
        private bool _isSpawning;
        
        public int CurrentWave => _currentWave;
        public int TotalWaves => _waves?.Length ?? 0;
        public int AliveCount => _aliveCount;
        public bool IsSpawning => _isSpawning;
        public bool AllWavesComplete => _currentWave >= TotalWaves;
        
        public event Action<int> OnWaveStarted; // Wave number
        public event Action<int> OnWaveCompleted; // Wave number
        public event Action OnAllWavesCompleted;
        public event Action<GameObject> OnEnemySpawned;
        public event Action<GameObject> OnEnemyDied;
        
        private void Start()
        {
            if (_autoStart)
            {
                StartSpawning();
            }
        }
        
        /// <summary>
        /// Start the wave spawning sequence.
        /// </summary>
        public void StartSpawning()
        {
            if (_isSpawning) return;
            
            _currentWave = 0;
            _isSpawning = true;
            SpawnNextWave().Forget();
        }
        
        /// <summary>
        /// Stop spawning.
        /// </summary>
        public void StopSpawning()
        {
            _isSpawning = false;
        }
        
        /// <summary>
        /// Skip to next wave.
        /// </summary>
        public void SkipToNextWave()
        {
            if (_currentWave < TotalWaves)
            {
                SpawnNextWave().Forget();
            }
        }
        
        private async UniTaskVoid SpawnNextWave()
        {
            if (_currentWave >= _waves.Length)
            {
                _isSpawning = false;
                OnAllWavesCompleted?.Invoke();
                return;
            }
            
            var wave = _waves[_currentWave];
            OnWaveStarted?.Invoke(_currentWave + 1);
            
            // Spawn enemies
            foreach (var entry in wave.entries)
            {
                for (int i = 0; i < entry.count; i++)
                {
                    if (!_isSpawning) return;
                    
                    SpawnEnemy(entry.prefab);
                    
                    if (_timeBetweenSpawns > 0)
                    {
                        await UniTask.Delay(TimeSpan.FromSeconds(_timeBetweenSpawns));
                    }
                }
            }
            
            // Wait for all enemies to die or time between waves
            if (wave.waitForClear)
            {
                await UniTask.WaitUntil(() => _aliveCount <= 0 || !_isSpawning);
            }
            else
            {
                await UniTask.Delay(TimeSpan.FromSeconds(_timeBetweenWaves));
            }
            
            OnWaveCompleted?.Invoke(_currentWave + 1);
            _currentWave++;
            
            if (_isSpawning)
            {
                SpawnNextWave().Forget();
            }
        }
        
        private void SpawnEnemy(GameObject prefab)
        {
            Vector3 position = GetSpawnPosition();
            var enemy = Instantiate(prefab, position, Quaternion.identity);
            
            _aliveCount++;
            OnEnemySpawned?.Invoke(enemy);
            
            // Listen for death (assumes enemy has some death notification)
            // You might need to adapt this to your enemy system
        }
        
        private Vector3 GetSpawnPosition()
        {
            if (_spawnArea != null)
            {
                return _spawnArea.GetRandomPosition();
            }
            
            if (_spawnPoints != null && _spawnPoints.Length > 0)
            {
                return _spawnPoints[Random.Range(0, _spawnPoints.Length)].Position;
            }
            
            return transform.position;
        }
        
        /// <summary>
        /// Call this when an enemy dies.
        /// </summary>
        public void OnEnemyKilled(GameObject enemy)
        {
            _aliveCount = Mathf.Max(0, _aliveCount - 1);
            OnEnemyDied?.Invoke(enemy);
        }
    }
    
    [Serializable]
    public class WaveData
    {
        public string waveName;
        public WaveEntry[] entries;
        public bool waitForClear = true;
    }
    
    [Serializable]
    public class WaveEntry
    {
        public GameObject prefab;
        public int count = 1;
    }
}
