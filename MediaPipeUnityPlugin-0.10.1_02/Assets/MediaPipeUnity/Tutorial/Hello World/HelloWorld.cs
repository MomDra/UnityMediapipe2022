// Copyright (c) 2021 homuler
//
// Use of this source code is governed by an MIT-style
// license that can be found in the LICENSE file or at
// https://opensource.org/licenses/MIT.

// ATTENTION!: This code is for a tutorial and it's broken as is.

using UnityEngine;

namespace Mediapipe.Unity.Tutorial
{
  public class HelloWorld : MonoBehaviour
  {
    private void Start()
    {
      var configText = @"
input_stream: ""in""
output_stream: ""out""
node {
  calculator: ""PassThroughCalculator""
  input_stream: ""in""
  output_stream: ""out1""
}
node {
  calculator: ""PassThroughCalculator""
  input_stream: ""out1""
  output_stream: ""out""
}
";
      var graph = new CalculatorGraph(configText);
      // Initialize an `OutputStreamPoller`.
      // NOTE: The type parameter is `string` since the output type is `string`. --> 왜 output type이 string인지 모르겠음
      var poller = graph.AddOutputStreamPoller<string>("out").Value();
      // 출력을 얻기 위해서 그래프를 돌리기 전에 작업을 해야 한다.
      // 출력 스트림의 이름은 out이었다.

      // AddOutputStreamPoller<T>는 StatusOrPoller<T>를 반환함
      // 이것은 Status와 유사해 보임
      // 그러나 이것은 Status가 OK라면 value 하나를 가지고 있음

      // 주의, 실제 사용은 Value()를 호출하기 전에 if 문으로 OK 확인을 해야 함
      // var statusOrPoller = graph.AddOutputStreamPoller<string>("out");
      // if (statusOrPoller.Ok())
      // {
      //   var poller = statusOrPoller.Value();
      // }  

      graph.StartRun().AssertOk();
      // StartRun()은 결과를 나타내는 Status 객체를 리턴함
      // AssertOK()는 만약 결과가 not OK이면 exception을 throw 하는 것 같음

      for (var i = 0; i < 10; i++)
      {
        var input = new StringPacket("Hello World!", new Timestamp(i));
        // input은 Packet이라는 클래스를 통해야 한다.
        // input 패킷은 타임 스탬프가 반드시 있어야 함

        graph.AddPacketToInputStream("in", input).AssertOk();
        // 그래프에 input을 넣기 위한 코드, 우리의 input stream의 이름은 "in"이였음
      }

      graph.CloseInputStream("in").AssertOk();
      // CloseInputStream

      // Initialize an empty packet
      var output = new StringPacket();

      while (poller.Next(output))
      {
        Debug.Log(output.Get());
      }
      
      // 밑 2줄 그래프를 dispose 하는 것
      graph.WaitUntilDone().AssertOk();
      graph.Dispose();

      Debug.Log("Done");
    }

  }
}
