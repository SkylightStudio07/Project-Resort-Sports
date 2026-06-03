using System.Collections.Generic;
using UnityEngine;

namespace ResortSports.Jogging
{
    /// <summary>
    /// 조깅 코스를 정의하는 컴포넌트. 인스펙터/씬뷰 핸들로 자유롭게 편집 가능.
    /// 내부적으로 폴리라인 + Catmull-Rom 보간으로 매끈한 경로를 제공한다.
    /// </summary>
    [ExecuteAlways]
    public class JogTrack : MonoBehaviour
    {
        [System.Serializable]
        public class Waypoint
        {
            [Tooltip("이 트랙의 로컬 좌표")]
            public Vector3 localPosition;

            [Tooltip("선택: 도달 시 표시할 라벨(체크포인트 이름 등)")]
            public string label;
        }

        [Header("Course")]
        [Tooltip("코스를 구성하는 웨이포인트 목록 (로컬 좌표)")]
        public List<Waypoint> waypoints = new List<Waypoint>
        {
            new Waypoint { localPosition = new Vector3(0f, 0f, 0f) },
            new Waypoint { localPosition = new Vector3(0f, 0f, 10f) },
            new Waypoint { localPosition = new Vector3(10f, 0f, 20f) },
        };

        [Tooltip("코스를 폐곡선(루프)으로 처리할지 여부")]
        public bool loop = false;

        [Tooltip("Catmull-Rom 스플라인 보간 사용 (해제 시 직선 폴리라인)")]
        public bool smooth = true;

        [Tooltip("내부 캐시 샘플 개수 (높을수록 부드럽지만 비용 증가)")]
        [Range(8, 512)] public int sampleCount = 128;

        [Header("Gizmos")]
        public Color trackColor = new Color(0.2f, 1f, 0.4f, 1f);
        public Color waypointColor = new Color(1f, 0.7f, 0.1f, 1f);
        public float waypointGizmoSize = 0.35f;

        private readonly List<Vector3> _samples = new List<Vector3>();
        private readonly List<float> _cumLength = new List<float>();
        private float _totalLength;
        private int _cachedHash;

        /// <summary>코스 전체 길이(월드 단위)</summary>
        public float TotalLength
        {
            get { EnsureCache(); return _totalLength; }
        }

        public int WaypointCount => waypoints != null ? waypoints.Count : 0;

        /// <summary>distance(미터)에 해당하는 월드 위치를 돌려준다. loop=false면 끝에서 클램프.</summary>
        public Vector3 GetPositionAtDistance(float distance)
        {
            EnsureCache();
            if (_samples.Count == 0) return transform.position;
            if (_totalLength <= 0f) return _samples[0];

            distance = loop
                ? Mathf.Repeat(distance, _totalLength)
                : Mathf.Clamp(distance, 0f, _totalLength);

            // 이진 탐색으로 구간 찾기
            int lo = 0, hi = _cumLength.Count - 1;
            while (lo < hi)
            {
                int mid = (lo + hi) >> 1;
                if (_cumLength[mid] < distance) lo = mid + 1;
                else hi = mid;
            }
            int i1 = Mathf.Max(1, lo);
            int i0 = i1 - 1;
            float segLen = _cumLength[i1] - _cumLength[i0];
            float t = segLen > 0f ? (distance - _cumLength[i0]) / segLen : 0f;
            return Vector3.Lerp(_samples[i0], _samples[i1], t);
        }

        /// <summary>distance에서 진행 방향(정규화).</summary>
        public Vector3 GetDirectionAtDistance(float distance)
        {
            const float delta = 0.1f;
            Vector3 a = GetPositionAtDistance(distance);
            Vector3 b = GetPositionAtDistance(distance + delta);
            Vector3 d = b - a;
            return d.sqrMagnitude < 1e-6f ? transform.forward : d.normalized;
        }

        public void InvalidateCache() => _cachedHash = 0;

        private void EnsureCache()
        {
            int h = ComputeHash();
            if (h == _cachedHash && _samples.Count > 0) return;
            RebuildSamples();
            _cachedHash = h;
        }

        private int ComputeHash()
        {
            unchecked
            {
                int h = 17;
                h = h * 31 + (loop ? 1 : 0);
                h = h * 31 + (smooth ? 1 : 0);
                h = h * 31 + sampleCount;
                if (waypoints != null)
                    foreach (var w in waypoints)
                        h = h * 31 + (w?.localPosition.GetHashCode() ?? 0);
                h = h * 31 + transform.localToWorldMatrix.GetHashCode();
                return h;
            }
        }

        private void RebuildSamples()
        {
            _samples.Clear();
            _cumLength.Clear();
            _totalLength = 0f;
            if (waypoints == null || waypoints.Count < 2) return;

            int segCount = loop ? waypoints.Count : waypoints.Count - 1;
            int perSeg = Mathf.Max(2, sampleCount / Mathf.Max(1, segCount));

            for (int s = 0; s < segCount; s++)
            {
                Vector3 p0 = WorldPoint(s - 1);
                Vector3 p1 = WorldPoint(s);
                Vector3 p2 = WorldPoint(s + 1);
                Vector3 p3 = WorldPoint(s + 2);

                int steps = (s == segCount - 1 && !loop) ? perSeg : perSeg;
                for (int i = 0; i < steps; i++)
                {
                    float t = i / (float)perSeg;
                    Vector3 pt = smooth ? CatmullRom(p0, p1, p2, p3, t) : Vector3.Lerp(p1, p2, t);
                    AppendSample(pt);
                }
            }
            // 마지막 점
            AppendSample(loop ? WorldPoint(0) : WorldPoint(waypoints.Count - 1));
        }

        private void AppendSample(Vector3 p)
        {
            if (_samples.Count == 0)
            {
                _samples.Add(p);
                _cumLength.Add(0f);
                return;
            }
            float d = Vector3.Distance(_samples[_samples.Count - 1], p);
            if (d < 1e-4f) return;
            _totalLength += d;
            _samples.Add(p);
            _cumLength.Add(_totalLength);
        }

        private Vector3 WorldPoint(int index)
        {
            int n = waypoints.Count;
            int i = loop ? ((index % n) + n) % n : Mathf.Clamp(index, 0, n - 1);
            return transform.TransformPoint(waypoints[i].localPosition);
        }

        private static Vector3 CatmullRom(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
        {
            float t2 = t * t;
            float t3 = t2 * t;
            return 0.5f * (
                (2f * p1) +
                (-p0 + p2) * t +
                (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2 +
                (-p0 + 3f * p1 - 3f * p2 + p3) * t3
            );
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            EnsureCache();
            if (_samples.Count < 2) return;

            Gizmos.color = trackColor;
            for (int i = 0; i < _samples.Count - 1; i++)
                Gizmos.DrawLine(_samples[i], _samples[i + 1]);
            if (loop)
                Gizmos.DrawLine(_samples[_samples.Count - 1], _samples[0]);

            Gizmos.color = waypointColor;
            foreach (var w in waypoints)
            {
                Vector3 wp = transform.TransformPoint(w.localPosition);
                Gizmos.DrawSphere(wp, waypointGizmoSize);
            }
        }
#endif
    }
}
