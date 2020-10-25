namespace Springboard365.Tools.DynamicsCrm.ImportCustomizations
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text.RegularExpressions;
    using System.Xml;
    using Microsoft.Crm.Sdk.Messages;
    using Microsoft.Xrm.Sdk;
    using Microsoft.Xrm.Sdk.Query;
    using Springboard365.Tools.CommandLine.Core;
    using Springboard365.Tools.DynamicsCrm.Common;

    public class ImportCrmCustomization : CrmToolBase
    {
        private ImportCrmCustomizationParameters parameters;

        public ImportCrmCustomization(string[] args)
            : base(new ImportCrmCustomizationParameters(), args)
        {
        }

        public override void Initialize()
        {
            parameters = (ImportCrmCustomizationParameters)CommandLineParameterBase;
        }

        public override void Run()
        {
            var byteArray = ReadCustomizationsFile();
            ImportCustomizationsFile(byteArray);
            PublishCustomizationsFile();
            PublishAllDuplicateDetectionRules();
        }

        private byte[] ReadCustomizationsFile()
        {
            ConsoleLogger.LogMessage("Reading customization file...");
            return File.ReadAllBytes(parameters.FileName);
        }

        private void ImportCustomizationsFile(byte[] compressedXml)
        {
            var importJobId = Guid.NewGuid();
            AssignWorkflowsToCurrentUser();
            ImportSolution(compressedXml, importJobId);
        }

        private void AssignWorkflowsToCurrentUser()
        {
            foreach (var workflowId in GetPublishedWorkflowIds())
            {
                AssignWorkflowToCurrentUser(workflowId);
            }
        }

        private IEnumerable<Guid> GetPublishedWorkflowIds()
        {
            return new List<Guid>();
        }

        private void AssignWorkflowToCurrentUser(Guid workflowId)
        {
            var assignRequest = new AssignRequest
            {
                Assignee = new EntityReference("systemuser", GetCurrentUserId()),
                Target = new EntityReference("workflow", workflowId),
            };
            OrganizationService.Execute(assignRequest);
        }

        private Guid GetCurrentUserId()
        {
            return ((WhoAmIResponse)OrganizationService.Execute(new WhoAmIRequest())).UserId;
        }

        private void ImportSolution(byte[] compressedXml, Guid importJobId)
        {
            try
            {
                var importSolutionRequest = new ImportSolutionRequest
                {
                    CustomizationFile = compressedXml,
                    OverwriteUnmanagedCustomizations = true, PublishWorkflows = true,
                    ImportJobId = importJobId,
                };
                OrganizationService.Execute(importSolutionRequest);
            }
            catch
            {
                MonitorCustomizations(importJobId);
                throw;
            }
        }

        private void MonitorCustomizations(Guid importJobId)
        {
            try
            {
                var importJob = OrganizationService.Retrieve("importjob", importJobId, new ColumnSet("data", "solutionname"));
                var importJobData = importJob.GetAttributeValue<string>("data");
                var dateTimeString = DateTime.UtcNow.ToString("yyyyMMddhhmmss");
                var importJobFileSavePath = string.IsNullOrEmpty(parameters.ImportJobFileSavePath) ? "." : parameters.ImportJobFileSavePath;
                var filePath = $"{importJobFileSavePath}\\import_log_file{dateTimeString}.xml";
                File.WriteAllText(filePath, importJobData);
                OutputImportJobData(importJobData);
            }
            catch (Exception)
            {
                ConsoleLogger.LogMessage("Unable to Save Import File to disk");
            }
        }

        private void OutputImportJobData(string importJobErrorData)
        {
            var xmlDocument = new XmlDocument();
            xmlDocument.LoadXml(importJobErrorData);

            LogSolutionDetails(xmlDocument);

            WriteErrorToConsole(xmlDocument, "//entities/entity");
        }

        private void WriteErrorToConsole(XmlDocument xmlDocument, string xpath)
        {
            var nodeList = xmlDocument.SelectNodes(xpath);
            if (nodeList == null)
            {
                return;
            }

            foreach (XmlNode node in nodeList)
            {
                if (node.Attributes == null || node.FirstChild == null || node.FirstChild.Attributes == null)
                {
                    continue;
                }

                var localizedName = node.Attributes["LocalizedName"].ToString();
                var friendlyName = GetFriendlyName(xpath, localizedName);
                var firstChildNode = node.FirstChild;

                if (firstChildNode == null || firstChildNode.Attributes == null)
                {
                    continue;
                }

                var result = firstChildNode.Attributes["result"].Value;
                var errorCode = firstChildNode.Attributes["errorcode"].Value;
                var errorText = firstChildNode.Attributes["errortext"].Value;
                if (result == "failure")
                {
                    ConsoleLogger.LogMessage($"{friendlyName} result: {result} Code: {errorCode} Description: {errorText}");
                }
            }
        }

        private string GetFriendlyName(string xpath, string localizedName)
        {
            if (string.IsNullOrEmpty(localizedName))
            {
                return localizedName;
            }

            var stringArray = xpath.Split('/');
            return stringArray[stringArray.Length - 1];
        }

        private void LogSolutionDetails(XmlDocument xmlDocument)
        {
            LogInnerText(xmlDocument, "UniqueName");
            LogInnerText(xmlDocument, "Version");
        }

        private void LogInnerText(XmlDocument xmlDocument, string elementName)
        {
            var xmlNode = xmlDocument.SelectSingleNode("//solutionManifest/" + elementName);

            if (xmlNode == null)
            {
                return;
            }

            var englishElementName = SplitNodeName(elementName);
            ConsoleLogger.LogMessage($"Solution {englishElementName}: {xmlNode.InnerText}");
        }

        private object SplitNodeName(string elementName)
        {
            var regex = new Regex(@"[A-Z](?:[A-Z]+|[a-z]*)(?=$|[A-Z])");
            var matchCollection = regex.Matches(elementName);

            var toReturn = matchCollection.Cast<Match>().Aggregate(string.Empty, (s, match) => s + (match.Value + " "));

            return toReturn.Trim();
        }

        private void PublishCustomizationsFile()
        {
            ConsoleLogger.LogMessage("Publishing customizations...");
            OrganizationService.Execute(new PublishAllXmlRequest());
        }

        private void PublishAllDuplicateDetectionRules()
        {
        }
    }
}