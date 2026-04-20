from __future__ import annotations

import argparse
import textwrap
import uuid
from pathlib import Path

from PIL import Image, ImageDraw, ImageFilter, ImageFont, ImageOps


ROOT = Path(__file__).resolve().parents[2]
OUT_DIR = ROOT / "game/Assets/Art/UI/Mockups"
RUNTIME_OUT_DIR = ROOT / "game/Assets/Resources/UI/BattleHud"
LAYERLAB = ROOT / "game/Assets/Layer Lab/GUI Pro-FantasyRPG/ResourcesData/Sprites/Component"
ULTIMATE_ICONS = ROOT / "game/Assets/UltimateCleanGUIPack/Common/Sprites/Icons"
AVATAR = ROOT / "game/Assets/FantasyWorkshop/AvatarMaker/Images"

BASE_WIDTH = 139
BASE_HEIGHT = 88
SCALE = 5

CARD_WIDTH = BASE_WIDTH * SCALE
CARD_HEIGHT = BASE_HEIGHT * SCALE

RED_ACCENT = (198, 47, 49, 255)
RED_ACCENT_SOFT = (233, 84, 79, 255)
BLUE_OUTLINE = (56, 113, 188, 255)
FRAME_OUTLINE = (46, 53, 67, 255)
PANEL_DARK = (18, 23, 33, 248)
PANEL_DARKER = (10, 14, 22, 255)
TEXT_MAIN = (237, 239, 244, 255)
TEXT_MUTED = (128, 136, 151, 255)
TEXT_DIM = (86, 95, 112, 255)
GOLD = (245, 185, 58, 255)
SWORD_TINT = (214, 156, 77, 255)
SHIELD_TINT = (122, 173, 222, 255)
HEAL_TINT = (114, 222, 179, 255)

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


def s(value: float) -> int:
    return int(round(value * SCALE))


def rect(x: float, y: float, width: float, height: float) -> tuple[int, int, int, int]:
    return (s(x), s(y), s(x + width), s(y + height))


def get_font(size: int) -> ImageFont.FreeTypeFont | ImageFont.ImageFont:
    for path in FONT_CANDIDATES:
        try:
            return ImageFont.truetype(str(path), size=size)
        except OSError:
            continue
    return ImageFont.load_default()


FONT_TAB = get_font(s(4.2))
FONT_KDA = get_font(s(4.2))
FONT_STAT = get_font(s(5.0))
FONT_SMALL_NUMBER = get_font(s(4.4))
FONT_NAME = get_font(s(4.6))
FONT_TRAIT = get_font(s(2.8))
FONT_CORE = get_font(s(6.4))


def fit(image: Image.Image, width: int, height: int) -> Image.Image:
    return image.resize((width, height), Image.Resampling.LANCZOS)


def fit_contain(image: Image.Image, max_width: int, max_height: int) -> Image.Image:
    ratio = min(max_width / image.width, max_height / image.height)
    width = max(1, int(round(image.width * ratio)))
    height = max(1, int(round(image.height * ratio)))
    return image.resize((width, height), Image.Resampling.LANCZOS)


def alpha_mask_to_color(image: Image.Image, rgba: tuple[int, int, int, int]) -> Image.Image:
    mask = image.getchannel("A")
    tinted = Image.new("RGBA", image.size, rgba)
    tinted.putalpha(mask)
    return tinted


def recolor_from_template(
    image: Image.Image,
    dark_rgb: tuple[int, int, int],
    light_rgb: tuple[int, int, int],
) -> Image.Image:
    alpha = image.getchannel("A")
    grayscale = ImageOps.grayscale(image)
    colored = ImageOps.colorize(grayscale, black=dark_rgb, white=light_rgb).convert("RGBA")
    colored.putalpha(alpha)
    return colored


def trim_to_alpha(image: Image.Image) -> Image.Image:
    bbox = image.getbbox()
    if bbox is None:
        return image
    return image.crop(bbox)


def centered_text(
    draw: ImageDraw.ImageDraw,
    box: tuple[int, int, int, int],
    text: str,
    font: ImageFont.FreeTypeFont | ImageFont.ImageFont,
    fill: tuple[int, int, int, int],
    stroke_width: int = 0,
    stroke_fill: tuple[int, int, int, int] = (0, 0, 0, 180),
) -> None:
    bbox = draw.textbbox((0, 0), text, font=font, stroke_width=stroke_width)
    width = bbox[2] - bbox[0]
    height = bbox[3] - bbox[1]
    x = box[0] + (box[2] - box[0] - width) / 2
    y = box[1] + (box[3] - box[1] - height) / 2 - s(0.4)
    draw.text((x, y), text, font=font, fill=fill, stroke_width=stroke_width, stroke_fill=stroke_fill)


def draw_text_right(
    draw: ImageDraw.ImageDraw,
    anchor_x: int,
    y_center: int,
    text: str,
    font: ImageFont.FreeTypeFont | ImageFont.ImageFont,
    fill: tuple[int, int, int, int],
    stroke_width: int = 0,
    stroke_fill: tuple[int, int, int, int] = (0, 0, 0, 180),
) -> None:
    bbox = draw.textbbox((0, 0), text, font=font, stroke_width=stroke_width)
    width = bbox[2] - bbox[0]
    height = bbox[3] - bbox[1]
    x = anchor_x - width
    y = y_center - (height // 2)
    draw.text((x, y), text, font=font, fill=fill, stroke_width=stroke_width, stroke_fill=stroke_fill)


def beveled_points(x0: int, y0: int, x1: int, y1: int, bevel: int) -> list[tuple[int, int]]:
    return [
        (x0 + bevel, y0),
        (x1 - bevel, y0),
        (x1, y0 + bevel),
        (x1, y1 - bevel),
        (x1 - bevel, y1),
        (x0 + bevel, y1),
        (x0, y1 - bevel),
        (x0, y0 + bevel),
    ]


def draw_beveled_panel(
    canvas: Image.Image,
    bounds: tuple[int, int, int, int],
    *,
    fill: tuple[int, int, int, int],
    inner_fill: tuple[int, int, int, int] | None = None,
    outline: tuple[int, int, int, int] | None = None,
    shadow_alpha: int = 90,
    bevel: int | None = None,
) -> None:
    x0, y0, x1, y1 = bounds
    bevel = bevel or max(4, s(1.8))
    shadow = Image.new("RGBA", canvas.size, (0, 0, 0, 0))
    shadow_draw = ImageDraw.Draw(shadow)
    points = beveled_points(x0, y0, x1, y1, bevel)
    shadow_draw.polygon([(x + s(0.8), y + s(0.8)) for x, y in points], fill=(0, 0, 0, shadow_alpha))
    canvas.alpha_composite(shadow.filter(ImageFilter.GaussianBlur(s(1.2))))

    draw = ImageDraw.Draw(canvas)
    draw.polygon(points, fill=fill)
    if inner_fill is not None:
        inner = beveled_points(x0 + s(0.8), y0 + s(0.8), x1 - s(0.8), y1 - s(0.8), max(2, bevel - s(0.6)))
        draw.polygon(inner, fill=inner_fill)
    if outline is not None:
        draw.line(points + [points[0]], fill=outline, width=max(1, s(0.45)))


def load_button(name: str) -> Image.Image:
    return Image.open(LAYERLAB / f"Button/{name}").convert("RGBA")


def load_icon(path: Path, tint: tuple[int, int, int, int], size: tuple[int, int]) -> Image.Image:
    image = trim_to_alpha(Image.open(path).convert("RGBA"))
    image = alpha_mask_to_color(image, tint)
    return fit_contain(image, size[0], size[1])


def load_rotated_icon(
    path: Path,
    tint: tuple[int, int, int, int],
    size: tuple[int, int],
    angle: int,
) -> Image.Image:
    image = load_icon(path, tint, size)
    return trim_to_alpha(image.rotate(angle, expand=True, resample=Image.Resampling.BICUBIC))


def draw_icon_value_group(
    canvas: Image.Image,
    draw: ImageDraw.ImageDraw,
    center_x: int,
    center_y: int,
    icon: Image.Image,
    text: str,
    font: ImageFont.FreeTypeFont | ImageFont.ImageFont,
    fill: tuple[int, int, int, int],
    *,
    gap: int,
    stroke_width: int = 1,
    stroke_fill: tuple[int, int, int, int] = (0, 0, 0, 170),
) -> None:
    bbox = draw.textbbox((0, 0), text, font=font, stroke_width=stroke_width)
    text_width = bbox[2] - bbox[0]
    text_height = bbox[3] - bbox[1]
    group_width = icon.width + gap + text_width
    start_x = center_x - (group_width // 2)
    icon_y = center_y - (icon.height // 2)
    text_x = start_x + icon.width + gap
    text_y = center_y - (text_height // 2) - s(0.3)
    canvas.alpha_composite(icon, (start_x, icon_y))
    draw.text(
        (text_x, text_y),
        text,
        font=font,
        fill=fill,
        stroke_width=stroke_width,
        stroke_fill=stroke_fill,
    )


def compose_avatar(size: tuple[int, int]) -> Image.Image:
    portrait = Image.new("RGBA", size, (0, 0, 0, 0))
    bg = Image.new("RGBA", size, (0, 0, 0, 0))
    bg_draw = ImageDraw.Draw(bg)
    bg_draw.rectangle((0, 0, size[0], size[1]), fill=(25, 33, 47, 255))
    bg_draw.rectangle((s(0.6), s(0.6), size[0] - s(0.6), size[1] - s(0.6)), fill=(38, 51, 76, 255))
    light = Image.new("RGBA", size, (0, 0, 0, 0))
    ImageDraw.Draw(light).ellipse((-s(6), -s(6), size[0] + s(2), size[1] + s(5)), fill=(120, 163, 235, 50))
    portrait.alpha_composite(bg)
    portrait.alpha_composite(light.filter(ImageFilter.GaussianBlur(s(2))))

    head = trim_to_alpha(Image.open(AVATAR / "Head/Type1.png").convert("RGBA"))
    hair = trim_to_alpha(Image.open(AVATAR / "Hair/Spiky.png").convert("RGBA"))
    eyes = trim_to_alpha(Image.open(AVATAR / "Eyes/Boy01.png").convert("RGBA"))
    brows = trim_to_alpha(Image.open(AVATAR / "Eyebrows/Eyebrows4.png").convert("RGBA"))

    head = alpha_mask_to_color(head, (245, 217, 187, 255))
    hair = alpha_mask_to_color(hair, (238, 245, 255, 255))
    eyes = alpha_mask_to_color(eyes, (44, 173, 255, 255))
    brows = alpha_mask_to_color(brows, (71, 54, 42, 255))

    head = fit(head, int(size[0] * 0.70), int(size[1] * 0.84))
    hair = fit(hair, int(size[0] * 0.76), int(size[1] * 0.48))
    eyes = fit(eyes, int(size[0] * 0.34), int(size[1] * 0.18))
    brows = fit(brows, int(size[0] * 0.36), int(size[1] * 0.13))

    portrait.alpha_composite(head, (int(size[0] * 0.14), int(size[1] * 0.14)))
    portrait.alpha_composite(hair, (int(size[0] * 0.12), int(size[1] * 0.02)))
    portrait.alpha_composite(brows, (int(size[0] * 0.32), int(size[1] * 0.32)))
    portrait.alpha_composite(eyes, (int(size[0] * 0.34), int(size[1] * 0.40)))

    draw = ImageDraw.Draw(portrait)
    draw.rounded_rectangle((0, 0, size[0] - 1, size[1] - 1), radius=s(1.6), outline=(255, 255, 255, 36), width=max(1, s(0.35)))
    return portrait


def draw_arrow_button(canvas: Image.Image, bounds: tuple[int, int, int, int]) -> None:
    template = fit(load_button("Button_Rectangle_01_Convex_Yellow.Png"), bounds[2] - bounds[0], bounds[3] - bounds[1])
    outer = recolor_from_template(template, (120, 22, 20), (232, 70, 68))
    canvas.alpha_composite(outer, (bounds[0], bounds[1]))

    inset_x = s(0.9)
    inner_bounds = (bounds[0] + inset_x, bounds[1], bounds[2] - inset_x, bounds[3])
    button = fit(load_button("Button_Rectangle_01_Convex_Yellow.Png"), inner_bounds[2] - inner_bounds[0], inner_bounds[3] - inner_bounds[1])
    canvas.alpha_composite(button, (inner_bounds[0], inner_bounds[1]))

    draw = ImageDraw.Draw(canvas)
    x0, y0, x1, y1 = inner_bounds
    draw.line((x0 + s(1.7), y1 - s(2.2), x1 - s(2.1), y0 + s(2.1)), fill=(71, 42, 14, 255), width=max(1, s(0.8)))
    draw.line((x1 - s(2.1), y0 + s(2.1), x1 - s(2.1), y0 + s(5.3)), fill=(71, 42, 14, 255), width=max(1, s(0.8)))
    draw.line((x1 - s(2.1), y0 + s(2.1), x1 - s(5.5), y0 + s(2.1)), fill=(71, 42, 14, 255), width=max(1, s(0.8)))


def make_preview(card: Image.Image) -> Image.Image:
    preview_width = card.size[0] + s(18)
    preview_height = card.size[1] + s(18)
    preview = Image.new("RGBA", (preview_width, preview_height), (10, 12, 18, 255))
    vignette = Image.new("RGBA", preview.size, (0, 0, 0, 0))
    vignette_draw = ImageDraw.Draw(vignette)
    for index in range(16):
        alpha = min(100, 8 + index * 4)
        inset = index * s(0.4)
        vignette_draw.rounded_rectangle(
            (inset, inset, preview_width - inset, preview_height - inset),
            radius=s(3.5),
            outline=(0, 0, 0, alpha),
            width=max(1, s(0.8)),
        )
    preview.alpha_composite(vignette.filter(ImageFilter.GaussianBlur(s(1.4))))
    preview.alpha_composite(card, (s(9), s(9)))
    return preview


def generate(prefix: str = "side_hero_sidebar_mockup_v8") -> tuple[Path, Path]:
    OUT_DIR.mkdir(parents=True, exist_ok=True)

    card = Image.new("RGBA", (CARD_WIDTH, CARD_HEIGHT), (0, 0, 0, 0))
    draw_beveled_panel(
        card,
        (0, 0, CARD_WIDTH - 1, CARD_HEIGHT - 1),
        fill=(16, 19, 27, 255),
        inner_fill=PANEL_DARK,
        outline=FRAME_OUTLINE,
        shadow_alpha=120,
        bevel=s(2.2),
    )

    header = fit(load_button("Button_Rectangle_01_Convex_Red.Png"), CARD_WIDTH, s(23))
    card.alpha_composite(header, (0, 0))
    draw = ImageDraw.Draw(card)

    # Header tabs
    left_tab = fit(load_button("Button_Rectangle_01_Convex_Dark.Png"), s(28), s(23))
    card.alpha_composite(left_tab, (0, 0))
    draw.rectangle(rect(0, 0, 28, 23), fill=(106, 48, 56, 82))
    draw.line((s(28), s(3), s(28), s(20)), fill=(84, 19, 22, 170), width=max(1, s(0.35)))
    centered_text(draw, rect(1, 0, 27, 23), "资讯", FONT_TAB, TEXT_MAIN, stroke_width=1, stroke_fill=(49, 12, 15, 180))
    centered_text(draw, rect(28, 0, 93, 23), "选手名", FONT_TAB, TEXT_MAIN, stroke_width=1, stroke_fill=(49, 12, 15, 180))
    draw_arrow_button(card, rect(121, 2.2, 18, 18))

    # Inner separators
    draw.line((s(28), s(23), s(28), s(88)), fill=BLUE_OUTLINE, width=max(1, s(0.4)))
    draw.line((s(29), s(62), s(139), s(62)), fill=(38, 52, 76, 200), width=max(1, s(0.35)))

    # Left stat column
    draw.rectangle(rect(0, 23, 28, 71), fill=(15, 20, 28, 220))
    for x in (0, 9.33, 18.66, 28):
        draw.line((s(x), s(23), s(x), s(46)), fill=(70, 97, 135, 170), width=max(1, s(0.28)))
    for y in (23, 34, 46, 60, 74, 88):
        draw.line((0, s(y), s(28), s(y)), fill=(70, 97, 135, 160), width=max(1, s(0.28)))

    centered_text(draw, rect(0, 24, 9.33, 8.5), "K", FONT_KDA, TEXT_MAIN)
    centered_text(draw, rect(9.33, 24, 9.33, 8.5), "D", FONT_KDA, TEXT_MAIN)
    centered_text(draw, rect(18.66, 24, 9.34, 8.5), "A", FONT_KDA, TEXT_MAIN)
    centered_text(draw, rect(0, 34.5, 9.33, 9.5), "0", FONT_SMALL_NUMBER, TEXT_MUTED)
    centered_text(draw, rect(9.33, 34.5, 9.33, 9.5), "0", FONT_SMALL_NUMBER, TEXT_MUTED)
    centered_text(draw, rect(18.66, 34.5, 9.34, 9.5), "0", FONT_SMALL_NUMBER, TEXT_MUTED)

    sword_small = load_rotated_icon(ULTIMATE_ICONS / "Tools/Sword.png", SWORD_TINT, (s(6.0), s(3.4)), 90)
    shield_small = load_icon(ULTIMATE_ICONS / "Shield/Shield.png", SHIELD_TINT, (s(4.8), s(4.8)))
    heal_small = load_icon(ULTIMATE_ICONS / "Life/Health.png", HEAL_TINT, (s(4.2), s(4.2)))

    stat_icon_center_x = s(5.8)
    for icon, value, y_center_units in ((sword_small, "0", 53.0), (shield_small, "0", 66.5), (heal_small, "0", 80.0)):
        center_y = s(y_center_units)
        icon_x = stat_icon_center_x - (icon.size[0] // 2)
        card.alpha_composite(icon, (icon_x, center_y - (icon.size[1] // 2)))
        draw_text_right(draw, s(24), center_y, value, FONT_SMALL_NUMBER, TEXT_MUTED)

    # Portrait block
    portrait_slot = rect(33, 27, 33, 33)
    slot_image = fit(load_button("Button_Rectangle_01_Convex_Dark.Png"), portrait_slot[2] - portrait_slot[0], portrait_slot[3] - portrait_slot[1])
    card.alpha_composite(slot_image, (portrait_slot[0], portrait_slot[1]))
    portrait = compose_avatar((portrait_slot[2] - portrait_slot[0] - s(2), portrait_slot[3] - portrait_slot[1] - s(2)))
    card.alpha_composite(portrait, (portrait_slot[0] + s(1), portrait_slot[1] + s(1)))

    # Trait reserve
    trait_rows = [
        rect(70, 27.0, 69, 9.0),
        rect(70, 37.5, 69, 9.0),
        rect(70, 48.0, 69, 9.0),
    ]
    for index, trait_row in enumerate(trait_rows, start=1):
        trait_panel = fit(load_button("Button_Rectangle_01_Convex_Dark.Png"), trait_row[2] - trait_row[0], trait_row[3] - trait_row[1])
        card.alpha_composite(trait_panel, (trait_row[0], trait_row[1]))
        centered_text(draw, trait_row, f"特性 {index}", FONT_TRAIT, TEXT_DIM)

    # Core stats section
    bottom_box = rect(29, 62, 110, 26)
    draw_beveled_panel(
        card,
        bottom_box,
        fill=(14, 18, 27, 230),
        inner_fill=(17, 23, 34, 242),
        outline=(44, 61, 88, 170),
        shadow_alpha=70,
        bevel=s(1.8),
    )

    sword_large = load_rotated_icon(ULTIMATE_ICONS / "Tools/Sword.png", SWORD_TINT, (s(7.2), s(4.0)), 90)
    shield_large = load_icon(ULTIMATE_ICONS / "Shield/Shield.png", SHIELD_TINT, (s(5.8), s(5.8)))
    left_stat_center_x = bottom_box[0] + ((bottom_box[2] - bottom_box[0]) // 4)
    right_stat_center_x = bottom_box[0] + (((bottom_box[2] - bottom_box[0]) * 3) // 4)
    core_center_y = bottom_box[1] + ((bottom_box[3] - bottom_box[1]) // 2)
    draw_icon_value_group(card, draw, left_stat_center_x, core_center_y, sword_large, "42", FONT_CORE, TEXT_MAIN, gap=s(1.6))
    draw_icon_value_group(card, draw, right_stat_center_x, core_center_y, shield_large, "43", FONT_CORE, TEXT_MAIN, gap=s(1.6))

    # Light trim at the end of the panel after removing the old bottom portrait strip.
    draw.line((s(31), s(88), s(146), s(88)), fill=(255, 255, 255, 12), width=max(1, s(0.2)))

    card_path = OUT_DIR / f"{prefix}.png"
    preview_path = OUT_DIR / f"{prefix}_preview.png"
    card.save(card_path)
    make_preview(card).save(preview_path)
    return card_path, preview_path


def generate_runtime_base(prefix: str = "side_hero_sidebar_runtime_base") -> Path:
    RUNTIME_OUT_DIR.mkdir(parents=True, exist_ok=True)

    card = Image.new("RGBA", (CARD_WIDTH, CARD_HEIGHT), (0, 0, 0, 0))
    draw_beveled_panel(
        card,
        (0, 0, CARD_WIDTH - 1, CARD_HEIGHT - 1),
        fill=(16, 19, 27, 255),
        inner_fill=PANEL_DARK,
        outline=FRAME_OUTLINE,
        shadow_alpha=120,
        bevel=s(2.2),
    )

    header = fit(load_button("Button_Rectangle_01_Convex_Red.Png"), CARD_WIDTH, s(23))
    card.alpha_composite(header, (0, 0))

    draw = ImageDraw.Draw(card)

    left_tab = fit(load_button("Button_Rectangle_01_Convex_Dark.Png"), s(28), s(23))
    card.alpha_composite(left_tab, (0, 0))
    draw.rectangle(rect(0, 0, 28, 23), fill=(106, 48, 56, 82))
    draw.line((s(28), s(3), s(28), s(20)), fill=(84, 19, 22, 170), width=max(1, s(0.35)))
    draw_arrow_button(card, rect(121, 2.2, 18, 18))

    draw.line((s(28), s(23), s(28), s(88)), fill=BLUE_OUTLINE, width=max(1, s(0.4)))
    draw.line((s(29), s(62), s(139), s(62)), fill=(38, 52, 76, 200), width=max(1, s(0.35)))

    draw.rectangle(rect(0, 23, 28, 71), fill=(15, 20, 28, 220))
    for x in (0, 9.33, 18.66, 28):
        draw.line((s(x), s(23), s(x), s(46)), fill=(70, 97, 135, 170), width=max(1, s(0.28)))
    for y in (23, 34, 46, 60, 74, 88):
        draw.line((0, s(y), s(28), s(y)), fill=(70, 97, 135, 160), width=max(1, s(0.28)))

    sword_small = load_rotated_icon(ULTIMATE_ICONS / "Tools/Sword.png", SWORD_TINT, (s(6.0), s(3.4)), 90)
    shield_small = load_icon(ULTIMATE_ICONS / "Shield/Shield.png", SHIELD_TINT, (s(4.8), s(4.8)))
    heal_small = load_icon(ULTIMATE_ICONS / "Life/Health.png", HEAL_TINT, (s(4.2), s(4.2)))
    stat_icon_center_x = s(5.8)
    for icon, y_center_units in ((sword_small, 53.0), (shield_small, 66.5), (heal_small, 80.0)):
        center_y = s(y_center_units)
        icon_x = stat_icon_center_x - (icon.size[0] // 2)
        card.alpha_composite(icon, (icon_x, center_y - (icon.size[1] // 2)))

    portrait_slot = rect(33, 27, 33, 33)
    slot_image = fit(load_button("Button_Rectangle_01_Convex_Dark.Png"), portrait_slot[2] - portrait_slot[0], portrait_slot[3] - portrait_slot[1])
    card.alpha_composite(slot_image, (portrait_slot[0], portrait_slot[1]))

    trait_rows = [
        rect(70, 27.0, 69, 9.0),
        rect(70, 37.5, 69, 9.0),
        rect(70, 48.0, 69, 9.0),
    ]
    for trait_row in trait_rows:
        trait_panel = fit(load_button("Button_Rectangle_01_Convex_Dark.Png"), trait_row[2] - trait_row[0], trait_row[3] - trait_row[1])
        card.alpha_composite(trait_panel, (trait_row[0], trait_row[1]))

    bottom_box = rect(29, 62, 110, 26)
    draw_beveled_panel(
        card,
        bottom_box,
        fill=(14, 18, 27, 230),
        inner_fill=(17, 23, 34, 242),
        outline=(44, 61, 88, 170),
        shadow_alpha=70,
        bevel=s(1.8),
    )

    sword_large = load_rotated_icon(ULTIMATE_ICONS / "Tools/Sword.png", SWORD_TINT, (s(7.2), s(4.0)), 90)
    shield_large = load_icon(ULTIMATE_ICONS / "Shield/Shield.png", SHIELD_TINT, (s(5.8), s(5.8)))
    left_stat_center_x = bottom_box[0] + ((bottom_box[2] - bottom_box[0]) // 4)
    right_stat_center_x = bottom_box[0] + (((bottom_box[2] - bottom_box[0]) * 3) // 4)
    core_center_y = bottom_box[1] + ((bottom_box[3] - bottom_box[1]) // 2)
    card.alpha_composite(sword_large, (left_stat_center_x - sword_large.width - s(1.8), core_center_y - (sword_large.height // 2)))
    card.alpha_composite(shield_large, (right_stat_center_x - shield_large.width - s(1.8), core_center_y - (shield_large.height // 2)))

    draw.line((s(31), s(88), s(146), s(88)), fill=(255, 255, 255, 12), width=max(1, s(0.2)))

    card_path = RUNTIME_OUT_DIR / f"{prefix}.png"
    card.save(card_path)
    return card_path


def write_meta(path: Path) -> None:
    meta_path = path.with_suffix(path.suffix + ".meta")
    if meta_path.exists():
        return
    meta_path.write_text(META_TEMPLATE.format(guid=uuid.uuid4().hex), encoding="utf-8")


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(description="Generate a static single-hero sidebar mockup.")
    parser.add_argument(
        "--prefix",
        default="side_hero_sidebar_mockup_v8",
        help="Output filename prefix without extension.",
    )
    parser.add_argument(
        "--runtime-base",
        action="store_true",
        help="Export the textless runtime base used by the in-game sidebar HUD.",
    )
    parser.add_argument(
        "--runtime-prefix",
        default="side_hero_sidebar_runtime_base",
        help="Runtime base filename prefix without extension.",
    )
    return parser.parse_args()


def main() -> None:
    args = parse_args()
    card_path, preview_path = generate(prefix=args.prefix)
    write_meta(card_path)
    write_meta(preview_path)
    print(card_path)
    print(preview_path)

    if args.runtime_base:
        runtime_base_path = generate_runtime_base(prefix=args.runtime_prefix)
        write_meta(runtime_base_path)
        print(runtime_base_path)


if __name__ == "__main__":
    main()
