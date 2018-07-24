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
