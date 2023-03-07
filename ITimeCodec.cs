using UnityEngine;
using System.Collections;

/*
    Time codec with color block overlay for time syncing of video live stream 
*/
namespace VideoPlayer
{
    public abstract class ITimeCodec : MonoBehaviour
    {
        public enum Type
        {
            Grid, Liner
        }
        public abstract void SetSource(Texture2D txd);
        public abstract void ReleaseSource();
        public abstract void SetTimeCodicWindows(Vector2 Origin, Vector2 Size);
        public abstract void SetInitStartupTime(long InitTime);
        public abstract void GetTimeFromFrame(ref System.Func<long> _FetchAction);
        public abstract void ForceRenderNow();
        public abstract IEnumerator HaltForTask(WaitUntil Task);

        protected IVideoPlayer MainPlayer;
    }

}