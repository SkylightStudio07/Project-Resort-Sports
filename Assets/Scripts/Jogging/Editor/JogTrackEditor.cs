using UnityEditor;
using UnityEngine;

namespace ResortSports.Jogging.EditorTools
{
    [CustomEditor(typeof(JogTrack))]
    public class JogTrackEditor : Editor
    {
        private int _selected = -1;

        public override void OnInspectorGUI()
        {
            var track = (JogTrack)target;

            DrawDefaultInspector();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Course Tools", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                $"길이: {track.TotalLength:0.0} m  |  웨이포인트: {track.WaypointCount}\n" +
                "씬뷰에서 핸들을 드래그해 이동, Shift+클릭으로 추가, X키로 선택 삭제.",
                MessageType.Info);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("끝에 추가"))
                {
                    Undo.RecordObject(track, "Append Waypoint");
                    Vector3 last = track.WaypointCount > 0
                        ? track.waypoints[track.WaypointCount - 1].localPosition
                        : Vector3.zero;
                    track.waypoints.Add(new JogTrack.Waypoint
                    {
                        localPosition = last + Vector3.forward * 5f
                    });
                    track.InvalidateCache();
                    EditorUtility.SetDirty(track);
                }
                if (GUILayout.Button("선택 삭제") && _selected >= 0 && _selected < track.WaypointCount)
                {
                    Undo.RecordObject(track, "Remove Waypoint");
                    track.waypoints.RemoveAt(_selected);
                    _selected = -1;
                    track.InvalidateCache();
                    EditorUtility.SetDirty(track);
                }
                if (GUILayout.Button("뒤집기"))
                {
                    Undo.RecordObject(track, "Reverse Track");
                    track.waypoints.Reverse();
                    track.InvalidateCache();
                    EditorUtility.SetDirty(track);
                }
            }
        }

        private void OnSceneGUI()
        {
            var track = (JogTrack)target;
            if (track.waypoints == null) return;

            Event e = Event.current;

            // Shift+클릭으로 새 웨이포인트 추가
            if (e.shift && e.type == EventType.MouseDown && e.button == 0)
            {
                Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);
                Plane plane = new Plane(Vector3.up, track.transform.position);
                if (plane.Raycast(ray, out float dist))
                {
                    Vector3 world = ray.GetPoint(dist);
                    Vector3 local = track.transform.InverseTransformPoint(world);
                    Undo.RecordObject(track, "Add Waypoint");
                    track.waypoints.Add(new JogTrack.Waypoint { localPosition = local });
                    _selected = track.waypoints.Count - 1;
                    track.InvalidateCache();
                    EditorUtility.SetDirty(track);
                    e.Use();
                }
            }

            // 선택된 웨이포인트 삭제 (X 키)
            if (e.type == EventType.KeyDown && e.keyCode == KeyCode.X
                && _selected >= 0 && _selected < track.waypoints.Count)
            {
                Undo.RecordObject(track, "Remove Waypoint");
                track.waypoints.RemoveAt(_selected);
                _selected = -1;
                track.InvalidateCache();
                EditorUtility.SetDirty(track);
                e.Use();
            }

            // 각 웨이포인트 핸들
            for (int i = 0; i < track.waypoints.Count; i++)
            {
                var w = track.waypoints[i];
                Vector3 world = track.transform.TransformPoint(w.localPosition);
                float size = HandleUtility.GetHandleSize(world) * 0.15f;

                Handles.color = (i == _selected) ? Color.cyan : track.waypointColor;
                if (Handles.Button(world, Quaternion.identity, size, size * 1.2f, Handles.SphereHandleCap))
                {
                    _selected = i;
                    Repaint();
                }

                if (i == _selected)
                {
                    EditorGUI.BeginChangeCheck();
                    Vector3 newWorld = Handles.PositionHandle(world, Quaternion.identity);
                    if (EditorGUI.EndChangeCheck())
                    {
                        Undo.RecordObject(track, "Move Waypoint");
                        w.localPosition = track.transform.InverseTransformPoint(newWorld);
                        track.InvalidateCache();
                        EditorUtility.SetDirty(track);
                    }
                }

                Handles.Label(world + Vector3.up * 0.4f,
                    string.IsNullOrEmpty(w.label) ? $"#{i}" : $"#{i} {w.label}");
            }
        }
    }
}
