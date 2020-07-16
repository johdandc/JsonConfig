# JsonConfig
JsonConfig是一个将按照json语法编写的配置文件加载到内存中的库。

它以ConfigNode作为节点，类型包括：Number（数字型，对应decimal）、Text（文本，即字符串，对应string）、布尔（对应bool）、数组（对应List<ConfigNode>）、对象（对应Dictionary<string,ConfigNode>）。

加载的时候，JsonConfig会根据json串中的值类型进行推导决定相应的ConfigNodeType。并以Key-Value的形式存放到内存中。

读取配置的时候，ConfigNode支持node[int index]的形式读取对应下标的对象以及node[string key]的形式读取到对象型节点（根节点也是一个对象型节点）的子节点对象。嵌套时，可以通过连续的[]不断向下获取。
