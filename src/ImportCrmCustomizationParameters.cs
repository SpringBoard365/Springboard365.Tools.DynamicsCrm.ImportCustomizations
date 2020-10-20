namespace Springboard365.Tools.DynamicsCrm.ImportCustomizations
{
    using Springboard365.Tools.DynamicsCrm.Common;
    using Springboard365.Tools.CommandLine.Core;

    public class ImportCrmCustomizationParameters : CrmCommandLineParameterBase
    {
        [CommandLineArgument(ArgumentType.Required, "File Name", Description = "Show the file name.", Shortcut = "filename")]
        public string FileName { get; set; }

        [CommandLineArgument(ArgumentType.Required, "Import Job File Save Path", Description = "Show the Import job file save path.", Shortcut = "importjobfilesavepath")]
        public string ImportJobFileSavePath { get; set; }
    }
}