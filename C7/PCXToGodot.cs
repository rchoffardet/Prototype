using Godot;
using System;
using ConvertCiv3Media;

public partial class PCXToGodot : GodotObject
{
	private const byte SHADOW_START_INDEX = 224;
	private const byte CIVCOLOR_START_INDEX = 239;
	private const byte TRANSPARENCY_START_INDEX = 254;
	private const byte SHADOW_INDEX_RANGE = CIVCOLOR_START_INDEX - SHADOW_START_INDEX;
	private const ushort MAX_PALETTE_SIZE = 256;

	private const byte GREEN_BITSHIFT = 8;
	private const byte BLUE_BITSHIFT = 16;
	private const byte ALPHA_BITSHIFT = 24;
	private const byte MAX_COLOR = 255;

	public static ImageTexture getImageTextureFromPCX(Pcx pcx) {
		Image ImgTxtr = ByteArrayToImage(pcx.ColorIndices, pcx.Palette, pcx.Width, pcx.Height);
		return getImageTextureFromImage(ImgTxtr);
	}

	public static ImageTexture getImageTextureFromPCX(Pcx pcx, int leftStart, int topStart, int croppedWidth, int croppedHeight, bool shadows = true) {
		Image image = getImageFromPCX(pcx, leftStart, topStart, croppedWidth, croppedHeight, shadows);
		return getImageTextureFromImage(image);
	}

	/**
	 * This method is for cases where we want to use components of multiple PCXs in a texture, such as for the popup background.
	 **/
	public static Image getImageFromPCX(Pcx pcx, int leftStart, int topStart, int croppedWidth, int croppedHeight, bool shadows = true) {
		int[] ColorData = loadPalette(pcx.Palette, shadows);
		int[] BufferData = new int[croppedWidth * croppedHeight];

		int DataIndex = 0;

		for (int y = topStart; y < topStart + croppedHeight; y++) {
			for (int x = leftStart; x < leftStart + croppedWidth; x++) {
				BufferData[DataIndex] = ColorData[pcx.ColorIndexAt(x, y)];
				DataIndex++;
			}
		}

		return getImageFromBufferData(croppedWidth, croppedHeight, BufferData);
	}

	public static ImageTexture getPureAlphaFromPCX(Pcx alphaPcx) {
		int[] bufferData = new int[alphaPcx.Width * alphaPcx.Height];
		int[] alphaData = new int[MAX_PALETTE_SIZE];
		for (int i = 0; i < MAX_PALETTE_SIZE; i++) {
			alphaData[i] = alphaPcx.Palette[i, 0];
		}
		int dataIndex = 0;
		for (int y = 0; y < alphaPcx.Height; y++) {
			for (int x = 0; x < alphaPcx.Width; x++, dataIndex++) {
				int index = alphaPcx.ColorIndexAt(x, y);
				if (index >= TRANSPARENCY_START_INDEX) {
					bufferData[dataIndex] = 0;
				} else {
					bufferData[dataIndex] = alphaData[index] << 24;
				}
			}
		}

		Image outImage = getImageFromBufferData(alphaPcx.Width, alphaPcx.Height, bufferData);
		return getImageTextureFromImage(outImage);
	}

	public static ImageTexture getImageFromPCXWithAlphaBlend(Pcx imagePcx, Pcx alphaPcx) {
		return getImageFromPCXWithAlphaBlend(imagePcx, alphaPcx, 0, 0, imagePcx.Width, imagePcx.Height);
	}

	//Combines two PCXs, one used for the alpha, to produce a final output image.
	//Some files, such as Art/interface/menuButtons.pcx and Art/interface/menuButtonsAlpha.pcx, use this method.
	public static ImageTexture getImageFromPCXWithAlphaBlend(Pcx imagePcx, Pcx alphaPcx, int leftStart, int topStart, int croppedWidth, int croppedHeight, int alphaRowOffset = 0) {
		int[] ColorData = loadPalette(imagePcx.Palette, false);
		int[] AlphaData = loadAlphaPalette(alphaPcx.Palette, ColorData);
		int[] BufferData = new int[croppedWidth * croppedHeight];

		int AlphaIndex;
		int DataIndex = 0;

		for (int y = topStart; y < topStart + croppedHeight; y++) {
			AlphaIndex = (y - alphaRowOffset) * imagePcx.Width + leftStart;
			for (int x = leftStart; x < leftStart + croppedWidth; x++) {
				BufferData[DataIndex] = ColorData[imagePcx.ColorIndexAt(x, y)] | AlphaData[alphaPcx.ColorIndices[AlphaIndex]];
				DataIndex++;
				AlphaIndex++;
			}
		}

		Image OutImage = getImageFromBufferData(croppedWidth, croppedHeight, BufferData);
		return getImageTextureFromImage(OutImage);
	}

	public static Image ByteArrayToImage(byte[] colorIndices, byte[,] palette, int width, int height, int[] transparent = null, bool shadows = false) {
		int[] ColorData = loadPalette(palette, shadows);
		int[] BufferData = new int[width * height];

		for (int i = 0; i < width * height; i++) {
			BufferData[i] = ColorData[colorIndices[i]];
		}

		return getImageFromBufferData(width, height, BufferData);
	}

	// ByteArrayWithTintToImage is used to load create images from flic frames
	// that contain a tinted layer such as unit animations, where the unit's
	// clothing is tinted by their civ color.
	public static (Image, Image) ByteArrayWithTintToImage(byte[] colorIndices, byte[,] palette, int width, int height, int[] transparent = null, bool shadows = false) {
		int[] colorData = loadPalette(palette, shadows);
		int[] baseLayer = new int[width * height];
		int[] tintLayer = new int[width * height];

		Pcx whitePcx = Util.LoadPCX("Art/Units/Palettes/ntp00.pcx");
		int[] whiteColorData = loadPalette(whitePcx.Palette, true);

		for (int i = 0; i < width * height; i++) {
			int index = colorIndices[i];
			bool tinted = index < 16 || (index < 64 && index % 2 == 0);
			bool shadow = index >= SHADOW_START_INDEX && index <= CIVCOLOR_START_INDEX;
			if (tinted) {
				tintLayer[i] = whiteColorData[index];
				baseLayer[i] = 0; // transparent
			} else if (shadow) {
				// shadow belongs to the base texture
				baseLayer[i] = ((int)new Color(1.0f, 1.0f, 1.0f, (float)(index - SHADOW_START_INDEX) / SHADOW_INDEX_RANGE).ToArgb32());
				tintLayer[i] = 0; // transparent
			} else {
				baseLayer[i] = colorData[index];
				tintLayer[i] = 0; // transparent
			}
		}
		return (getImageFromBufferData(width, height, baseLayer), getImageFromBufferData(width, height, tintLayer));
	}

	// Utility for loading a civilization color from an ntp file
	// Note that this retrieves the color of the (only) pixel, not a particular palette index.
	public static Color GetColorFromPCX(Pcx pcx) {
		int paletteLookupIdx = pcx.ColorIndexAt(0, 0);

		return Color.Color8(
			pcx.Palette[paletteLookupIdx, 0],
			pcx.Palette[paletteLookupIdx, 1],
			pcx.Palette[paletteLookupIdx, 2]
		);
	}

	private static Image getImageFromBufferData(int width, int height, int[] bufferData) {
		byte[] Data = new byte[4 * width * height];
		Buffer.BlockCopy(bufferData, 0, Data, 0, 4 * width * height);
		Image image = Image.CreateFromData(width, height, false, Image.Format.Rgba8, Data);
		return image;
	}

	private static ImageTexture getImageTextureFromImage(Image image) {
		return ImageTexture.CreateFromImage(image);
	}

	private static int[] loadPalette(byte[,] palette, bool shadows) {
		int Red, Green, Blue;
		int Alpha = MAX_COLOR << ALPHA_BITSHIFT;
		int[] ColorData = new int[MAX_PALETTE_SIZE];

		for (int i = 0; i < MAX_PALETTE_SIZE; i++) {
			Red = palette[i, 0];
			Green = palette[i, 1] << GREEN_BITSHIFT;
			Blue = palette[i, 2] << BLUE_BITSHIFT;
			ColorData[i] = Red + Green + Blue + Alpha;
		}

		for (int i = TRANSPARENCY_START_INDEX; i < MAX_PALETTE_SIZE; i++) {
			ColorData[i] &= 0x00ffffff;
		}

		if (shadows) {
			for (int i = SHADOW_START_INDEX+1; i < MAX_PALETTE_SIZE; i++) {
				ColorData[i] = ((MAX_COLOR - i) * 16) << ALPHA_BITSHIFT;
			}
		}

		return ColorData;
	}

	private static int[] loadAlphaPalette(byte[,] palette, int[] ColorData) {
		int[] AlphaData = new int[MAX_PALETTE_SIZE];

		for (int i = 0; i < MAX_PALETTE_SIZE; i++) {
			// Assumption based on menuButtonsAlpha.pcx: The palette in the alpha PCX always has the same red, green, and blue values (i.e. is grayscale).
			// Examining it with breakpoints in my Java code, it appears it starts at 255, 255, 255, and goes down one at a time.  But this code
			// doesn't assume that, it only assumes the grayscale aspect.  In theory, this should work for any transparency, 0 to 255.
			AlphaData[i] = palette[i, 0] << 24;
			ColorData[i] = ColorData[i] &= 0x00ffffff;
		}

		return AlphaData;
	}
}
