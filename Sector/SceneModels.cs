using System.Drawing;

namespace Sector;

public enum SceneElementType { Text, Image }

public sealed record SceneElement(
    string Id,
    string Name,
    SceneElementType Type,
    float X,
    float Y,
    float Width,
    float Height,
    string Text,
    string FontFamily,
    float FontSize,
    string ColorHex,
    string? ImageBase64 = null);

public sealed record SceneProject(
    int SchemaVersion,
    string Name,
    int CanvasWidth,
    int CanvasHeight,
    string BackgroundHex,
    List<SceneElement> Elements);
