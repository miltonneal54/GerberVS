// GerberStatsFrm.cs - Builds and displays the statistics of a selected gerber layer file.

/*  Copyright (C) 2015-2020 Milton Neal <milton200954@gmail.com>

    Redistribution and use in source and binary forms, with or without
    modification, are permitted provided that the following conditions
    are met:

    1. Redistributions of source code must retain the above copyright
       notice, this list of conditions and the following disclaimer.
    2. Redistributions in binary form must reproduce the above copyright
       notice, this list of conditions and the following disclaimer in the
       documentation and/or other materials provided with the distribution.
    3. Neither the name of the project nor the names of its contributors
       may be used to endorse or promote products derived from this software
       without specific prior written permission.

    THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND
    ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE
    IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE
    ARE DISCLAIMED.  IN NO EVENT SHALL THE COPYRIGHT OWNER OR CONTRIBUTORS BE LIABLE
    FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL
    DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS
    OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION)
    HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT
    LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY
    OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF
    SUCH DAMAGE.
 */

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using GerberVS;

namespace GerberView
{
    public partial class GerberStatsForm : Form
    {
        private GerberProject project = null;
        private int fileIndex = -1;

        private string[,] GCodes = new string[,] { {"G0", "Move"}, {"G1", "1x Linear Interpolation"}, {"G2", "CW Interpolation"}, {"G3", "CCW Interpolation"},
                                                   {"G4", "Comment"}, {"G10", "10x Linear Interpolation"}, {"G11", "0.1x Interpolation"}, {"G12", "0.01x Interpolation"},
                                                   {"G36", "Poly Fill On"}, {"G37", "Poly Fill Off"}, {"G54", "Tool Prepare"}, {"G55", "Flash Prepare"},
                                                   {"G70", "Units = Inch"}, {"G71", "Units = Millimeter"}, {"G74", "Disable 360° Circlar Interpolation"},
                                                   {"G75", "Enable 360° Circular Interpolation"}, {"G90", "Absolute Units"}, {"G91", "Incremental Units"},
                                                   {"G??", "Unknown G Codes"} };

        private string[,] DCodes = new string[,] { { "D1", "Exposure On" }, { "D2", "Exposure Off" }, { "D3", "Flash Aperture" }, {"D??", "Unknown D Codes"},
                                                   {"DXX", "D Code Errors"} };

        private string[,] MCodes = new string[,] { { "M00", "Program Stop" }, { "M01", "Optional Stop" }, { "M2", "Program End" }, { "M?", "Unknown M Codes" } };

        private string[] MiscCodes = new string[] { "X", "Y", "I", "J", "*", "Unknown" };
        private List<bool> IsRS274X = new List<bool>();

        public GerberStatsForm(GerberProject project)
        {
            InitializeComponent();
            this.project = project;
            if (project.FileInfo.Count > 0)
                fileIndex = 0;
        }

        private void GerberStatsForm_Load(object sender, EventArgs e)
        {
            GetGeneralInfo();
            GetAllCodes();
        }

        private void GerberStatsForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            DialogResult = DialogResult.OK;
        }

        private void GeneralDataGridView_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex > -1)
            {
                fileIndex = (int)(generalDataGridView.Rows[e.RowIndex].Cells["Layer"].Value) - 1;
                GetAllCodes();
            }
        }

        private void GetAllCodes()
        {
            GetNextIndex();
            GetGCodes();
            GetDCodes();
            GetMCodes();
            GetMiscCodes();
            GetApertureDefinitions();
            GetApertureUse();
        }

        private void GetGeneralInfo()
        {
            DataRow row = null;
            DataTable infoTable = new DataTable();
            bool hasErrors = false;

            infoTable.Columns.Add("Layer", typeof(Int32));
            infoTable.Columns.Add("Filename", typeof(String));
            errorLabel.Text = "No message(s) in visible files.";

            // Check if any visible layers have errors and if necessary, add columns to support error reporting.
            for (int i = 0; i < project.FileInfo.Count; i++)
            {
                if (project.FileInfo[i].Image.FileType == GerberFileType.RS274X 
                    && project.FileInfo[i].IsVisible 
                    && project.FileInfo[i].Image.GerberStats.ErrorList.Count > 0)
                        hasErrors = true;

                if(hasErrors)
                {
                    errorLabel.Text = "Message(s) exist in visible files.";
                    infoTable.Columns.Add("Message", typeof(String));
                    infoTable.Columns.Add("Line", typeof(Int32));
                    break;
                }
            }

            for (int i = 0; i < project.FileInfo.Count; i++)
            {
                if (project.FileInfo[i].Image.FileType == GerberFileType.RS274X && project.FileInfo[i].IsVisible)
                {
                    IsRS274X.Add(true);
                    if (project.FileInfo[i].Image.GerberStats.ErrorList.Count == 0)
                    {
                        row = infoTable.NewRow();
                        row["Layer"] = i + 1;
                        row["Filename"] = project.FileInfo[i].FileName;
                        infoTable.Rows.Add(row);
                    }

                    else
                    {
                        // Display found gerber errors.
                        for (int c = 0; c < project.FileInfo[i].Image.GerberStats.ErrorList.Count; c++)
                        {
                            row = infoTable.NewRow();
                            row["Layer"] = i + 1;
                            row["Filename"] = project.FileInfo[i].FileName;
                            row["Message"] = project.FileInfo[i].Image.GerberStats.ErrorList[c].ErrorMessage;
                            row["Line"] = project.FileInfo[i].Image.GerberStats.ErrorList[c].LineNumber;
                            infoTable.Rows.Add(row);
                        }
                    }
                }

                else
                    IsRS274X.Add(false);
            }

            generalDataGridView.DataSource = infoTable;
            generalDataGridView.AutoGenerateColumns = true;
            generalDataGridView.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            generalDataGridView.RowHeadersVisible = false;
            generalDataGridView.RowTemplate.Resizable = DataGridViewTriState.False;
            generalDataGridView.Columns["Layer"].Width = 40;
            generalDataGridView.Columns["Layer"].SortMode = DataGridViewColumnSortMode.NotSortable;
            generalDataGridView.Columns["Filename"].Width = hasErrors ? 190 : 430;
            generalDataGridView.Columns["Filename"].SortMode = DataGridViewColumnSortMode.NotSortable;
            if (hasErrors)
            {
                generalDataGridView.Columns["Message"].Width = 250;
                generalDataGridView.Columns["Message"].SortMode = DataGridViewColumnSortMode.NotSortable;
                generalDataGridView.Columns["Line"].Width = 50;
                generalDataGridView.Columns["Line"].SortMode = DataGridViewColumnSortMode.NotSortable;
            }
        }

        private void GetGCodes()
        {
            int totalCount;

            DataTable gCodeTable = new DataTable();
            gCodeTable.Columns.Add("Code", typeof(String));
            gCodeTable.Columns.Add("Count", typeof(Int32));
            gCodeTable.Columns.Add("Description", typeof(String));

            gCodeLabel.Text = "File: " + project.FileInfo[fileIndex].FileName;
            for (int i = 0; i < GCodes.Length / 2; i++)
            {
                totalCount = 0;
                DataRow row = gCodeTable.NewRow();
                row["Code"] = GCodes[i, 0];
                row["Description"] = GCodes[i, 1];

                switch (i)
                {
                    case 0:
                        totalCount = project.FileInfo[fileIndex].Image.GerberStats.G0;
                        break;

                    case 1:
                        totalCount = project.FileInfo[fileIndex].Image.GerberStats.G1;
                        break;

                    case 2:
                        totalCount = project.FileInfo[fileIndex].Image.GerberStats.G2;
                        break; ;

                    case 3:
                        totalCount = project.FileInfo[fileIndex].Image.GerberStats.G3;
                        break;

                    case 4:
                        totalCount = project.FileInfo[fileIndex].Image.GerberStats.G4;
                        break;

                    case 5:
                        totalCount = project.FileInfo[fileIndex].Image.GerberStats.G10;
                        break;

                    case 6:
                        totalCount = project.FileInfo[fileIndex].Image.GerberStats.G11;
                        break;

                    case 7:
                        totalCount = project.FileInfo[fileIndex].Image.GerberStats.G12;
                        break;

                    case 8:
                        totalCount = project.FileInfo[fileIndex].Image.GerberStats.G36;
                        break;

                    case 9:
                        totalCount = project.FileInfo[fileIndex].Image.GerberStats.G37;
                        break;

                    case 10:
                        totalCount = project.FileInfo[fileIndex].Image.GerberStats.G54;
                        break;

                    case 11:
                        totalCount = project.FileInfo[fileIndex].Image.GerberStats.G55;
                        break;

                    case 12:
                        totalCount = project.FileInfo[fileIndex].Image.GerberStats.G70;
                        break;

                    case 13:
                        totalCount = project.FileInfo[fileIndex].Image.GerberStats.G71;
                        break;

                    case 14:
                        totalCount = project.FileInfo[fileIndex].Image.GerberStats.G74;
                        break;

                    case 15:
                        totalCount = project.FileInfo[fileIndex].Image.GerberStats.G75;
                        break;

                    case 16:
                        totalCount = project.FileInfo[fileIndex].Image.GerberStats.G90;
                        break;

                    case 17:
                        totalCount = project.FileInfo[fileIndex].Image.GerberStats.G91;
                        break;

                    case 18:
                        totalCount = project.FileInfo[fileIndex].Image.GerberStats.UnknowGCodes;
                        break;
                }

                row["Count"] = totalCount;
                gCodeTable.Rows.Add(row);
            }

            gCodeDataGridView.DataSource = gCodeTable;
            gCodeDataGridView.AutoGenerateColumns = true;
            gCodeDataGridView.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            gCodeDataGridView.RowHeadersVisible = false;
            gCodeDataGridView.RowTemplate.Resizable = DataGridViewTriState.False;
            gCodeDataGridView.Columns["Code"].Width = 75;
            gCodeDataGridView.Columns["Code"].DefaultCellStyle.BackColor = Color.LightGreen;
            gCodeDataGridView.Columns["Count"].Width = 75;
            gCodeDataGridView.Columns["Count"].SortMode = DataGridViewColumnSortMode.NotSortable;
            gCodeDataGridView.Columns["Description"].Width = 305;
            gCodeDataGridView.Columns["Description"].SortMode = DataGridViewColumnSortMode.NotSortable;
        }

        private void GetDCodes()
        {
            int totalCount;

            DataTable dCodeTable = new DataTable();
            dCodeTable.Columns.Add("Code", typeof(String));
            dCodeTable.Columns.Add("Count", typeof(Int32));
            dCodeTable.Columns.Add("Description", typeof(String));

            dCodeLabel.Text = "File: " + project.FileInfo[fileIndex].FileName;
            for (int i = 0; i < DCodes.Length / 2; i++)
            {
                totalCount = 0;
                DataRow row = dCodeTable.NewRow();
                row["Code"] = DCodes[i, 0];
                row["Description"] = DCodes[i, 1];

                switch (i)
                {
                    case 0:
                        totalCount += project.FileInfo[fileIndex].Image.GerberStats.D1;
                        break;

                    case 1:
                        totalCount = project.FileInfo[fileIndex].Image.GerberStats.D2;
                        break;

                    case 2:
                        totalCount = project.FileInfo[fileIndex].Image.GerberStats.D3;
                        break;

                    case 3:
                        totalCount = project.FileInfo[fileIndex].Image.GerberStats.UnknownDCodes;
                        break;

                    case 4:
                        totalCount = project.FileInfo[fileIndex].Image.GerberStats.DCodeErrors;
                        break;
                }

                row["Count"] = totalCount;
                dCodeTable.Rows.Add(row);
            }

            dCodeDataGridView.DataSource = dCodeTable;
            dCodeDataGridView.AutoGenerateColumns = true;
            dCodeDataGridView.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dCodeDataGridView.RowHeadersVisible = false;
            dCodeDataGridView.RowTemplate.Resizable = DataGridViewTriState.False;
            dCodeDataGridView.Columns["Code"].Width = 75;
            dCodeDataGridView.Columns["Code"].DefaultCellStyle.BackColor = Color.LightGreen;
            dCodeDataGridView.Columns["Count"].Width = 75;
            dCodeDataGridView.Columns["Count"].SortMode = DataGridViewColumnSortMode.NotSortable;
            dCodeDataGridView.Columns["Description"].Width = 323;
            dCodeDataGridView.Columns["Description"].SortMode = DataGridViewColumnSortMode.NotSortable;
        }

        private void GetMCodes()
        {
            int totalCount;

            DataTable mCodeTable = new DataTable();
            mCodeTable.Columns.Add("Code", typeof(String));
            mCodeTable.Columns.Add("Count", typeof(Int32));
            mCodeTable.Columns.Add("Description", typeof(String));

            mCodeLabel.Text = "File: " + project.FileInfo[fileIndex].FileName;
            for (int i = 0; i < MCodes.Length / 2; i++)
            {
                totalCount = 0;
                DataRow row = mCodeTable.NewRow();
                row["Code"] = MCodes[i, 0];
                row["Description"] = MCodes[i, 1];

                switch (i)
                {
                    case 0:
                        totalCount = project.FileInfo[fileIndex].Image.GerberStats.M0;
                        break;

                    case 1:
                        totalCount = project.FileInfo[fileIndex].Image.GerberStats.M1;
                        break;

                    case 2:
                        totalCount = project.FileInfo[fileIndex].Image.GerberStats.M2;
                        break;

                    case 3:
                        totalCount = project.FileInfo[fileIndex].Image.GerberStats.UnknownMCodes;
                        break;
                }

                row["Count"] = totalCount;
                mCodeTable.Rows.Add(row);
            }

            mCodeDataGridView.DataSource = mCodeTable;
            mCodeDataGridView.AutoGenerateColumns = true;
            mCodeDataGridView.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            mCodeDataGridView.RowHeadersVisible = false;
            mCodeDataGridView.RowTemplate.Resizable = DataGridViewTriState.False;
            mCodeDataGridView.Columns["Code"].Width = 75;
            mCodeDataGridView.Columns["Code"].DefaultCellStyle.BackColor = Color.LightGreen;
            mCodeDataGridView.Columns["Count"].Width = 75;
            mCodeDataGridView.Columns["Count"].SortMode = DataGridViewColumnSortMode.NotSortable;
            mCodeDataGridView.Columns["Description"].Width = 323;
            mCodeDataGridView.Columns["Description"].SortMode = DataGridViewColumnSortMode.NotSortable;
        }

        private void GetMiscCodes()
        {
            int totalCount;

            DataTable miscCodeTable = new DataTable();
            miscCodeTable.Columns.Add("Code", typeof(String));
            miscCodeTable.Columns.Add("Count", typeof(Int32));

            miscLabel.Text = "File: " + project.FileInfo[fileIndex].FileName;
            for (int i = 0; i < MiscCodes.Length; i++)
            {
                totalCount = 0;
                DataRow row = miscCodeTable.NewRow();
                row["Code"] = MiscCodes[i];

                switch (i)
                {
                    case 0:
                        totalCount = project.FileInfo[fileIndex].Image.GerberStats.XCount;
                        break;

                    case 1:
                        totalCount = project.FileInfo[fileIndex].Image.GerberStats.YCount;
                        break;

                    case 2:
                        totalCount = project.FileInfo[fileIndex].Image.GerberStats.ICount;
                        break;

                    case 3:
                        totalCount = project.FileInfo[fileIndex].Image.GerberStats.JCount;
                        break;

                    case 4:
                        totalCount = project.FileInfo[fileIndex].Image.GerberStats.StarCount;
                        break;

                    case 5:
                        totalCount = project.FileInfo[fileIndex].Image.GerberStats.UnknownCount;
                        break;
                }

                row["Count"] = totalCount;
                miscCodeTable.Rows.Add(row);
            }

            miscCodeDataGridView.DataSource = miscCodeTable;
            miscCodeDataGridView.AutoGenerateColumns = true;
            miscCodeDataGridView.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            miscCodeDataGridView.RowHeadersVisible = false;
            miscCodeDataGridView.RowTemplate.Resizable = DataGridViewTriState.False;
            miscCodeDataGridView.Columns["Code"].Width = 75;
            miscCodeDataGridView.Columns["Code"].DefaultCellStyle.BackColor = Color.LightGreen;
            miscCodeDataGridView.Columns["Count"].Width = 381;
            miscCodeDataGridView.Columns["Count"].SortMode = DataGridViewColumnSortMode.NotSortable;
        }

        private void GetApertureDefinitions()
        {
            DataTable apertureTable = new DataTable();
            apertureTable.Columns.Add("D Code", typeof(string));
            apertureTable.Columns.Add("Aperture", typeof(String));
            apertureTable.Columns.Add("Param[0]", typeof(String));
            apertureTable.Columns.Add("Param[1]", typeof(String));
            apertureTable.Columns.Add("Param[2]", typeof(String));
            apertureTable.Columns.Add("Param[3]", typeof(String));

            int count = 0;

            apDefLabel.Text = "File: " + project.FileInfo[fileIndex].FileName;
            DataRow row = null;
            GerberFileInformation fileInfo = project.FileInfo[fileIndex];
            count = fileInfo.Image.GerberStats.ApertureList.Count;
            for (int i = 0; i < count; i++)
            {
                row = apertureTable.NewRow();
                row["D Code"] = "D" + fileInfo.Image.GerberStats.ApertureList[i].Number.ToString();
                row["Aperture"] = fileInfo.Image.GerberStats.ApertureList[i].ApertureType;
                row["Param[0]"] = String.Format("{0:0.000000}", fileInfo.Image.GerberStats.ApertureList[i].Parameters[0]);
                row["Param[1]"] = String.Format("{0:0.000000}", fileInfo.Image.GerberStats.ApertureList[i].Parameters[1]);
                row["Param[2]"] = String.Format("{0:0.000000}", fileInfo.Image.GerberStats.ApertureList[i].Parameters[2]);
                row["Param[3]"] = String.Format("{0:0.000000}", fileInfo.Image.GerberStats.ApertureList[i].Parameters[3]);
                apertureTable.Rows.Add(row);
            }

            apertureDefinitionGridView.DataSource = apertureTable;
            apertureDefinitionGridView.AutoGenerateColumns = true;
            apertureDefinitionGridView.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            apertureDefinitionGridView.RowHeadersVisible = false;
            apertureDefinitionGridView.RowTemplate.Resizable = DataGridViewTriState.False;
            apertureDefinitionGridView.Columns["D Code"].Width = 75;
            apertureDefinitionGridView.Columns["D Code"].DefaultCellStyle.BackColor = Color.LightGreen;
            apertureDefinitionGridView.Columns["D Code"].SortMode = DataGridViewColumnSortMode.NotSortable;
            apertureDefinitionGridView.Columns["Aperture"].Width = 85;
            apertureDefinitionGridView.Columns["Aperture"].SortMode = DataGridViewColumnSortMode.NotSortable;
            apertureDefinitionGridView.Columns["Param[0]"].Width = 74;
            apertureDefinitionGridView.Columns["Param[0]"].SortMode = DataGridViewColumnSortMode.NotSortable;
            apertureDefinitionGridView.Columns["Param[1]"].Width = 74;
            apertureDefinitionGridView.Columns["Param[1]"].SortMode = DataGridViewColumnSortMode.NotSortable;
            apertureDefinitionGridView.Columns["Param[2]"].Width = 74;
            apertureDefinitionGridView.Columns["Param[2]"].SortMode = DataGridViewColumnSortMode.NotSortable;
            apertureDefinitionGridView.Columns["Param[3]"].Width = 74;
            apertureDefinitionGridView.Columns["Param[3]"].SortMode = DataGridViewColumnSortMode.NotSortable;
        }

        private void GetApertureUse()
        {
            DataRow row = null;
            DataTable apertureUseTable = new DataTable();

            apertureUseTable.Columns.Add("D Code", typeof(String));
            apertureUseTable.Columns.Add("Count", typeof(Int32));
            apUseLabel.Text = "File: " + project.FileInfo[fileIndex].FileName;
            GerberFileInformation fileInfo = project.FileInfo[fileIndex];
            foreach (GerberApertureInfo apertureInfo  in fileInfo.Image.GerberStats.DCodeList)
            {
                // D codes can be defined but not used, so diplaying only used apertures.
                if (apertureInfo.Count > 0)
                {
                    row = apertureUseTable.NewRow();
                    row["D Code"] = "D" + apertureInfo.Number.ToString();
                    row["Count"] = apertureInfo.Count;
                    apertureUseTable.Rows.Add(row);
                }
            }

            apertureUseDataGridView.DataSource = apertureUseTable;
            apertureUseDataGridView.AutoGenerateColumns = true;
            apertureUseDataGridView.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            apertureUseDataGridView.RowHeadersVisible = false;
            apertureUseDataGridView.RowTemplate.Resizable = DataGridViewTriState.False;
            apertureUseDataGridView.Columns["D Code"].Width = 75;
            apertureUseDataGridView.Columns["D Code"].DefaultCellStyle.BackColor = Color.LightGreen;
            apertureUseDataGridView.Columns["Count"].Width = 381;
            apertureUseDataGridView.Columns["Count"].SortMode = DataGridViewColumnSortMode.NotSortable;
        }

        private void GetNextIndex()
        {
            // Look for the next RS274X file.
            for (int i = fileIndex; i < IsRS274X.Count; i++)
            {
                if (IsRS274X[i])
                {
                    fileIndex = i;
                    break;
                }
            }
        }
    }
}
