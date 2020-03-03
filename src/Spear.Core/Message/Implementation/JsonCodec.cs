﻿using System;
using System.Text;
using Newtonsoft.Json;

namespace Spear.Core.Message.Implementation
{
    public class JsonCodec : DMessageCodec
    {
        protected override byte[] EncodeInternal(object message)
        {
            var content = JsonConvert.SerializeObject(message);
            return Encoding.UTF8.GetBytes(content);
        }

        protected override object DecodeInternal(byte[] data, Type type)
        {
            var content = Encoding.UTF8.GetString(data);
            return JsonConvert.DeserializeObject(content, type);
        }
    }
}