using System;
using ShaniSoft.IO.DataReader;

namespace ShaniSoft.Drawing.PNMReader
{
	/// <summary>
	/// Summary description for IPNMReader.
	/// </summary>
	internal interface IPNMReader
	{
		System.Drawing.Image ReadImageData(IPNMDataReader dr, int width, int height);
	}
}
