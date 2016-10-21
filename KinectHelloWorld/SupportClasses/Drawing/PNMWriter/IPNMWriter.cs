using System;
using ShaniSoft.IO.DataWriter;

namespace ShaniSoft.Drawing
{
	/// <summary>
	/// Summary description for IPNMWriter.
	/// </summary>
	internal interface IPNMWriter
	{
		void WriteImageData(IPNMDataWriter dw, System.Drawing.Image im);
	}
}
