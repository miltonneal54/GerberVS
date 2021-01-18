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
using ColorDialogEx;

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
        string[] fileList = null;
        string clFileName = String.Empty;
        string formName = String.Empty;
        private float userScale;
        private float userTranslateX;
        private float userTranslateY;
        private float userRotation;
        private bool userMirrorX;
        private bool userMirrorY;
        private bool userInverted;
        //private bool applyToAll = false;        // True if user transformation applies to all layers.

        // Mouse tracking.
        private double startLocationX = 0.0;
        private double startLocationY = 0.0;
        private double lastLocationX = 0.0;
        private double lastLocationY = 0.0;
        private bool hasMouse = false;
        private bool isDragging = false;
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
            clFileName = fileName;
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            selectedObjectsContextMenuStrip.Visible = false;
            formName = this.Text;
            Initialise();
        }

        // Initialise variables and objects;
        private void Initialise()
        {
            hasProject = false;
            hasSelection = false;
            userScale = 1.0f;
            userTranslateX = 0.0f;
            userTranslateY = 0.0f;
            userRotation = 0.0f;
            userMirrorX = false;
            userMirrorY = false;
            userInverted = false;
            visibleDrillFiles = 0;
            visibleGerberFiles = 0;

            gerberLib = new LibGerberVS();
            renderInfo = new GerberRenderInformation();
            RenderModeComboBox.SelectedIndex = 0;
            renderMode = GerberRenderMode.TranslateToCentre;
            pcbImagePanel.BackColor = Color.Black;
            project = gerberLib.CreateNewProject();
            project.BackgroundColor = pcbImagePanel.BackColor;
            fileListBox.Clear();
            this.Text = formName;
            if (!String.IsNullOrEmpty(clFileName))
            {
                if (Path.GetExtension(clFileName).ToLower() == ".gpf")
                    OpenProjectWithName(clFileName);

                else
                {
                    fileList = new string[] { clFileName };
                    OpenLayers();
                }
            }

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
                {
                    fileList = openFileDialog.FileNames;
                    OpenLayers();
                }
            }
        }

        private void OpenLayers()
        {
            int index = -1;
            foreach (string file in fileList)
            {
                // Don't try to open gerber project files here.
                if (Path.GetExtension(file).ToLower() == ".gpf")
                    continue;

                if (OpenLayer(file))
                {
                    if (fileListBox.SelectedIndex == -1)
                        fileListBox.SelectedIndex = 0;

                    index = project.FileCount - 1;
                    fileListBox.AddItem(project.FileInfo[index].IsVisible, project.FileInfo[index].Color, project.FileInfo[index].FileName);
                    LayerNameToolStripStatusLabel.Text = project.FileInfo[fileListBox.SelectedIndex].FileName;
                    // Open all layers in the same scale.
                    project.FileInfo[index].UserTransform.ScaleX = userScale;
                    project.FileInfo[index].UserTransform.ScaleY = userScale;
                }
            }

            pcbImagePanel.Refresh();
            UpdateMenus();
            if (project.ProjectName == string.Empty)
                this.Text = formName + " [Untitled Project]";
        }

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

        // Open file and if sucessful, add it to file list box.
        private bool OpenLayer(string file, Color color, int alpha)
        {
            int index = 0;
            try
            {
                gerberLib.OpenLayerFromFilenameAndColor(project, file, color, alpha);
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

        private void OpenProject()
        {
            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Title = "Open Gerber Project";
                openFileDialog.Filter = "Gerber Project File (.gpf)|*.gpf";
                openFileDialog.RestoreDirectory = true;
                openFileDialog.Multiselect = false;
                if (openFileDialog.ShowDialog() == DialogResult.OK)
                    OpenProjectWithName(openFileDialog.FileName);
            }
        }

        private void OpenProjectWithName(string fileName)
        {
            if (hasProject) // Save current project;
                SaveProject();

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
                this.Text = formName + " [Project: " + project.ProjectName + "]";
                userScale = (float)project.FileInfo[0].UserTransform.ScaleX;
                scaleToolStripStatusLabel.Text = (userScale * 100).ToString() + "%";
                pcbImagePanel.Refresh();
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
                ProjectFile.WriteProject(project);
            }

            catch (Exception ex)
            {
                MessageBox.Show("Error saving project." + Environment.NewLine + ex.Message, "Save Project Error", MessageBoxButtons.OK, MessageBoxIcon.Information);
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
                fileListBox.Clear();
                visibleDrillFiles = 0;
                visibleGerberFiles = 0;
                hasProject = false;
                userScale = 1.0f;
                UpdateUserTransform();
                UpdateRulers();
                pcbImagePanel.Refresh();
                UpdateMenus();
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
                        GerberVS.ExportGerberRS274X.RS274xFromImage(project.FileInfo[i].FullPathName, project.FileInfo[project.CurrentIndex].Image,
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

        // Unloads the current layer.
        private void UnloadLayer()
        {
            int index = fileListBox.SelectedIndex;
            if (index > -1)
            {
                // Check if this layer has a user selection.
                if (hasSelection && selectionInfo.SelectedFileInfo.FileName == project.FileInfo[index].FileName)
                    hasSelection = false;

                CheckDirtyFlag(index);

                // Then unload the layer.
                gerberLib.UnloadLayer(project, index);
                fileListBox.RemoveAt(index);
                pcbImagePanel.Refresh();
                UpdateFileTypeCounts();
                UpdateMenus();
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
                    GerberVS.ExportGerberRS274X.RS274xFromImage(project.FileInfo[index].FullPathName, project.FileInfo[project.CurrentIndex].Image);
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
                float panelWidth = pcbImagePanel.Width / graphics.DpiX;
                float panelHeight = pcbImagePanel.Height / graphics.DpiY;

                if (renderMode == GerberRenderMode.TranslateToCentre)
                {
                    renderInfo.DisplayWidth = panelWidth;
                    renderInfo.DisplayHeight = panelHeight;
                    gerberLib.TranslateToCentreDisplay(project, renderInfo);
                }

                // Manage scrollbars.
                Size imageSize = new Size((int)(renderInfo.ImageWidth * graphics.DpiX), (int)(renderInfo.ImageHeight * graphics.DpiY));   // Image size in pixels.
                pcbImagePanel.AutoScrollMinSize = imageSize;
                float scrollValueX = pcbImagePanel.AutoScrollPosition.X / graphics.DpiX;
                float scrollValueY = pcbImagePanel.AutoScrollPosition.Y / graphics.DpiY;
                renderInfo.ScrollValueX = scrollValueX;
                renderInfo.ScrollValueY = scrollValueY;
                gerberLib.RenderAllLayers(graphics, project, renderInfo);
                if (hasSelection)
                    gerberLib.RenderSelectionLayer(graphics, selectionInfo, renderInfo);

                UpdateRulers();
            }

            splitContainer1.Panel2.Focus();
        }

        // Use the mouse wheel to set the image scale in 20% steps and apply to loaded all layers independant of visiblity.
        private void PcbImagePanel_MouseWheel(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            if (project.FileInfo.Count == 0)
                return;

            if (e.Delta < 0)
            {
                userScale -= 0.25f;
                if (userScale < 0.25f)
                    userScale = 0.25f;
            }

            if (e.Delta > 0)
            {
                userScale += 0.25f;
                if (userScale > 10.0f)
                    userScale = 10.0f;
            }

            UpdateUserTransform();
            pcbImagePanel.Refresh();

        }

        private void UpdateUserTransform()
        {
            if (userScale < 0.25f)
                userScale = 0.25f;

            if (userScale > 10.0f)
                userScale = 10.0f;

            foreach (GerberFileInformation fileInfo in project.FileInfo)
            {
                fileInfo.UserTransform.ScaleX = userScale;
                fileInfo.UserTransform.ScaleY = userScale;
                fileInfo.UserTransform.Rotation = userRotation;
                fileInfo.UserTransform.TranslateX = userTranslateX;
                fileInfo.UserTransform.TranslateY = userTranslateY;
            }

            int scale = (int)Math.Ceiling(userScale * 100);
            scaleToolStripStatusLabel.Text = scale.ToString() + "%";
        }

        // Resets the global user translations to default values.
        private void ResetUserTransform()
        {
            userScale = 1.0f;
            userTranslateX = 0.0f;
            userTranslateY = 0.0f;
            userRotation = 0.0f;
            userMirrorX = false;
            userMirrorY = false;
            userInverted = false;
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

        private void ChangeLayerColour()
        {
            Color currentColor = project.FileInfo[fileListBox.SelectedIndex].Color;
            using (AlphaColorDialog colorDialog = new AlphaColorDialog())
            // using (ColorDialog colorDialog = new ColorDialog())
            {
                colorDialog.AnyColor = true;
                colorDialog.AllowFullOpen = true;
                colorDialog.SolidColorOnly = false;
                colorDialog.Color = currentColor;
                colorDialog.ShowDialog();
                project.FileInfo[fileListBox.SelectedIndex].Color = colorDialog.Color;
                project.FileInfo[fileListBox.SelectedIndex].Alpha = colorDialog.Color.A;
                fileListBox.ItemColor = colorDialog.Color;
                pcbImagePanel.Refresh();
            }
        }

        private void UpdateRulers()
        {
            if (userScale < 1.0f)
            {
                horizonalRuler.ScaleMode = RulerControl.ScaleMode.Inches;
                verticleRuler.ScaleMode = RulerControl.ScaleMode.Inches;
            }

            else
            {
                horizonalRuler.ScaleMode = RulerControl.ScaleMode.Mils;
                verticleRuler.ScaleMode = RulerControl.ScaleMode.Mils;
            }

            // Calibrate ruler scale.
            //float horizonalOffset = (float)((renderInfo.Left - renderInfo.ScrollValueX) - project.UserTransform.TranslateX);
            //float verticleOffset = (float)((renderInfo.DisplayHeight - (renderInfo.Bottom + renderInfo.ImageHeight + renderInfo.ScrollValueY)) + project.UserTransform.TranslateY);
            float horizonalOffset = -(float)((renderInfo.Left + renderInfo.ScrollValueX) - userTranslateX);
            float verticleOffset = (float)(((renderInfo.DisplayHeight + renderInfo.Bottom - renderInfo.ScrollValueY)) + userTranslateX);
            horizonalRuler.ZoomFactor = userScale;
            verticleRuler.ZoomFactor = userScale;

            //horizonalRuler.StartValue = -((renderInfo.Left + project.UserTransform.TranslateX) / userScaleX) * horizonalRuler.MajorInterval;
            //verticleRuler.StartValue = -((renderInfo.DisplayHeight + renderInfo.Bottom + project.UserTransform.TranslateY) / userScaleY) * verticleRuler.MajorInterval;
            horizonalRuler.StartValue = (horizonalOffset / userScale) * horizonalRuler.MajorInterval;
            verticleRuler.StartValue = -(verticleOffset / userScale) * verticleRuler.MajorInterval;

            // Update ruler displays.
            horizonalRuler.Refresh();
            verticleRuler.Refresh();
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

        private void HorizontalRuler_HoverValue(object sender, RulerControl.Ruler.HoverValueChangedEventArgs e)
        {
            if (horizonalRuler.MouseLocation > -1)
            {
                xLocationToolStripStatusLabel.Text = String.Format("{0:0.000}", e.Value);
                lastLocationX = e.Value;
            }

            else
                xLocationToolStripStatusLabel.Text = String.Format("{0:0.000}", lastLocationX);

            lastLocationX /= verticleRuler.MajorInterval;
        }

        private void VerticalRuler_HoverValue(object sender, RulerControl.Ruler.HoverValueChangedEventArgs e)
        {
            if (verticleRuler.MouseLocation > -1)
            {
                yLocationToolStripStatusLabel.Text = String.Format("{0:0.000}", e.Value);
                lastLocationY = e.Value;
            }

            else
                yLocationToolStripStatusLabel.Text = String.Format("{0:0.000}", lastLocationY);

            lastLocationY /= verticleRuler.MajorInterval;
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
            verticleRuler.MouseTrackingOn = false;
            horizonalRuler.MouseTrackingOn = false;
            using (GerberStatsForm gerberStats = new GerberStatsForm(project))
            {
                if (gerberStats.ShowDialog(this) == DialogResult.OK)
                {
                    verticleRuler.MouseTrackingOn = true;
                    horizonalRuler.MouseTrackingOn = true;
                }
            }
        }

        private void DrillLayersToolStripMenuItem_Click(object sender, EventArgs e)
        {
            verticleRuler.MouseTrackingOn = false;
            horizonalRuler.MouseTrackingOn = false;
            using (DrillStatsForm drillStats = new DrillStatsForm(project))
            {
                if (drillStats.ShowDialog(this) == DialogResult.OK)
                {
                    verticleRuler.MouseTrackingOn = true;
                    horizonalRuler.MouseTrackingOn = true;
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
            reloadAllLayersoolStripMenuItem.Enabled = !project.IsEmpty;
            saveProjectToolStripMenuItem.Enabled = !project.IsEmpty && project.Path != String.Empty;
            saveProjectAsToolStripMenuItem.Enabled = !project.IsEmpty;
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

            // Later stats menu.
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
                {
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
                }
            }
        }

        private void PcbImagePanel_MouseDown(object sender, MouseEventArgs e)
        {
            hasMouse = true;
            if (e.Button == MouseButtons.Left)
                startPoint = pcbImagePanel.PointToScreen(new Point(e.X, e.Y));

            startLocationX = lastLocationX;
            startLocationY = lastLocationY;
        }

        private void PcbImagePanel_MouseMove(object sender, MouseEventArgs e)
        {
            int width = 0;
            int height = 0;

            isSelecting = false;
            if (e.Button == MouseButtons.Left && hasMouse && project.CurrentIndex > -1)
            {
                Graphics graphics = pcbImagePanel.CreateGraphics();
                ControlPaint.DrawReversibleFrame(selectionRectangle, this.BackColor, FrameStyle.Dashed);
                Point endPoint = pcbImagePanel.PointToScreen(new Point(e.X, e.Y));
                //Point endPoint = new Point(e.X, e.Y);
                width = endPoint.X - startPoint.X;
                height = endPoint.Y - startPoint.Y;
                selectionRectangle = new Rectangle(startPoint.X, startPoint.Y, width, height);
                //DrawSelection(graphics, selectionRectangle);
                // Draw the new rectangle by calling DrawReversibleFrame again.  
                ControlPaint.DrawReversibleFrame(selectionRectangle, this.BackColor, FrameStyle.Dashed);
                if (Math.Abs(width) > 5 || Math.Abs(height) > 5)
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
                Graphics graphics = pcbImagePanel.CreateGraphics();
                graphics.PageUnit = GraphicsUnit.Inch;

                if (project.CurrentIndex > -1 && project.ShowHiddenSelection == true)
                {
                    if (hasSelection && selectionFormOpen)
                        selectionPropertiesFrm.Close();

                    selectionRectangle = new Rectangle(0, 0, 0, 0);
                    selectionInfo = new SelectionInformation(project.FileInfo[project.CurrentIndex]);
                    hasSelection = false;

                    // Point selecting.
                    if (hasMouse && !isSelecting)
                    {
                        selectionInfo.SelectionType = GerberSelection.PointClick;
                        selectionInfo.LowerLeftX = lastLocationX;
                        selectionInfo.LowerLeftY = lastLocationY;

                        int count = selectionInfo.SelectedFileInfo.Image.GerberNetList.Count;
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
                        selectionInfo.SelectionType = GerberSelection.DragBox;
                        selectionInfo.UpperRightX = startLocationX;
                        selectionInfo.LowerLeftX = lastLocationX;
                        selectionInfo.UpperRightY = startLocationY;
                        selectionInfo.LowerLeftY = lastLocationY;
                        if (startLocationX < lastLocationX)
                        {
                            selectionInfo.UpperRightX = lastLocationX;
                            selectionInfo.LowerLeftX = startLocationX;
                        }

                        if (startLocationY < lastLocationY)
                        {
                            selectionInfo.UpperRightY = lastLocationY;
                            selectionInfo.LowerLeftY = startLocationY;
                        }

                        int count = selectionInfo.SelectedFileInfo.Image.GerberNetList.Count;
                        for (int index = 0; index < count; index++)
                        {
                            gerberLib.ObjectInSelectedRegion(graphics, selectionInfo, ref index);
                            if (selectionInfo.Count > 0)
                                hasSelection = true;
                        }

                    }

                    hasMouse = false;
                    selectedObjectsToolStripStatusLabel.Text = selectionInfo.Count.ToString();
                    UpdateMenus();
                    pcbImagePanel.Refresh();
                    graphics.Dispose();
                }
            }
        }

        /*private void DrawSelection(Graphics graphics, Rectangle sel)
        {
            graphics.DrawRectangle(new Pen(Color.White, 1), sel);
            pcbImagePanel.Invalidate(Rectangle.Inflate(sel, 1, 1));
        }*/

        private void ZoomIn()
        {
            if (project.FileInfo.Count == 0 || userScale >= 10.0f)
                return;

            userScale += 0.25f;
            UpdateUserTransform();
            pcbImagePanel.Refresh();
        }

        private void ZoomOut()
        {
            if (project.FileInfo.Count == 0 || userScale <= 0.25f)
                return;

            userScale -= 0.25f;
            UpdateUserTransform();
            pcbImagePanel.Refresh();
        }

        // Scale the layers to fit the current view area.
        private void ScaleToFit()
        {
            BoundingBox projectBounds;
            double width, height;
            double scaleX, scaleY;

            userScale = 1.0f;
            UpdateUserTransform();
            projectBounds = gerberLib.GetProjectBounds(project);
            width = projectBounds.Right - projectBounds.Left;
            height = projectBounds.Top - projectBounds.Bottom;
            scaleX = (float)((renderInfo.DisplayWidth - 0.5) / width);
            scaleY = (float)((renderInfo.DisplayHeight - 0.5) / height);
            userScale = (float)Math.Min(scaleX, scaleY);
            UpdateUserTransform();
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
        private void ScaleToFitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ScaleToFit();
            pcbImagePanel.Refresh();
        }

        private void ZoomInToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ZoomIn();
        }

        private void ZoomOutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ZoomOut();
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
            ScaleToFit();
            pcbImagePanel.Refresh();
        }

        private void ScaleToFullSizeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            userScale = 1.0f;
            UpdateUserTransform();
            pcbImagePanel.Refresh();
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
            Initialise();
            UpdateMenus();
            pcbImagePanel.AutoScroll = false;
            pcbImagePanel.Refresh();
        }

        private void OpenProjectToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenProject();
        }

        private void SaveProjectAsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SaveProjectAs();
        }

        private void SaveProjectToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SaveProject();
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

        private void PrintPage(object sender, PrintPageEventArgs e)
        {
            Graphics graphics = e.Graphics;

            graphics.PageUnit = GraphicsUnit.Inch;
            float pageWidth = e.PageBounds.Width / 100.0f;
            float pageHeight = e.PageBounds.Height / 100.0f;
            renderInfo.DisplayWidth = pageWidth;
            renderInfo.DisplayHeight = pageHeight;
            gerberLib.TranslateToCentreDisplay(project, renderInfo);
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
                    GerberVS.ExportGerberRS274X.RS274xFromImage(saveFileDialog.FileName, project.FileInfo[project.CurrentIndex].Image,
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
                    GerberVS.ExportExcellonDrill.DrillFileFromImage(saveFileDialog.FileName, project.FileInfo[project.CurrentIndex].Image,
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
                {
                    renderInfo.DisplayWidth = 0;
                    renderInfo.DisplayHeight = 0;
                    gerberLib.TranslateToCentreDisplay(project, renderInfo);
                    gerberLib.ProjectToPng(saveFileDialog.FileName, project, renderInfo, graphics);
                }
            }
        }
    }
}
