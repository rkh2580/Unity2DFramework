using UnityEngine;

namespace KH.Framework2D.Events
{
    /// <summary>
    /// Event channel with int parameter.
    /// Use for: score change, damage dealt, level up, etc.
    /// </summary>
    [CreateAssetMenu(menuName = "Events/Int Event Channel", fileName = "IntEventChannel")]
    public class IntEventChannel : EventChannel<int> { }
    
    /// <summary>
    /// Event channel with float parameter.
    /// Use for: health percentage, timer, progress, etc.
    /// </summary>
    [CreateAssetMenu(menuName = "Events/Float Event Channel", fileName = "FloatEventChannel")]
    public class FloatEventChannel : EventChannel<float> { }
    
    /// <summary>
    /// Event channel with string parameter.
    /// Use for: messages, notifications, etc.
    /// </summary>
    [CreateAssetMenu(menuName = "Events/String Event Channel", fileName = "StringEventChannel")]
    public class StringEventChannel : EventChannel<string> { }
    
    /// <summary>
    /// Event channel with bool parameter.
    /// Use for: toggle states, enable/disable, etc.
    /// </summary>
    [CreateAssetMenu(menuName = "Events/Bool Event Channel", fileName = "BoolEventChannel")]
    public class BoolEventChannel : EventChannel<bool> { }
    
    /// <summary>
    /// Event channel with Vector2 parameter.
    /// Use for: movement input, screen position, etc.
    /// </summary>
    [CreateAssetMenu(menuName = "Events/Vector2 Event Channel", fileName = "Vector2EventChannel")]
    public class Vector2EventChannel : EventChannel<Vector2> { }
    
    /// <summary>
    /// Event channel with Vector3 parameter.
    /// Use for: world position, direction, etc.
    /// </summary>
    [CreateAssetMenu(menuName = "Events/Vector3 Event Channel", fileName = "Vector3EventChannel")]
    public class Vector3EventChannel : EventChannel<Vector3> { }
    
    /// <summary>
    /// Event channel with GameObject parameter.
    /// Use for: spawned objects, selected targets, etc.
    /// </summary>
    [CreateAssetMenu(menuName = "Events/GameObject Event Channel", fileName = "GameObjectEventChannel")]
    public class GameObjectEventChannel : EventChannel<GameObject> { }
    
    /// <summary>
    /// Event channel with Transform parameter.
    /// Use for: spawn points, targets, etc.
    /// </summary>
    [CreateAssetMenu(menuName = "Events/Transform Event Channel", fileName = "TransformEventChannel")]
    public class TransformEventChannel : EventChannel<Transform> { }
}
