using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using KH.Framework2D.UI;

namespace KH.Framework2D.Utils
{
    // Define은 KH.Framework2D 네임스페이스에 있음
    using Define = KH.Framework2D.Define;
{
    /// <summary>
    /// Extension methods for Unity types.
    /// </summary>
    public static class Extensions
    {
        #region Transform
        
        /// <summary>
        /// Set only the X position.
        /// </summary>
        public static void SetX(this Transform transform, float x)
        {
            var pos = transform.position;
            pos.x = x;
            transform.position = pos;
        }
        
        /// <summary>
        /// Set only the Y position.
        /// </summary>
        public static void SetY(this Transform transform, float y)
        {
            var pos = transform.position;
            pos.y = y;
            transform.position = pos;
        }
        
        /// <summary>
        /// Set only the Z position.
        /// </summary>
        public static void SetZ(this Transform transform, float z)
        {
            var pos = transform.position;
            pos.z = z;
            transform.position = pos;
        }
        
        /// <summary>
        /// Set local X position.
        /// </summary>
        public static void SetLocalX(this Transform transform, float x)
        {
            var pos = transform.localPosition;
            pos.x = x;
            transform.localPosition = pos;
        }
        
        /// <summary>
        /// Set local Y position.
        /// </summary>
        public static void SetLocalY(this Transform transform, float y)
        {
            var pos = transform.localPosition;
            pos.y = y;
            transform.localPosition = pos;
        }
        
        /// <summary>
        /// Destroy all children.
        /// </summary>
        public static void DestroyChildren(this Transform transform)
        {
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                UnityEngine.Object.Destroy(transform.GetChild(i).gameObject);
            }
        }
        
        /// <summary>
        /// Destroy all children immediately (for editor use).
        /// </summary>
        public static void DestroyChildrenImmediate(this Transform transform)
        {
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                UnityEngine.Object.DestroyImmediate(transform.GetChild(i).gameObject);
            }
        }
        
        /// <summary>
        /// Reset transform to default values.
        /// </summary>
        public static void Reset(this Transform transform)
        {
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one;
        }
        
        /// <summary>
        /// Look at target in 2D (Z rotation).
        /// </summary>
        public static void LookAt2D(this Transform transform, Vector3 target)
        {
            Vector3 diff = target - transform.position;
            float angle = Mathf.Atan2(diff.y, diff.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0, 0, angle);
        }
        
        /// <summary>
        /// Look at target in 2D with offset angle.
        /// </summary>
        public static void LookAt2D(this Transform transform, Vector3 target, float offsetAngle)
        {
            Vector3 diff = target - transform.position;
            float angle = Mathf.Atan2(diff.y, diff.x) * Mathf.Rad2Deg + offsetAngle;
            transform.rotation = Quaternion.Euler(0, 0, angle);
        }
        
        /// <summary>
        /// Get 2D direction towards target.
        /// </summary>
        public static Vector2 DirectionTo2D(this Transform transform, Transform target)
        {
            return ((Vector2)target.position - (Vector2)transform.position).normalized;
        }
        
        /// <summary>
        /// Get 2D distance to target.
        /// </summary>
        public static float DistanceTo2D(this Transform transform, Transform target)
        {
            return Vector2.Distance(transform.position, target.position);
        }
        
        #endregion
        
        #region Vector
        
        /// <summary>
        /// Return a copy with modified X.
        /// </summary>
        public static Vector3 WithX(this Vector3 v, float x) => new Vector3(x, v.y, v.z);
        
        /// <summary>
        /// Return a copy with modified Y.
        /// </summary>
        public static Vector3 WithY(this Vector3 v, float y) => new Vector3(v.x, y, v.z);
        
        /// <summary>
        /// Return a copy with modified Z.
        /// </summary>
        public static Vector3 WithZ(this Vector3 v, float z) => new Vector3(v.x, v.y, z);
        
        /// <summary>
        /// Return a copy with modified X.
        /// </summary>
        public static Vector2 WithX(this Vector2 v, float x) => new Vector2(x, v.y);
        
        /// <summary>
        /// Return a copy with modified Y.
        /// </summary>
        public static Vector2 WithY(this Vector2 v, float y) => new Vector2(v.x, y);
        
        /// <summary>
        /// Convert Vector3 to Vector2 (XY).
        /// </summary>
        public static Vector2 ToVector2(this Vector3 v) => new Vector2(v.x, v.y);
        
        /// <summary>
        /// Convert Vector2 to Vector3 with specified Z.
        /// </summary>
        public static Vector3 ToVector3(this Vector2 v, float z = 0) => new Vector3(v.x, v.y, z);
        
        /// <summary>
        /// Get flat (XZ) distance ignoring Y.
        /// </summary>
        public static float FlatDistance(this Vector3 a, Vector3 b)
        {
            var dx = a.x - b.x;
            var dz = a.z - b.z;
            return Mathf.Sqrt(dx * dx + dz * dz);
        }
        
        /// <summary>
        /// Rotate a Vector2 by angle in degrees.
        /// </summary>
        public static Vector2 Rotate(this Vector2 v, float degrees)
        {
            float radians = degrees * Mathf.Deg2Rad;
            float sin = Mathf.Sin(radians);
            float cos = Mathf.Cos(radians);
            return new Vector2(v.x * cos - v.y * sin, v.x * sin + v.y * cos);
        }
        
        /// <summary>
        /// Get perpendicular vector (rotated 90 degrees).
        /// </summary>
        public static Vector2 Perpendicular(this Vector2 v) => new Vector2(-v.y, v.x);
        
        /// <summary>
        /// Get angle in degrees from Vector2 direction.
        /// </summary>
        public static float ToAngle(this Vector2 v) => Mathf.Atan2(v.y, v.x) * Mathf.Rad2Deg;
        
        /// <summary>
        /// Get a random point within a radius.
        /// </summary>
        public static Vector2 RandomInRadius(this Vector2 center, float radius)
        {
            return center + UnityEngine.Random.insideUnitCircle * radius;
        }
        
        /// <summary>
        /// Get a random point on a radius (circle edge).
        /// </summary>
        public static Vector2 RandomOnRadius(this Vector2 center, float radius)
        {
            return center + UnityEngine.Random.insideUnitCircle.normalized * radius;
        }
        
        /// <summary>
        /// Clamp vector magnitude.
        /// </summary>
        public static Vector2 ClampMagnitude(this Vector2 v, float maxLength)
        {
            return Vector2.ClampMagnitude(v, maxLength);
        }
        
        #endregion
        
        #region Color
        
        /// <summary>
        /// Return a copy with modified alpha.
        /// </summary>
        public static Color WithAlpha(this Color c, float alpha) => new Color(c.r, c.g, c.b, alpha);
        
        /// <summary>
        /// Convert to hex string.
        /// </summary>
        public static string ToHex(this Color c) => ColorUtility.ToHtmlStringRGBA(c);
        
        /// <summary>
        /// Invert color (preserves alpha).
        /// </summary>
        public static Color Invert(this Color c) => new Color(1f - c.r, 1f - c.g, 1f - c.b, c.a);
        
        #endregion
        
        #region Collections
        
        /// <summary>
        /// Get a random element from the list.
        /// </summary>
        public static T GetRandom<T>(this IList<T> list)
        {
            if (list == null || list.Count == 0)
                return default;
            return list[UnityEngine.Random.Range(0, list.Count)];
        }
        
        /// <summary>
        /// Get multiple random elements from the list.
        /// </summary>
        public static List<T> GetRandom<T>(this IList<T> list, int count)
        {
            var result = new List<T>();
            var copy = new List<T>(list);
            copy.Shuffle();
            
            for (int i = 0; i < Mathf.Min(count, copy.Count); i++)
            {
                result.Add(copy[i]);
            }
            
            return result;
        }
        
        /// <summary>
        /// Shuffle the list in place (Fisher-Yates).
        /// </summary>
        public static void Shuffle<T>(this IList<T> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = UnityEngine.Random.Range(0, i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }
        
        /// <summary>
        /// Check if list is null or empty.
        /// </summary>
        public static bool IsNullOrEmpty<T>(this IList<T> list)
        {
            return list == null || list.Count == 0;
        }
        
        /// <summary>
        /// Get element at index or default if out of bounds.
        /// </summary>
        public static T GetOrDefault<T>(this IList<T> list, int index, T defaultValue = default)
        {
            if (list == null || index < 0 || index >= list.Count)
                return defaultValue;
            return list[index];
        }
        
        /// <summary>
        /// Find the element with minimum value.
        /// </summary>
        public static T MinBy<T, TKey>(this IEnumerable<T> source, Func<T, TKey> selector) 
            where TKey : IComparable<TKey>
        {
            using var enumerator = source.GetEnumerator();
            if (!enumerator.MoveNext())
                throw new InvalidOperationException("Sequence is empty");
            
            T minElement = enumerator.Current;
            TKey minKey = selector(minElement);
            
            while (enumerator.MoveNext())
            {
                TKey key = selector(enumerator.Current);
                if (key.CompareTo(minKey) < 0)
                {
                    minKey = key;
                    minElement = enumerator.Current;
                }
            }
            
            return minElement;
        }
        
        /// <summary>
        /// Find the element with maximum value.
        /// </summary>
        public static T MaxBy<T, TKey>(this IEnumerable<T> source, Func<T, TKey> selector) 
            where TKey : IComparable<TKey>
        {
            using var enumerator = source.GetEnumerator();
            if (!enumerator.MoveNext())
                throw new InvalidOperationException("Sequence is empty");
            
            T maxElement = enumerator.Current;
            TKey maxKey = selector(maxElement);
            
            while (enumerator.MoveNext())
            {
                TKey key = selector(enumerator.Current);
                if (key.CompareTo(maxKey) > 0)
                {
                    maxKey = key;
                    maxElement = enumerator.Current;
                }
            }
            
            return maxElement;
        }
        
        #endregion
        
        #region Component
        
        /// <summary>
        /// Get or add a component.
        /// </summary>
        public static T GetOrAddComponent<T>(this GameObject go) where T : Component
        {
            var component = go.GetComponent<T>();
            return component != null ? component : go.AddComponent<T>();
        }
        
        /// <summary>
        /// Get or add a component.
        /// </summary>
        public static T GetOrAddComponent<T>(this Component c) where T : Component
        {
            return c.gameObject.GetOrAddComponent<T>();
        }
        
        /// <summary>
        /// Try to get component, returns false if not found.
        /// </summary>
        public static bool TryGetComponent<T>(this GameObject go, out T component) where T : Component
        {
            component = go.GetComponent<T>();
            return component != null;
        }
        
        /// <summary>
        /// Check if GameObject has component.
        /// </summary>
        public static bool HasComponent<T>(this GameObject go) where T : Component
        {
            return go.GetComponent<T>() != null;
        }
        
        #endregion
        
        #region String
        
        /// <summary>
        /// Check if string is null or whitespace.
        /// </summary>
        public static bool IsNullOrEmpty(this string s)
        {
            return string.IsNullOrWhiteSpace(s);
        }
        
        /// <summary>
        /// Truncate string to max length with ellipsis.
        /// </summary>
        public static string Truncate(this string s, int maxLength, string suffix = "...")
        {
            if (string.IsNullOrEmpty(s) || s.Length <= maxLength)
                return s;
            return s.Substring(0, maxLength - suffix.Length) + suffix;
        }
        
        #endregion
        
        #region LayerMask
        
        /// <summary>
        /// Check if layer is in mask.
        /// </summary>
        public static bool Contains(this LayerMask mask, int layer)
        {
            return (mask.value & (1 << layer)) != 0;
        }
        
        #endregion
        
        #region RectTransform
        
        /// <summary>
        /// Set anchor position.
        /// </summary>
        public static void SetAnchoredX(this RectTransform rect, float x)
        {
            var pos = rect.anchoredPosition;
            pos.x = x;
            rect.anchoredPosition = pos;
        }
        
        /// <summary>
        /// Set anchor position Y.
        /// </summary>
        public static void SetAnchoredY(this RectTransform rect, float y)
        {
            var pos = rect.anchoredPosition;
            pos.y = y;
            rect.anchoredPosition = pos;
        }
        
        /// <summary>
        /// Set size delta width.
        /// </summary>
        public static void SetWidth(this RectTransform rect, float width)
        {
            var size = rect.sizeDelta;
            size.x = width;
            rect.sizeDelta = size;
        }
        
        /// <summary>
        /// Set size delta height.
        /// </summary>
        public static void SetHeight(this RectTransform rect, float height)
        {
            var size = rect.sizeDelta;
            size.y = height;
            rect.sizeDelta = size;
        }
        
        #endregion
        
        #region SpriteRenderer
        
        /// <summary>
        /// Set sprite alpha.
        /// </summary>
        public static void SetAlpha(this SpriteRenderer sr, float alpha)
        {
            var color = sr.color;
            color.a = alpha;
            sr.color = color;
        }
        
        /// <summary>
        /// Fade sprite to target alpha.
        /// </summary>
        public static void SetColor(this SpriteRenderer sr, Color color, bool preserveAlpha = false)
        {
            if (preserveAlpha)
            {
                color.a = sr.color.a;
            }
            sr.color = color;
        }
        
        #endregion
        
        #region Rigidbody2D
        
        /// <summary>
        /// Set velocity X only.
        /// </summary>
        public static void SetVelocityX(this Rigidbody2D rb, float x)
        {
            rb.linearVelocity = new Vector2(x, rb.linearVelocity.y);
        }
        
        /// <summary>
        /// Set velocity Y only.
        /// </summary>
        public static void SetVelocityY(this Rigidbody2D rb, float y)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, y);
        }
        
        #endregion
        
        #region Number Formatting
        
        /// <summary>
        /// Format large number with suffix (K, M, B).
        /// </summary>
        public static string ToShortString(this int num)
        {
            if (num >= 1000000000) return (num / 1000000000f).ToString("0.#") + "B";
            if (num >= 1000000) return (num / 1000000f).ToString("0.#") + "M";
            if (num >= 1000) return (num / 1000f).ToString("0.#") + "K";
            return num.ToString();
        }
        
        /// <summary>
        /// Format time in seconds to MM:SS.
        /// </summary>
        public static string ToTimeString(this float seconds)
        {
            int mins = (int)(seconds / 60);
            int secs = (int)(seconds % 60);
            return $"{mins:D2}:{secs:D2}";
        }
        
        /// <summary>
        /// Format time in seconds to HH:MM:SS.
        /// </summary>
        public static string ToLongTimeString(this float seconds)
        {
            int hours = (int)(seconds / 3600);
            int mins = (int)((seconds % 3600) / 60);
            int secs = (int)(seconds % 60);
            return $"{hours:D2}:{mins:D2}:{secs:D2}";
        }
        
        #endregion
        
        #region Transform - Additional Methods (Phase 4 Enhancement)
        
        /// <summary>
        /// Execute action for each direct child.
        /// </summary>
        public static void ForEachChild(this Transform transform, Action<Transform> action)
        {
            if (transform == null || action == null) return;
            
            for (int i = 0; i < transform.childCount; i++)
            {
                action(transform.GetChild(i));
            }
        }
        
        /// <summary>
        /// Execute action for each descendant (recursive).
        /// </summary>
        public static void ForEachDescendant(this Transform transform, Action<Transform> action)
        {
            if (transform == null || action == null) return;
            
            foreach (Transform child in transform.GetComponentsInChildren<Transform>(true))
            {
                if (child != transform)
                    action(child);
            }
        }
        
        /// <summary>
        /// Find child matching predicate.
        /// </summary>
        public static Transform FindChildWhere(this Transform transform, Func<Transform, bool> predicate)
        {
            if (transform == null || predicate == null) return null;
            
            for (int i = 0; i < transform.childCount; i++)
            {
                var child = transform.GetChild(i);
                if (predicate(child))
                    return child;
            }
            return null;
        }
        
        #endregion
        
        #region GameObject - Safe Methods
        
        /// <summary>
        /// Null-safe SetActive.
        /// </summary>
        public static void SafeSetActive(this GameObject go, bool active)
        {
            if (go != null)
                go.SetActive(active);
        }
        
        #endregion
        
        #region UI Event Binding (Phase 4 Enhancement)
        
        /// <summary>
        /// Bind UI event to GameObject.
        /// </summary>
        public static void BindEvent(
            this GameObject go,
            Action<UnityEngine.EventSystems.PointerEventData> action,
            Define.UIEvent eventType = Define.UIEvent.Click)
        {
            KH.Framework2D.UI.UI_Base.BindEvent(go, action, eventType);
        }
        
        /// <summary>
        /// Unbind UI event from GameObject.
        /// </summary>
        public static void UnbindEvent(
            this GameObject go,
            Action<UnityEngine.EventSystems.PointerEventData> action,
            Define.UIEvent eventType = Define.UIEvent.Click)
        {
            KH.Framework2D.UI.UI_Base.UnbindEvent(go, action, eventType);
        }
        
        /// <summary>
        /// Bind simple click event (no PointerEventData).
        /// </summary>
        public static void BindClick(this GameObject go, Action onClick)
        {
            KH.Framework2D.UI.UI_Base.BindClick(go, onClick);
        }
        
        #endregion
        
        #region Action - Safe Invoke
        
        /// <summary>
        /// Null-safe Invoke.
        /// </summary>
        public static void SafeInvoke(this Action action)
        {
            action?.Invoke();
        }
        
        /// <summary>
        /// Null-safe Invoke with one parameter.
        /// </summary>
        public static void SafeInvoke<T>(this Action<T> action, T arg)
        {
            action?.Invoke(arg);
        }
        
        /// <summary>
        /// Null-safe Invoke with two parameters.
        /// </summary>
        public static void SafeInvoke<T1, T2>(this Action<T1, T2> action, T1 arg1, T2 arg2)
        {
            action?.Invoke(arg1, arg2);
        }
        
        /// <summary>
        /// Null-safe Invoke with three parameters.
        /// </summary>
        public static void SafeInvoke<T1, T2, T3>(this Action<T1, T2, T3> action, T1 arg1, T2 arg2, T3 arg3)
        {
            action?.Invoke(arg1, arg2, arg3);
        }
        
        #endregion
        
        #region Enum Extensions
        
        /// <summary>
        /// Get the count of values in an enum.
        /// </summary>
        public static int GetCount<T>() where T : Enum
        {
            return Enum.GetValues(typeof(T)).Length;
        }
        
        /// <summary>
        /// Parse string to enum safely.
        /// </summary>
        public static T ToEnum<T>(this string str, T defaultValue = default) where T : struct, Enum
        {
            if (Enum.TryParse<T>(str, true, out var result))
                return result;
            return defaultValue;
        }
        
        /// <summary>
        /// Get next enum value (wraps around).
        /// </summary>
        public static T Next<T>(this T src) where T : struct, Enum
        {
            var values = (T[])Enum.GetValues(typeof(T));
            int idx = Array.IndexOf(values, src);
            return values[(idx + 1) % values.Length];
        }
        
        /// <summary>
        /// Get previous enum value (wraps around).
        /// </summary>
        public static T Previous<T>(this T src) where T : struct, Enum
        {
            var values = (T[])Enum.GetValues(typeof(T));
            int idx = Array.IndexOf(values, src);
            return values[(idx - 1 + values.Length) % values.Length];
        }
        
        #endregion
        
        #region Nullable Extensions
        
        /// <summary>
        /// Get value or default for nullable types.
        /// </summary>
        public static T OrDefault<T>(this T? nullable, T defaultValue = default) where T : struct
        {
            return nullable ?? defaultValue;
        }
        
        #endregion
    }
}
