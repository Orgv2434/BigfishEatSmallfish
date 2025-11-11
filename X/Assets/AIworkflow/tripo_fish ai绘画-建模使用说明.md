# tripo_fish ai绘画-建模使用说明

### 1. 工具Set up

#### ai绘画：触站ai

##### api设置

`AIworkflow-> SketchDemo ->Data ->Draw_Config : 输入触站AIToken`

##### 网站

[API开放平台](https://open.czhanai.com/platform)

#### ai建模：tripo3D v1.1.0

##### api设置

`Hierarchy-> Tripo_Manager -> TripoController：输入Tripo3D APIkey `

##### 网站

 [Tripo API | Integrate, Automate, and Scale with AI 3D Modeling](https://www.tripo3d.ai/api)



### 2. ai绘画编辑器内设置：

#### 预设提示词更改：

`SketchDemo.cs -> InitDrawingSettings`

#### ai绘画参数修改：

`DrawModel.cs`

#### 更改ai绘画sprite要求：

| 项目                 | 必须设置为                       |
| -------------------- | -------------------------------- |
| Texture Type         | Sprite (2D and UI)               |
| Sprite Mode          | Single                           |
| Mesh Type            | Full Rect                        |
| Read/Write Enabled   | ✅ 开启                           |
| Compression          | RGBA32 / RGB24 / None / 可写格式 |
| Mipmap               | ❌ 关闭                           |
| Sprite Packing       | ❌ 关闭                           |
| Texture size         | 可任意，但越大性能越差           |
| SpriteRenderer scale | 推荐保持 1:1                     |
