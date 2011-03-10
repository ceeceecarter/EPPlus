﻿/*******************************************************************************
 * You may amend and distribute as you like, but don't remove this header!
 * 
 * All rights reserved.
 * 
 * EPPlus is an Open Source project provided under the 
 * GNU General Public License (GPL) as published by the 
 * Free Software Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA 02111-1307 USA
 * 
 * See http://epplus.codeplex.com/ for details
 * 
 * The GNU General Public License can be viewed at http://www.opensource.org/licenses/gpl-license.php
 * If you unfamiliar with this license or have questions about it, here is an http://www.gnu.org/licenses/gpl-faq.html
 * 
 * The code for this project may be used and redistributed by any means PROVIDING it is 
 * not sold for profit without the author's written consent, and providing that this notice 
 * and the author's name and all copyright notices remain intact.
 * 
 * All code and executables are provided "as is" with no warranty either express or implied. 
 * The author accepts no liability for any damage or loss of business that this product may cause.
 *
 * Code change notes:
 * 
 * Author							Change						Date
 *******************************************************************************
 * Jan Källman		Added		21-MAR-2011
 *******************************************************************************/
using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using System.IO.Packaging;

namespace OfficeOpenXml.Table.PivotTable
{
    public class ExcelPivotCacheDefinition : XmlHelper
    {
        public ExcelPivotCacheDefinition(XmlNamespaceManager ns, ExcelPivotTable pivotTable) :
            base(ns, null)
        {
            foreach (var r in pivotTable.Part.GetRelationshipsByType(ExcelPackage.schemaRelationships + "/pivotCacheDefinition"))
            {
                Relationship = r;
            }
            CacheDefinitionUri = PackUriHelper.ResolvePartUri(Relationship.SourceUri, Relationship.TargetUri);

            var pck = pivotTable.WorkSheet.xlPackage.Package;
            Part = pck.GetPart(CacheDefinitionUri);
            CacheDefinitionXml = new XmlDocument();
            CacheDefinitionXml.Load(Part.GetStream());

            TopNode = CacheDefinitionXml.DocumentElement;
            SourceAddress = new ExcelAddressBase(GetXmlNodeString("d:cacheSource/d:worksheetSource/@ref"));            
        }
        public ExcelPivotCacheDefinition(XmlNamespaceManager ns, ExcelPivotTable pivotTable, ExcelAddressBase sourceAddress, int tblId) :
            base(ns, null)
        {
            SourceAddress = sourceAddress;
            PivotTable = pivotTable;

            var pck = pivotTable.WorkSheet.xlPackage.Package;
            
            //CacheDefinition
            CacheDefinitionXml = new XmlDocument();
            CacheDefinitionXml.LoadXml(GetStartXml());
            CacheDefinitionUri=new Uri(string.Format("/xl/pivotCache/pivotCacheDefinition{0}.xml", tblId), UriKind.Relative);
            Part = pck.CreatePart(CacheDefinitionUri, ExcelPackage.schemaPivotCacheDefinition);
            TopNode = CacheDefinitionXml.DocumentElement;

            //CacheRecord. Create an empty one.
            CacheRecordUri = new Uri(string.Format("/xl/pivotCache/pivotCacheRecords{0}.xml", tblId), UriKind.Relative);
            var cacheRecord = new XmlDocument();
            cacheRecord.LoadXml("<pivotCacheRecords xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\" xmlns:r=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships\" count=\"0\" />");
            var recPart = pck.CreatePart(CacheRecordUri, ExcelPackage.schemaPivotCacheRecords);
            cacheRecord.Save(recPart.GetStream());

            RecordRelationship = Part.CreateRelationship(PackUriHelper.ResolvePartUri(CacheDefinitionUri, CacheRecordUri), TargetMode.Internal, ExcelPackage.schemaRelationships + "/pivotCacheRecords");
            RecordRelationshipID = RecordRelationship.Id;

            CacheDefinitionXml.Save(Part.GetStream());
        }        
        internal PackagePart Part
        {
            get;
            set;
        }
        public XmlDocument CacheDefinitionXml { get; private set; }
        public Uri CacheDefinitionUri
        {
            get;
            internal set;
        }
        internal Uri CacheRecordUri
        {
            get;
            set;
        }
        internal PackageRelationship Relationship
        {
            get;
            set;
        }
        internal PackageRelationship RecordRelationship
        {
            get;
            set;
        }
        internal string RecordRelationshipID 
        {
            get
            {
                return GetXmlNodeString("@r:id");
            }
            set
            {
                SetXmlNodeString("@r:id", value);
            }
        }
        /// <summary>
        /// Referece to the PivoTable object
        /// </summary>
        public ExcelPivotTable PivotTable
        {
            get;
            private set;
        }
        /// <summary>
        /// The address to the Source data
        /// </summary>
        internal ExcelAddressBase SourceAddress
        {
            get;
            private set;
        }
        private string GetStartXml()
        {
            string xml="<pivotCacheDefinition xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\" xmlns:r=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships\" r:id=\"\" refreshOnLoad=\"1\" refreshedBy=\"SomeUser\" refreshedDate=\"40504.582403125001\" createdVersion=\"1\" refreshedVersion=\"3\" recordCount=\"5\" upgradeOnRefresh=\"1\">";

            xml += "<cacheSource type=\"worksheet\">";
            xml += string.Format("<worksheetSource ref=\"{0}\" sheet=\"{1}\" /> ", SourceAddress.Address, SourceAddress.WorkSheet);
            xml += "</cacheSource>";
            xml += string.Format("<cacheFields count=\"{0}\">",SourceAddress._toCol - SourceAddress._fromCol + 1);
            var sourceWorksheet = PivotTable.WorkSheet.Workbook.Worksheets[SourceAddress.WorkSheet];
            for (int col = SourceAddress._fromCol; col <= SourceAddress._toCol; col++)
            {
                if (sourceWorksheet==null || sourceWorksheet.Cell(SourceAddress._fromRow, col).Value == null || sourceWorksheet.Cell(SourceAddress._fromRow, col).Value.ToString().Trim() == "")
                {
                    xml += string.Format("<cacheField name=\"Column{0}\" numFmtId=\"0\">", col - SourceAddress._fromCol+1);
                }
                else
                {
                    xml += string.Format("<cacheField name=\"{0}\" numFmtId=\"0\">", sourceWorksheet.Cell(SourceAddress._fromRow, col).Value);
                }
                //xml += "<sharedItems containsNonDate=\"0\" containsString=\"0\" containsBlank=\"1\" /> ";
                xml += "<sharedItems containsBlank=\"1\" /> ";
                xml += "</cacheField>";
            }
            xml += "</cacheFields>";
            xml += "</pivotCacheDefinition>";

            return xml;
        }
    }
}
