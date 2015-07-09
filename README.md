# SharpDups
fast duplicate file search with C#



logic:
1. Group files with same size

2. Check first/middle/last bytes for quick hash

3. Group files with same quick hash

4. Get full hash for files with same quick hash

5. Group files with same full hash
