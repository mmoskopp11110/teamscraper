using System;

namespace Spielerplus
{
    /// <summary>
    /// User is not logged into spielerplus but tries to access restricted resources
    /// </summary>
    public class NotLoggedInException : InvalidOperationException
    {
        public NotLoggedInException() : base()
        {
        }
        
        public NotLoggedInException(string message) : base(message)
        {
        }
    }
}