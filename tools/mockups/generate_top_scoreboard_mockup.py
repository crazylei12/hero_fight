from __future__ import annotations

import textwrap
import uuid
from pathlib import Path

from PIL import Image, ImageDraw, ImageFilter, ImageFont


ROOT = Path(__file__).resolve().parents[2]
OUT_DIR = ROOT / "game/Assets/Art/UI/Mockups"
LAYERLAB = ROOT / "game/Assets/Layer Lab/GUI Pro-FantasyRPG/ResourcesData/Sprites/Component"


BLUE = (46, 125, 255, 255)
BLUE_SOFT = (88, 173, 255, 255)
RED = (255, 74, 74, 255)
RED_SOFT = (255, 126, 126, 255)
DARK = (24, 28, 36, 232)
DARK_INNER = (12, 16, 24, 236)
TEXT_MAIN = (244, 246, 250, 255)
TEXT_MUTED = (179, 191, 208, 255)

FONT_CANDIDATES = [
    Path("C:/Windows/Fonts/msyhbd.ttc"),
    Path("C:/Windows/Fonts/msyh.ttc"),
    Path("C:/Windows/Fonts/arialbd.ttf"),
    Path("C:/Windows/Fonts/arial.ttf"),
]

META_TEMPLATE = textwrap.dedent(
    """\
    fileFormatVersion: 2
    guid: {guid}
    TextureImporter:
      internalIDToNameTable: []
      externalObjects: {{}}
      serializedVersion: 13
      mipmaps:
        mipMapMode: 0
        enableMipMap: 1
        sRGBTexture: 1
        linearTexture: 0
        fadeOut: 0
        borderMipMap: 0
        mipMapsPreserveCoverage: 0
        alphaTestReferenceValue: 0.5
        mipMapFadeDistanceStart: 1
        mipMapFadeDistanceEnd: 3
      bumpmap:
        convertToNormalMap: 0
        externalNormalMap: 0
        heightScale: 0.25
        normalMapFilter: 0
        flipGreenChannel: 0
      isReadable: 0
      streamingMipmaps: 0
      streamingMipmapsPriority: 0
      vTOnly: 0
      ignoreMipmapLimit: 0
      grayScaleToAlpha: 0
      generateCubemap: 6
      cubemapConvolution: 0
      seamlessCubemap: 0
      textureFormat: 1
      maxTextureSize: 2048
      textureSettings:
        serializedVersion: 2
        filterMode: 1
        aniso: 1
        mipBias: 0
        wrapU: 0
        wrapV: 0
        wrapW: 0
      nPOTScale: 1
      lightmap: 0
      compressionQuality: 50
      spriteMode: 0
      spriteExtrude: 1
      spriteMeshType: 1
      alignment: 0
      spritePivot: {{x: 0.5, y: 0.5}}
      spritePixelsToUnits: 100
      spriteBorder: {{x: 0, y: 0, z: 0, w: 0}}
      spriteGenerateFallbackPhysicsShape: 1
      alphaUsage: 1
      alphaIsTransparency: 0
      spriteTessellationDetail: -1
      textureType: 0
      textureShape: 1
      singleChannelComponent: 0
      flipbookRows: 1
      flipbookColumns: 1
      maxTextureSizeSet: 0
      compressionQualitySet: 0
      textureFormatSet: 0
      ignorePngGamma: 0
      applyGammaDecoding: 0
      swizzle: 50462976
      cookieLightType: 0
      platformSettings:
      - serializedVersion: 4
        buildTarget: DefaultTexturePlatform
        maxTextureSize: 2048
        resizeAlgorithm: 0
        textureFormat: -1
        textureCompression: 1
        compressionQuality: 50
        crunchedCompression: 0
        allowsAlphaSplitting: 0
        overridden: 0
        ignorePlatformSupport: 0
        androidETC2FallbackOverride: 0
        forceMaximumCompressionQuality_BC6H_BC7: 0
      - serializedVersion: 4
        buildTarget: Standalone
        maxTextureSize: 2048
        resizeAlgorithm: 0
        textureFormat: -1
        textureCompression: 1
        compressionQuality: 50
        crunchedCompression: 0
        allowsAlphaSplitting: 0
        overridden: 0
        ignorePlatformSupport: 0
        androidETC2FallbackOverride: 0
        forceMaximumCompressionQuality_BC6H_BC7: 0
      spriteSheet:
        serializedVersion: 2
        sprites: []
        outline: []
        customData:
        physicsShape: []
        bones: []
        spriteID:
        internalID: 0
        vertices: []
        indices:
        edges: []
        weights: []
        secondaryTextures: []
        spriteCustomMetadata:
          entries: []
        nameFileIdTable: {{}}
      mipmapLimitGroupName:
      pSDRemoveMatte: 0
      userData:
      assetBundleName:
      assetBundleVariant:
    """
)


def get_font(size: int) -> ImageFont.FreeTypeFont | ImageFont.ImageFont:
    for path in FONT_CANDIDATES:
        try:
            return ImageFont.truetype(str(path), size=size)
        except OSError:
            continue
    return ImageFont.load_default()


FONT_TEAM = get_font(36)
FONT_KILL = get_font(86)
FONT_TIMER = get_font(48)
FONT_PHASE = get_font(22)
FONT_SMALL = get_font(18)
FONT_LOGO = get_font(36)


def fit(image: Image.Image, width: int, height: int) -> Image.Image:
    return image.resize((width, height), Image.Resampling.LANCZOS)


def alpha_mask_to_color(image: Image.Image, rgba: tuple[int, int, int, int]) -> Image.Image:
    mask = image.getchannel("A")
    tinted = Image.new("RGBA", image.size, rgba)
    tinted.putalpha(mask)
    return tinted


def centered_text(draw: ImageDraw.ImageDraw, box: tuple[int, int, int, int], text: str, font, fill, stroke_width=2, stroke_fill=(0, 0, 0, 180)) -> None:
    bbox = draw.textbbox((0, 0), text, font=font, stroke_width=stroke_width)
    width = bbox[2] - bbox[0]
    height = bbox[3] - bbox[1]
    x = box[0] + (box[2] - box[0] - width) / 2
    y = box[1] + (box[3] - box[1] - height) / 2 - 2
    draw.text((x, y), text, font=font, fill=fill, stroke_width=stroke_width, stroke_fill=stroke_fill)


def slanted_panel(size: tuple[int, int] = (1920, 260)) -> Image.Image:
    width, height = size
    image = Image.new("RGBA", size, (0, 0, 0, 0))
    shadow = Image.new("RGBA", size, (0, 0, 0, 0))
    shadow_draw = ImageDraw.Draw(shadow)
    polygon = [
        (52, 96),
        (790, 96),
        (854, 60),
        (1066, 60),
        (1130, 96),
        (1868, 96),
        (1892, 120),
        (1892, 206),
        (1868, 230),
        (52, 230),
        (28, 206),
        (28, 120),
    ]
    shadow_draw.polygon([(x, y + 8) for x, y in polygon], fill=(0, 0, 0, 110))
    image.alpha_composite(shadow.filter(ImageFilter.GaussianBlur(8)))

    draw = ImageDraw.Draw(image)
    draw.polygon(polygon, fill=DARK)

    inner = [
        (72, 112),
        (782, 112),
        (860, 72),
        (1060, 72),
        (1138, 112),
        (1848, 112),
        (1868, 130),
        (1868, 196),
        (1848, 214),
        (72, 214),
        (52, 196),
        (52, 130),
    ]
    draw.polygon(inner, fill=DARK_INNER)
    draw.line(polygon + [polygon[0]], fill=(255, 255, 255, 38), width=2)
    draw.line(inner + [inner[0]], fill=(255, 255, 255, 18), width=2)

    for index in range(14):
        alpha = int(26 * (1 - index / 14))
        draw.rounded_rectangle((56 + index * 2, 100 + index, 770 - index * 3, 224 - index), radius=18, outline=(BLUE_SOFT[0], BLUE_SOFT[1], BLUE_SOFT[2], alpha), width=2)
        draw.rounded_rectangle((1150 + index * 3, 100 + index, 1864 - index * 2, 224 - index), radius=18, outline=(RED_SOFT[0], RED_SOFT[1], RED_SOFT[2], alpha), width=2)

    return image


def make_logo(text: str, accent: tuple[int, int, int, int], accent_fill: tuple[int, int, int, int]) -> Image.Image:
    image = Image.new("RGBA", (82, 82), (0, 0, 0, 0))
    shadow = Image.new("RGBA", image.size, (0, 0, 0, 0))
    shadow_draw = ImageDraw.Draw(shadow)
    shadow_draw.ellipse((8, 10, 74, 76), fill=(0, 0, 0, 120))
    image.alpha_composite(shadow.filter(ImageFilter.GaussianBlur(5)))

    draw = ImageDraw.Draw(image)
    draw.ellipse((7, 7, 75, 75), fill=(24, 30, 42, 238), outline=accent, width=3)
    draw.ellipse((14, 14, 68, 68), fill=accent_fill, outline=(255, 255, 255, 30), width=2)
    centered_text(draw, (14, 12, 68, 66), text, FONT_LOGO, TEXT_MAIN, stroke_width=2, stroke_fill=(0, 0, 0, 200))
    return image


def make_background(size: tuple[int, int] = (1920, 560)) -> Image.Image:
    width, height = size
    image = Image.new("RGBA", size, (0, 0, 0, 255))
    pixels = image.load()

    top = (76, 52, 44)
    middle = (114, 82, 58)
    bottom = (30, 24, 23)
    for y in range(height):
        ratio = y / (height - 1)
        if ratio < 0.58:
            blend = ratio / 0.58
            color = tuple(int(top[i] * (1 - blend) + middle[i] * blend) for i in range(3))
        else:
            blend = (ratio - 0.58) / 0.42
            color = tuple(int(middle[i] * (1 - blend) + bottom[i] * blend) for i in range(3))
        for x in range(width):
            pixels[x, y] = (*color, 255)

    draw = ImageDraw.Draw(image)
    for x in (210, 520, 960, 1410, 1710):
        draw.rounded_rectangle((x - 56, 26, x + 56, 244), radius=18, fill=(132, 94, 74, 36))
        draw.rounded_rectangle((x - 42, 48, x + 42, 224), radius=14, outline=(255, 255, 255, 20), width=2)

    for index in range(18):
        draw.rectangle((0, index * 2, width, index * 2 + 1), fill=(255, 200, 120, max(0, 18 - index)))

    light = Image.new("RGBA", size, (0, 0, 0, 0))
    light_draw = ImageDraw.Draw(light)
    light_draw.ellipse((560, -40, 1360, 360), fill=(255, 180, 80, 36))
    image.alpha_composite(light.filter(ImageFilter.GaussianBlur(36)))

    vignette = Image.new("RGBA", size, (0, 0, 0, 0))
    vignette_draw = ImageDraw.Draw(vignette)
    for index in range(36):
        x0 = index * 18
        y0 = index * 10
        x1 = width - index * 18
        y1 = height - index * 10
        if x1 <= x0 or y1 <= y0:
            break
        alpha = int(4 + index * 2.2)
        vignette_draw.rounded_rectangle((x0, y0, x1, y1), radius=40, outline=(0, 0, 0, alpha), width=16)
    image.alpha_composite(vignette.filter(ImageFilter.GaussianBlur(10)))
    return image


def generate(prefix: str = "top_scoreboard_mockup_v3") -> tuple[Path, Path]:
    OUT_DIR.mkdir(parents=True, exist_ok=True)

    ribbon_blue = Image.open(LAYERLAB / "Label-Title/Title_Ribbon_03_Blue.png").convert("RGBA")
    ribbon_red = Image.open(LAYERLAB / "Label-Title/Title_Ribbon_03_Red.png").convert("RGBA")
    center_orn = Image.open(LAYERLAB / "Frame/LineTextFrame_04_Demo.png").convert("RGBA")
    center_line = Image.open(LAYERLAB / "Frame/LineFrame_02.png").convert("RGBA")

    scoreboard = slanted_panel()
    canvas = Image.new("RGBA", scoreboard.size, (0, 0, 0, 0))
    canvas.alpha_composite(scoreboard)

    canvas.alpha_composite(fit(ribbon_blue, 620, 92), (82, 8))
    canvas.alpha_composite(fit(ribbon_red, 620, 92), (1218, 8))

    ornament_dark = alpha_mask_to_color(center_orn, (28, 22, 20, 230))
    ornament_gold = alpha_mask_to_color(center_orn, (209, 145, 64, 110))
    ornament_dark = fit(ornament_dark, 500, 112)
    ornament_gold = fit(ornament_gold, 500, 112)
    canvas.alpha_composite(ornament_gold.filter(ImageFilter.GaussianBlur(4)), (710, 50))
    canvas.alpha_composite(ornament_dark, (710, 44))

    center_bar = fit(alpha_mask_to_color(center_line, (177, 146, 95, 210)), 420, 30)
    canvas.alpha_composite(center_bar, (750, 170))

    panel_draw = ImageDraw.Draw(canvas)
    info_y = 112
    info_height = 102
    logo_size = 102
    left_logo_x = 56
    right_logo_x = 1762
    panel_draw.rounded_rectangle((left_logo_x, info_y, left_logo_x + logo_size, info_y + info_height), radius=26, fill=(16, 20, 30, 240), outline=BLUE, width=2)
    panel_draw.rounded_rectangle((right_logo_x, info_y, right_logo_x + logo_size, info_y + info_height), radius=26, fill=(16, 20, 30, 240), outline=RED, width=2)

    left_panel_x = 170
    right_panel_x = 1180
    panel_width = 555
    panel_draw.rounded_rectangle((left_panel_x, info_y, left_panel_x + panel_width, info_y + info_height), radius=18, fill=(10, 14, 22, 235))
    panel_draw.rounded_rectangle((right_panel_x, info_y, right_panel_x + panel_width, info_y + info_height), radius=18, fill=(10, 14, 22, 235))
    panel_draw.line((278, info_y + 10, 278, info_y + info_height - 10), fill=(255, 255, 255, 42), width=2)
    panel_draw.line((1702, info_y + 10, 1702, info_y + info_height - 10), fill=(255, 255, 255, 42), width=2)

    left_logo = make_logo("SS", BLUE, (88, 173, 255, 220))
    right_logo = make_logo("鸡", RED, (255, 138, 138, 220))
    canvas.alpha_composite(left_logo, (left_logo_x + 10, info_y + 10))
    canvas.alpha_composite(right_logo, (right_logo_x + 10, info_y + 10))

    centered_text(panel_draw, (160, 16, 650, 88), "Strange Seals", FONT_TEAM, TEXT_MAIN)
    centered_text(panel_draw, (1270, 16, 1760, 88), "鸡腿大大", FONT_TEAM, TEXT_MAIN)

    panel_draw.text((782, 92), "4", font=FONT_KILL, fill=BLUE_SOFT, stroke_width=3, stroke_fill=(6, 10, 18, 220))
    panel_draw.text((1128, 92), "3", font=FONT_KILL, fill=RED_SOFT, stroke_width=3, stroke_fill=(6, 10, 18, 220))

    centered_text(panel_draw, (800, 56, 1120, 84), "常规时间", FONT_PHASE, (236, 210, 170, 255), stroke_width=1, stroke_fill=(30, 20, 12, 180))
    centered_text(panel_draw, (790, 82, 1130, 140), "00:58", FONT_TIMER, TEXT_MAIN)
    centered_text(panel_draw, (904, 168, 1018, 194), "VS", FONT_PHASE, (245, 223, 179, 255), stroke_width=2, stroke_fill=(30, 20, 12, 180))

    for index in range(3):
        x = 684 + index * 28
        fill = BLUE if index < 2 else (70, 78, 92, 255)
        panel_draw.ellipse((x, 170, x + 18, 188), fill=fill, outline=(255, 255, 255, 26), width=2)
    for index in range(3):
        x = 1178 + index * 28
        fill = RED if index < 1 else (70, 78, 92, 255)
        panel_draw.ellipse((x, 170, x + 18, 188), fill=fill, outline=(255, 255, 255, 26), width=2)

    scoreboard_path = OUT_DIR / f"{prefix}.png"
    canvas.save(scoreboard_path)

    preview = make_background()
    shadow = Image.new("RGBA", preview.size, (0, 0, 0, 0))
    blurred = canvas.filter(ImageFilter.GaussianBlur(12))
    shadow.alpha_composite(blurred, (0, 20))
    preview.alpha_composite(shadow.filter(ImageFilter.GaussianBlur(6)))
    preview.alpha_composite(canvas, (0, 12))

    preview_draw = ImageDraw.Draw(preview)
    preview_draw.text((134, 304), "预览图：顶部计分板素材 v3", font=get_font(24), fill=(255, 237, 212, 180), stroke_width=1, stroke_fill=(0, 0, 0, 150))
    preview_draw.text((134, 336), "收掉双层队徽框，并把蓝方击杀数与圆点彻底错开", font=get_font(18), fill=(233, 235, 242, 150), stroke_width=1, stroke_fill=(0, 0, 0, 150))

    preview_path = OUT_DIR / f"{prefix}_preview.png"
    preview.save(preview_path)

    return scoreboard_path, preview_path


def write_meta(path: Path) -> None:
    meta_path = path.with_suffix(path.suffix + ".meta")
    if meta_path.exists():
        return
    meta_path.write_text(META_TEMPLATE.format(guid=uuid.uuid4().hex), encoding="utf-8")


def main() -> None:
    scoreboard_path, preview_path = generate()
    write_meta(scoreboard_path)
    write_meta(preview_path)
    print(scoreboard_path)
    print(preview_path)


if __name__ == "__main__":
    main()
