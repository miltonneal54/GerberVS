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

using GerberVS;

namespace GerberView
{
    public partial class Form1 : Form
    {
        private LibGerberVS gerberLib = null;
        private GerberProject project = null;
        private BoundingBox projectBounds;
        //private GerberUserTransform userTransform = null;
        private RenderInformation renderInfo = null;
        private SelectionInformation selectionInfo = null;   // List containing the currenly selected objects (nets).
        private int selectedLayerIndex = 0;
        // User transformations.
        private GerberRenderMode renderMode;
        //private float rScaleX = 1.0f;
        //private float rScaleY = 1.0f;
        private float userScale = 1.0f;
        //private float userTranslateX = 0.0f;
        //private float userTranslateY = 0.0f;
        private bool userMirrorX = false;
        private bool userMirrorY = false;
        private bool userInverted = false;
        private bool applyToAll = false;        // True if user transformation applies to all layers.

        // Mouse tracking.
        private double startLocationX = 0.0;
        private double startLocationY = 0.0;
        private double lastLocationX = 0.0;
        private double lastLocationY = 0.0;
        private bool hasMouse = false;
        private bool isDragging = false;
        private bool isSelecting = false;
        private bool hasSelection = false;

        Rectangle selectionRectangle = new Rectangle(new Point(0, 0), new Size(0, 0));
        Point startPoint;
        SelectionPropertiesFrm selectionPropertiesFrm;     // Form for displaying selected object properties.
        int visibleGerberFiles = 0;
        int visibleDrillFiles = 0;
        bool fullScreen;
        public Form1()
        {
            InitializeComponent();

           /* typeof(Panel).InvokeMember(
                "DoubleBuffered",
                System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Instance |
                System.Reflection.BindingFlags.SetProperty,
                null,
                pcbImagePanel,
                new object[] { true });*/

            pcbImagePanel.GetType().GetMethod("SetStyle", 
	            System.Reflection.BindingFlags.Instance |
	            System.Reflection.BindingFlags.NonPublic).Invoke(pcbImagePanel, 
	            new object[]{ System.Windows.Forms.ControlStyles.UserPaint | 
	            System.Windows.Forms.ControlStyles.AllPaintingInWmPaint |
	            System.Windows.Forms.ControlStyles.OptimizedDoubleBuffer, true});
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            gerberLib = new LibGerberVS();
            project = gerberLib.CreateNewProject();
            renderInfo = new RenderInformation();
            selectionInfo = new SelectionInformation();
            RenderModeComboBox.SelectedIndex = 0;
            renderMode = GerberRenderMode.TranslateToCentre;
            pcbImagePanel.BackColor = Color.Black;
            project.BackgroundColor = pcbImagePanel.BackColor;
            fullScreen = false;
            UpdateMenus();
        }

        private void OpenFiles()
        {
            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.RestoreDirectory = true;
                openFileDialog.Multiselect = true;
                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    foreach (string file in openFileDialog.FileNames)
                    {
                        if (OpenFile(file))
                        {
                            fileListBox.SelectedIndex = selectedLayerIndex;
                            LayerNameToolStripStatusLabel.Text = project.FileInfo[selectedLayerIndex].FileName;
                            pcbImagePanel.Refresh();
                            UpdateMenus();
                        }
                    }
                }
            }
        }

        private bool OpenFile(string file)
        {
            int index = 0;
            // Open file and if sucessful, add it to file list box.
            try
            {
                gerberLib.OpenLayerFromFilename(project, file);
                index = project.FileInfo.Count - 1;
                fileListBox.AddItem(true, project.FileInfo[index].Color, project.FileInfo[index].FileName);

                if (project.FileInfo[index].Image.FileType == GerberFileType.RS274X)
                    visibleGerberFiles++;

                else if (project.FileInfo[index].Image.FileType == GerberFileType.Drill)
                    visibleDrillFiles++;

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

        private bool OpenFile(string file, Color color, int alpha)
        {
            int index = 0;
            // Open file and if sucessful, add it to file list box.
            try
            {
                gerberLib.OpenLayerFromFilenameAndColor(project, file, color, alpha);
                index = project.FileInfo.Count - 1;
                fileListBox.AddItem(true, project.FileInfo[index].Color, project.FileInfo[index].FileName);

                if (project.FileInfo[index].Image.FileType == GerberFileType.RS274X)
                    visibleGerberFiles++;

                else if (project.FileInfo[index].Image.FileType == GerberFileType.Drill)
                    visibleDrillFiles++;

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

        private void PcbImagePanel_Paint(object sender, PaintEventArgs e)
        {
            Graphics graphics = e.Graphics;
            if (project.FileInfo.Count > 0)
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
                    gerberLib.RenderSelectionLayer(graphics, project, renderInfo, selectionInfo);

                CalibrateRulers();
            }

            //fileListBox.Focus();
            splitContainer1.Panel2.Focus();
        }

        /*private void UpdateUserTransformations()
        {
            project.UserTransform.ScaleX = userScaleX;
            project.UserTransform.ScaleY = userScaleY;
            project.UserTransform.TranslateX = userTranslateX;
            project.UserTransform.TranslateY = userTranslateY;
            project.UserTransform.MirrorAroundX = userMirrorX;
            project.UserTransform.MirrorAroundY = userMirrorY;
            project.UserTransform.Inverted = userInverted;
        }*/

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

            UpdateScale();
            pcbImagePanel.Refresh();

        }

        private void UpdateScale()
        {
            if (userScale < 0.25f)
                userScale = 0.25f;

            if (userScale > 10.0f)
                userScale = 10.0f;

            project.UserTransform.ScaleX = userScale;
            project.UserTransform.ScaleY = userScale;
            int scale = (int)Math.Ceiling(userScale * 100);
            scaleToolStripStatusLabel.Text = scale.ToString() + "%";
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
            using (ColorDialog colorDialog = new ColorDialog())
            {
                colorDialog.AnyColor = true;
                colorDialog.AllowFullOpen = true;
                colorDialog.SolidColorOnly = false;
                colorDialog.Color = currentColor;
                colorDialog.ShowDialog();
                project.FileInfo[fileListBox.SelectedIndex].Color = colorDialog.Color;
                fileListBox.ItemColor = colorDialog.Color;
                pcbImagePanel.Refresh();
            }
        }

        private void CalibrateRulers()
        {
            /*if (!projectBounds.IsValid())
                return;*/

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
            float horizonalOffset = -(float)((renderInfo.Left + renderInfo.ScrollValueX) - project.UserTransform.TranslateX);
            float verticleOffset = (float)(((renderInfo.DisplayHeight + renderInfo.Bottom - renderInfo.ScrollValueY)) + project.UserTransform.TranslateY);
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

        private void OpenToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenFiles();
        }

        private void RefreshFileList()
        {
            int index = fileListBox.SelectedIndex;
            fileListBox.Clear();
            for (int i = 0; i < project.FileInfo.Count; i++)
                fileListBox.AddItem(project.FileInfo[i].IsVisible, project.FileInfo[i].Color, project.FileInfo[i].FileName);

            fileListBox.SelectedIndex = index;
        }

        private void RenderModeCombobox_SelectedIndexChanged(object sender, EventArgs e)
        {
            switch (RenderModeComboBox.SelectedIndex)
            {
                case 0:
                    renderInfo.RenderType = GerberRenderQuality.Default;
                    break;

                case 1:
                    renderInfo.RenderType = GerberRenderQuality.HighSpeed;
                    break;

                case 2:
                    renderInfo.RenderType = GerberRenderQuality.HighQuality;
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

        private void MoveLayerDownToolStripButton_Click(object sender, EventArgs e)
        {
            MoveLayerDown();
        }

        private void MoveLayerDownToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MoveLayerDown();
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
                    RefreshFileList();
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
                RefreshFileList();
                pcbImagePanel.Refresh();
            }
        }

        private void AddFileStripButton_Click(object sender, EventArgs e)
        {
            OpenFiles();
        }

        private void UnloadLayerToolStripButton_Click(object sender, EventArgs e)
        {
            UnloadLayer();
        }

        private void UnloadLayer()
        {
            int index = fileListBox.SelectedIndex;
            if (index > -1)
            {
                // Update the visible layers counts.
                if (project.FileInfo[index].Image.FileType == GerberFileType.RS274X && project.FileInfo[index].IsVisible)
                    visibleGerberFiles--;

                else if (project.FileInfo[index].Image.FileType == GerberFileType.Drill && project.FileInfo[index].IsVisible)
                    visibleDrillFiles--;

                // First check if this file (layer) has a current selection.
                if (hasSelection)
                {
                    if (selectionInfo.Filename == project.FileInfo[index].FileName)
                    {
                        gerberLib.ClearSelectionBuffer(selectionInfo);
                        hasSelection = false;
                    }
                }

                // Then unload the layer.
                gerberLib.UnloadLayer(project, index);
                if (index == 0)
                {
                    if (project.FileInfo.Count == 0)        // No files left in project.
                    {
                        fileListBox.SelectedIndex = -1;
                        selectedLayerIndex = 0;
                    }
                }

                else
                    fileListBox.SelectedIndex--;

                

                RefreshFileList();
                pcbImagePanel.Refresh();
                UpdateMenus();
            }
        }

        private void FileListBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            project.CurrentIndex = fileListBox.SelectedIndex;
            if (fileListBox.SelectedIndex == -1)
                LayerNameToolStripStatusLabel.Text = String.Empty;

            else
                LayerNameToolStripStatusLabel.Text = project.FileInfo[project.CurrentIndex].FileName;

            selectedLayerIndex = fileListBox.SelectedIndex;
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
            using (DrillStatsForm gerberStats = new DrillStatsForm(project))
            {
                if (gerberStats.ShowDialog(this) == DialogResult.OK)
                {
                    verticleRuler.MouseTrackingOn = true;
                    horizonalRuler.MouseTrackingOn = true;
                }
            }
        }

        private void SelectionToolStripMenuItem_Click(object sender, EventArgs e)
        {
            selectionToolStripMenuItem.Enabled = false;
            ShowSelectionForm();
        }

        private void ShowSelectionForm()
        {
            selectionPropertiesFrm = new SelectionPropertiesFrm(selectionInfo);
            selectionPropertiesFrm.FormClosing += new FormClosingEventHandler(CloseSelectionForm);
            selectionPropertiesFrm.Location = new Point(this.Left, this.Top);
            selectionPropertiesFrm.Show();
        }

        private void CloseSelectionForm(object sender, EventArgs e)
        {
            UpdateMenus();
        }

        private void UpdateMenus()
        {
            layerToolStripMenuItem.Enabled = project.FileInfo.Count > 0;
            reportsToolStripMenuItem.Enabled = project.FileInfo.Count > 0;
            gerberLayersToolStripMenuItem.Enabled = visibleGerberFiles > 0;
            drillLayersToolStripMenuItem.Enabled = visibleDrillFiles > 0;
            selectionToolStripMenuItem.Enabled = hasSelection;

            moveLayerUpToolStripButton.Enabled = selectedLayerIndex != 0;
            moveLayerUpToolStripMenuItem.Enabled = selectedLayerIndex != 0;
            moveLayerDownToolStripButton.Enabled = selectedLayerIndex < fileListBox.ItemCount - 1;
            moveLayerDownToolStripMenuItem.Enabled = selectedLayerIndex < fileListBox.ItemCount - 1;
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
                        if (OpenFile(file))
                        {
                            fileListBox.SelectedIndex = selectedLayerIndex;
                            LayerNameToolStripStatusLabel.Text = project.FileInfo[selectedLayerIndex].FileName;
                            UpdateMenus();
                            pcbImagePanel.Refresh();
                        }
                    }
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
            // Note: Selection is only possible on the current selected image (layer).
            Graphics graphics = pcbImagePanel.CreateGraphics();
            graphics.PageUnit = GraphicsUnit.Inch;
            graphics.ScaleTransform(userScale, userScale);

            // Clear and reset the selection rectangle.
            selectionRectangle = new Rectangle(0, 0, 0, 0);
            if (e.Button == MouseButtons.Left && project.CurrentIndex > -1)
            {
                // Clear any previous layer selections.
                if (hasSelection && selectionInfo.SelectionImage != null)
                {
                    gerberLib.ClearSelectionBuffer(selectionInfo);
                    if (selectionPropertiesFrm != null)
                        selectionPropertiesFrm.Close();
                }

                hasSelection = false;
                if (hasMouse && !isSelecting)
                {
                    selectionInfo.Filename = project.FileInfo[project.CurrentIndex].FileName;
                    selectionInfo.SelectionImage = project.FileInfo[project.CurrentIndex].Image;
                    selectionInfo.SelectionType = GerberSelection.PointClick;
                    selectionInfo.LowerLeftX = lastLocationX;
                    selectionInfo.LowerLeftY = lastLocationY;

                    int count = selectionInfo.SelectionImage.GerberNetList.Count;
                    for (int i = 0; i < count; i++)
                    {
                        gerberLib.ObjectInSelectedRegion(selectionInfo, ref i, graphics);
                        if (selectionInfo.SelectionCount > 0)
                            hasSelection = true;
                    }
                }

                if (hasMouse && isSelecting)
                {
                    selectionInfo.Filename = project.FileInfo[project.CurrentIndex].FileName;
                    selectionInfo.SelectionImage = project.FileInfo[project.CurrentIndex].Image;
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

                    int count = selectionInfo.SelectionImage.GerberNetList.Count;
                    for (int i = 0; i < count; i++)
                    {
                        gerberLib.ObjectInSelectedRegion(selectionInfo, ref i, graphics);
                        if (selectionInfo.SelectionCount > 0)
                            hasSelection = true;
                    }

                }

                hasMouse = false;
                selectedObjectsToolStripStatusLabel.Text = selectionInfo.SelectionCount.ToString();
                UpdateMenus();
                pcbImagePanel.Refresh();
                graphics.Dispose();
            }
        }

        private void DrawSelection(Graphics graphics, Rectangle sel)
        {
            graphics.DrawRectangle(new Pen(Color.White, 1), sel);
            pcbImagePanel.Invalidate(Rectangle.Inflate(sel, 1, 1));
        }

        private void ZoomIn()
        {
            if (project.FileInfo.Count == 0 || userScale >= 10.0f)
                return;

            userScale += 0.25f;
            UpdateScale();
            pcbImagePanel.Refresh();
        }

        private void ZoomOut()
        {
            if (project.FileInfo.Count == 0 || userScale <= 0.25f)
                return;

            userScale -= 0.25f;
            UpdateScale();
            pcbImagePanel.Refresh();
        }
        private void ScaleToFit()
        {
            BoundingBox projectBounds;
            double width, height;
            double scaleX, scaleY;

            project.UserTransform.ScaleX = 1.0f;
            project.UserTransform.ScaleY = 1.0f;
            projectBounds = gerberLib.GetProjectBounds(project);
            width = projectBounds.Right - projectBounds.Left;
            height = projectBounds.Top - projectBounds.Bottom;
            scaleX = (float)((renderInfo.DisplayWidth - 0.5) / width);
            scaleY = (float)((renderInfo.DisplayHeight - 0.5) / height);
            userScale = (float)Math.Min(scaleX, scaleY);
            UpdateScale();
            pcbImagePanel.Refresh();
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
        }

        private void UnloadAllLayersToolStripMenuItem_Click(object sender, EventArgs e)
        {
            gerberLib.UnloadAllLayers(project);
            fileListBox.SelectedIndex = -1;
            selectedLayerIndex = 0;
            visibleDrillFiles = 0;
            visibleGerberFiles = 0;
            RefreshFileList();
            pcbImagePanel.Refresh();
            UpdateMenus();
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
            bool state = project.FileInfo[fileListBox.SelectedIndex].Inverted;
            project.FileInfo[fileListBox.SelectedIndex].Inverted = !state;
            pcbImagePanel.Refresh();
        }

        private void MoveLayerUpToolStripButton_Click(object sender, EventArgs e)
        {
            MoveLayerUp();
        }

        private void MoveLayerUpToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MoveLayerUp();
        }

        private void UnloadLayerToolStripMenuItem_Click(object sender, EventArgs e)
        {
            UnloadLayer();
        }

        private void reloadLayerToolStripMenuItem_Click(object sender, EventArgs e)
        {
            gerberLib.ReloadFile(project, selectedLayerIndex);
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

        private void ZoomInToolStripButton_Click(object sender, EventArgs e)
        {
            ZoomIn();
        }

        private void ZoomOutToolStripButton_Click(object sender, EventArgs e)
        {
            ZoomOut();
        }

        private void ZoomToFitToolStripButton3_Click(object sender, EventArgs e)
        {
            ScaleToFit();
        }

        private void saveAsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            project.ProjectName = "Test Project";
            Project.WriteProject(project);
        }

    }
}
