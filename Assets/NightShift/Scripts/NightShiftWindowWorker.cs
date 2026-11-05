using UnityEngine;

namespace NightShift
{
    internal sealed class NightShiftWindowWorker : MonoBehaviour
    {
        private MeshRenderer worker;
        private MeshFilter meshFilter;
        private Mesh[] frames;
        private float phase;

        internal void Initialize(MeshRenderer renderer, Mesh[] gaitFrames, float offset)
        {
            worker = renderer;
            meshFilter = renderer.GetComponent<MeshFilter>();
            frames = gaitFrames;
            phase = offset;
            SetPose();
        }

        private void LateUpdate()
        {
            if (worker.isVisible)
                SetPose();
        }

        private void SetPose()
        {
            float time = Time.time + phase;
            float path = Mathf.Repeat(time * .43f, 2f);
            float direction = path < 1f ? 1f : -1f;
            transform.localPosition = Vector3.right * Mathf.Lerp(-.245f, .245f, 1f - Mathf.Abs(path - 1f));
            transform.localScale = new Vector3(direction, 1f, 1f);
            int frame = Mathf.FloorToInt(time * 10f) % frames.Length;
            if (meshFilter.sharedMesh != frames[frame])
                meshFilter.sharedMesh = frames[frame];
        }
    }
}
