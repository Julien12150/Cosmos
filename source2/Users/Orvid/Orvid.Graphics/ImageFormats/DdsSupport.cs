﻿using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using Orvid.Graphics;
using au.id.micolous.libs.DDSReader;

namespace Orvid.Graphics.ImageFormats
{
    public class DdsImage : ImageFormat
    {
        public override void Save(Image i, Stream dest)
        {
            //byte[] b = DDS.Encode((System.Drawing.Bitmap)i, "dxt5");
            //dest.Write(b, 0, b.Length);
            //b = null;
        }

        public override Image Load(Stream s)
        {
            DDSImage im = new DDSImage(s);
            return (Image)im.BitmapImage;
        }
    }
}

#region Internals
// Please note, everything below this
// point was originally from 
// http://sourceforge.net/projects/igaeditor/
//
//
// The source has been modified for use in this library,
// the modifications include extending functionality.
//
// The extended functionality is licensed seperately from
// the rest of this file. Permission for it's use have been 
// granted only for use in Cosmos, and products created
// using the Cosmos toolkit. It is not allowed to be used
// in any other way, shape, or form.
// 
//
// This disclaimer and license was last
// modified on August 10, 2011.

namespace au.id.micolous.libs.DDSReader
{
    public class DDSImage
    {
        #region PixelFormat
        private enum PixelFormat
        {
            /// <summary>
            /// 32-bit image, with 8-bit red, green, blue and alpha.
            /// </summary>
            ARGB,
            /// <summary>
            /// 24-bit image with 8-bit red, green, blue.
            /// </summary>
            RGB,
            /// <summary>
            /// 16-bit DXT-1 compression, 1-bit alpha.
            /// </summary>
            DXT1,
            /// <summary>
            /// DXT-2 Compression
            /// </summary>
            DXT2,
            /// <summary>
            /// DXT-3 Compression
            /// </summary>
            DXT3,
            /// <summary>
            /// DXT-4 Compression
            /// </summary>
            DXT4,
            /// <summary>
            /// DXT-5 Compression
            /// </summary>
            DTX5,
            /// <summary>
            /// 3DC Compression
            /// </summary>
            THREEDC,
            /// <summary>
            /// ATI1n Compression
            /// </summary>
            ATI1N,
            LUMINANCE,
            LUMINANCE_ALPHA,
            RXGB,
            A16B16G16R16,
            R16F,
            G16R16F,
            A16B16G16R16F,
            R32F,
            G32R32F,
            A32B32G32R32F,
            /// <summary>
            /// Unknown pixel format.
            /// </summary>
            UNKNOWN
        }
        #endregion

        /*
         * This class is based on parts of DevIL.net, specifically;
         * /DevIL-1.6.8/src-IL/src/il_dds.c
         *
         * All ported to c#/.net.
         * 
         * http://msdn.microsoft.com/library/default.asp?url=/library/en-us/directx9_c/Opaque_and_1_Bit_Alpha_Textures.asp
         */

        private static byte[] DDS_HEADER = Convert.FromBase64String("RERTIA==");

        // fourccs
        private const uint FOURCC_DXT1 = 827611204;
        private const uint FOURCC_DXT2 = 844388420;
        private const uint FOURCC_DXT3 = 861165636;
        private const uint FOURCC_DXT4 = 877942852;
        private const uint FOURCC_DXT5 = 894720068;
        private const uint FOURCC_ATI1 = 826889281;
        private const uint FOURCC_ATI2 = 843666497;
        private const uint FOURCC_RXGB = 1111971922;
        private const uint FOURCC_DOLLARNULL = 36;
        private const uint FOURCC_oNULL = 111;
        private const uint FOURCC_pNULL = 112;
        private const uint FOURCC_qNULL = 113;
        private const uint FOURCC_rNULL = 114;
        private const uint FOURCC_sNULL = 115;
        private const uint FOURCC_tNULL = 116;


        // other defines
        private const uint DDS_LINEARSIZE = 524288;
        private const uint DDS_PITCH = 8;
        private const uint DDS_FOURCC = 4;
        private const uint DDS_LUMINANCE = 131072;
        private const uint DDS_ALPHAPIXELS = 1;

        // headers 
        // DDSURFACEDESC2 structure
        private byte[] signature;
        private uint size1;
        private uint flags1;
        private uint height;
        private uint width;
        private uint linearsize;
        private uint depth;
        private uint mipmapcount;
        private uint alphabitdepth;
        // DDPIXELFORMAT structure
        private uint size2;
        private uint flags2;
        private uint fourcc;
        private uint rgbbitcount;
        private uint rbitmask;
        private uint bbitmask;
        private uint gbitmask;
        private uint alphabitmask;

        // DDCAPS2 structure
        private uint ddscaps1;
        private uint ddscaps2;
        private uint ddscaps3;
        private uint ddscaps4;
        // end DDCAPS2 structure
        private uint texturestage;
        // end DDSURFACEDESC2 structure

        private PixelFormat CompFormat;
        private uint blocksize;

        private uint bpp;
        private uint bps;
        private uint sizeofplane;
        private uint compsize;
        private byte[] compdata;
        private byte[] rawidata;
        private BinaryReader br;
        private Image img;

        /// <summary>
        /// Returns a System.Imaging.Bitmap containing the DDS image.
        /// </summary>
        public Image BitmapImage { get { return this.img; } }

        /// <summary>
        /// Constructs a new DDSImage object using the given byte array, which
        /// contains the raw DDS file.
        /// </summary>
        /// <param name="ddsimage">A byte[] containing the DDS file.</param>
        public DDSImage(Stream ms)
        {
            br = new BinaryReader(ms);
            this.signature = br.ReadBytes(4);

            if (!IsByteArrayEqual(this.signature, DDS_HEADER))
            {
                System.Console.WriteLine("Got header of '" + ASCIIEncoding.ASCII.GetString(this.signature, 0, this.signature.Length) + "'.");

                throw new Exception("Not a dds File!");
            }

            //System.Console.WriteLine("Got dds header okay");

            // now read in the rest
            this.size1 = br.ReadUInt32();
            this.flags1 = br.ReadUInt32();
            this.height = br.ReadUInt32();
            this.width = br.ReadUInt32();
            this.linearsize = br.ReadUInt32();
            this.depth = br.ReadUInt32();
            this.mipmapcount = br.ReadUInt32();
            this.alphabitdepth = br.ReadUInt32();

            // skip next 10 uints
            for (int x = 0; x < 10; x++)
            {
                br.ReadUInt32();
            }

            this.size2 = br.ReadUInt32();
            this.flags2 = br.ReadUInt32();
            this.fourcc = br.ReadUInt32();
            this.rgbbitcount = br.ReadUInt32();
            this.rbitmask = br.ReadUInt32();
            this.gbitmask = br.ReadUInt32();
            this.bbitmask = br.ReadUInt32();
            this.alphabitmask = br.ReadUInt32();
            this.ddscaps1 = br.ReadUInt32();
            this.ddscaps2 = br.ReadUInt32();
            this.ddscaps3 = br.ReadUInt32();
            this.ddscaps4 = br.ReadUInt32();
            this.texturestage = br.ReadUInt32();


            // patches for stuff
            if (this.depth == 0)
            {
                this.depth = 1;
            }

            if ((this.flags2 & DDS_FOURCC) > 0)
            {
                blocksize = ((this.width + 3) / 4) * ((this.height + 3) / 4) * this.depth;

                switch (this.fourcc)
                {
                    case FOURCC_DXT1:
                        CompFormat = PixelFormat.DXT1;
                        blocksize *= 8;
                        break;

                    case FOURCC_DXT2:
                        CompFormat = PixelFormat.DXT2;
                        blocksize *= 16;
                        break;

                    case FOURCC_DXT3:
                        CompFormat = PixelFormat.DXT3;
                        blocksize *= 16;
                        break;

                    case FOURCC_DXT4:
                        CompFormat = PixelFormat.DXT4;
                        blocksize *= 16;
                        break;

                    case FOURCC_DXT5:
                        CompFormat = PixelFormat.DTX5;
                        blocksize *= 16;
                        break;

                    case FOURCC_ATI1:
                        CompFormat = PixelFormat.ATI1N;
                        blocksize *= 8;
                        break;

                    case FOURCC_ATI2:
                        CompFormat = PixelFormat.THREEDC;
                        blocksize *= 16;
                        break;

                    case FOURCC_RXGB:
                        CompFormat = PixelFormat.RXGB;
                        blocksize *= 16;
                        break;

                    case FOURCC_DOLLARNULL:
                        CompFormat = PixelFormat.A16B16G16R16;
                        blocksize = this.width * this.height * this.depth * 8;
                        break;

                    case FOURCC_oNULL:
                        CompFormat = PixelFormat.R16F;
                        blocksize = this.width * this.height * this.depth * 2;
                        break;

                    case FOURCC_pNULL:
                        CompFormat = PixelFormat.G16R16F;
                        blocksize = this.width * this.height * this.depth * 4;
                        break;

                    case FOURCC_qNULL:
                        CompFormat = PixelFormat.A16B16G16R16F;
                        blocksize = this.width * this.height * this.depth * 8;
                        break;

                    case FOURCC_rNULL:
                        CompFormat = PixelFormat.R32F;
                        blocksize = this.width * this.height * this.depth * 4;
                        break;

                    case FOURCC_sNULL:
                        CompFormat = PixelFormat.G32R32F;
                        blocksize = this.width * this.height * this.depth * 8;
                        break;

                    case FOURCC_tNULL:
                        CompFormat = PixelFormat.A32B32G32R32F;
                        blocksize = this.width * this.height * this.depth * 16;
                        break;

                    default:
                        CompFormat = PixelFormat.UNKNOWN;
                        blocksize *= 16;
                        break;
                } // switch
            }
            else
            {
                // uncompressed image
                if ((this.flags2 & DDS_LUMINANCE) > 0)
                {
                    if ((this.flags2 & DDS_ALPHAPIXELS) > 0)
                    {
                        CompFormat = PixelFormat.LUMINANCE_ALPHA;
                    }
                    else
                    {
                        CompFormat = PixelFormat.LUMINANCE;
                    }
                }
                else
                {
                    if ((this.flags2 & DDS_ALPHAPIXELS) > 0)
                    {
                        CompFormat = PixelFormat.ARGB;
                    }
                    else
                    {
                        CompFormat = PixelFormat.RGB;
                    }
                }

                blocksize = (this.width * this.height * this.depth * (this.rgbbitcount >> 3));
            }

            if (CompFormat == PixelFormat.UNKNOWN)
            {
                throw new Exception("Invalid Header Format!");
            }

            if ((this.flags1 & (DDS_LINEARSIZE | DDS_PITCH)) == 0
                || this.linearsize == 0)
            {
                this.flags1 |= DDS_LINEARSIZE;
                this.linearsize = blocksize;
            }


            // get image data
            this.ReadData();

            // allocate bitmap
            this.bpp = this.PixelFormatToBpp(this.CompFormat);
            this.bps = this.width * this.bpp * this.PixelFormatToBpc(this.CompFormat);
            this.sizeofplane = this.bps * this.height;
            this.rawidata = new byte[this.depth * this.sizeofplane + this.height * this.bps + this.width * this.bpp];

            // decompress
            switch (this.CompFormat)
            {
                case PixelFormat.ARGB:
                case PixelFormat.RGB:
                case PixelFormat.LUMINANCE:
                case PixelFormat.LUMINANCE_ALPHA:
                    this.DecompressARGB();
                    break;

                case PixelFormat.DXT1:
                    this.DecompressDXT1();
                    break;

                case PixelFormat.DXT3:
                    this.DecompressDXT3();
                    break;

                case PixelFormat.DTX5:
                    this.DecompressDXT5();
                    break;

                default:
                    //throw new Exception("Unknown file format!");
                    break;
            }

            this.img = new Image((int)this.width, (int)this.height);

            // now fill bitmap with raw image datas.

            for (int y = 0; y < this.height; y++)
            {
                for (int x = 0; x < this.width; x++)
                {
                    // draw
                    ulong pos = (ulong)(((y * this.width) + x) * 4);
                    this.img.SetPixel((uint)x, (uint)y, new Pixel(this.rawidata[pos], this.rawidata[pos + 1], this.rawidata[pos + 2], this.rawidata[pos + 3]));
                }
            }


            // cleanup
            this.rawidata = null;
            this.compdata = null;

        }

        private static bool IsByteArrayEqual(byte[] arg0, byte[] arg1)
        {
            if (arg0.Length != arg1.Length)
            {
                return false;
            }

            for (int x = 0; x < arg0.Length; x++)
            {
                if (arg0[x] != arg1[x])
                {
                    return false;
                }
            }

            return true;
        }

        // iCompFormatToBpp
        private uint PixelFormatToBpp(PixelFormat pf)
        {
            switch (pf)
            {
                case PixelFormat.LUMINANCE:
                case PixelFormat.LUMINANCE_ALPHA:
                case PixelFormat.ARGB:
                    return this.rgbbitcount / 8;

                case PixelFormat.RGB:
                case PixelFormat.THREEDC:
                case PixelFormat.RXGB:
                    return 3;

                case PixelFormat.ATI1N:
                    return 1;

                case PixelFormat.R16F:
                    return 2;

                case PixelFormat.A16B16G16R16:
                case PixelFormat.A16B16G16R16F:
                case PixelFormat.G32R32F:
                    return 8;

                case PixelFormat.A32B32G32R32F:
                    return 16;

                default:
                    return 4;
            }
        }

        // iCompFormatToBpc
        private uint PixelFormatToBpc(PixelFormat pf)
        {
            switch (pf)
            {
                case PixelFormat.R16F:
                case PixelFormat.G16R16F:
                case PixelFormat.A16B16G16R16F:
                    return 4;

                case PixelFormat.R32F:
                case PixelFormat.G32R32F:
                case PixelFormat.A32B32G32R32F:
                    return 4;

                case PixelFormat.A16B16G16R16:
                    return 2;

                default:
                    return 1;
            }
        }

        // iCompFormatToChannelCount
        private uint PixelFormatToChannelCount(PixelFormat pf)
        {
            switch (pf)
            {
                case PixelFormat.RGB:
                case PixelFormat.THREEDC:
                case PixelFormat.RXGB:
                    return 3;

                case PixelFormat.LUMINANCE:
                case PixelFormat.R16F:
                case PixelFormat.R32F:
                case PixelFormat.ATI1N:
                    return 1;

                case PixelFormat.LUMINANCE_ALPHA:
                case PixelFormat.G16R16F:
                case PixelFormat.G32R32F:
                    return 2;

                default:
                    return 4;
            }
        }

        private void ReadData()
        {
            this.compdata = null;

            if ((this.flags1 & DDS_LINEARSIZE) > 1)
            {
                this.compdata = this.br.ReadBytes((int)this.linearsize);
                this.compsize = (uint)this.compdata.Length;
            }
            else
            {
                uint bps = this.width * this.rgbbitcount / 8;
                this.compsize = bps * this.height * this.depth;
                this.compdata = new byte[this.compsize];

                MemoryStream mem = new MemoryStream((int)this.compsize);


                byte[] temp;
                for (int z = 0; z < this.depth; z++)
                {
                    for (int y = 0; y < this.height; y++)
                    {
                        temp = this.br.ReadBytes((int)this.bps);
                        mem.Write(temp, 0, temp.Length);
                    }
                }
                mem.Seek(0, SeekOrigin.Begin);

                mem.Read(this.compdata, 0, this.compdata.Length);
                mem.Close();
            }
        }

        private void DecompressARGB()
        {
            // not done
            //throw new Exception("Un-compressed images not yet supported!");
        }

        #region Dxt1
        private void DecompressDXT1()
        {
            // DXT1 decompressor
            Pixel[] colours = new Pixel[4];
            ushort colour0, colour1;
            uint bitmask, offset;
            int i, j, k, x, y, z, Select;


            MemoryStream mem = new MemoryStream(this.compdata.Length);
            mem.Write(this.compdata, 0, this.compdata.Length);
            mem.Seek(0, SeekOrigin.Begin);
            BinaryReader r = new BinaryReader(mem);

            colours[0].A = 255;
            colours[1].A = 255;
            colours[2].A = 255;

            for (z = 0; z < this.depth; z++)
            {
                for (y = 0; y < this.height; y += 4)
                {
                    for (x = 0; x < this.width; x += 4)
                    {
                        colour0 = r.ReadUInt16();
                        colour1 = r.ReadUInt16();

                        this.ReadColour(colour0, ref colours[0]);
                        this.ReadColour(colour1, ref colours[1]);

                        bitmask = r.ReadUInt32();

                        if (colour0 > colour1)
                        {
                            // Four-color block: derive the other two colors.
                            // 00 = color_0, 01 = color_1, 10 = color_2, 11 = color_3
                            // These 2-bit codes correspond to the 2-bit fields
                            // stored in the 64-bit block.
                            colours[2].B = (byte)((2 * colours[0].B + colours[1].B + 1) / 3);
                            colours[2].G = (byte)((2 * colours[0].G + colours[1].G + 1) / 3);
                            colours[2].R = (byte)((2 * colours[0].R + colours[1].R + 1) / 3);

                            colours[3].B = (byte)((colours[0].B + 2 * colours[1].B + 1) / 3);
                            colours[3].G = (byte)((colours[0].G + 2 * colours[1].G + 1) / 3);
                            colours[3].R = (byte)((colours[0].R + 2 * colours[1].R + 1) / 3);
                            colours[3].A = 0xFF;
                        }
                        else
                        {
                            // Three-color block: derive the other color.
                            // 00 = color_0,  01 = color_1,  10 = color_2,
                            // 11 = transparent.
                            // These 2-bit codes correspond to the 2-bit fields 
                            // stored in the 64-bit block. 
                            colours[2].B = (byte)((colours[0].B + colours[1].B) / 2);
                            colours[2].G = (byte)((colours[0].G + colours[1].G) / 2);
                            colours[2].R = (byte)((colours[0].R + colours[1].R) / 2);

                            colours[3].B = 0;
                            colours[3].G = 0;
                            colours[3].R = 0;
                            colours[3].A = 0;
                        }


                        for (j = 0, k = 0; j < 4; j++)
                        {
                            for (i = 0; i < 4; i++, k++)
                            {
                                Select = (int)((bitmask & (0x03 << k * 2)) >> k * 2);
                                if (((x + i) < this.width) && ((y + j) < this.height))
                                {
                                    offset = (uint)(z * this.sizeofplane + (y + j) * this.bps + (x + i) * this.bpp);
                                    this.rawidata[offset] = (byte)colours[Select].R;
                                    this.rawidata[offset + 1] = (byte)colours[Select].G;
                                    this.rawidata[offset + 2] = (byte)colours[Select].B;
                                    this.rawidata[offset + 3] = (byte)colours[Select].A;
                                }
                            }
                        }
                    }
                }
            }
        }
        #endregion

        #region Dxt3
        private void DecompressDXT3()
        {
            Pixel[] colours = new Pixel[4];
            uint bitmask, offset;
            int i, j, k, x, y, z, Select;
            ushort word, colour0, colour1;
            byte[] alpha; //temp;

            MemoryStream mem = new MemoryStream(this.compdata.Length);
            mem.Write(this.compdata, 0, this.compdata.Length);
            mem.Seek(0, SeekOrigin.Begin);
            BinaryReader r = new BinaryReader(mem);

            for (z = 0; z < this.depth; z++)
            {
                for (y = 0; y < this.height; y += 4)
                {
                    for (x = 0; x < this.width; x += 4)
                    {
                        alpha = r.ReadBytes(8);

                        colour0 = r.ReadUInt16();
                        colour1 = r.ReadUInt16();
                        this.ReadColour(colour0, ref colours[0]);
                        this.ReadColour(colour1, ref colours[1]);

                        bitmask = r.ReadUInt32();

                        colours[2].B = (byte)((2 * colours[0].B + colours[1].B + 1) / 3);
                        colours[2].G = (byte)((2 * colours[0].G + colours[1].G + 1) / 3);
                        colours[2].R = (byte)((2 * colours[0].R + colours[1].R + 1) / 3);

                        colours[3].B = (byte)((colours[0].B + 2 * colours[1].B + 1) / 3);
                        colours[3].G = (byte)((colours[0].G + 2 * colours[1].G + 1) / 3);
                        colours[3].R = (byte)((colours[0].R + 2 * colours[1].R + 1) / 3);

                        for (j = 0, k = 0; j < 4; j++)
                        {
                            for (i = 0; i < 4; k++, i++)
                            {
                                Select = (int)((bitmask & (0x03 << k * 2)) >> k * 2);

                                if (((x + i) < this.width) && ((y + j) < this.height))
                                {
                                    offset = (uint)(z * this.sizeofplane + (y + j) * this.bps + (x + i) * this.bpp);
                                    this.rawidata[offset] = (byte)colours[Select].R;
                                    this.rawidata[offset + 1] = (byte)colours[Select].G;
                                    this.rawidata[offset + 2] = (byte)colours[Select].B;
                                }
                            }
                        }

                        for (j = 0; j < 4; j++)
                        {
                            word = (ushort)(alpha[2 * j] + 256 * alpha[2 * j + 1]);
                            for (i = 0; i < 4; i++)
                            {
                                if (((x + i) < this.width) && ((y + j) < this.height))
                                {
                                    offset = (uint)(z * this.sizeofplane + (y + j) * this.bps + (x + i) * this.bpp + 3);
                                    this.rawidata[offset] = (byte)(word & 0x0F);
                                    this.rawidata[offset] = (byte)(this.rawidata[offset] | (this.rawidata[offset] << 4));
                                }
                                word >>= 4;
                            }
                        }
                    }
                }
            }
        }
        #endregion

        #region Dxt5
        private void DecompressDXT5()
        {
            uint x, y, z, i, j, Select, bitmask, Offset, bits;
            Pixel[] Colors = new Pixel[4];
            byte[] Alphas = new byte[8];
            byte[] alphaMask = new byte[6];
            int k;

            MemoryStream mem = new MemoryStream(this.compdata.Length);
            mem.Write(this.compdata, 0, this.compdata.Length);
            mem.Seek(0, SeekOrigin.Begin);
            BinaryReader r = new BinaryReader(mem);
            for (z = 0; z < this.depth; z++)
            {
                for (y = 0; y < this.height; y += 4)
                {
                    for (x = 0; x < this.width; x += 4)
                    {
                        if (y >= this.height || x >= this.width)
                            break;
                        Alphas[0] = r.ReadByte();
                        Alphas[1] = r.ReadByte();
                        alphaMask = r.ReadBytes(6);

                        ReadColour(r.ReadUInt16(), ref Colors[0]);
                        ReadColour(r.ReadUInt16(), ref Colors[1]);
                        bitmask = r.ReadUInt32();

                        // Four-color block: derive the other two colors.    
                        // 00 = color_0, 01 = color_1, 10 = color_2, 11 = color_3
                        // These 2-bit codes correspond to the 2-bit fields 
                        // stored in the 64-bit block.
                        Colors[2].B = (byte)((2 * Colors[0].B + Colors[1].B + 1) / 3);
                        Colors[2].G = (byte)((2 * Colors[0].G + Colors[1].G + 1) / 3);
                        Colors[2].R = (byte)((2 * Colors[0].R + Colors[1].R + 1) / 3);

                        Colors[3].B = (byte)((Colors[0].B + 2 * Colors[1].B + 1) / 3);
                        Colors[3].G = (byte)((Colors[0].G + 2 * Colors[1].G + 1) / 3);
                        Colors[3].R = (byte)((Colors[0].R + 2 * Colors[1].R + 1) / 3);

                        k = 0;
                        for (j = 0; j < 4; j++)
                        {
                            for (i = 0; i < 4; i++, k++)
                            {

                                Select = (uint)((bitmask & (0x03 << k * 2)) >> k * 2);

                                // only put pixels out < width or height
                                if (((x + i) < this.width) && ((y + j) < this.height))
                                {
                                    Offset = z * this.sizeofplane + (y + j) * this.bps + (x + i) * this.bpp;
                                    this.rawidata[Offset + 0] = Colors[Select].R;
                                    this.rawidata[Offset + 1] = Colors[Select].G;
                                    this.rawidata[Offset + 2] = Colors[Select].B;
                                }
                            }
                        }

                        // 8-alpha or 6-alpha block?    
                        if (Alphas[0] > Alphas[1])
                        {
                            // 8-alpha block:  derive the other six alphas.    
                            // Bit code 000 = alpha_0, 001 = alpha_1, others are interpolated.
                            Alphas[2] = (byte)((6 * Alphas[0] + 1 * Alphas[1] + 3) / 7);	// bit code 010
                            Alphas[3] = (byte)((5 * Alphas[0] + 2 * Alphas[1] + 3) / 7);	// bit code 011
                            Alphas[4] = (byte)((4 * Alphas[0] + 3 * Alphas[1] + 3) / 7);	// bit code 100
                            Alphas[5] = (byte)((3 * Alphas[0] + 4 * Alphas[1] + 3) / 7);	// bit code 101
                            Alphas[6] = (byte)((2 * Alphas[0] + 5 * Alphas[1] + 3) / 7);	// bit code 110
                            Alphas[7] = (byte)((1 * Alphas[0] + 6 * Alphas[1] + 3) / 7);	// bit code 111
                        }
                        else
                        {
                            // 6-alpha block.
                            // Bit code 000 = alpha_0, 001 = alpha_1, others are interpolated.
                            Alphas[2] = (byte)((4 * Alphas[0] + 1 * Alphas[1] + 2) / 5);	// Bit code 010
                            Alphas[3] = (byte)((3 * Alphas[0] + 2 * Alphas[1] + 2) / 5);	// Bit code 011
                            Alphas[4] = (byte)((2 * Alphas[0] + 3 * Alphas[1] + 2) / 5);	// Bit code 100
                            Alphas[5] = (byte)((1 * Alphas[0] + 4 * Alphas[1] + 2) / 5);	// Bit code 101
                            Alphas[6] = 0x00;										// Bit code 110
                            Alphas[7] = 0xFF;										// Bit code 111
                        }

                        // Note: Have to separate the next two loops,
                        //	it operates on a 6-byte system.

                        // First three bytes
                        //bits = *((ILint*)alphamask);
                        bits = (uint)(alphaMask[0]) | (uint)(alphaMask[1] << 8) | (uint)(alphaMask[2] << 16);
                        for (j = 0; j < 2; j++)
                        {
                            for (i = 0; i < 4; i++)
                            {
                                // only put pixels out < width or height
                                if (((x + i) < this.width) && ((y + j) < this.height))
                                {
                                    Offset = z * this.sizeofplane + (y + j) * this.bps + (x + i) * this.bpp + 3;
                                    this.rawidata[Offset] = Alphas[bits & 0x07];
                                }
                                bits >>= 3;
                            }
                        }

                        // Last three bytes
                        //bits = *((ILint*)&alphamask[3]);
                        bits = (uint)(alphaMask[3]) | (uint)(alphaMask[4] << 8) | (uint)(alphaMask[5] << 16);
                        for (j = 2; j < 4; j++)
                        {
                            for (i = 0; i < 4; i++)
                            {
                                // only put pixels out < width or height
                                if (((x + i) < this.width) && ((y + j) < this.height))
                                {
                                    Offset = z * this.sizeofplane + (y + j) * this.bps + (x + i) * this.bpp + 3;
                                    this.rawidata[Offset] = Alphas[bits & 0x07];
                                }
                                bits >>= 3;
                            }
                        }
                    }
                }
            }

        }
        #endregion


        private void ReadColour(ushort Data, ref Pixel op)
        {
            byte r, g, b;

            b = (byte)(Data & 0x1f);
            g = (byte)((Data & 0x7E0) >> 5);
            r = (byte)((Data & 0xF800) >> 11);

            op.R = (byte)(r * 255 / 31);
            op.G = (byte)(g * 255 / 63);
            op.B = (byte)(b * 255 / 31);
        }
    }
}

#endregion