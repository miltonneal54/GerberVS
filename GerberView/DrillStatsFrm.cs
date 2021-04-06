// DrillStatsFrm.cs - Builds and displays the statistics of a selected drill layer file.

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
    public partial class DrillStatsForm : Form
    {
        private GerberProject project = null;
        private int fileIndex = -1;

        private string[,] GCodes = new string[,] { {"G00", "Rout Mode"}, {"G01", "1x Linear Interpolation"}, {"G02", "CW Interpolation"}, {"G03", "CCW Interpolation"},
                                                   {"G04", "Variable Dwell"}, {"G05", "Drill Mode"}, {"G85", "Cut Slot"}, {"G90", "Absolute Units"},
                                                   {"G91", "Incremental Units"}, {"G93", "Zero Set"} };

        private string[,] MCodes = new string[,] { { "M00", "End Of Program" }, { "M01", "End Of Pattern" }, { "M18", "Tool Tip Check" }, { "M25", "Begin Pattern"},
                                                   { "M30", "End Program Rewind"}, { "M31", "Begin Pattern"}, { "M45", "Long Message"}, { "M47", "Operator Message"},
                                                   { "M48", "Begin Program Header"}, { "M71", "Metric Units"}, { "M72", "Imperial Units"}, { "M95", "End Program Header"},
                                                   { "M97", "Canned Text X"}, { "M98", "Canned Text Y"},{ "M??", "Unknown M Codes" } };

        private string[] MiscCodes = new string[] { "Comments", "Repeat Hole(R)", "Unknown" };
        private List<bool> IsDrill = new List<bool>();

        public DrillStatsForm(GerberProject project)
        {
            InitializeComponent();
            this.project = project;
            if (project.FileInfo.Count > 0)
                fileIndex = 0;
        }

        private void DrillStatsForm_Load(object sender, EventArgs e)
        {
            GetGeneralInfo();
            GetAllCodes();
        }

        private void GerberStatsForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            this.DialogResult = DialogResult.OK;
        }

        private void GeneralDataGridView_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            fileIndex = (int)(generalDataGridView.Rows[e.RowIndex].Cells["Layer"].Value) - 1;
            GetAllCodes();
        }

        private void GetAllCodes()
        {
            GetNextIndex();
            GetGCodes();
            GetMCodes();
            GetMiscCodes();
            GetDrillUse();
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
                if (project.FileInfo[i].Image.FileType == GerberFileType.Drill
                    && project.FileInfo[i].IsVisible
                    && project.FileInfo[i].Image.DrillStats.ErrorList.Count > 0)
                    hasErrors = true;

                if (hasErrors)
                {
                    errorLabel.Text = "Message(s) exist in visible files.";
                    infoTable.Columns.Add("Message", typeof(String));
                    infoTable.Columns.Add("Line", typeof(Int32));
                    break;
                }
            }

            for (int i = 0; i < project.FileInfo.Count; i++)
            {
                if (project.FileInfo[i].Image.FileType == GerberFileType.Drill && project.FileInfo[i].IsVisible)
                {
                    IsDrill.Add(true);
                    if (project.FileInfo[i].Image.DrillStats.ErrorList.Count == 0)
                    {
                        row = infoTable.NewRow();
                        row["Layer"] = i + 1;
                        row["Filename"] = project.FileInfo[i].FileName;
                        infoTable.Rows.Add(row);
                    }

                    else
                    {
                        // Display found gerber errors.
                        for (int c = 0; c < project.FileInfo[i].Image.DrillStats.ErrorList.Count; c++)
                        {
                            row = infoTable.NewRow();
                            row["Layer"] = i + 1;
                            row["Filename"] = project.FileInfo[i].FileName;
                            row["Message"] = project.FileInfo[i].Image.DrillStats.ErrorList[c].ErrorMessage;
                            row["Line"] = project.FileInfo[i].Image.DrillStats.ErrorList[c].LineNumber;
                            infoTable.Rows.Add(row);
                        }
                    }
                }

                else
                    IsDrill.Add(false);
            }

            generalDataGridView.DataSource = infoTable;
            generalDataGridView.AutoGenerateColumns = true;
            generalDataGridView.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            generalDataGridView.RowHeadersVisible = false;
            generalDataGridView.RowTemplate.Resizable = DataGridViewTriState.False;
            generalDataGridView.Columns["Layer"].Width = 40;
            generalDataGridView.Columns["Layer"].SortMode = DataGridViewColumnSortMode.NotSortable;
            generalDataGridView.Columns["Filename"].Width = hasErrors ? 190 : 330;
            generalDataGridView.Columns["Filename"].SortMode = DataGridViewColumnSortMode.NotSortable;
            if (hasErrors)
            {
                generalDataGridView.Columns["Message"].Width = 300;
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
                        totalCount = project.FileInfo[fileIndex].Image.DrillStats.G00;
                        break;

                    case 1:
                        totalCount = project.FileInfo[fileIndex].Image.DrillStats.G01;
                        break;

                    case 2:
                        totalCount = project.FileInfo[fileIndex].Image.DrillStats.G02;
                        break; ;

                    case 3:
                        totalCount = project.FileInfo[fileIndex].Image.DrillStats.G03;
                        break;

                    case 4:
                        totalCount = project.FileInfo[fileIndex].Image.DrillStats.G04;
                        break;

                    case 5:
                        totalCount = project.FileInfo[fileIndex].Image.DrillStats.G05;
                        break;

                    case 6:
                        totalCount = project.FileInfo[fileIndex].Image.DrillStats.G85;
                        break;

                    case 7:
                        totalCount = project.FileInfo[fileIndex].Image.DrillStats.G90;
                        break;

                    case 8:
                        totalCount = project.FileInfo[fileIndex].Image.DrillStats.G91;
                        break;

                    case 9:
                        totalCount = project.FileInfo[fileIndex].Image.DrillStats.G93;
                        break;

                    case 10:
                        totalCount = project.FileInfo[fileIndex].Image.DrillStats.GUnknown;
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
            gCodeDataGridView.Columns["Description"].Width = 210;
            gCodeDataGridView.Columns["Description"].SortMode = DataGridViewColumnSortMode.NotSortable;
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
                        totalCount = project.FileInfo[fileIndex].Image.DrillStats.M00;
                        break;

                    case 1:
                        totalCount = project.FileInfo[fileIndex].Image.DrillStats.M01;
                        break;

                    case 2:
                        totalCount = project.FileInfo[fileIndex].Image.DrillStats.M18;
                        break;

                    case 3:
                        totalCount = project.FileInfo[fileIndex].Image.DrillStats.M25;
                        break;

                    case 4:
                        totalCount = project.FileInfo[fileIndex].Image.DrillStats.M30;
                        break;

                    case 5:
                        totalCount = project.FileInfo[fileIndex].Image.DrillStats.M31;
                        break;

                    case 6:
                        totalCount = project.FileInfo[fileIndex].Image.DrillStats.M45;
                        break;

                    case 7:
                        totalCount = project.FileInfo[fileIndex].Image.DrillStats.M47;
                        break;

                    case 8:
                        totalCount = project.FileInfo[fileIndex].Image.DrillStats.M48;
                        break;

                    case 9:
                        totalCount = project.FileInfo[fileIndex].Image.DrillStats.M71;
                        break;

                    case 10:
                        totalCount = project.FileInfo[fileIndex].Image.DrillStats.M72;
                        break;

                    case 11:
                        totalCount = project.FileInfo[fileIndex].Image.DrillStats.M95;
                        break;

                    case 12:
                        totalCount = project.FileInfo[fileIndex].Image.DrillStats.M97;
                        break;

                    case 13:
                        totalCount = project.FileInfo[fileIndex].Image.DrillStats.M98;
                        break;

                    case 14:
                        totalCount = project.FileInfo[fileIndex].Image.DrillStats.MUnknown;
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
            mCodeDataGridView.Columns["Description"].Width = 210;
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
                        totalCount = project.FileInfo[fileIndex].Image.DrillStats.Comment;
                        break;

                    case 1:
                        totalCount = project.FileInfo[fileIndex].Image.DrillStats.R;
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
            miscCodeDataGridView.Columns["Code"].Width = 95;
            miscCodeDataGridView.Columns["Code"].DefaultCellStyle.BackColor = Color.LightGreen;
            miscCodeDataGridView.Columns["Count"].Width = 278;
            miscCodeDataGridView.Columns["Count"].SortMode = DataGridViewColumnSortMode.NotSortable;
        }

        private void GetDrillUse()
        {
            DataRow row = null;
            DataTable drillUseTable = new DataTable();

            drillUseTable.Columns.Add("Drill Number", typeof(Int32));
            drillUseTable.Columns.Add("Diameter", typeof(String));
            drillUseTable.Columns.Add("Units", typeof(String));
            drillUseTable.Columns.Add("Count", typeof(Int32));
            drillUseLabel.Text = "File: " + project.FileInfo[fileIndex].FileName;
            GerberFileInformation fileInfo = project.FileInfo[fileIndex];
            foreach(DrillInfo drillInfo in fileInfo.Image.DrillStats.DrillInfoList)
            {
                if (drillInfo.DrillCount > 0)
                {
                    row = drillUseTable.NewRow();
                    row["Drill Number"] = drillInfo.DrillNumber;
                    row["Diameter"] = String.Format("{0:0.000000}", (float)drillInfo.DrillSize);
                    row["Units"] = drillInfo.DrillUnit;
                    row["Count"] = drillInfo.DrillCount;
                    drillUseTable.Rows.Add(row);
                }
            }

            drillUseDataGridView.DataSource = drillUseTable;
            drillUseDataGridView.AutoGenerateColumns = true;
            drillUseDataGridView.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            drillUseDataGridView.RowHeadersVisible = false;
            drillUseDataGridView.RowTemplate.Resizable = DataGridViewTriState.False;
            drillUseDataGridView.Columns["Drill Number"].Width = 75;
            drillUseDataGridView.Columns["Drill Number"].DefaultCellStyle.BackColor = Color.LightGreen;
            drillUseDataGridView.Columns["Diameter"].Width = 75;
            drillUseDataGridView.Columns["Diameter"].SortMode = DataGridViewColumnSortMode.NotSortable;
            drillUseDataGridView.Columns["Units"].Width = 75;
            drillUseDataGridView.Columns["Units"].SortMode = DataGridViewColumnSortMode.NotSortable;
            drillUseDataGridView.Columns["Count"].Width = 135;
            drillUseDataGridView.Columns["Count"].SortMode = DataGridViewColumnSortMode.NotSortable;
        }

        private void GetNextIndex()
        {
            // Look for the next drill file.
            for(int i = fileIndex; i < IsDrill.Count; i++)
            {
                if (IsDrill[i])
                {
                    fileIndex = i;
                    break;
                }
            }
        }
    }
}
