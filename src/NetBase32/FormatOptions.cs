namespace NetBase32
{
    /// <summary>
    /// Specifies whether <see cref="Encode(byte[], FormatOptions)"/> method inserts "-"
    /// characters in its output.
    /// </summary>
    public enum FormatOptions
    {
        /// <summary>
        /// Does not insert any "-" characters in the string representation.
        /// </summary>
        None,
        /// <summary>
        /// Inserts a "-" character after every 8 characters in the string representation.
        /// </summary>
        IncludeDashes
    }
}