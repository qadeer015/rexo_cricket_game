using UnityEngine;

namespace CricketBlitz.World
{
    public enum CamMode
    {
        BehindBatsman = 0,
        BehindBowler = 1,
        FollowBall = 2,
        Sky = 3
    }

    public sealed class MatchCameraDirector : MonoBehaviour
    {
        public CamMode Mode = CamMode.BehindBatsman;
        public Transform Ball;
        public float Shake;

        Vector3 desired;
        Quaternion desiredRotation;

        public static MatchCameraDirector Attach(Camera camera)
        {
            MatchCameraDirector director = camera.GetComponent<MatchCameraDirector>();
            if (director == null)
            {
                director = camera.gameObject.AddComponent<MatchCameraDirector>();
            }

            camera.nearClipPlane = 0.05f;
            camera.farClipPlane = 250f;
            camera.fieldOfView = 55f;
            return director;
        }

        public void Cycle()
        {
            Mode = Mode == CamMode.BehindBatsman ? CamMode.BehindBowler : CamMode.BehindBatsman;
        }

        public void SetBackground(Color color)
        {
            Camera camera = GetComponent<Camera>();
            if (camera == null)
            {
                return;
            }

            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = color;
        }

        void LateUpdate()
        {
            Vector3 ball = Ball != null ? Ball.position : new Vector3(0f, 1f, 9f);
            switch (Mode)
            {
                case CamMode.BehindBowler:
                    desired = new Vector3(0f, 2.6f, -18.5f);
                    desiredRotation = Quaternion.LookRotation(new Vector3(0f, 0.4f, 8f) - desired, Vector3.up);
                    break;
                case CamMode.FollowBall:
                    desired = ball + new Vector3(0f, 3.2f, 6.5f);
                    desiredRotation = Quaternion.LookRotation(ball - desired, Vector3.up);
                    break;
                case CamMode.Sky:
                    desired = ball + new Vector3(4f, 10f, 4f);
                    desiredRotation = Quaternion.LookRotation(ball - desired, Vector3.up);
                    break;
                default:
                    desired = new Vector3(0f, 2.35f, 16.8f);
                    desiredRotation = Quaternion.LookRotation(new Vector3(0f, 0.6f, 4f) - desired, Vector3.up);
                    break;
            }

            float follow = Mode == CamMode.FollowBall || Mode == CamMode.Sky ? 8f : 4f;
            transform.position = Vector3.Lerp(transform.position, desired, 1f - Mathf.Exp(-follow * Time.deltaTime));
            transform.rotation = Quaternion.Slerp(transform.rotation, desiredRotation, 1f - Mathf.Exp(-follow * Time.deltaTime));
            if (Shake > 0f)
            {
                Shake = Mathf.MoveTowards(Shake, 0f, Time.deltaTime);
                transform.position += Random.insideUnitSphere * Shake * 0.15f;
            }
        }
    }
}
