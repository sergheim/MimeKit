﻿//
// TnefComplianceStatus.cs
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

namespace MimeKit.Tnef {
	[Flags]
	public enum TnefComplianceStatus {
		Compliant                = 0,
		AttributeOverflow        = 1 << 0,
		InvalidAttribute         = 1 << 1,
		InvalidAttributeChecksum = 1 << 2,
		InvalidAttributeLength   = 1 << 3,
		InvalidAttributeLevel    = 1 << 4,
		InvalidAttributeValue    = 1 << 5,
		InvalidDate              = 1 << 6,
		InvalidMessageClass      = 1 << 7,
		InvalidMessageCodepage   = 1 << 8,
		InvalidPropertyLength    = 1 << 9,
		InvalidRowCount          = 1 << 10,
		InvalidTnefSignature     = 1 << 11,
		InvalidTnefVersion       = 1 << 12,
		NestingTooDeep           = 1 << 13,
		StreamTruncated          = 1 << 14,
		UnsupportedPropertyType  = 1 << 15
	}
}
