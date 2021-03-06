﻿namespace SecureFileUploadService
{
    public class OperationResult
    {
        public string Name { get; set; }
        public string Message { get; set; }
        public long? ElapsedTimeInMilliseconds { get; set; }
        public bool Complete { get; set; }
    }
}