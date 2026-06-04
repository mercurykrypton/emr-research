using System.Collections.Generic;
using System.Linq;

namespace EMR_Search.Models
{
    public class PatientRecord
    {
        public int    PatientID     { get; set; }
        public string SurName       { get; set; } = "";
        public string FirstName     { get; set; } = "";
        public string DOB           { get; set; } = "";
        public string PHN           { get; set; } = "";
        public string Sex           { get; set; } = "";
        public string HomePhone     { get; set; } = "";
        public int    PatientStatus { get; set; }

        public List<DiagnosisEntry> Diagnoses { get; set; } = new();

        public string FullName      => $"{SurName}, {FirstName}".Trim(',', ' ');
        public string DxSummary     => string.Join("  |  ", Diagnoses.Select(d => d.DxCode).Distinct());
    }

    public class DiagnosisEntry
    {
        public string DxCode        { get; set; } = "";
        public string DxDescription { get; set; } = "";
        public string DxDate        { get; set; } = "";
        public string DxStatus      { get; set; } = "";
        public string DxAlert       { get; set; } = "";
        public string DxComment     { get; set; } = "";
        public string DxID          { get; set; } = "";
    }
}
