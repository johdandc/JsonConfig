using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace JsonConfig
{
    /// <summary>
    /// 配置节点类
    /// </summary>
    public class ConfigNode
    {
        #region 静态函数

        /// <summary>
        /// 将配置节点转换为Json的字节数组，用于写入流
        /// </summary>
        /// <param name="rootNode">根配置节点</param>
        /// <returns>字节数组</returns>
        public static byte[] ToByteArray(ConfigNode rootNode)
        {
            if (rootNode == null)
            {
                throw new ArgumentNullException(nameof(rootNode));
            }

            ArrayBufferWriter<byte> buffer = new ArrayBufferWriter<byte>();

            Utf8JsonWriter writer = new Utf8JsonWriter(buffer, new JsonWriterOptions() { Encoder = JavaScriptEncoder.Default, Indented = true, SkipValidation = false });

            ConfigNode.WriteToJson(writer, rootNode);

            writer.Dispose();

            return buffer.WrittenSpan.ToArray();
        }

        /// <summary>
        /// 从byte数组读取到ConfigNode的配置，并转换成ConfigNode对象
        /// </summary>
        /// <param name="content">配置内容</param>
        /// <returns>ConfigNode对象</returns>
        public static ConfigNode Parse(byte[] content)
        {
            // 用JsonReader来解析配置，由于JsonReader是一个结构体，所以嵌套调用的过程中需要使用ref强制为对象传递而非值传递
            Utf8JsonReader reader = new Utf8JsonReader(content,
                new JsonReaderOptions() { AllowTrailingCommas = true, CommentHandling = JsonCommentHandling.Skip });


            return ConfigNode.Parse(ref reader);
        }

        #endregion

        #region 公共属性

        /// <summary>
        /// 配置节点类型
        /// </summary>
        public ConfigNodeType Type { get; private set; }

        /// <summary>
        /// 配置的Key值，只能是字符串
        /// </summary>
        public string Key { get; private set; }

        #endregion

        #region 构造函数

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="type"></param>
        /// <param name="key"></param>
        public ConfigNode(ConfigNodeType type, string key) : this()
        {
            this.Type = type;
            this.Key = key;
            if (this.Type == ConfigNodeType.Array)
            {
                this.arrayValue = new List<ConfigNode>();
            }
            else if (this.Type == ConfigNodeType.ConfigNodes)
            {
                this.configNodesValue = new Dictionary<string, ConfigNode>();
            }
        }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="type"></param>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public ConfigNode(ConfigNodeType type, string key, object value) : this(type, key)
        {
            this.SetValue(value);
        }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public ConfigNode(string key, decimal value) : this(ConfigNodeType.Number, key)
        {
            this.SetNumberValue(value);
        }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public ConfigNode(string key, string value) : this(ConfigNodeType.Text, key)
        {
            this.SetTextValue(value);
        }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public ConfigNode(string key, bool value) : this(ConfigNodeType.Boolean, key)
        {
            this.SetBooleanValue(value);
        }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public ConfigNode(string key, List<ConfigNode> value) : this(ConfigNodeType.Array, key)
        {
            this.SetArrayValue(value);
        }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public ConfigNode(string key, Dictionary<string, ConfigNode> value) : this(ConfigNodeType.ConfigNodes, key)
        {
            this.SetConfigNodesValue(value);
        }

        #endregion

        #region 公共函数

        /// <summary>
        /// 设置值
        /// </summary>
        /// <param name="value"></param>
        public void SetValue(object value)
        {
            this.funcs[(int)this.Type](value);
        }

        /// <summary>
        /// 设置数值型值
        /// </summary>
        /// <param name="value"></param>
        public void SetNumberValue(decimal value)
        {
            Debug.Assert(this.Type == ConfigNodeType.Number);

            this.numberValue = value;
        }

        /// <summary>
        /// 设置字符串值
        /// </summary>
        /// <param name="value"></param>
        public void SetTextValue(string value)
        {
            Debug.Assert(this.Type == ConfigNodeType.Text);

            this.textValue = value;
        }

        /// <summary>
        /// 设置布尔型值
        /// </summary>
        /// <param name="value"></param>
        public void SetBooleanValue(bool value)
        {
            Debug.Assert(this.Type == ConfigNodeType.Boolean);

            this.booleanValue = value;
        }

        /// <summary>
        /// 设置数组值
        /// </summary>
        /// <param name="value"></param>
        public void SetArrayValue(List<ConfigNode> value)
        {
            Debug.Assert(this.Type == ConfigNodeType.Array);

            this.arrayValue = value;
        }

        /// <summary>
        /// 设置子配置节点值
        /// </summary>
        /// <param name="value"></param>
        public void SetConfigNodesValue(Dictionary<string, ConfigNode> value)
        {
            Debug.Assert(this.Type == ConfigNodeType.ConfigNodes);

            this.configNodesValue = value;
        }

        /// <summary>
        /// 获取值
        /// </summary>
        /// <returns></returns>
        public object GetValue()
        {
            switch (this.Type)
            {
                case ConfigNodeType.Number:
                    return this.numberValue;
                case ConfigNodeType.Text:
                    return this.textValue;
                case ConfigNodeType.Boolean:
                    return this.booleanValue;
                case ConfigNodeType.Array:
                    return this.arrayValue;
                case ConfigNodeType.ConfigNodes:
                    return this.configNodesValue;
                default:
                    return null;
            }
        }

        /// <summary>
        /// 获取数值型值
        /// </summary>
        /// <returns></returns>
        public decimal ToNumber()
        {
            return this.Type == ConfigNodeType.Number ? this.numberValue : default;
        }

        /// <summary>
        /// 获取字符串值
        /// </summary>
        /// <returns></returns>
        public string ToText()
        {
            return this.Type == ConfigNodeType.Text ? this.textValue : default;
        }

        /// <summary>
        /// 获取布尔值
        /// </summary>
        /// <returns></returns>
        public bool ToBoolean()
        {
            return this.Type == ConfigNodeType.Boolean ? this.booleanValue : default;
        }

        /// <summary>
        /// 获取数组值
        /// </summary>
        /// <returns></returns>
        public IList<ConfigNode> ToArray()
        {
            return this.Type == ConfigNodeType.Array ? this.arrayValue : default;
        }

        /// <summary>
        /// 获取子配置节点值
        /// </summary>
        /// <returns></returns>
        public IDictionary<string, ConfigNode> ToConfigNodes()
        {
            return this.Type == ConfigNodeType.ConfigNodes ? this.configNodesValue : default;
        }

        /// <summary>
        /// 依据数组下标获取配置对象
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public ConfigNode this[int index]
        {
            get
            {
                Debug.Assert(this.Type == ConfigNodeType.Array);

                return this.arrayValue[index];
            }
        }

        /// <summary>
        /// 依据key获取配置对象
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public ConfigNode this[string key]
        {
            get
            {
                Debug.Assert(this.Type == ConfigNodeType.ConfigNodes);

                return this.configNodesValue != null && this.configNodesValue.ContainsKey(key) ? this.configNodesValue[key] : null;
            }
        }

        #endregion

        #region 私有静态函数

        private static void WriteToJson(Utf8JsonWriter writer, ConfigNode rootNode)
        {
            rootNode.WriteToJson(writer);
        }

        private static ConfigNode Parse(ref Utf8JsonReader reader)
        {
            // 根节点的key为空
            ConfigNode root = new ConfigNode(ConfigNodeType.ConfigNodes, string.Empty);

            try
            {
                if (!reader.Read())
                {
                    return root;
                }

                Debug.Assert(reader.TokenType == JsonTokenType.StartObject);

                if (!root.TryParse(ref reader))
                {
                    root.ClearConfig();
                    return root;
                }
            }
            catch (JsonException)
            {
                root.ClearConfig();
            }

            return root;
        }

        private void ClearConfig()
        {
            switch (this.Type)
            {
                case ConfigNodeType.Number:
                    this.numberValue = default;
                    break;
                case ConfigNodeType.Text:
                    this.textValue = string.Empty;
                    break;
                case ConfigNodeType.Boolean:
                    this.booleanValue = default;
                    break;
                case ConfigNodeType.Array:
                    this.arrayValue.Clear();
                    break;
                case ConfigNodeType.ConfigNodes:
                    this.configNodesValue.Clear();
                    break;
                default:
                    break;
            }
        }

        #endregion

        #region 私有构造函数

        private ConfigNode()
        {
            this.funcs = new SetValueFunc[] { null, this.setNumberValue, this.setTextValue, this.setBooleanValue, this.setArrayValue, this.setConfigNodesValue };
        }

        #endregion

        #region 私有函数

        private void WriteToJson(Utf8JsonWriter writer)
        {
            // 根据不同的节点类型进行不同的写入操作，数组和对象需要进行嵌套
            switch (this.Type)
            {
                case ConfigNodeType.Number:
                    if (string.IsNullOrEmpty(this.Key))
                    {
                        writer.WriteNumberValue(this.numberValue);
                    }
                    else
                    {
                        writer.WriteNumber(JsonEncodedText.Encode(this.Key, JavaScriptEncoder.Default), this.numberValue);
                    }
                    break;
                case ConfigNodeType.Text:
                    if (string.IsNullOrEmpty(this.Key))
                    {
                        writer.WriteStringValue(JsonEncodedText.Encode(this.textValue, JavaScriptEncoder.Default));
                    }
                    else
                    {
                        writer.WriteString(JsonEncodedText.Encode(this.Key, JavaScriptEncoder.Default), JsonEncodedText.Encode(this.textValue, JavaScriptEncoder.Default));
                    }
                    break;
                case ConfigNodeType.Boolean:
                    if (string.IsNullOrEmpty(this.Key))
                    {
                        writer.WriteBooleanValue(this.booleanValue);
                    }
                    else
                    {
                        writer.WriteBoolean(JsonEncodedText.Encode(this.Key, JavaScriptEncoder.Default), this.booleanValue);
                    }
                    break;
                case ConfigNodeType.Array:
                    if (string.IsNullOrEmpty(this.Key))
                    {
                        writer.WriteStartArray();
                    }
                    else
                    {
                        writer.WriteStartArray(JsonEncodedText.Encode(this.Key, JavaScriptEncoder.Default));
                    }
                    // 数组必须有序写入
                    for (int i = 0; i < this.arrayValue.Count; ++i)
                    {
                        this.arrayValue[i].WriteToJson(writer);
                    }
                    writer.WriteEndArray();
                    break;
                case ConfigNodeType.ConfigNodes:
                    if (string.IsNullOrEmpty(this.Key))
                    {
                        writer.WriteStartObject();
                    }
                    else
                    {
                        writer.WriteStartObject(JsonEncodedText.Encode(this.Key, JavaScriptEncoder.Default));
                    }
                    // 对象写入不必确保有序
                    foreach (ConfigNode item in this.configNodesValue.Values)
                    {
                        item.WriteToJson(writer);
                    }
                    writer.WriteEndObject();
                    break;
                default:
                    break;
            }
        }

        /// <summary>
        /// 对于从Json转换的时候，只有当ConfigNode的类型为ConfigNodes的时候，才会使用本方法迭代进行转换
        /// </summary>
        /// <param name="reader">Json的读取器</param>
        /// <returns>是否转换成功，成功后对象中存放的是配置内容，不成功后对象内容不可预知</returns>
        private bool TryParse(ref Utf8JsonReader reader)
        {
            Debug.Assert(this.Type == ConfigNodeType.ConfigNodes);

            bool failed = false;

            while (!failed && reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndObject)
                {
                    // 当前嵌套的对象转换完成，返回
                    return true;
                }

                // 先读到PropertyName
                if(reader.TokenType != JsonTokenType.PropertyName)
                {
                    break;
                }

                string key = reader.GetString();

                if (!reader.Read())
                {
                    break;
                }

                // 根据不同的TokenType生成不同的子ConfigNode对象，数组和对象都会比较复杂，其他比较简单
                switch (reader.TokenType)
                {
                    case JsonTokenType.String:
                        this.configNodesValue.Add(key, new ConfigNode(key, reader.GetString()));
                        break;
                    case JsonTokenType.Number:
                        this.configNodesValue.Add(key, new ConfigNode(key, reader.GetDecimal()));
                        break;
                    case JsonTokenType.True:
                        this.configNodesValue.Add(key, new ConfigNode(key, true));
                        break;
                    case JsonTokenType.False:
                        this.configNodesValue.Add(key, new ConfigNode(key, false));
                        break;
                    case JsonTokenType.StartArray:
                        {
                            ConfigNode node = new ConfigNode(ConfigNodeType.Array, key);
                            if (!node.TryParseFromArray(ref reader))
                            {
                                failed = true;
                                break;
                            }
                            else
                            {
                                this.configNodesValue.Add(key, node);
                            }
                            break;
                        }
                    case JsonTokenType.StartObject:
                        {
                            ConfigNode node = new ConfigNode(ConfigNodeType.ConfigNodes, key);
                            if (!node.TryParse(ref reader))
                            {
                                failed = true;
                            }
                            else
                            {
                                this.configNodesValue.Add(key, node);
                            }
                            break;
                        }
                    default:
                        // 其他情况都认为是失败的，因为PropertyName之后应该跟着的是Value
                        failed = true;
                        break;
                }
            }

            // 成功的话应该在中间就返回，此处只有可能是失败，因此，需要清除当前对象的配置内容
            this.configNodesValue.Clear();
            return false;
        }

        /// <summary>
        /// 数组特定的转换方法
        /// </summary>
        /// <param name="reader"></param>
        /// <returns></returns>
        private bool TryParseFromArray(ref Utf8JsonReader reader)
        {
            Debug.Assert(this.Type == ConfigNodeType.Array);

            bool failed = false;

            while (!failed && reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndArray)
                {
                    // 当前嵌套的对象转换完成，返回
                    return true;
                }

                // Array对象的内容部分是没有key的

                // 根据不同的TokenType生成不同的子ConfigNode对象，数组和对象都会比较复杂，其他比较简单
                switch (reader.TokenType)
                {
                    case JsonTokenType.String:
                        this.arrayValue.Add(new ConfigNode(string.Empty, reader.GetString()));
                        break;
                    case JsonTokenType.Number:
                        this.arrayValue.Add(new ConfigNode(string.Empty, reader.GetDecimal()));
                        break;
                    case JsonTokenType.True:
                        this.arrayValue.Add(new ConfigNode(string.Empty, true));
                        break;
                    case JsonTokenType.False:
                        this.arrayValue.Add(new ConfigNode(string.Empty, false));
                        break;
                    case JsonTokenType.StartArray:
                        {
                            ConfigNode node = new ConfigNode(ConfigNodeType.Array, string.Empty);
                            if (!node.TryParseFromArray(ref reader))
                            {
                                failed = true;
                                break;
                            }
                            else
                            {
                                this.arrayValue.Add(node);
                            }
                            break;
                        }
                    case JsonTokenType.StartObject:
                        {
                            ConfigNode node = new ConfigNode(ConfigNodeType.ConfigNodes, string.Empty);
                            if (!node.TryParse(ref reader))
                            {
                                failed = true;
                            }
                            else
                            {
                                this.arrayValue.Add(node);
                            }
                            break;
                        }
                    default:
                        // 其他情况都认为是失败的
                        failed = true;
                        break;
                }
            }

            // 成功的话应该在中间就返回，此处只有可能是失败，因此，需要清除当前对象的配置内容
            this.arrayValue.Clear();
            return false;
        }

        private void setConfigNodesValue(object value)
        {
            this.SetConfigNodesValue(value as Dictionary<string, ConfigNode>);
        }

        private void setArrayValue(object value)
        {
            this.SetArrayValue(value as List<ConfigNode>);
        }

        private void setBooleanValue(object value)
        {
            this.SetBooleanValue((bool)value);
        }

        private void setTextValue(object value)
        {
            this.SetTextValue(value as string);
        }

        private void setNumberValue(object value)
        {
            this.SetNumberValue((decimal)value);
        }

        #endregion

        #region 私有委托

        private delegate void SetValueFunc(object value);

        #endregion

        #region 私有变量

        private SetValueFunc[] funcs;

        private decimal numberValue;

        private string textValue;

        private bool booleanValue;

        private List<ConfigNode> arrayValue;

        private Dictionary<string, ConfigNode> configNodesValue;

        #endregion
    }
}
