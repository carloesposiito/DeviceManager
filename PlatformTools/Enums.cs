namespace PlatformTools
{
    /// <summary>
    /// Class containing all useful structures.
    /// </summary>
    public class Enums
    {
        /// <summary>
        /// Describes device autorization status.
        /// </summary>
        public enum DeviceAuthStatus
        {
            /// <summary>
            /// Device is authorized
            /// </summary>
            AUTHORIZED,

            /// <summary>
            /// Device is not authorized
            /// </summary>
            UNAUTHORIZED,

            /// <summary>
            /// Device has unknown status (ex. offline)
            /// </summary>
            UNKNOWN
        }
    }
}
