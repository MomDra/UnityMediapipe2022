// Copyright (c) 2021 homuler
//
// Use of this source code is governed by an MIT-style
// license that can be found in the LICENSE file or at
// https://opensource.org/licenses/MIT.

// ATTENTION!: This code is for a tutorial.

using System.Collections;
using System.Resources;
using UnityEngine;
using UnityEngine.UI;

using Stopwatch = System.Diagnostics.Stopwatch;


namespace Mediapipe.Unity.Tutorial
{
  public class FaceMesh : MonoBehaviour
  {
    [SerializeField] private TextAsset _configAsset;
    
    [SerializeField] private RawImage _screen;
    [SerializeField] private int _width;
    [SerializeField] private int _height;
    [SerializeField] private int _fps;

    private CalculatorGraph _graph;

    private WebCamTexture _webCamTexture;
    private ResourceManager _resourceManager;

    private Texture2D _inputTexture;
    private Color32[] _inputPixelData;
    private Texture2D _outputTexture;
    private Color32[] _outputPixelData;
    
    private IEnumerator Start()
    {
      if (WebCamTexture.devices.Length == 0)
      {
        throw new System.Exception("Web Camera devices are not found");
      }
      var webCamDevice = WebCamTexture.devices[0];
      _webCamTexture = new WebCamTexture(webCamDevice.name, _width, _height, _fps);
      _webCamTexture.Play();

      yield return new WaitUntil(() => _webCamTexture.width > 16);

      _screen.rectTransform.sizeDelta = new Vector2(_width, _height);

      // MediaPipe에서, CPU에 있는 이미지 데이터는 ImageFrame 클래스에 저장됨
      _inputTexture = new Texture2D(_width, _height, TextureFormat.RGBA32, false);
      _inputPixelData = new Color32[_width * _height];

      _outputTexture = new Texture2D(_width, _height, TextureFormat.RGBA32, false);
      _outputPixelData = new Color32[_width * _height];
      
      // !! 
      _screen.texture = _outputTexture;

      _resourceManager = new LocalResourceManager();
      yield return _resourceManager.PrepareAssetAsync("face_detection_short_range.bytes");
      yield return _resourceManager.PrepareAssetAsync("face_landmark_with_attention.bytes");
      
      var stopwatch = new Stopwatch();
      
      _graph = new CalculatorGraph(_configAsset.text);
      // configAsset을 통해 그래프 생성

      var outputVideoStream = new OutputStream<ImageFramePacket, ImageFrame>(_graph, "output_video");
      // outputstream을 얻기위한 새로운 방법
      outputVideoStream.StartPolling().AssertOk();
      // 그래프를 시작하기 전에 startpolling()함수를 호출해야 함

      _graph.StartRun().AssertOk();

      stopwatch.Start();
      // timestamp를 알기 위해 사용되는 stopwatch임

      while (true)
      {
        _inputTexture.SetPixels32(_webCamTexture.GetPixels32(_inputPixelData));
        // ImageFrame 객체는 NativeArray<bytes>를 이용해서 초기화 할 수 있음
        // webCamTexture 데이터를 Texture2D로 복사하는 거임, NativeArray<byte>를 얻으려고
        var imageFrame = new ImageFrame(ImageFormat.Types.Format.Srgba, _width, _height, _width * 4, _inputTexture.GetRawTextureData<byte>());
        // 픽셀 값과 다음 행의 동일한 픽셀 및 채널 사이의 바이트 오프셋입니다.
        // 대부분의 경우, 이는 폭과 채널 수의 곱과 같습니다.

        var currentTimestamp = stopwatch.ElapsedTicks / (System.TimeSpan.TicksPerMillisecond / 1000);
        // timestamp의 단위는 ms임

        _graph.AddPacketToInputStream("input_video", new ImageFramePacket(imageFrame, new Timestamp(currentTimestamp))).AssertOk();
        // ImageFramePacket을 이용해 그래프에 이미지를 입력함

        yield return new WaitForEndOfFrame();

        if (outputVideoStream.TryGetNext(out var outputVideo)) // true를 반환하면 output을 가져올 수 있음
        {
          if (outputVideo.TryReadPixelData(_outputPixelData))
          {
            _outputTexture.SetPixels32(_outputPixelData);
            _outputTexture.Apply();
          }
        }
      }
      
    }

    private void OnDestroy()
    {
      if (_webCamTexture != null)
      {
        _webCamTexture.Stop();
      }
      
      if (_graph != null)
      {
        try
        {
          _graph.CloseInputStream("input_video").AssertOk();
          _graph.WaitUntilDone().AssertOk();
        }
        finally
        {

          _graph.Dispose();
        }
      }
    }
  }
}
