using System;

namespace ShaniSoft.IO.DataReader
{
	/// <summary>
	/// Summary description for IPNMDataReader.
	/// </summary>
	internal interface IPNMDataReader
	{
		string ReadLine();
		byte ReadByte();
		void Close();
	}
}
