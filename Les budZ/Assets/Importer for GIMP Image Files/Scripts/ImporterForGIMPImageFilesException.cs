namespace ImporterForGIMPImageFiles {
    using System;

    internal class ImporterForGIMPImageFilesException : Exception {

        //Constructor.
        public ImporterForGIMPImageFilesException(int exceptionCode, string message) :
            base($"{exceptionCode.ToString().PadLeft(4, '0')} - {message}") { }
    }
}