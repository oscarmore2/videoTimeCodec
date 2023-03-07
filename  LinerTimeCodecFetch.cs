using UnityEngine;
using System.Collections;
using System;

namespace VideoPlayer
{
    //[RequireComponent(typeof(Camera))]
    public class LinerTimeCodecFetch : ITimeCodec
    {
        protected static readonly DateTime UnixTime = new DateTime(1970, 1, 1, 0, 0, 0, 0);
        [SerializeField]
        private RenderTexture Target;

        private bool ForbinFetch;
        
        static long number = -1;
        long timeCodeResult = 0;
        public long TimeCodeResult
        {
            get {
                if (number != -1)
                    timeCodeResult = number;
                return timeCodeResult;
            }
        }

        TimeSpan StartupTime = TimeSpan.MinValue;

        Renderer TargetRender;
        Material TargetMaterial;
        [SerializeField]
        Texture2D SourceTexture;
        [SerializeField]
        Mesh TargetMesh;

        [SerializeField]
        Rect ReadArea = Rect.zero;

        Vector2[] Positions;

        [SerializeField]
        public static int[] Results;

        Camera _camera;

        Rect rFetch;

        [SerializeField]
        Color[] Buffers;

        [SerializeField]
        Texture2D RenderTarget = null;

        void SetupMesh()
        {
            Buffers = new Color[9];
            GameObject child = GameObject.CreatePrimitive(PrimitiveType.Quad);
            child.transform.parent = this.transform;
            child.transform.localScale = new Vector3(5.76f, 0.64f, 1f);
            TargetRender = child.GetComponent<MeshRenderer>();
            TargetMaterial = new Material(Shader.Find("SoccerVR/VideoPlane")); //NOTE: This shader has already optimize for different platform Y scale.
            if (SourceTexture)
                TargetMaterial.SetTexture("_MainTex", SourceTexture);

            TargetRender.material = TargetMaterial;
            child.transform.localPosition = new Vector3(0f, 0f, 1f);
            child.layer = LayerMask.NameToLayer("ColorBlock");
            transform.localPosition = new Vector3(0f, -5000f, 0f);
        }

        void SetCameraEvent()
        {
            _camera = gameObject.AddComponent<Camera>();
            _camera.orthographic = true;
            _camera.targetTexture = Target;
            _camera.aspect = 9f;
            _camera.depth = -3;
            _camera.orthographicSize = 0.32f;
            //Camera.onPostRender += OnFetchPixel;
            _camera.clearFlags = CameraClearFlags.Nothing;
            _camera.cullingMask = 1 << LayerMask.NameToLayer("ColorBlock");
        }
        
        public void OnPostRender()
        {
            OnFetchPixel(_camera);
        }

        public override void ReleaseSource()
        {
            if (TargetMaterial)
                TargetMaterial.SetTexture("_MainTex", null);

            SourceTexture = null;
            GameObject.Destroy(gameObject);
        }

        void Start()
        {
            OnStart();
        }

        // Use this for initialization
        protected void OnStart()
        {
            if (!Target)
            {
                Target = new RenderTexture(576, 64, 0);
            }

            if (!RenderTarget)
                RenderTarget = new Texture2D(Target.width, Target.height, TextureFormat.RGB24, false);

            SetLinerPosition();
            SetupMesh();

            rFetch = new Rect(0f, 0f, Target.width, Target.height);

            SetCameraEvent();


            if (ReadArea == Rect.zero)
            {
                SetCaptureRect(Vector3.one, Vector3.zero);
            }
            else
            {
                if (SourceTexture)
                {
                    Rect rt = new Rect(ReadArea.position, ReadArea.size);
                    VideoUtility.PixelToUV(new Vector2(SourceTexture.width, SourceTexture.height), ref rt);
                    SetCaptureRect(rt.size, rt.position);
                }
            }

            Results = new int[Positions.Length];
        }

        void SetLinerPosition()
        {
            Positions = new Vector2[9];
            float cubeWidth = RenderTarget.height;
            Positions[0] = new Vector2(cubeWidth * 0.5f, cubeWidth * 0.5f);
            Positions[1] = new Vector2(cubeWidth * (1 + 0.5f), cubeWidth * 0.5f);
            Positions[2] = new Vector2(cubeWidth * (2 + 0.5f), cubeWidth * 0.5f);
            Positions[3] = new Vector2(cubeWidth * (3 + 0.5f), cubeWidth * 0.5f);
            Positions[4] = new Vector2(cubeWidth * (4 + 0.5f), cubeWidth * 0.5f);
            Positions[5] = new Vector2(cubeWidth * (5 + 0.5f), cubeWidth * 0.5f);
            Positions[6] = new Vector2(cubeWidth * (6 + 0.5f), cubeWidth * 0.5f);
            Positions[7] = new Vector2(cubeWidth * (7 + 0.5f), cubeWidth * 0.5f);
            Positions[8] = new Vector2(cubeWidth * (8 + 0.5f), cubeWidth * 0.5f);
        }

        public override void SetTimeCodicWindows(Vector2 Origin, Vector2 Size)
        {
            ReadArea = new Rect(Origin, Size);
            if (TargetMaterial && SourceTexture)
            {
                Rect rt = new Rect(Origin, Size);

                VideoUtility.PixelToUV(new Vector2(SourceTexture.width, SourceTexture.height), ref rt);
                SetCaptureRect(rt.size, rt.position);
            }
        }

        void SetCaptureRect(Vector2 tilling, Vector2 Offset)
        {
            TargetMaterial.SetTextureOffset("_MainTex", Offset);
            TargetMaterial.SetTextureScale("_MainTex", tilling);
        }

        public override void SetSource(Texture2D txd)
        {
            SourceTexture = txd;

            if (ReadArea != Rect.zero)
            {
                SetTimeCodicWindows(ReadArea.position, ReadArea.size);
            }

            if (TargetMaterial)
                TargetMaterial.SetTexture("_MainTex", SourceTexture);
        }

        string err = null;
        int frameCount = 0;
        Color buffer;
        // Update is called once per frame
        void OnFetchPixel(Camera cam)
        {
            if (frameCount == 20)
            {
                if (RenderTarget && Target)
                {
                    RenderTexture old = RenderTexture.active;
                    RenderTexture.active = Target;
                    RenderTarget.ReadPixels(rFetch, 0, 0);
                    RenderTarget.Apply();
                    RenderTexture.active = old;

                    //Debug.Log(buffer.Length);

                    for (int i = 0; i < Positions.Length; i++)
                    {
                        Buffers[i] = RenderTarget.GetPixel(Mathf.CeilToInt(Positions[i].x), Mathf.CeilToInt(Positions[i].y));
                        Results[i] = ParseColor(Buffers[i]);
                    }
                    var _now = DateTime.UtcNow;

                    //Fetch time value from color block
                    try
                    {
                        int verySum = (Results[0] + Results[1] + Results[2] + Results[3] + Results[4] + Results[5] + Results[6] + Results[7] + 4) % 10;
                        if (Results[8] == verySum)
                        {
                            TimeSpan span = new TimeSpan(StartupTime.Days, Results[0] * 10 + Results[1], Results[2] * 10 + Results[3], Results[4] * 10 + Results[5], Results[6] * 100 + Results[7] * 10);

                            /*
                            System.DateTime _time = new System.DateTime(_now.Year, _now.Month, _now.Day, //Need to check the date in the future
                              Results[0] * 10 + Results[1],
                              Results[2] * 10 + Results[3],
                              Results[4] * 10 + Results[5],
                              Results[6] * 100 + Results[7] * 10
                              );*/

                            number = (long)span.TotalMilliseconds;
                            //number = (UnixTime + span).Ticks;
                        }
                        else
                        {
                            number = -1;
                        }

                    }
                    catch (ArgumentOutOfRangeException ex)
                    {
                        if (err == null)
                        {
                            err = ex.Message;
                            Debug.Log(err);
                        }
                    }
                }
                frameCount = 0;
            }
            else
            {
                frameCount++;
            }
        }

        public override void SetInitStartupTime(long InitTime)
        {
            StartupTime = TimeSpan.FromMilliseconds(InitTime);
        }

        int ParseColor(Color x)
        {
            int num;
            num = parseChannel(x.r) + parseChannel(x.g) + parseChannel(x.b);
            return num;
        }

        int parseChannel(float ch)
        {
            int num = 0;
            num = (int)(ch * 255);
            if (num <= 60)
            {
                num = 0;
            }
            else if (num > 60 && num <= 120)
            {
                num = 1;
            }
            else if (num > 120 && num <= 180)
            {
                num = 2;
            }
            else if (num > 180 && num <= 255)
            {
                num = 3;
            }
            return num;
        }

        public override void GetTimeFromFrame(ref System.Func<long> _FetchAction)
        {
            _FetchAction = GetFetchResult;
            //StartCoroutine(_DelayFetchFrame(_FetchAction));
        }

        private IEnumerator _DelayFetchFrame(System.Func<long> _FetchAction)
        {
            yield return 1; //Must wait for 1 frame until it flush all the color value to the blocks
            yield return new WaitForEndOfFrame();
            //Camera.onPreRender += OnFetchPixel;
        }

        static int gap = 100;
        long GetFetchResult()
        {
            gap--;
            if (gap <= 0)
            {
                gap = 100;
                Debug.LogFormat("Fetch color {0}, result is {1}", buffer, number);
            }
            return TimeCodeResult;
        }

        public void GetTimeFromSingleFrame(System.Action<long> _FetchAction)
        {
            frameCount = 20;
            StartCoroutine(_DelayFetchSingleFrame(_FetchAction));
        }

        private IEnumerator _DelayFetchSingleFrame(System.Action<long> _FetchAction)
        {
            yield return 2; //Must wait for 1 frame until it flush all the color value to the blocks
            _FetchAction(TimeCodeResult);
        }

        public void setStartUpDays(int days)
        {
            StartupTime = TimeSpan.FromDays(days);
        }

        public override void ForceRenderNow()
        {
            frameCount = 20;
        }

        public Texture FetchResult
        {
            get
            {
                return RenderTarget;
            }
        }

        public override IEnumerator HaltForTask(WaitUntil Task)
        {
            ForbinFetch = true;
            yield return Task;
            ForbinFetch = false;
        }
    }
}