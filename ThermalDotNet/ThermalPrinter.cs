using System;
using System.Drawing;
using System.IO;
using System.IO.Ports;
using System.Text;

namespace ThermalDotNet
{
	public class ThermalPrinter
	{	
		private SerialPort _serialPort;
		private byte _maxPrintingDots = 7;
		private byte _heatingTime = 80;
		private byte _heatingInterval = 2;
		
		/// <summary>
		/// Delay between two picture lines. (in ms)
		/// </summary>
		public int PictureLineSleepTimeMs = 20;
		/// <summary>
		/// Delay between two text lines. (in ms)
		/// </summary>
		public int WriteLineSleepTimeMs = 0;
		public string Encoding		{ get; private set; }

		public ThermalPrinter(SerialPort serialPort, byte maxPrintingDots, byte heatingTime, byte heatingInterval)
		{
			_constructor(serialPort,maxPrintingDots,heatingTime,heatingInterval);
		}
		
		public ThermalPrinter(SerialPort serialPort)
		{
			_constructor(serialPort,7,80,2);
		}
		
		private void _constructor(SerialPort serialPort, byte maxPrintingDots, byte heatingTime, byte heatingInterval)
		{
			this.Encoding = "ibm850";
			
			if (maxPrintingDots > 0) {
				_maxPrintingDots = maxPrintingDots;
			}
			if (heatingTime > 0) {
				_heatingTime = heatingTime;
			}
			if (_heatingInterval > 0) {
				_heatingInterval = heatingInterval;
			}
			
			_serialPort = serialPort;
			Reset();
			SetPrintingParameters(maxPrintingDots,heatingTime,heatingInterval);
			_sendEncoding(this.Encoding);
		}
		
		/// <summary>
		/// Prints the line of text.
		/// </summary>
		/// <param name='text'>
		/// Text to print.
		/// </param>
		public void WriteLine(string text)
		{
			WriteToBuffer(text);
			_writeByte(10);
			System.Threading.Thread.Sleep(WriteLineSleepTimeMs);
		}
		/// <summary>
		/// Writes the text to the printer buffer. Does not print until a line feed (0x10) is sent.
		/// </summary>
		/// <param name='text'>
		/// Text to print.
		/// </param>
		public void WriteToBuffer(string text)
		{
			text = text.Trim('\n').Trim('\r');
			byte[] originalBytes = System.Text.Encoding.UTF8.GetBytes(text);
			byte[] outputBytes = System.Text.Encoding.Convert(System.Text.Encoding.UTF8,System.Text.Encoding.GetEncoding(this.Encoding),originalBytes);
			_serialPort.Write(outputBytes,0,outputBytes.Length);
		}
		
		public void WriteLine_Invert(string text)
		{
			//Sets inversion on
			_writeByte(29);
			_writeByte(66);
			_writeByte(1);
			
			//Sends the text
			WriteLine(text);
			
			//Sets inversion off
			_writeByte(29);
			_writeByte(66);
			_writeByte(0);
			
			LineFeed();
		}
		
		public void WriteLine_Big(string text)
		{
			const byte DoubleHeight = 1 << 4;
			const byte DoubleWidth = 1 << 5;
			const byte Bold = 1 << 3;
			
			//big on
			_writeByte(27);
			_writeByte(33);
			_writeByte(DoubleHeight + DoubleWidth + Bold);
			
			//Sends the text
			WriteLine(text);
			
			//big off
			_writeByte(27);
			_writeByte(33);
			_writeByte(0);
		}
	
		/// <summary>
		/// Writes the text to the printer buffer. Does not print until a line feed (0x10) is sent.
		/// </summary>
		/// <param name='text'>
		/// Text to print.
		/// </param>
		/// <param name='style'>
		/// Style of the text.
		/// </param> 
		public void WriteLine(string text, PrintingStyle style)
		{
			WriteLine(text,(byte)style);
		}
		
		/// <summary>
		/// Writes the text to the printer buffer. Does not print until a line feed (0x10) is sent.
		/// </summary>
		/// <param name='text'>
		/// Text to print.
		/// </param>
		/// <param name='style'>
		/// Style of the text. Can be the sum of PrintingStyle enums.
		/// </param> 		
		public void WriteLine(string text, byte style)
		{
			//style on
			_writeByte(27);
			_writeByte(33);
			_writeByte((byte)style);
			
			//Sends the text
			WriteLine(text);
			
			//style off
			_writeByte(27);
			_writeByte(33);
			_writeByte(0);
		}
		
		public void WriteLine_Bold(string text)
		{
			//bold on
			BoldOn();
			
			//Sends the text
			WriteLine(text);
			
			//bold off
			BoldOff();
			
			LineFeed();
		}
		
		public void BoldOn()
		{
			_writeByte(27);
			_writeByte(32);
			_writeByte(1);
			_writeByte(27);
			_writeByte(69);
			_writeByte(1);
		}
		
		public void BoldOff()
		{
			_writeByte(27);
			_writeByte(32);
			_writeByte(0);
			_writeByte(27);
			_writeByte(69);
			_writeByte(0);
		}
		
		public void SetSize(bool doubleWidth, bool doubleHeight)
		{
			int sizeValue = (Convert.ToInt32(doubleWidth))*(0xF0) + (Convert.ToInt32(doubleHeight))*(0x0F);
			_writeByte(29);
			_writeByte(33);
			_writeByte((byte)sizeValue);
		}
		
		public void LineFeed()
		{
			_writeByte(10);
		}
		
		public void LineFeed(byte lines)
		{
			_writeByte(27);
			_writeByte(100);
			_writeByte(lines);
		}
		
		public void Ident(byte columns)
		{
			if (columns < 0 || columns > 31) {
				columns = 0;
			}
			
			_writeByte(27);
			_writeByte(66);
			_writeByte(columns);
		}
		
		/// <summary>
		/// Sets the line spacing.
		/// </summary>
		/// <param name='lineSpacing'>
		/// Line spacing (in dots), default value: 32 dots.
		/// </param>
		public void SetLineSpacing(byte lineSpacing)
		{
			_writeByte(27);
			_writeByte(51);
			_writeByte(lineSpacing);
		}
		
		public void SetAlignLeft()
		{
			_writeByte(27);
			_writeByte(97);
			_writeByte(0);
		}
		
		public void SetAlignCenter()
		{
			_writeByte(27);
			_writeByte(97);
			_writeByte(1);
		}
		
		public void SetAlignRight()
		{
			_writeByte(27);
			_writeByte(97);
			_writeByte(2);
		}
		
		public void HorizontalLine(int length)
		{
			if (length > 0) {
				if (length > 32) {
					length = 32;
				}
				
				for (int i = 0; i < length; i++) {
					_writeByte(0xC4);
				}
			_writeByte(10);
			}
		}
		
		public void Reset()
		{
			_writeByte(27);
			_writeByte(64);	
			System.Threading.Thread.Sleep(50);
		}
		
		public enum BarcodeType
		{
			upc_a = 0,
			upc_e = 1,
			ean13 = 2,
			ean8 = 3,
			code39 = 4,
			i25 = 5,
			codebar = 6,
			code93 = 7,
			code128 = 8,
			code11 = 9,
			msi = 10
		}
		
		public void PrintBarcode(BarcodeType type, string data)
		{
			byte[] originalBytes;
			byte[] outputBytes;
			
			if (type == BarcodeType.code93 || type == BarcodeType.code128)
			{
				originalBytes = System.Text.Encoding.UTF8.GetBytes(data);
				outputBytes = originalBytes;
			} else {
				originalBytes = System.Text.Encoding.UTF8.GetBytes(data.ToUpper());
				outputBytes = System.Text.Encoding.Convert(System.Text.Encoding.UTF8,System.Text.Encoding.GetEncoding(this.Encoding),originalBytes);
			}
			
			switch (type) {
			case BarcodeType.upc_a:
				if (data.Length ==  11 || data.Length ==  12) {
					_writeByte(29);
					_writeByte(107);
					_writeByte(0);
					_serialPort.Write(outputBytes,0,data.Length);
					_writeByte(0);
				}
				break;
			case BarcodeType.upc_e:
				if (data.Length ==  11 || data.Length ==  12) {
					_writeByte(29);
					_writeByte(107);
					_writeByte(1);
					_serialPort.Write(outputBytes,0,data.Length);
					_writeByte(0);
				}
				break;
			case BarcodeType.ean13:
				if (data.Length ==  12 || data.Length ==  13) {
					_writeByte(29);
					_writeByte(107);
					_writeByte(2);
					_serialPort.Write(outputBytes,0,data.Length);
					_writeByte(0);
				}
				break;
			case BarcodeType.ean8:
				if (data.Length ==  7 || data.Length ==  8) {
					_writeByte(29);
					_writeByte(107);
					_writeByte(3);
					_serialPort.Write(outputBytes,0,data.Length);
					_writeByte(0);
				}
				break;
			case BarcodeType.code39:
				if (data.Length > 1) {
					_writeByte(29);
					_writeByte(107);
					_writeByte(4);
					_serialPort.Write(outputBytes,0,data.Length);
					_writeByte(0);
				}
				break;
			case BarcodeType.i25:
				if (data.Length > 1 || data.Length % 2 == 0) {
					_writeByte(29);
					_writeByte(107);
					_writeByte(5);
					_serialPort.Write(outputBytes,0,data.Length);
					_writeByte(0);
				}
				break;
			case BarcodeType.codebar:
				if (data.Length > 1) {
					_writeByte(29);
					_writeByte(107);
					_writeByte(6);
					_serialPort.Write(outputBytes,0,data.Length);
					_writeByte(0);
				}
				break;
			case BarcodeType.code93: //todo: overload this method with a byte array parameter
				if (data.Length > 1) {
					_writeByte(29);
					_writeByte(107);
					_writeByte(7); //todo: use format 2 (init string :  (0x00 can be a value, too)
					_serialPort.Write(outputBytes,0,data.Length);
					_writeByte(0);
				}
				break;
			case BarcodeType.code128: //todo: overload this method with a byte array parameter
				if (data.Length > 1) {
					_writeByte(29);
					_writeByte(107);
					_writeByte(8); //todo: use format 2
					_serialPort.Write(outputBytes,0,data.Length);
					_writeByte(0);
				}
				break;
			case BarcodeType.code11:
				if (data.Length > 1) {
					_writeByte(29);
					_writeByte(107);
					_writeByte(9);
					_serialPort.Write(outputBytes,0,data.Length);
					_writeByte(0);
				}
				break;
			case BarcodeType.msi:
				if (data.Length > 1) {
					_writeByte(29);
					_writeByte(107);
					_writeByte(10);
					_serialPort.Write(outputBytes,0,data.Length);
					_writeByte(0);
				}
				break;
			}
		}
		
		public void SetLargeBarcode(bool large)
		{
			if (large) {
				_writeByte(29);
				_writeByte(119);
				_writeByte(3);
			} else {
				_writeByte(29);
				_writeByte(119);
				_writeByte(2);
			}
		}
		
		public void SetBarcodeLeftSpace(byte spacingDots)
		{
				_writeByte(29);
				_writeByte(120);
				_writeByte(spacingDots);
		}
		
		public void PrintImage(string fileName)
		{
			
			if (!File.Exists(fileName)) {
				throw(new Exception("File does not exist."));
			}
			
			PrintImage(new Bitmap(fileName));

		}
		
		public void PrintImage(Bitmap image)
		{
			int width = image.Width;
			int height = image.Height;
			
			byte[,] imgArray = new byte[width,height];
			
			if (width != 384 || height > 65635) {
				throw(new Exception("Image width must be 384px, height cannot exceed 65635px."));
			}
			
			//Processing image data	
			for (int y = 0; y < image.Height; y++) {
				for (int x = 0; x < (image.Width/8); x++) {
					imgArray[x,y] = 0;
					for (byte n = 0; n < 8; n++) {
						Color pixel = image.GetPixel(x*8+n,y);
						if (pixel.GetBrightness() < 0.5) {
							imgArray[x,y] += (byte)(1 << n);
						}
					}
				}	
			}
			
			//Print LSB first bitmap
			_writeByte(18);
			_writeByte(118);
			
			if (height == 0) {
				_writeByte((byte)height); 	//height LSB
				_writeByte(0); 				//heignt MSB
			} else {
				_writeByte((byte)(height-((height / 256)*256))); 	//height LSB
				_writeByte((byte)(height / 256)); 					//height MSB
			}
			
			for (int y = 0; y < height; y++) {
				System.Threading.Thread.Sleep(PictureLineSleepTimeMs);
				for (int x = 0; x < (width/8); x++) {
					_writeByte(imgArray[x,y]);
				}	
			}
		}
		
		/// <summary>
		/// Sets the printing parameters.
		/// </summary>
		/// <param name='maxPrintingDots'>
		/// Max printing dots (0-255), unit: 8 dots, default: 7 (64 dots)
		/// </param>
		/// <param name='heatingTime'>
		/// Heating time (3-255), unit: 10µs, default: 80 (800µs)
		/// </param>
		/// <param name='heatingInterval'>
		/// Heating interval (0-255), unit: 10µs, default: 2 (20µs)
		/// </param>
		public void SetPrintingParameters(byte maxPrintingDots, byte heatingTime, byte heatingInterval)
		{
			_writeByte(27);
			_writeByte(55);	
			_writeByte(maxPrintingDots);
			_writeByte(heatingTime);				
			_writeByte(heatingInterval);
		}
		
		public void WhiteOnBlackOn()
		{
			_writeByte(29);
			_writeByte(66);
			_writeByte(1);
		}
		
		public void WhiteOnBlackOff()
		{
			_writeByte(29);
			_writeByte(66);
			_writeByte(00);
		}
		
		/// <summary>
		/// Sets the printer offine.
		/// </summary>
		public void Sleep()
		{
			_writeByte(27);
			_writeByte(61);
			_writeByte(0);
		}
		
		/// <summary>
		/// Sets the printer online.
		/// </summary>		
		public void WakeUp()
		{
			_writeByte(27);
			_writeByte(61);
			_writeByte(1);
		}
		
		public override string ToString()
		{
			return string.Format("ThermalPrinter:\n\t_serialPort={0},\n\t_maxPrintingDots={1}," +
				"\n\t_heatingTime={2},\n\t_heatingInterval={3},\n\tPictureLineSleepTimeMs={4}," +
				"\n\tWriteLineSleepTimeMs={5},\n\tEncoding={6}", _serialPort.PortName , _maxPrintingDots,
				_heatingTime, _heatingInterval, PictureLineSleepTimeMs, WriteLineSleepTimeMs, Encoding);
		}
		
		public enum PrintingStyle
		{
			Reverse = 1 << 1,
			Updown = 1 << 2,
			Bold = 1 << 3,
			DoubleHeight = 1 << 4,
			DoubleWidth = 1 << 5,
			DeleteLine = 1 << 6
		}
		
		/// <summary>
		/// Prints the contents of the buffer and feeds n dots.
		/// </summary>
		/// <param name='dotsToFeed'>
		/// Number of dots to feed.
		/// </param>
		public void FeedDots(byte dotsToFeed)
		{
			_writeByte(27);
			_writeByte(74);
			_writeByte(dotsToFeed);
		}
		
		private void _writeByte(byte valueToWrite)
		{
			byte[] tempArray = {valueToWrite};
			_serialPort.Write(tempArray,0,1);
		}
		
		private void _sendEncoding(string encoding)
		{
			switch (encoding)
			{
				case "IBM437":
					_writeByte(27);
					_writeByte(116);
					_writeByte(0);
					break;
				case "ibm850":
					_writeByte(27);
					_writeByte(116);
					_writeByte(1);
					break;				
			}
		}
	}
}
