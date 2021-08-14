using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using System.Threading;
using System.Diagnostics;
using System.Drawing.Drawing2D;
using System.Drawing.Printing;

using GerberVS;
using CustomCommonDialog;
using Ruler;

namespace GerberView
{
    public partial class Form1 : Form
    {
        private LibGerberVS gerberLib = null;
        private GerberProject project = null;
        private GerberRenderInformation renderInfo = null;
        private SelectionInformation selectionInfo = null;   // List containing the currenly selected objects (nets).
        private GerberRenderMode renderMode;

        bool hasProject = false;
        string clFileName = String.Empty;
        string formName = String.Empty;
        private float userScale;
        private float userTranslateX;
        private float userTranslateY;
        private float userRotation;
        private bool userMirrorX;
        private bool userMirrorY;
        private bool userInverted;

        private bool selectTool = true;

        // Mouse tracking.
        private double startLocationX = 0.0;
        private double startLocationY = 0.0;
        private double lastLocationX = 0.0;
        private double lastLocationY = 0.0;
        private bool hasMouse = false;
        private bool isPanning = false;
        private bool isSelecting = false;
        private bool hasSelection = false;
        private bool selectionFormOpen = false;

        Rectangle selectionRectangle = new Rectangle(new Point(0, 0), new Size(0, 0));
        Point startPoint;
        SelectionPropertiesFrm selectionPropertiesFrm;     // Form for displaying selected object properties.
        int visibleGerberFiles = 0;
        int visibleDrillFiles = 0;
        bool fullScreen = false;

        public Form1(string fileName)
        {
            InitializeComponent();

            pcbImagePanel.GetType().GetMethod("SetStyle",
                System.Reflection.BindingFlags.Instance |
                System.Reflection.BindingFlags.NonPublic).Invoke(pcbImagePanel,
                new object[]{ System.Windows.Forms.ControlStyles.UserPaint | 
	            System.Windows.Forms.ControlStyles.AllPaintingInWmPaint |
	            System.Windows.Forms.ControlStyles.OptimizedDoubleBuffer, true});

            pcbImagePanel.ContextMenuStrip = selectedObjectsContextMenuStrip;
            gerberLib = new LibGerberVS();

            clFileName = fileName;
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            selectedObjectsContextMenuStrip.Visible = false;
            formName = Text;
            Initialise();

            // Get the command line arguments, if any.
            if (!String.IsNullOrEmpty(clFileName))
            {
                if (Path.GetExtension(clFileName).ToLower() == ".gpf")
                    OpenProject(clFileName);

                else
                    OpenLayers(new string[] { clFileName });
            }
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            SaveAsGerberProject();
        }

        // Initialise variables and objects;
        private void Initialise()
        {
            Text = formName;
            hasProject = false;
            hasSelection = false;
            visibleDrillFiles = 0;
            visibleGerberFiles = 0;
            pcbImagePanel.BackColor = Color.Black;
            selectedObjectsToolStripStatusLabel.Text = "0";
            // Create a new project.
            project = gerberLib.CreateNewProject();
            project.BackgroundColor = pcbImagePanel.BackColor;

            renderInfo = new GerberRenderInformation();
            renderMode = GerberRenderMode.TranslateToCentre;
            RenderModeComboBox.SelectedIndex = 0;

            pcbImagePanel.AutoScrollMinSize = new System.Drawing.Size(0, 0);
            UpdateRulers();
            UpdateScale();
            fileListBox.Clear();
            UpdateMenus();
        }

        // Refresh the file list box after re-ordering layers either up or down.
        private void ReOrderFileList()
        {
            int index = fileListBox.SelectedIndex;
            if (fileListBox.ItemCount > 0)
                fileListBox.Clear();

            for (int i = 0; i < project.FileCount; i++)
                fileListBox.AddItem(project.FileInfo[i].IsVisible, project.FileInfo[i].Color, project.FileInfo[i].FileName);

            fileListBox.SelectedIndex = index;
        }

        private void GetLayerFiles()
        {
            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Title = "Open Gerber File(s)";
                openFileDialog.RestoreDirectory = true;
                openFileDialog.Multiselect = true;
                if (openFileDialog.ShowDialog() == DialogResult.OK)
                    OpenLayers(openFileDialog.FileNames);
            }
        }

        private void OpenLayers(string[] fileList)
        {
            int index = -1;
            foreach (string file in fileList)
            {
                // Don't open gerber project files here.
                if (Path.GetExtension(file).ToLower() == ".gpf")
                    continue;

                if (OpenLayer(file))
                {
                    if (fileListBox.SelectedIndex == -1)
                        fileListBox.SelectedIndex = 0;

                    index = project.FileCount - 1;
                    fileListBox.AddItem(project.FileInfo[index].IsVisible, project.FileInfo[index].Color, project.FileInfo[index].FileName);
                    LayerNameToolStripStatusLabel.Text = project.FileInfo[fileListBox.SelectedIndex].FileName;
                }
            }

            pcbImagePanel.Refresh();
            UpdateMenus();
            if (project.FileCount > 0)
                hasProject = true;

            if (project.ProjectName == string.Empty)
                this.Text = formName + " [Untitled Project]";
        }

        // Open layer file and if sucessful, add it to file list box.
        private bool OpenLayer(string fileName)
        {
            try
            {
                gerberLib.OpenLayerFromFilename(project, fileName);
                UpdateFileTypeCounts();
                return true;
            }

            catch (GerberDLLException ex)
            {
                string errorMessage = ex.Message;
                if (ex.InnerException != null)
                    errorMessage += ex.InnerException.Message;

                System.Windows.Forms.MessageBox.Show(errorMessage, "GerberView", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }

        // Open layer file with colour.
        private bool OpenLayer(string fileName, Color color)
        {
            int index = 0;
            try
            {
                gerberLib.OpenLayerFromFilenameAndColor(project, fileName, color);
                index = project.FileInfo.Count - 1;
                fileListBox.AddItem(true, project.FileInfo[index].Color, project.FileInfo[index].FileName);
                UpdateFileTypeCounts();
                return true;
            }

            catch (GerberDLLException ex)
            {
                string errorMessage = ex.Message;
                if (ex.InnerException != null)
                    errorMessage += ex.InnerException.Message;

                System.Windows.Forms.MessageBox.Show(errorMessage, "GerberView", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }

        private void NewProject()
        {
            SaveAsGerberProject();
            Initialise();
            UpdateMenus();
            pcbImagePanel.Refresh();
        }

        private void OpenProject()
        {
            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Title = "Open Gerber Project";
                openFileDialog.Filter = "Gerber Project File (.gpf)|*.gpf";
                openFileDialog.RestoreDirectory = true;
                openFileDialog.Multiselect = false;
                if (openFileDialog.ShowDialog() == DialogResult.OK)
                    OpenProject(openFileDialog.FileName);
            }
        }

        private void OpenProject(string fileName)
        {
            SaveAsGerberProject();
            UnloadAllLayers();
            ProjectFile.ReadProject(project, fileName);
            for (int i = project.FileCount - 1; i >= 0; i--)
                ReloadLayer(i);

            if (project.FileCount > 0)
            {
                hasProject = true;
                fileListBox.SelectedIndex = project.CurrentIndex;
                ReOrderFileList();
                UpdateFileTypeCounts();
                UpdateMenus();
                this.Text = formName + " [Project: " + project.Path + "]";
                userScale = (float)project.FileInfo[0].UserTransform.ScaleX;
                scaleToolStripStatusLabel.Text = (userScale * 100).ToString() + "%";
                pcbImagePanel.Refresh();
            }

            else
            {
                MessageBox.Show("No files in project, closing project.", "Empty Project", MessageBoxButtons.OK, MessageBoxIcon.Information);
                Initialise();
            }
        }

        private void SaveProjectAs()
        {
            try
            {
                using (SaveFileDialog saveFileDialog = new SaveFileDialog())
                {
                    saveFileDialog.Title = "Save Gerber Project As";
                    saveFileDialog.Filter = "Gerber Project File (.gpf)|*.gpf";
                    saveFileDialog.RestoreDirectory = true;
                    if (saveFileDialog.ShowDialog() == DialogResult.OK)
                    {
                        project.Path = saveFileDialog.FileName;
                        project.ProjectName = Path.GetFileNameWithoutExtension(saveFileDialog.FileName);
                        ProjectFile.WriteProject(project);
                        this.Text = formName + " [" + project.ProjectName + "]";
                    }
                }
            }

            catch (Exception ex)
            {
                MessageBox.Show("Error saving project." + Environment.NewLine + ex.Message, "Save Project As Error", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }

            finally
            {
                UpdateMenus();
            }
        }

        private void SaveProject()
        {
            try
            {
                if(project.FileCount > 0)
                    ProjectFile.WriteProject(project);
            }

            catch (Exception ex)
            {
                MessageBox.Show("Error saving project." + Environment.NewLine + ex.Message, "Save Project Error",
                                MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void SaveAsGerberProject()
        {
            if (hasProject && project.FileCount > 1)
            {
                if (project.ProjectName == String.Empty)
                {
                    DialogResult result = MessageBox.Show("Do you want to save this session as a project?", "Save As Gerber Project",
                                                          MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                    if (result == DialogResult.Yes)
                        SaveProjectAs();

                    return;
                }

                SaveProject();
            }
        }

        // Unloads the current layer.
        private void UnloadLayer()
        {
            int index = fileListBox.SelectedIndex;
            if (index > -1)
            {
                // Check if this layer has a user selection.
                if (hasSelection && selectionInfo.FileInfo.FileName == project.FileInfo[index].FileName)
                    hasSelection = false;

                CheckDirtyFlag(index);

                // Then unload the layer.
                gerberLib.UnloadLayer(project, index);
                fileListBox.RemoveAt(index);
                if (project.FileCount > 0)
                {
                    UpdateFileTypeCounts();
                    UpdateMenus();
                }

                else
                    Initialise();

                pcbImagePanel.Refresh();
            }
        }

        // Unload all open layers in current project.
        private void UnloadAllLayers()
        {
            if (project.FileCount > 0)
            {
                hasSelection = false;
                // Check if any files have been modified and require saving.
                for (int i = 0; i < project.FileCount; i++)
                    CheckDirtyFlag(i);

                gerberLib.UnloadAllLayers(project);
                Initialise();
                pcbImagePanel.Refresh();
            }
        }

        private void CheckDirtyFlag()
        {
            for (int i = 0; i < project.FileCount; i++)
            {
                if (project.FileInfo[i].LayerDirty)
                {
                    DialogResult result = MessageBox.Show(project.FileInfo[i].FileName + "has been changed./nSave changes.", "File Changed", MessageBoxButtons.YesNo);
                    if (result == System.Windows.Forms.DialogResult.Yes)
                    {
                        GerberVS.WriteGerberRS274X.RS274XFromImage(project.FileInfo[i].FullPathName, project.FileInfo[project.CurrentIndex].Image,
                        project.FileInfo[project.CurrentIndex].UserTransform);
                    }
                }
            }
        }

        // Reloads a file that already exists in a project.
        private void ReloadLayer(int index)
        {
            try
            {
                gerberLib.ReloadLayer(project, index);
            }

            catch (GerberDLLException ex)
            {
                // Error reloading file so report and remove it from the project.
                string errorMessage = ex.Message;
                if (ex.InnerException != null)
                    errorMessage += ex.InnerException.Message;

                System.Windows.Forms.MessageBox.Show(errorMessage + Environment.NewLine + "Removing file from project.", "GerberView",
                                                     MessageBoxButtons.OK, MessageBoxIcon.Error);

                gerberLib.UnloadLayer(project, index);
            }
        }

        // Check project files for changes.
        private void CheckDirtyFlag(int index)
        {
            if (project.FileInfo[index].LayerDirty)
            {
                DialogResult result = MessageBox.Show(project.FileInfo[index].FileName + " has been changed."
                                                      + Environment.NewLine + "Save changes?", "File Changed", MessageBoxButtons.YesNo);
                if (result == System.Windows.Forms.DialogResult.Yes)
                {
                    GerberVS.WriteGerberRS274X.RS274XFromImage(project.FileInfo[index].FullPathName, project.FileInfo[project.CurrentIndex].Image);
                }
            }
        }

        // Keeps track of the number of rs274x and drill files that are in the project and visible.
        private void UpdateFileTypeCounts()
        {
            visibleGerberFiles = 0;
            visibleDrillFiles = 0;
            foreach (GerberFileInformation fileInfo in project.FileInfo)
            {
                if (fileInfo.IsVisible)
                {
                    if (fileInfo.Image.FileType == GerberFileType.RS274X)
                        visibleGerberFiles++;

                    else if (fileInfo.Image.FileType == GerberFileType.Drill)
                        visibleDrillFiles++;
                }
            }
        }

        private void PcbImagePanel_Paint(object sender, PaintEventArgs e)
        {
            Graphics graphics = e.Graphics;
            if (project.FileCount > 0)
            {
                renderInfo.DisplayWidth = pcbImagePanel.Width / graphics.DpiX;
                renderInfo.DisplayHeight = pcbImagePanel.Height / graphics.DpiY;

                if (renderMode == GerberRenderMode.TranslateToCentre)
                {
                    gerberLib.TranslateToCentre(project, renderInfo);
                }

                // Manage scrollbars.
                Size imageSize = new Size((int)(renderInfo.ImageWidth * graphics.DpiX), (int)(renderInfo.ImageHeight * graphics.DpiY));   // Image size in pixels.
                pcbImagePanel.AutoScrollMinSize = imageSize;
                float scrollValueX = pcbImagePanel.AutoScrollPosition.X / graphics.DpiX;
                float scrollValueY = pcbImagePanel.AutoScrollPosition.Y / graphics.DpiY;
                renderInfo.Left += scrollValueX;
                renderInfo.Bottom -= scrollValueY;
                gerberLib.RenderAllLayers(graphics, project, renderInfo);
                if (hasSelection)
                    gerberLib.RenderSelectionLayer(graphics, selectionInfo, renderInfo);

                UpdateRulers();
            }

            fileListBox.Focus();
        }

        private void PcbImagePanel_MouseWheel(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            if (project.FileInfo.Count == 0)
                return;

            if (e.Delta < 0)
            {
                if (renderInfo.ScaleFactorX > 0.25f && renderInfo.ScaleFactorY > 0.25f)
                {
                    renderInfo.ScaleFactorX -= 0.25f;
                    renderInfo.ScaleFactorY -= 0.25f;
                }
            }

            if (e.Delta > 0)
            {
                if (renderInfo.ScaleFactorX < 10.0f && renderInfo.ScaleFactorY < 10.0f)
                {
                    renderInfo.ScaleFactorX += 0.25f;
                    renderInfo.ScaleFactorY += 0.25f;
                }
            }

            UpdateScale();
            pcbImagePanel.Refresh();
        }

        private void PcbImagePanel_Resize(object sender, EventArgs e)
        {
            pcbImagePanel.Refresh();
        }

        private void PcbImagePanel_Scroll(object sender, ScrollEventArgs e)
        {
            pcbImagePanel.Refresh();
        }

        private void FileListBox_CheckBoxClick(object sender, EventArgs e)
        {
            ChangeLayerVisiblity();
        }

        private void FileListBox_ColorBoxClick(object sender, EventArgs e)
        {
            ChangeLayerColour();
        }

        private void ChangeLayerVisiblity()
        {
            fileListBox.ItemChecked = !fileListBox.ItemChecked;
            project.FileInfo[fileListBox.SelectedIndex].IsVisible = fileListBox.ItemChecked;

            // Update the visible layers counts.
            if (project.FileInfo[fileListBox.SelectedIndex].Image.FileType == GerberFileType.RS274X)
            {
                if (project.FileInfo[fileListBox.SelectedIndex].IsVisible)
                    visibleGerberFiles++;

                else
                    visibleGerberFiles--;
            }

            if (project.FileInfo[fileListBox.SelectedIndex].Image.FileType == GerberFileType.Drill)
            {
                if (project.FileInfo[fileListBox.SelectedIndex].IsVisible)
                    visibleDrillFiles++;

                else
                    visibleDrillFiles--;
            }

            pcbImagePanel.Refresh();
            UpdateMenus();
        }

       /* private void ChangeLayerColour()
        {
            Color currentColor = project.FileInfo[fileListBox.SelectedIndex].Color;
            using (AlphaColorDialog colorDialog = new AlphaColorDialog())
            {
                colorDialog.AnyColor = true;
                colorDialog.AllowFullOpen = true;
                colorDialog.SolidColorOnly = false;
                colorDialog.Color = currentColor;
                if (colorDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    project.FileInfo[fileListBox.SelectedIndex].Color = colorDialog.Color;
                    project.FileInfo[fileListBox.SelectedIndex].Alpha = colorDialog.Color.A;
                    fileListBox.ItemColor = colorDialog.Color;
                    pcbImagePanel.Refresh();
                }
            }
        }*/

        private void ChangeLayerColour()
        {
            Color currentColor = project.FileInfo[fileListBox.SelectedIndex].Color;
            using (CustomColorDialog colorDialog = new CustomColorDialog(Handle))
            {
                colorDialog.Color = currentColor;
                if (colorDialog.ShowDialog())
                {
                    project.FileInfo[fileListBox.SelectedIndex].Color = colorDialog.Color;
                    fileListBox.ItemColor = colorDialog.Color;
                    pcbImagePanel.Refresh();
                }
            }
        }

        private void UpdateRulers()
        {
            if (renderInfo.ScaleFactorX < 1.0f)
            {
                horizonalRuler.ScaleMode = Ruler.ScaleMode.Inches;
                verticleRuler.ScaleMode = Ruler.ScaleMode.Inches;
                rulerScaleLabel.Text = "ins";
            }

            else
            {
                horizonalRuler.ScaleMode = Ruler.ScaleMode.Mils;
                verticleRuler.ScaleMode = Ruler.ScaleMode.Mils;
                rulerScaleLabel.Text = "mils";
            }

            // Calibrate ruler scale.
            float horizonalOffset = -(float)((renderInfo.Left) - userTranslateX);
            float verticleOffset = (float)(((renderInfo.DisplayHeight + renderInfo.Bottom)) + userTranslateX);
            horizonalRuler.ZoomFactor = renderInfo.ScaleFactorX;
            verticleRuler.ZoomFactor = renderInfo.ScaleFactorY;

            // Set start values.
            horizonalRuler.StartValue = (horizonalOffset / renderInfo.ScaleFactorX) * horizonalRuler.MajorInterval;
            verticleRuler.StartValue = -(verticleOffset / renderInfo.ScaleFactorY) * verticleRuler.MajorInterval;
        }

        private void RenderModeCombobox_SelectedIndexChanged(object sender, EventArgs e)
        {
            switch (RenderModeComboBox.SelectedIndex)
            {
                case 0:
                    renderInfo.RenderQuality = GerberRenderQuality.Default;
                    break;

                case 1:
                    renderInfo.RenderQuality = GerberRenderQuality.HighSpeed;
                    break;

                case 2:
                    renderInfo.RenderQuality = GerberRenderQuality.HighQuality;
                    break;
            }

            pcbImagePanel.Refresh();
            fileListBox.Focus();
        }

        private void HorizonalRuler_HoverValueChanged(object sender, Ruler.HoverValueChangedEventArgs e)
        {
            if (horizonalRuler.MouseLocation > -1)
            {
                xLocationToolStripStatusLabel.Text = String.Format("{0:0.000}", e.Value);
                lastLocationX = e.Value / verticleRuler.MajorInterval;
            }

            else
                xLocationToolStripStatusLabel.Text = String.Format("{0:0.000}", lastLocationX);
        }

        private void VerticleRuler_HoverValueChanged(object sender, HoverValueChangedEventArgs e)
        {
            if (verticleRuler.MouseLocation > -1)
            {
                yLocationToolStripStatusLabel.Text = String.Format("{0:0.000}", e.Value);
                lastLocationY = e.Value / verticleRuler.MajorInterval;
            }

            else
                yLocationToolStripStatusLabel.Text = String.Format("{0:0.000}", lastLocationY);
        }

        private void MoveLayerDown()
        {
            int index = fileListBox.SelectedIndex;
            int count = fileListBox.ItemCount - 1;
            if (index > -1)
            {
                if (index < count)
                {
                    gerberLib.ChangeLayerOrder(project, index, index + 1);
                    fileListBox.SelectedIndex++;
                    ReOrderFileList();
                    pcbImagePanel.Refresh();
                }
            }
        }

        private void MoveLayerUp()
        {
            int index = fileListBox.SelectedIndex;
            if (index > 0)
            {
                gerberLib.ChangeLayerOrder(project, index, index - 1);
                fileListBox.SelectedIndex--;
                ReOrderFileList();
                pcbImagePanel.Refresh();
            }
        }

        private void MoveLayerUpToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MoveLayerUp();
        }

        private void MoveLayerDownToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MoveLayerDown();
        }

        private void UnloadLayerToolStripMenuItem_Click(object sender, EventArgs e)
        {
            UnloadLayer();
        }

        private void MoveLayerUpToolStripButton_Click(object sender, EventArgs e)
        {
            MoveLayerUp();
        }

        private void MoveLayerDownToolStripButton_Click(object sender, EventArgs e)
        {
            MoveLayerDown();
        }

        private void AddFileToolStripButton_Click(object sender, EventArgs e)
        {
            GetLayerFiles();
        }

        private void UnloadLayerToolStripButton_Click(object sender, EventArgs e)
        {
            UnloadLayer();
        }

        private void FileListBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            project.CurrentIndex = fileListBox.SelectedIndex;
            if (fileListBox.SelectedIndex == -1)
                LayerNameToolStripStatusLabel.Text = String.Empty;

            else
                LayerNameToolStripStatusLabel.Text = project.FileInfo[project.CurrentIndex].FileName;

            UpdateMenus();
        }

        private void ExitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void GerberLayersToolStripMenuItem_Click(object sender, EventArgs e)
        {
            verticleRuler.MouseTracking = false;
            horizonalRuler.MouseTracking = false;
            using (GerberStatsForm gerberStats = new GerberStatsForm(project))
            {
                if (gerberStats.ShowDialog(this) == DialogResult.OK)
                {
                    verticleRuler.MouseTracking = true;
                    horizonalRuler.MouseTracking = true;
                }
            }
        }

        private void DrillLayersToolStripMenuItem_Click(object sender, EventArgs e)
        {
            verticleRuler.MouseTracking = false;
            horizonalRuler.MouseTracking = false;
            using (DrillStatsForm drillStats = new DrillStatsForm(project))
            {
                if (drillStats.ShowDialog(this) == DialogResult.OK)
                {
                    verticleRuler.MouseTracking = true;
                    horizonalRuler.MouseTracking = true;
                }
            }
        }

        // Subscribed to the selection properties form closed event.
        private void CloseSelectionForm(object sender, EventArgs e)
        {
            selectionFormOpen = false;
            displaySelectedOjectsToolStripMenuItem.Enabled = !selectionFormOpen;
            UpdateMenus();
        }

        private void UpdateMenus()
        {
            int index = fileListBox.SelectedIndex;

            // File menu.
            saveLayerToolStripMenuItem.Enabled = !project.IsEmpty;
            reloadAllLayersoolStripMenuItem.Enabled = !project.IsEmpty;
            saveProjectToolStripMenuItem.Enabled = !project.IsEmpty && project.Path != String.Empty;
            saveToolStripButton.Enabled = !project.IsEmpty && project.Path != String.Empty;
            saveProjectAsToolStripMenuItem.Enabled = !project.IsEmpty;
            newProjectToolStripMenuItem.Enabled = !project.IsEmpty;
            newToolStripButton.Enabled = !project.IsEmpty;
            printPreviewToolStripMenuItem.Enabled = !project.IsEmpty;
            printToolStripMenuItem.Enabled = !project.IsEmpty;
            exportImageToolStripMenuItem.Enabled = !project.IsEmpty;

            // Layer menu.
            layerToolStripMenuItem.Enabled = !project.IsEmpty;
            showAllLayersToolStripMenuItem.Enabled = (visibleGerberFiles + visibleDrillFiles) != fileListBox.ItemCount;
            hideAllLayersToolStripMenuItem.Enabled = (visibleGerberFiles + visibleDrillFiles) > 0;
            moveLayerUpToolStripButton.Enabled = index > 0;
            moveLayerUpToolStripMenuItem.Enabled = index > 0;
            moveLayerDownToolStripButton.Enabled = index < fileListBox.ItemCount - 1;
            moveLayerDownToolStripMenuItem.Enabled = index < fileListBox.ItemCount - 1;

            // Layer stats menu.
            statisticsToolStripMenuItem.Enabled = !project.IsEmpty;
            gerberLayersToolStripMenuItem.Enabled = visibleGerberFiles > 0;
            drillLayersToolStripMenuItem.Enabled = visibleDrillFiles > 0;
        }
        /*protected bool GetFilename(out string filename, DragEventArgs e)
        {
            bool ret = false;
            filename = String.Empty;

            if ((e.AllowedEffect & DragDropEffects.Copy) == DragDropEffects.Copy)
            {
                Array data = ((IDataObject)e.Data).GetData("FileName") as Array;
                if (data != null)
                {
                    if ((data.Length == 1) && (data.GetValue(0) is String))
                    {
                        filename = ((string[])data)[0];
                        string ext = Path.GetExtension(filename).ToLower();
                        if ((ext == ".jpg") || (ext == ".png") || (ext == ".bmp"))
                        {
                            ret = true;
                        }
                    }
                }
            }
            return ret;
        }*/

        private void FileListBox_DragEnter(object sender, DragEventArgs e)
        {
            //Debug.WriteLine("OnDragEnter");
            // Check if the Dataformat of the data can be accepted, only accept file drops from Explorer, etc.)
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
                e.Effect = DragDropEffects.Copy; // Ok.

            else
                e.Effect = DragDropEffects.None; // Unknown data, ignore it.
        }

        private void FileListBox_DragDrop(object sender, DragEventArgs e)
        {
            if (e.Effect != DragDropEffects.None)
            {
                string[] fileList = (string[])e.Data.GetData(DataFormats.FileDrop, false);
                OpenLayers(fileList);
                /*{
                    foreach (string file in fileList)
                    {
                        if (OpenLayer(file))
                        {
                            if (fileListBox.SelectedIndex == -1)
                                fileListBox.SelectedIndex = 0;

                            LayerNameToolStripStatusLabel.Text = project.FileInfo[fileListBox.SelectedIndex].FileName;
                            // Open all layers in the same scale.
                            int index = project.FileCount - 1;
                            project.FileInfo[index].UserTransform.ScaleX = userScale;
                            project.FileInfo[index].UserTransform.ScaleY = userScale;
                        }
                    }

                    ReOrderFileList();
                    pcbImagePanel.Refresh();
                    UpdateMenus();
                }*/
            }
        }

        private void PcbImagePanel_MouseDown(object sender, MouseEventArgs e)
        {
            hasMouse = true;
            isSelecting = false;
            selectionRectangle = Rectangle.Empty;

            if (e.Button == MouseButtons.Left)
            {
                startPoint = pcbImagePanel.PointToScreen(new Point(e.X, e.Y));
                //startPoint = new Point(e.X, e.Y);
            }

            startLocationX = lastLocationX;
            startLocationY = lastLocationY;
        }

        private void PcbImagePanel_MouseMove(object sender, MouseEventArgs e)
        {
            int width = 0;
            int height = 0;

            if (e.Button == MouseButtons.Left && project.CurrentIndex > -1 && pcbImagePanel.Capture)
            {
                //Graphics graphics = pcbImagePanel.CreateGraphics();
                ControlPaint.DrawReversibleFrame(selectionRectangle, this.BackColor, FrameStyle.Dashed);
                Point endPoint = pcbImagePanel.PointToScreen(new Point(e.X, e.Y));
                //Point endPoint = new Point(e.X, e.Y);
                width = endPoint.X - startPoint.X;
                height = endPoint.Y - startPoint.Y;
                selectionRectangle.X = startPoint.X;
                selectionRectangle.Y = startPoint.Y;
                selectionRectangle.Width = width;
                selectionRectangle.Height = height;
                //DrawSelection(graphics, selectionRectangle);
                //Draw the new rectangle by calling DrawReversibleFrame again.  
                ControlPaint.DrawReversibleFrame(selectionRectangle, this.BackColor, FrameStyle.Dashed);
                isSelecting = true;
            }
        }

        private void PcbImagePanel_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                selectedObjectsContextMenuStrip.Visible = false;
                displaySelectedOjectsToolStripMenuItem.Enabled = !selectionFormOpen;
                if (project.FileCount > 0 && hasSelection)
                {
                    selectedObjectsContextMenuStrip.Visible = true;
                    selectedObjectsContextMenuStrip.Show();
                }
            }

            else if (e.Button == MouseButtons.Left)
            {

                // Note: Selection is only possible on the currently selected visible layer.
                using (Graphics graphics = pcbImagePanel.CreateGraphics())
                {
                    graphics.PageUnit = GraphicsUnit.Inch;

                    if (project.CurrentIndex > -1 || project.ShowHiddenSelection == true)
                    {
                        if (hasSelection && selectionFormOpen)
                            selectionPropertiesFrm.Close();

                        selectionInfo = new SelectionInformation(project.FileInfo[project.CurrentIndex]);
                        hasSelection = false;

                        // Point selecting.
                        if (hasMouse && !isSelecting)
                        {
                            selectionInfo.SelectionType = GerberSelection.PointClick;
                            selectionInfo.LowerLeftX = lastLocationX;
                            selectionInfo.LowerLeftY = lastLocationY;

                            int count = selectionInfo.FileInfo.Image.GerberNetList.Count;
                            for (int index = 0; index < count; index++)
                            {
                                gerberLib.ObjectInSelectedRegion(graphics, selectionInfo, ref index);
                                if (selectionInfo.Count > 0)
                                    hasSelection = true;
                            }
                        }

                        // Region selecting.
                        if (hasMouse && isSelecting)
                        {
                            ControlPaint.DrawReversibleFrame(selectionRectangle, this.BackColor, FrameStyle.Dashed);
                            selectionInfo.SelectionType = GerberSelection.DragBox;
                            selectionInfo.LowerLeftX = startLocationX;
                            selectionInfo.UpperRightX = (float)lastLocationX;
                            selectionInfo.UpperRightY = startLocationY;
                            selectionInfo.LowerLeftY = (float)lastLocationY;
                            if (startLocationX > lastLocationX)
                            {
                                selectionInfo.UpperRightX = startLocationX;
                                selectionInfo.LowerLeftX = lastLocationX;
                            }

                            if (startLocationY < lastLocationY)
                            {
                                selectionInfo.UpperRightY = lastLocationY;
                                selectionInfo.LowerLeftY = startLocationY;
                            }


                            int count = selectionInfo.FileInfo.Image.GerberNetList.Count;
                            for (int index = 0; index < count; index++)
                            {
                                gerberLib.ObjectInSelectedRegion(graphics, selectionInfo, ref index);
                                if (selectionInfo.Count > 0)
                                    hasSelection = true;
                            }

                            isSelecting = false;
                        }

                        hasMouse = false;
                        selectedObjectsToolStripStatusLabel.Text = selectionInfo.Count.ToString();
                        UpdateMenus();
                        pcbImagePanel.Refresh();
                    }
                }
            }
        }

       /* private void DrawSelection(Graphics graphics, Rectangle sel)
        {
            graphics.DrawRectangle(new Pen(Color.White, 1), sel);
            pcbImagePanel.Invalidate(Rectangle.Inflate(sel, 1, 1));
        }*/

        private void ZoomIn()
        {
            if (project.FileInfo.Count == 0)
                return;

            if (renderInfo.ScaleFactorX < 10.0f && renderInfo.ScaleFactorY < 10.0f)
            {
                renderInfo.ScaleFactorX += 0.25f;
                renderInfo.ScaleFactorY += 0.25f;
                UpdateScale();
                pcbImagePanel.Refresh();
            }
        }

        private void ZoomOut()
        {
            if (project.FileInfo.Count == 0)
                return;

            if(renderInfo.ScaleFactorX > 0.25f && renderInfo.ScaleFactorY > 0.25f)
            {
                renderInfo.ScaleFactorX -= 0.25f;
                renderInfo.ScaleFactorY -= 0.25f;
                UpdateScale();
                pcbImagePanel.Refresh();
            }
        }

        private void ZoomToFit()
        {
            if (project.FileInfo.Count == 0)
                return;

            gerberLib.ZoomToFit(project, renderInfo);
            UpdateScale();
            pcbImagePanel.Refresh();
        }

        private void UpdateScale()
        {
            int scale = (int)Math.Ceiling(renderInfo.ScaleFactorX * 100);
            scaleToolStripStatusLabel.Text = scale.ToString() + "%";
        }

        #region Layer menu events
        private void ShowAllLayersToolStripMenuItem_Click(object sender, EventArgs e)
        {
            int index = fileListBox.SelectedIndex;

            for (int i = 0; i < fileListBox.ItemCount; i++)
            {
                fileListBox.SelectedIndex = i;
                fileListBox.ItemChecked = true;
                project.FileInfo[i].IsVisible = true;
            }

            fileListBox.SelectedIndex = index;
            pcbImagePanel.Refresh();
            UpdateFileTypeCounts();
            UpdateMenus();
        }

        private void HideAllLayersToolStripMenuItem_Click(object sender, EventArgs e)
        {
            int index = fileListBox.SelectedIndex;

            for (int i = 0; i < fileListBox.ItemCount; i++)
            {
                fileListBox.SelectedIndex = i;
                fileListBox.ItemChecked = false;
                project.FileInfo[i].IsVisible = false;
            }

            fileListBox.SelectedIndex = index;
            pcbImagePanel.Refresh();
            UpdateFileTypeCounts();
            UpdateMenus();
        }

        private void UnloadAllLayersToolStripMenuItem_Click(object sender, EventArgs e)
        {
            UnloadAllLayers();
            this.Text = formName;
        }

        private void ToggleVisabiltyToolStripMenuItem_Click(object sender, EventArgs e)
        {
            fileListBox.ItemChecked = !fileListBox.ItemChecked;
            ChangeLayerVisiblity();
        }

        private void ChangeColorToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ChangeLayerColour();
        }

        private void InvertColorToolStripMenuItem_Click(object sender, EventArgs e)
        {
            bool state = project.FileInfo[fileListBox.SelectedIndex].UserTransform.Inverted;
            project.FileInfo[fileListBox.SelectedIndex].UserTransform.Inverted = !state;
            pcbImagePanel.Refresh();
        }

        private void ReloadLayerToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ReloadLayer(fileListBox.SelectedIndex);
            pcbImagePanel.Refresh();
        }

        private void EditLayerToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (LayerEditForm layerEditForm = new LayerEditForm(project))
            {
                layerEditForm.ShowDialog(this);
            }

        }

        #endregion

        #region View menu events

        private void ZoomInToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ZoomIn();
        }

        private void ZoomOutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ZoomOut();
        }

        private void ZoomToFitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ZoomToFit();
        }

        private void ZoomToFullSizeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            renderInfo.ScaleFactorX = renderInfo.ScaleFactorY = 1.0f;
            UpdateScale();
            pcbImagePanel.Refresh();
        }

        private void ChangeBackgroundToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Color currentColor = project.BackgroundColor;
            using (ColorDialog colorDialog = new ColorDialog())
            {
                colorDialog.AnyColor = true;
                colorDialog.AllowFullOpen = true;
                colorDialog.SolidColorOnly = false;
                colorDialog.Color = currentColor;
                colorDialog.ShowDialog();
                project.BackgroundColor = colorDialog.Color;
                pcbImagePanel.BackColor = colorDialog.Color;
                pcbImagePanel.Refresh();
            }
        }

        private void FullScreenToolStripMenuItem_Click(object sender, EventArgs e)
        {
            fullScreen = !fullScreen;
            if (fullScreen)
            {
                if (this.WindowState == FormWindowState.Maximized)
                    this.WindowState = FormWindowState.Normal;

                this.FormBorderStyle = FormBorderStyle.None;
                this.WindowState = FormWindowState.Maximized;
            }

            else
            {
                this.FormBorderStyle = FormBorderStyle.Sizable;
                this.WindowState = FormWindowState.Normal;
            }
        }

        private void ShowToolbarToolStripMenuItem_Click(object sender, EventArgs e)
        {
            toolStrip1.Visible = showToolbarToolStripMenuItem.Checked;
            if (!toolStrip1.Visible)
            {
                splitContainer1.Top -= toolStrip1.Height;
                splitContainer1.Height += toolStrip1.Height;
            }

            else
            {
                splitContainer1.Top += toolStrip1.Height;
                splitContainer1.Height -= toolStrip1.Height;
            }
        }

        private void ShowSidepaneToolStripMenuItem_Click(object sender, EventArgs e)
        {
            splitContainer1.Panel1Collapsed = !showSidepaneToolStripMenuItem.Checked;
        }

        #endregion

        #region Toolstrip events

        private void SaveToolStripButton_Click(object sender, EventArgs e)
        {
            SaveProject();
        }

        private void NewToolStripButton_Click(object sender, EventArgs e)
        {
            NewProject();
        }

        private void OpenToolStripButton_Click(object sender, EventArgs e)
        {
            OpenProject();
        }
        private void ZoomInToolStripButton_Click(object sender, EventArgs e)
        {
            ZoomIn();
        }

        private void ZoomOutToolStripButton_Click(object sender, EventArgs e)
        {
            ZoomOut();
        }

        private void ZoomToFitToolStripButton_Click(object sender, EventArgs e)
        {
            ZoomToFit();
            pcbImagePanel.Refresh();
        }

        private void SelectToolStripButton_Click(object sender, EventArgs e)
        {
            selectTool = true;
        }

        #endregion

        #region File menu events

        private void OpenLayersToolStripMenuItem_Click(object sender, EventArgs e)
        {
            GetLayerFiles();
        }

        private void ReloadAllLayersToolStripMenuItem_Click(object sender, EventArgs e)
        {
            for (int i = 0; i < project.FileCount; i++)
                ReloadLayer(i);

            pcbImagePanel.Refresh();
        }

        private void NewProjectToolStripMenuItem_Click(object sender, EventArgs e)
        {
            NewProject();
        }

        private void OpenProjectToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenProject();
        }

        private void SaveProjectToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SaveProject();
        }

        private void SaveProjectAsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SaveProjectAs();
        }

        private void PrintPreviewToolStripMenuItem_Click(object sender, EventArgs e)
        {
            bool isWider = (renderInfo.ImageWidth >= renderInfo.ImageHeight);
            PrintDocument printDocument = new PrintDocument();
            PrintPreviewDialog printPreviewDialog = new PrintPreviewDialog();
            printPreviewDialog.Icon = System.Drawing.Icon.ExtractAssociatedIcon(Application.ExecutablePath);
            printPreviewDialog.WindowState = FormWindowState.Normal;
            printPreviewDialog.Text = "Gerber Image Preview";
            printPreviewDialog.Document = printDocument;
            printDocument.DefaultPageSettings.Landscape = isWider;
            printDocument.PrintPage += new PrintPageEventHandler(this.PrintPage);
            printPreviewDialog.ShowDialog();
        }

        private void PrintToolStripMenuItem_Click(object sender, EventArgs e)
        {
            PrintGerberImage();
        }

        #endregion

        private void PrintGerberImage()
        {
            bool isWider = (renderInfo.ImageWidth > renderInfo.ImageHeight);
            PrintDocument printDocument = new PrintDocument();
            PrintDialog printDialog = new PrintDialog();

            printDialog.UseEXDialog = true;
            if (printDialog.ShowDialog() == DialogResult.OK)
            {
                printDialog.Document = printDocument;
                printDocument.DefaultPageSettings.Landscape = isWider;
                printDocument.PrintPage += new PrintPageEventHandler(this.PrintPage);
                printDocument.Print();
            }

            printDialog.Dispose();
            printDocument.Dispose();
        }

        private void PrintPage(object sender, PrintPageEventArgs e)
        {
            Graphics graphics = e.Graphics;

            graphics.PageUnit = GraphicsUnit.Inch;
            float pageWidth = e.PageBounds.Width / 100.0f;
            float pageHeight = e.PageBounds.Height / 100.0f;
            renderInfo.DisplayWidth = pageWidth;
            renderInfo.DisplayHeight = pageHeight;
            gerberLib.TranslateToCentre(project, renderInfo);
            gerberLib.RenderAllLayersForVectorOutput(graphics, project, renderInfo);
        }

        private void DisplaySelectedOjectsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            selectionPropertiesFrm = new SelectionPropertiesFrm(selectionInfo);
            selectionPropertiesFrm.FormClosing += new FormClosingEventHandler(CloseSelectionForm);
            selectionPropertiesFrm.Location = new Point(this.Left, this.Top);
            selectionPropertiesFrm.Show();
            selectionFormOpen = true;
        }

        private void DeleteSelectedObjectsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (project.CheckBeforeDelete)
            {
                DialogResult result = MessageBox.Show("Confirm to delete the selected nets.\nThis operation can not undone.", "GerberView", MessageBoxButtons.YesNo);
                if (result == DialogResult.Yes)
                {
                    GerberImage image = project.FileInfo[project.CurrentIndex].Image;
                    foreach (int i in selectionInfo.SelectedNodeArray.SelectedNetIndex)
                        image.DeleteNet(i);

                    project.FileInfo[project.CurrentIndex].LayerDirty = true;
                    pcbImagePanel.Refresh();
                }
            }
        }

        private void RS274XGerberToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (project.FileInfo[project.CurrentIndex].Image.FileType != GerberFileType.RS274X)
            {
                MessageBox.Show("Can not export a NC Drill file to a Gerber RS247-X.", "File Export Error", MessageBoxButtons.OK);
                return;
            }

            using (SaveFileDialog saveFileDialog = new SaveFileDialog())
            {
                saveFileDialog.Title = "Save Gerber File As";
                saveFileDialog.Filter = "Gerber Project File (.gbx)|*.gbx";
                saveFileDialog.RestoreDirectory = true;
                if (saveFileDialog.ShowDialog() == DialogResult.OK)
                {
                    GerberVS.WriteGerberRS274X.RS274XFromImage(saveFileDialog.FileName, project.FileInfo[project.CurrentIndex].Image,
                    project.FileInfo[project.CurrentIndex].UserTransform);
                }
            }
        }

        private void ExcellonDrillFileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (project.FileInfo[project.CurrentIndex].Image.FileType != GerberFileType.Drill)
            {
                MessageBox.Show("Can not export a Gerber RS247-X file to NC Drill.", "File Export Error", MessageBoxButtons.OK);
                return;
            }

            using (SaveFileDialog saveFileDialog = new SaveFileDialog())
            {
                saveFileDialog.Title = "Save Drill File As";
                saveFileDialog.Filter = "Gerber Project File (.nc)|*.nc";
                saveFileDialog.RestoreDirectory = true;
                if (saveFileDialog.ShowDialog() == DialogResult.OK)
                {
                    GerberVS.WriteExcellonDrill.DrillFileFromImage(saveFileDialog.FileName, project.FileInfo[project.CurrentIndex].Image,
                    project.FileInfo[project.CurrentIndex].UserTransform);
                }
            }
        }

        private void PngImageToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            using (SaveFileDialog saveFileDialog = new SaveFileDialog())
            using (Graphics graphics = pcbImagePanel.CreateGraphics())
            {
                saveFileDialog.Title = "Save Gerber Project As";
                saveFileDialog.Filter = "PNG Image (.png)|*.png";
                saveFileDialog.RestoreDirectory = true;
                if (saveFileDialog.ShowDialog() == DialogResult.OK)
                    gerberLib.ExportProjectToPng(saveFileDialog.FileName, project, renderInfo);
            }
        }

        private void SaveLayerToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (project.FileInfo[project.CurrentIndex].Image.FileType == GerberFileType.RS274X)
            {
                GerberVS.WriteGerberRS274X.RS274XFromImage(project.FileInfo[project.CurrentIndex].FullPathName, project.FileInfo[project.CurrentIndex].Image,
                    project.FileInfo[project.CurrentIndex].UserTransform);
            }

            if (project.FileInfo[project.CurrentIndex].Image.FileType != GerberFileType.Drill)
            {
                GerberVS.WriteExcellonDrill.DrillFileFromImage(project.FileInfo[project.CurrentIndex].FullPathName, project.FileInfo[project.CurrentIndex].Image,
                    project.FileInfo[project.CurrentIndex].UserTransform);
            }
        }


    }
}
