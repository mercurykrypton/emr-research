using System;
using System.Collections.Generic;
using System.IO;

namespace EMR_Search.Models
{
    public static class CsvLoader
    {
        // CSV columns:
        // PatientID,SurName,FirstName,DOB,PHN,Sex,HomePhone,PatientStatus,
        // DxCode,DxDescription,DxDate,DxStatus,DxAlert,DxComment,DxID

        public static (List<PatientRecord> patients, DateTime loadedAt) Load(string path)
        {
            var patients = new Dictionary<int, PatientRecord>();

            using var reader = new StreamReader(path);
            string? header = reader.ReadLine(); // skip header

            string? line;
            int lineNum = 1;
            while ((line = reader.ReadLine()) != null)
            {
                lineNum++;
                if (string.IsNullOrWhiteSpace(line)) continue;

                var cols = SplitCsv(line);
                if (cols.Length < 8) continue;

                if (!int.TryParse(cols[0], out int pid)) continue;

                if (!patients.TryGetValue(pid, out var patient))
                {
                    patient = new PatientRecord
                    {
                        PatientID     = pid,
                        SurName       = cols[1],
                        FirstName     = cols[2],
                        DOB           = cols[3],
                        PHN           = cols[4],
                        Sex           = cols[5],
                        HomePhone     = cols[6],
                        PatientStatus = int.TryParse(cols[7], out int st) ? st : 0
                    };
                    patients[pid] = patient;
                }

                // Only add a diagnosis row if DxCode is present
                if (cols.Length >= 15 && !string.IsNullOrWhiteSpace(cols[8]))
                {
                    patient.Diagnoses.Add(new DiagnosisEntry
                    {
                        DxCode        = cols[8],
                        DxDescription = cols[9],
                        DxDate        = cols[10],
                        DxStatus      = cols[11],
                        DxAlert       = cols[12],
                        DxComment     = cols[13],
                        DxID          = cols[14]
                    });
                }
            }

            return (new List<PatientRecord>(patients.Values), DateTime.Now);
        }

        // Simple CSV splitter - handles quoted fields
        private static string[] SplitCsv(string line)
        {
            var fields = new List<string>();
            int i = 0;
            while (i <= line.Length)
            {
                if (i == line.Length) { fields.Add(""); break; }

                if (line[i] == '"')
                {
                    i++;
                    int start = i;
                    var sb = new System.Text.StringBuilder();
                    while (i < line.Length)
                    {
                        if (line[i] == '"' && i + 1 < line.Length && line[i + 1] == '"')
                        { sb.Append('"'); i += 2; }
                        else if (line[i] == '"')
                        { i++; break; }
                        else
                        { sb.Append(line[i++]); }
                    }
                    fields.Add(sb.ToString());
                    if (i < line.Length && line[i] == ',') i++;
                }
                else
                {
                    int start = i;
                    while (i < line.Length && line[i] != ',') i++;
                    fields.Add(line.Substring(start, i - start));
                    if (i < line.Length) i++;
                }
            }
            return fields.ToArray();
        }
    }
}
