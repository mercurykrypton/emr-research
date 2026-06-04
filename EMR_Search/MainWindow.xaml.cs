using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using EMR_Search.Models;

namespace EMR_Search
{
    public partial class MainWindow : Window
    {
        private List<PatientRecord> _allPatients = new();
        private List<PatientRecord> _filtered    = new();
        private DateTime            _loadedAt;
        private string              _csvPath = DefaultCsvPath();

        private static string DefaultCsvPath() =>
            Path.Combine(AppContext.BaseDirectory, "..", "data", "diagnosis-export.csv");

        public MainWindow()
        {
            InitializeComponent();
            TxtFile.Text = _csvPath;
            if (File.Exists(_csvPath))
                LoadData();
            else
                TxtStatus.Text = $"CSV not found: {_csvPath}  -  Run DIAGNOSIS_EXPORT macro first.";
        }

        // ── Data loading ────────────────────────────────────────────────

        private void LoadData()
        {
            try
            {
                (_allPatients, _loadedAt) = CsvLoader.Load(_csvPath);
                ApplyFilter();
                UpdateStatus();
            }
            catch (Exception ex)
            {
                TxtStatus.Text = $"Error loading CSV: {ex.Message}";
            }
        }

        private void UpdateStatus()
        {
            int total = _allPatients.Count;
            int shown = _filtered.Count;
            TxtStatus.Text = shown == total
                ? $"{total:N0} patients loaded    |    Last updated: {_loadedAt:yyyy-MM-dd HH:mm}"
                : $"{shown:N0} of {total:N0} patients match    |    Last updated: {_loadedAt:yyyy-MM-dd HH:mm}";
            TxtFile.Text = _csvPath;
        }

        // ── Search / filter ─────────────────────────────────────────────

        private void Filter_Changed(object sender, TextChangedEventArgs e) => ApplyFilter();

        private void ApplyFilter()
        {
            string name   = TxtName.Text.Trim();
            string dob    = TxtDOB.Text.Trim();
            string phn    = TxtPHN.Text.Trim();
            string dxCode = TxtDxCode.Text.Trim();
            string dxDesc = TxtDxDesc.Text.Trim();

            _filtered = _allPatients.Where(p =>
            {
                // Name filter — matches surname or firstname
                if (!string.IsNullOrEmpty(name) &&
                    !p.SurName.Contains(name, StringComparison.OrdinalIgnoreCase) &&
                    !p.FirstName.Contains(name, StringComparison.OrdinalIgnoreCase) &&
                    !p.FullName.Contains(name, StringComparison.OrdinalIgnoreCase))
                    return false;

                if (!string.IsNullOrEmpty(dob) &&
                    !p.DOB.Contains(dob, StringComparison.OrdinalIgnoreCase))
                    return false;

                if (!string.IsNullOrEmpty(phn) &&
                    !p.PHN.Contains(phn, StringComparison.OrdinalIgnoreCase))
                    return false;

                // Diagnosis filters — patient matches if ANY diagnosis row matches
                if (!string.IsNullOrEmpty(dxCode) &&
                    !p.Diagnoses.Any(d => d.DxCode.Contains(dxCode, StringComparison.OrdinalIgnoreCase)))
                    return false;

                if (!string.IsNullOrEmpty(dxDesc) &&
                    !p.Diagnoses.Any(d => d.DxDescription.Contains(dxDesc, StringComparison.OrdinalIgnoreCase)))
                    return false;

                return true;
            }).ToList();

            GridPatients.ItemsSource = null;
            GridPatients.ItemsSource = _filtered;
            GridDiagnoses.ItemsSource = null;
            TxtDetailHeader.Text = "Select a patient to view diagnoses";
            BtnCopyPHN.IsEnabled  = false;
            BtnCopyName.IsEnabled = false;
            UpdateStatus();
        }

        // ── Grid selection ───────────────────────────────────────────────

        private void GridPatients_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (GridPatients.SelectedItem is PatientRecord p)
            {
                // When dx filters are active, show only matching diagnoses in detail
                string dxCode = TxtDxCode.Text.Trim();
                string dxDesc = TxtDxDesc.Text.Trim();

                var diags = p.Diagnoses.AsEnumerable();
                if (!string.IsNullOrEmpty(dxCode))
                    diags = diags.Where(d => d.DxCode.Contains(dxCode, StringComparison.OrdinalIgnoreCase));
                if (!string.IsNullOrEmpty(dxDesc))
                    diags = diags.Where(d => d.DxDescription.Contains(dxDesc, StringComparison.OrdinalIgnoreCase));

                GridDiagnoses.ItemsSource = diags.ToList();
                TxtDetailHeader.Text = $"{p.FullName}  |  DOB: {p.DOB}  |  PHN: {p.PHN}  |  {p.Diagnoses.Count} diagnoses";
                BtnCopyPHN.IsEnabled  = true;
                BtnCopyName.IsEnabled = true;
            }
            else
            {
                GridDiagnoses.ItemsSource = null;
                TxtDetailHeader.Text = "Select a patient to view diagnoses";
                BtnCopyPHN.IsEnabled  = false;
                BtnCopyName.IsEnabled = false;
            }
        }

        // ── Buttons ──────────────────────────────────────────────────────

        private void BtnRefresh_Click(object sender, RoutedEventArgs e)
        {
            if (!File.Exists(_csvPath))
            {
                MessageBox.Show($"File not found:\n{_csvPath}\n\nRun DIAGNOSIS_EXPORT macro in Profile first.",
                    "File Not Found", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            LoadData();
        }

        private void BtnBrowse_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog
            {
                Title  = "Select diagnosis-export CSV",
                Filter = "CSV files|*.csv|All files|*.*",
                FileName = Path.GetFileName(_csvPath)
            };
            if (File.Exists(_csvPath))
                dlg.InitialDirectory = Path.GetDirectoryName(_csvPath);

            if (dlg.ShowDialog() == true)
            {
                _csvPath = dlg.FileName;
                LoadData();
            }
        }

        private void BtnClear_Click(object sender, RoutedEventArgs e)
        {
            TxtName.Text   = "";
            TxtDOB.Text    = "";
            TxtPHN.Text    = "";
            TxtDxCode.Text = "";
            TxtDxDesc.Text = "";
        }

        private void BtnCopyPHN_Click(object sender, RoutedEventArgs e)
        {
            if (GridPatients.SelectedItem is PatientRecord p)
            {
                Clipboard.SetText(p.PHN);
                TxtStatus.Text = $"Copied PHN: {p.PHN}  -  Paste into Profile search bar.";
            }
        }

        private void BtnCopyName_Click(object sender, RoutedEventArgs e)
        {
            if (GridPatients.SelectedItem is PatientRecord p)
            {
                Clipboard.SetText(p.FullName);
                TxtStatus.Text = $"Copied: {p.FullName}";
            }
        }
    }
}
