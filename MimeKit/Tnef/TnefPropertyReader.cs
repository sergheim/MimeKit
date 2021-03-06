﻿//
// TnefPropertyReader.cs
//
// Author: Jeffrey Stedfast <jeff@xamarin.com>
//
// Copyright (c) 2014 Xamarin Inc. (www.xamarin.com)
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
//

using System;
using System.IO;

#if PORTABLE
using Encoding = Portable.Text.Encoding;
#else
using Encoding = System.Text.Encoding;
#endif

using MimeKit.IO;

namespace MimeKit.Tnef {
	public struct TnefPropertyReader
	{
		TnefPropertyTag propertyTag;
		readonly TnefReader reader;
		TnefNameId propertyName;
		int rawValueOffset;
		int rawValueLength;
		int propertyIndex;
		int propertyCount;
		int valueIndex;
		int valueCount;
		int rowIndex;
		int rowCount;

		public bool IsComputedProperty {
			get { throw new NotImplementedException (); }
		}

		public bool IsEmbeddedMessage {
			get { return propertyTag == TnefPropertyTag.AttachDataObj; }
		}

		public bool IsLargeValue {
			get { throw new NotImplementedException (); }
		}

		public bool IsMultiValuedProperty {
			get { return propertyTag.IsMultiValued; }
		}

		public bool IsNamedProperty {
			get { return propertyTag.IsNamed; }
		}

		public bool IsObjectProperty {
			get { throw new NotImplementedException (); }
		}

		public Guid ObjectIid {
			get { throw new NotImplementedException (); }
		}

		public int PropertyCount {
			get { return propertyCount; }
		}

		public TnefNameId PropertyNameId {
			get { return propertyName; }
		}

		public TnefPropertyTag PropertyTag {
			get { return propertyTag; }
		}

		public int RawValueLength {
			get { return rawValueLength; }
		}

		public int RawValueStreamOffset {
			get { return rawValueOffset; }
		}

		public int RowCount {
			get { return rowCount; }
		}

		public int ValueCount {
			get { return valueCount; }
		}

		public Type ValueType {
			get { throw new NotImplementedException (); }
		}

		internal TnefPropertyReader (TnefReader tnef)
		{
			propertyTag = TnefPropertyTag.Null;
			propertyName = new TnefNameId ();
			rawValueOffset = 0;
			rawValueLength = 0;
			propertyIndex = 0;
			propertyCount = 0;
			valueIndex = 0;
			valueCount = 0;
			rowIndex = 0;
			rowCount = 0;

			reader = tnef;
		}

		public TnefReader GetEmbeddedMessageReader ()
		{
			if (!IsEmbeddedMessage)
				throw new InvalidOperationException ();

			return new TnefReader (GetRawValueReadStream (), reader.MessageCodepage, reader.ComplianceMode);
		}

		public Stream GetRawValueReadStream ()
		{
			long start = reader.AttributeRawValueStreamOffset;
			long end = start + reader.AttributeRawValueLength;

			return new BoundStream (reader.InputStream, start, end, true);
		}

		void CheckAvailable (long bytes)
		{
			long start = reader.AttributeRawValueStreamOffset;
			long end = start + reader.AttributeRawValueLength;

			if (reader.StreamOffset + bytes > end) {
				reader.ComplianceStatus |= TnefComplianceStatus.InvalidAttributeLength;
				if (reader.ComplianceMode == TnefComplianceMode.Strict)
					throw new TnefException ("Invalid attribute length.");

				throw new IOException ();
			}
		}

		byte ReadByte ()
		{
			CheckAvailable (1);

			return reader.ReadByte ();
		}

		byte[] ReadBytes (int count)
		{
			CheckAvailable (count);

			var bytes = new byte[count];
			int offset = 0;
			int nread;

			while ((nread = reader.ReadAttributeRawValue (bytes, offset, count - offset)) > 0)
				offset += nread;

			return bytes;
		}

		short ReadInt16 ()
		{
			CheckAvailable (2);

			return reader.ReadInt16 ();
		}

		int ReadInt32 ()
		{
			CheckAvailable (4);

			return reader.ReadInt32 ();
		}

		long ReadInt64 ()
		{
			CheckAvailable (8);

			return reader.ReadInt64 ();
		}

		float ReadSingle ()
		{
			CheckAvailable (4);

			return reader.ReadSingle ();
		}

		double ReadDouble ()
		{
			CheckAvailable (8);

			return reader.ReadDouble ();
		}

		byte[] ReadByteArray ()
		{
			int length = ReadInt32 ();
			var bytes = ReadBytes (length);

			if ((length % 4) != 0) {
				// remaining bytes are padding
				int padding = 4 - (length % 4);

				ReadBytes (padding);
			}

			return bytes;
		}

		string ReadUnicodeString ()
		{
			var bytes = ReadByteArray ();

			return Encoding.Unicode.GetString (bytes);
		}

		string ReadString ()
		{
			var bytes = ReadByteArray ();

			// FIXME: probably need to use the codepage...
			return Encoding.ASCII.GetString (bytes);
		}

		void LoadPropertyName ()
		{
			var guid = new Guid (ReadBytes (16));
			var kind = (TnefNameIdKind) ReadInt32 ();

			if (kind == TnefNameIdKind.Name) {
				var name = ReadUnicodeString ();

				propertyName = new TnefNameId (guid, name);
			} else if (kind == TnefNameIdKind.Id) {
				int id = ReadInt32 ();

				propertyName = new TnefNameId (guid, id);
			} else {
				reader.ComplianceStatus |= TnefComplianceStatus.InvalidAttributeValue;
				if (reader.ComplianceMode == TnefComplianceMode.Strict)
					throw new TnefException ("Invalid TnefNameIdKind.");

				propertyName = new TnefNameId (guid, 0);
			}
		}

		public bool ReadNextProperty ()
		{
			if (propertyIndex >= propertyCount)
				return false;

			while (ReadNextValue ()) {
				// skip over the value...
			}

			var type = (TnefPropertyType) ReadInt16 ();
			var id = ReadInt16 ();

			propertyTag = new TnefPropertyTag ((TnefPropertyId) id, type);

			if (propertyTag.IsNamed)
				LoadPropertyName ();

			LoadValueCount ();
			propertyIndex++;

			return true;
		}

		public bool ReadNextRow ()
		{
			if (rowIndex >= rowCount)
				return false;

			while (ReadNextProperty ()) {
				// skip over the property...
			}

			LoadPropertyCount ();
			rowIndex++;

			return true;
		}

		public bool ReadNextValue ()
		{
			throw new NotImplementedException ();
		}

		public int ReadRawValue (byte[] buffer, int offset, int count)
		{
			if (buffer == null)
				throw new ArgumentNullException ("buffer");

			if (offset < 0 || offset >= buffer.Length)
				throw new ArgumentOutOfRangeException ("offset");

			if (count < 0 || count > (buffer.Length - offset))
				throw new ArgumentOutOfRangeException ("count");

			throw new NotImplementedException ();
		}

		public int ReadTextValue (char[] buffer, int offset, int count)
		{
			if (buffer == null)
				throw new ArgumentNullException ("buffer");

			if (offset < 0 || offset >= buffer.Length)
				throw new ArgumentOutOfRangeException ("offset");

			if (count < 0 || count > (buffer.Length - offset))
				throw new ArgumentOutOfRangeException ("count");

			throw new NotImplementedException ();
		}

		public object ReadValue ()
		{
			if (valueIndex >= valueCount)
				throw new InvalidOperationException ();

			object value;

			switch (propertyTag.ValueTnefType) {
			case TnefPropertyType.Null:
				value = null;
				break;
			case TnefPropertyType.I2:
				// 2 bytes for the short followed by 2 bytes of padding
				value = (short) (ReadInt32 () >> 16);
				break;
			case TnefPropertyType.Error:
			case TnefPropertyType.Long:
				value = ReadInt32 ();
				break;
			case TnefPropertyType.R4:
				value = ReadSingle ();
				break;
			case TnefPropertyType.Double:
				value = ReadDouble ();
				break;
			case TnefPropertyType.Currency:
			case TnefPropertyType.I8:
				value = ReadInt64 ();
				break;
			case TnefPropertyType.Boolean:
				value = ReadInt32 () != 0;
				break;
			case TnefPropertyType.Object:
				value = new Guid (ReadBytes (16));
				break;
			case TnefPropertyType.String8:
				value = ReadString ();
				break;
			case TnefPropertyType.Unicode:
				value = ReadUnicodeString ();
				break;
			case TnefPropertyType.AppTime:
			case TnefPropertyType.SysTime:
				long ticks = ReadInt64 () / 10000;
				ticks -= 1000L * 60L * 60L * 24L * (365L * 369L + 89L);
				value = new DateTime (ticks);
				break;
			case TnefPropertyType.ClassId:
				value = ReadBytes (16);
				break;
			case TnefPropertyType.Binary:
				value = ReadByteArray ();
				break;
			default:
				reader.ComplianceStatus |= TnefComplianceStatus.UnsupportedPropertyType;
				if (reader.ComplianceMode == TnefComplianceMode.Strict)
					throw new TnefException ("Unsupported property type.");

				value = null;
				break;
			}

			valueIndex++;

			return value;
		}

		public bool ReadValueAsBoolean ()
		{
			throw new NotImplementedException ();
		}

		public byte[] ReadValueAsBytes ()
		{
			throw new NotImplementedException ();
		}

		public DateTime ReadValueAsDateTime ()
		{
			throw new NotImplementedException ();
		}

		public double ReadValueAsDouble ()
		{
			throw new NotImplementedException ();
		}

		public float ReadValueAsFloat ()
		{
			throw new NotImplementedException ();
		}

		public Guid ReadValueAsGuid ()
		{
			throw new NotImplementedException ();
		}

		public short ReadValueAsInt16 ()
		{
			throw new NotImplementedException ();
		}

		public int ReadValueAsInt32 ()
		{
			throw new NotImplementedException ();
		}

		public long ReadValueAsInt64 ()
		{
			throw new NotImplementedException ();
		}

		public string ReadValueAsString ()
		{
			throw new NotImplementedException ();
		}

		public override int GetHashCode ()
		{
			return reader.GetHashCode ();
		}

		public override bool Equals (object obj)
		{
			if (!(obj is TnefPropertyReader))
				return false;

			var prop = (TnefPropertyReader) obj;

			return prop.reader == reader;
		}

		void LoadPropertyCount ()
		{
			if ((propertyCount = ReadInt32 ()) < 0) {
				reader.ComplianceStatus |= TnefComplianceStatus.InvalidAttributeValue;
				if (reader.ComplianceMode == TnefComplianceMode.Strict)
					throw new TnefException ("Invalid attribute value.");

				propertyCount = 0;
			}

			propertyIndex = 0;
			valueCount = 0;
			valueIndex = 0;
		}

		void LoadValueCount ()
		{
			if (propertyTag.IsMultiValued) {
				if ((valueCount = ReadInt32 ()) < 0) {
					reader.ComplianceStatus |= TnefComplianceStatus.InvalidAttributeValue;
					if (reader.ComplianceMode == TnefComplianceMode.Strict)
						throw new TnefException ("Invalid attribute value.");

					valueCount = 0;
				}
			} else {
				valueCount = 1;
			}

			valueIndex = 0;
		}

		void LoadRowCount ()
		{
			if ((rowCount = ReadInt32 ()) < 0) {
				reader.ComplianceStatus |= TnefComplianceStatus.InvalidAttributeValue;
				if (reader.ComplianceMode == TnefComplianceMode.Strict)
					throw new TnefException ("Invalid attribute value.");

				rowCount = 0;
			}

			propertyCount = 0;
			propertyIndex = 0;
			valueCount = 0;
			valueIndex = 0;
			rowIndex = 0;
		}

		internal void Load ()
		{
			propertyTag = TnefPropertyTag.Null;
			rawValueOffset = 0;
			rawValueLength = 0;

			if (reader.AttributeTag == TnefAttributeTag.MapiProperties) {
				LoadPropertyCount ();
				rowCount = 0;
				rowIndex = 0;
			} else if (reader.AttributeTag == TnefAttributeTag.RecipientTable) {
				LoadRowCount ();
			} else {
				propertyCount = 0;
				propertyIndex = 0;
				valueCount = 1;
				valueIndex = 0;
				rowCount = 0;
				rowIndex = 0;
			}
		}
	}
}
