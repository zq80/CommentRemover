namespace CommentRemover
{
    using System;
    
    /// <summary>
    /// Helper class that exposes all GUIDs used across VS Package.
    /// </summary>
    internal sealed partial class PackageGuids
    {
        public const string guidPackageString = "34f42dd5-2285-4902-bdfa-6d721e867b57";
        public const string guidPackageCmdSetString = "d7952971-879b-431d-b743-70ad7e99e846";
        public const string guidAdvancedString = "9adf33d0-8aad-11d0-b606-00a0c922e851";
        public static Guid guidPackage = new Guid(guidPackageString);
        public static Guid guidPackageCmdSet = new Guid(guidPackageCmdSetString);
        public static Guid guidAdvanced = new Guid(guidAdvancedString);
    }
    /// <summary>
    /// Helper class that encapsulates all CommandIDs uses across VS Package.
    /// </summary>
    internal sealed partial class PackageIds
    {
        public const int MyMenuGroup = 0x1020;
        public const int RemoveComment = 0x0100;
        public const int EDIT_ADVANCED = 0x3EA0;
    }
}
