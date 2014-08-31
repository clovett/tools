//---------------------------------------------------------------------
// <copyright file="TimeConverter.cs" company="Microsoft Corp">
//      Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//---------------------------------------------------------------------
// Use of this source code is subject to the terms of the Microsoft
// premium shared source license agreement under which you licensed
// this source code. If you did not accept the terms of the license
// agreement, you are not authorized to use this source code.
// For the terms of the license, please see the license agreement
// signed by you and Microsoft.
// THE SOURCE CODE IS PROVIDED "AS IS", WITH NO WARRANTIES OR INDEMNITIES.

namespace Microsoft.Journal.Common
{
    using System;
    using Windows.UI.Xaml.Data;

    class TimeSpanConverter : IValueConverter
    {
        public Object Convert(Object value, Type targetType, Object parameter, String culture)
        {
            if (value is TimeSpan)
            {
                TimeSpan span = (TimeSpan)value;
                return span.ToString();
            }
            return "???";
        }

        public Object ConvertBack(Object value, Type targetType, Object parameter, String culture)
        {
            throw new InvalidOperationException();
        }
    }
}
