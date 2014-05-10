﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;

namespace SolidEdge.Draft.BatchPrint
{
    public class InteropProxy : MarshalByRefObject
    {
        public void DoPrint(string filename, DraftPrintUtilityOptions options)
        {
            SolidEdgeFramework.Application application = null;
            SolidEdgeFramework.Documents documents = null;
            SolidEdgeDraft.DraftDocument draftDocument = null;
            SolidEdgeDraft.DraftPrintUtility draftPrintUtility = null;

            try
            {
                // Register with OLE to handle concurrency issues on the current thread.
                SolidEdge.Common.OleMessageFilter.Register();

                // Connect to or start Solid Edge.
                application = SolidEdge.Common.ApplicationHelper.Connect(true);

                // Make sure Solid Edge is visible.
                application.Visible = true;

                // Get a reference to the Documents collection.
                documents = application.Documents;

                // Get a reference to the DraftPrintUtility.
                draftPrintUtility = (SolidEdgeDraft.DraftPrintUtility)application.GetDraftPrintUtility();

                // Copy all of the settings from DraftPrintUtilityOptions to the DraftPrintUtility object.
                CopyOptions(draftPrintUtility, options);

                // Open the document.
                draftDocument = (SolidEdgeDraft.DraftDocument)documents.Open(filename);

                // Give Solid Edge time to process.
                application.DoIdle();

                // Add the draft document to the queue.
                draftPrintUtility.AddDocument(draftDocument);

                // Print out.
                draftPrintUtility.PrintOut();

                // Cleanup queue.
                draftPrintUtility.RemoveAllDocuments();
            }
            catch
            {
                throw;
            }
            finally
            {
                // Make sure we close the document.
                if (draftDocument != null)
                {
                    draftDocument.Close();
                }

                SolidEdge.Common.OleMessageFilter.Register();
            }
        }

        private void CopyOptions(SolidEdgeDraft.DraftPrintUtility draftPrintUtility, DraftPrintUtilityOptions options)
        {
            Type fromType = typeof(DraftPrintUtilityOptions);
            Type toType = typeof(SolidEdgeDraft.DraftPrintUtility);
            PropertyInfo[] properties = toType.GetProperties().Where(x => x.CanWrite).ToArray();

            // Copy all of the properties from DraftPrintUtility to this object.
            foreach (PropertyInfo toProperty in properties)
            {
                // Some properties may throw an exception if options are incompatible.
                // For instance, if PrintToFile = false, setting PrintToFileName = "" will cause an exception.
                // Mostly irrelevant but handle it as you see fit.
                try
                {
                    PropertyInfo fromProperty = fromType.GetProperty(toProperty.Name);
                    if (fromProperty != null)
                    {
                        object val = fromProperty.GetValue(options, null);

                        toType.InvokeMember(toProperty.Name, BindingFlags.SetProperty, null, draftPrintUtility, new object[] { val });

                    }
                }
                catch (System.Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }
        }
    }
}
