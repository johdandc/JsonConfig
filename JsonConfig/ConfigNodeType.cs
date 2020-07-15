namespace JsonConfig
{
    /// <summary>
    /// 配置类型枚举
    /// </summary>
    public enum ConfigNodeType : int
    {
        /// <summary>
        /// 数值型，支持整数和浮点数，表示范围-79,228,162,514,264,337,593,543,950,335到79,228,162,514,264,337,593,543,950,335之间
        /// </summary>
        Number = 1,

        /// <summary>
        /// 字符串
        /// </summary>
        Text = 2,

        /// <summary>
        /// 布尔
        /// </summary>
        Boolean = 3,

        /// <summary>
        /// 列表(数组)
        /// </summary>
        Array = 4,

        /// <summary>
        /// 子配置
        /// </summary>
        ConfigNodes = 5
    }
}
