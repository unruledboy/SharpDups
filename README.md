# SharpDups
Fast duplicate file search via parallel processing with C#.

The tool will find duplicate files using Map/Reduce method. It accepts a list of files and then perform the duplicate checking. It could be extended easily to support file search filter etc.


Logic:

1. Group files with same size
2. Check first/middle/last bytes for quick hash
3. Group files with same quick hash
4. Get progressive hash for files with same quick hash, if intermediate hash is different, discard the remaining comparison
5. Group files with same full hash


Methods:

1. V1: uses sequential processing
2. V2: uses parallel processing, 3 times faster with 5 worker threads
3. V3: use parallel processing and progressive hashing


--------------------------

现有方案
我们判断文件是否重复，一般是给两个需要比较的文件进行哈希，然后比较哈希值。

这个做法有个问题，就是比较慢：
 - 如果文件大小不一样，那还需要比较吗？
 - 如果文件的一部分不一样，那还需要比较吗？

新的方案
步骤如下：
 - 把文件列表按文件大小分组，大小不一的文件会被认为不一样，尽管有可能差异只是空格或者空行（回车换行）
 - 快速比较文件头、尾、中间的三个字符，如果不一样，则会视为不是一样的文件
 - 渐进式比较区块，任何一个区块的哈希值不一样，则文件为不一样
 - 事实上，BT等下载引擎也是用了类似的办法。

方案特色
支持并行计算，使用MapReduce方式，分而治之，加快比较速度
支持保留比较结果，以备以后和别的文件比较，而且这个比较逻辑和批量比较是一致的
