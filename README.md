# SharpDups
fast duplicate file search with C#



logic:
1. group files with same size
2. check first/middle/last bytes for quick hash
3. group files with same quick hash
4. get full hash for files with same quick hash
5. group files with same full hash
