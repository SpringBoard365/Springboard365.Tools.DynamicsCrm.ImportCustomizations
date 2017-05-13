namespace Springboard365.Tools.DynamicsCrm.ImportCustomizations
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text.RegularExpressions;
    using System.Xml;
    using log4net;
    using Microsoft.Crm.Sdk.Messages;
    using Microsoft.Xrm.Sdk;
    using Microsoft.Xrm.Sdk.Query;
    using Springboard365.Tools.CommandLine.Core;
    using Springboard365.Tools.DynamicsCrm.Common;

    public class ImportCrmCustomization : CrmToolBase
    {
        private readonly ImportCrmCustomizationParameters parameters;
        private readonly ILog logger;

        public ImportCrmCustomization(string[] args)
            : base(new ImportCrmCustomizationParameters(), args)
        {
            logger = LogManager.GetLogger(typeof(ImportCrmCustomization));
            parameters = (ImportCrmCustomizationParameters)CommandLineParameterBase;
        }

        public override void Run()
        {
            try
            {
                var byteArray = ReadCustomizationsFile();
                ImportCustomizationsFile(byteArray);
                PublishCustomizationsFile();
                PublishAllDuplicateDetectionRules();
            }
            catch (Exception exception)
            {
                logger.Error(exception.Message, exception);
            }
        }

        private byte[] ReadCustomizationsFile()
        {
            logger.Info("Reading customization file...");
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
            throw new NotImplementedException();
        }

        private void AssignWorkflowToCurrentUser(Guid workflowId)
        {
            var assignRequest = new AssignRequest
            {
                Assignee = new EntityReference("systemuser", GetCurrentUserId()),
                Target = new EntityReference("workflow", workflowId)
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
                    ImportJobId = importJobId
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
                File.WriteAllText(string.Format("{0}\\import_log_file{1}.xml", parameters.ImportJobFileSavePath, DateTime.UtcNow.ToString("yyyyMMddhhmmss")), importJobData);
                OutputImportJobData(importJobData);
            }
            catch (Exception)
            {
                logger.Warn("Unable to Save Import File to disk");
            }
        }

        private void OutputImportJobData(string importJobErrorData)
        {
            var xmlDocument = new XmlDocument();
            xmlDocument.LoadXml(importJobErrorData);

            LogSolutionDetails(xmlDocument);

            WriteErrorToConsole(xmlDocument, "//entities/entity");
        }

        private void WriteErrorToConsole(XmlDocument xmlDocument, string xPath)
        {
            var nodeList = xmlDocument.SelectNodes(xPath);
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
                var friendlyName = GetFriendlyName(xPath, localizedName);
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
                    logger.WarnFormat("{0} result: {1} Code: {2} Description: {3}", friendlyName, result, errorCode, errorText);
                }
            }
        }

        private string GetFriendlyName(string xPath, string localizedName)
        {
            if (string.IsNullOrEmpty(localizedName))
            {
                return localizedName;
            }

            var stringArray = xPath.Split('/');
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
            logger.InfoFormat("Solution {0}: {1}", englishElementName, xmlNode.InnerText);
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
            logger.Info("Publishing customizations...");
            OrganizationService.Execute(new PublishAllXmlRequest());
        }

        private void PublishAllDuplicateDetectionRules()
        {
        }
    }
}