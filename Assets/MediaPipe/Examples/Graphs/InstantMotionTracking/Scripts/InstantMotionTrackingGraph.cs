using Mediapipe;
using UnityEngine;

using Google.Protobuf;

public class InstantMotionTrackingGraph : OfficialDemoGraph {
  [SerializeField] TextAsset texture3dAsset = null;

  int stickerSentinelId = -1;
  int renderId = 1;

  Sticker currentSticker;
  float[] imuRotationMatrix;

  void Update() {

  }

  public override Status StartRun(Texture texture) {
    stopwatch.Start();
    currentSticker = InitializeSticker(1);
    imuRotationMatrix = new float[] { 1, 0, 0, 0, 1, 0, 0, 0, 1 };

    sidePacket = new SidePacket();
    sidePacket.Emplace("vertical_fov_radians", new FloatPacket(GetVerticalFovRadians()));
    sidePacket.Emplace("aspect_ratio", new FloatPacket(4.0f / 3.0f));

    sidePacket.Emplace("texture_3d", new ImageFramePacket(GetImageFrameFromImage(texture3dAsset)));
    sidePacket.Emplace("asset_3d", new StringPacket("robot/robot.obj.uuu"));

#if UNITY_ANDROID && !UNITY_EDITOR
    // Tell MediaPipe the target texture
    destinationNativeTexturePtr = texture.GetNativeTexturePtr();
    destinationWidth = texture.width;
    destinationHeight = texture.height;

    gpuHelper.RunInGlContext(BuildDestination).AssertOk();
    sidePacket.Emplace(destinationBufferName, outputPacket);

    return graph.StartRun(sidePacket);
#else
    return StartRun();
#endif
  }

  public override Status PushInput(TextureFrame textureFrame) {
    base.PushInput(textureFrame).AssertOk();

    graph.AddPacketToInputStream("sticker_sentinel", new IntPacket(stickerSentinelId, currentTimestamp)).AssertOk();
    stickerSentinelId = -1;

    var stickerRoll = new StickerRoll();
    stickerRoll.Sticker.Add(currentSticker);

    graph.AddPacketToInputStream("sticker_proto_string", new StringPacket(stickerRoll.ToByteArray(), currentTimestamp)).AssertOk();
    graph.AddPacketToInputStream("imu_rotation_matrix", new FloatArrayPacket(imuRotationMatrix, currentTimestamp)).AssertOk();

    return Status.Ok();
  }

  float GetVerticalFovRadians() {
    // TODO: acquire it automatically
    return Mathf.Deg2Rad * 68.0f;
  }

  ImageFrame GetImageFrameFromImage(TextAsset image) {
    var texture = new Texture2D(1, 1, TextureFormat.RGBA32, 0, false);
    texture.LoadImage(image.bytes);

    return new ImageFrame(ImageFormat.Format.SRGBA, texture.width, texture.height, 4 * texture.width, texture.GetRawTextureData<byte>());
  }

  Sticker InitializeSticker(int id, float x = 0.5f, float y = 0.5f) {
    var sticker = new Sticker();

    sticker.Id = id;
    sticker.X = x;
    sticker.Y = y;
    sticker.Scale = 1;
    sticker.RenderId = renderId;

    stickerSentinelId = id;

    return sticker;
  }

  void UpdateImuRotationMatrix(Gyroscope gyroscope) {
    return;
  }
}
