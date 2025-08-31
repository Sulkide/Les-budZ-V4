namespace ImporterForGIMPImageFiles {
    internal static class RandomFunctions {

        //Constants.
        public const int randomTableSize = 4096;
        const int randomTableRandomSeed = 314159265;
        const int n = 624;
        const int m = 397;
        const uint upperMask = 0x80000000;
        const uint lowerMask = 0x7fffffff;

        //Variables.
        static uint[] _randomTable = null;
        static uint[] randomTable {
            get {
                if (_randomTable == null) {
                    _randomTable = new uint[randomTableSize];
                    newRandomWithSeed(randomTableRandomSeed, false);
                    for (int i = 0; i < randomTableSize; i++)
                        _randomTable[i] = getInt();
                }
                return _randomTable;
            }
        }
        static uint[] _stateVectors = null;
        static uint[] stateVectors {
            get {
                if (_stateVectors == null)
                    _stateVectors = new uint[n];
                return _stateVectors;
            }
        }
        static uint stateVectorIndex = 0;
        static uint[] _matrixArray = null;
        static uint[] matrixArray {
            get {
                if (_matrixArray == null)
                    _matrixArray = new uint[] { 0, 0x9908b0df };
                return _matrixArray;
            }
        }

        //Generate a new random instance with a given seed, which is an offset into the random table.
        public static void newRandomWithSeed(uint seed) => newRandomWithSeed(seed, true);
        static void newRandomWithSeed(uint seed, bool referenceRandomTable) {
            if (referenceRandomTable)
                seed = randomTable[seed % randomTableSize];
            stateVectors[0] = seed;
            for (stateVectorIndex = 1; stateVectorIndex < n; stateVectorIndex++)
                stateVectors[stateVectorIndex] = 1812433253 * (stateVectors[stateVectorIndex - 1] ^ (stateVectors[stateVectorIndex - 1] >> 30)) +
                        stateVectorIndex;
        }

        //Get a random integer.
        public static uint getInt() {
            uint randomInteger;

            //Throw an exception if attempting to get a random integer before the random seed has been initialised.
            if (_stateVectors == null)
                throw new ImporterForGIMPImageFilesException(60, "Attempt to get random integer before random instance created.");
            
            //Perform the algorithm to get the random integer.
            if (stateVectorIndex >= n) {
                int i;
                for (i = 0; i < n - m; i++) {
                    randomInteger = (stateVectors[i] & upperMask) | (stateVectors[i + 1] & lowerMask);
                    stateVectors[i] = stateVectors[i + m] ^ (randomInteger >> 1) ^ matrixArray[randomInteger & 1];
                }
                for (; i < n - 1; i++) {
                    randomInteger = (stateVectors[i] & upperMask) | (stateVectors[i + 1] & lowerMask);
                    stateVectors[i] = stateVectors[i + (m - n)] ^ (randomInteger >> 1) ^ matrixArray[randomInteger & 1];
                }
                randomInteger = (stateVectors[n - 1] & upperMask) | (stateVectors[0] & lowerMask);
                stateVectors[n - 1] = stateVectors[m - 1] ^ (randomInteger >> 1) ^ matrixArray[randomInteger & 1];
                stateVectorIndex = 0;
            }
            randomInteger = stateVectors[stateVectorIndex++];
            randomInteger ^= randomInteger >> 11;
            randomInteger ^= (randomInteger << 7) & 0x9d2c5680;
            randomInteger ^= (randomInteger << 15) & 0xefc60000;
            randomInteger ^= randomInteger >> 18;

            //Return the random integer.
            return randomInteger;
        }

        //Get a random integer within a specified range.
        public static uint getInt(uint begin, uint end) {
            uint dist = end - begin;
            uint random;

            //Throw an exception if attempting to get a random integer before the random seed has been initialised.
            if (_stateVectors == null)
                throw new ImporterForGIMPImageFilesException(61, "Attempt to get random integer with range before random instance created.");

            //Throw an exception if the beginning of the range is after the end of the range.
            else if (begin > end)
                throw new ImporterForGIMPImageFilesException(62, "End cannot be greater than begin when getting random range.");

            //If begin is the same as end, just return that value.
            else if (dist == 0)
                random = 0;

            //Otherwise perform the algorithm that returns the random integer.
            else {
                uint maxvalue;
                if (dist <= 0x80000000u) {
                    uint leftover = (0x80000000u % dist) * 2;
                    if (leftover >= dist)
                        leftover -= dist;
                    maxvalue = 0xffffffffu - leftover;
                }
                else
                    maxvalue = dist - 1;
                do
                    random = getInt();
                while (random > maxvalue);
                random %= dist;
            }

            //Return the random integer.
            return begin + random;
        }
    }
}