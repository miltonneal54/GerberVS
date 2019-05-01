// SelectionPropertiesFrm.cs - Builds and displays the selected objects properties.

/*  Copyright (C) 2015-2019 Milton Neal <milton200954@gmail.com>

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
    public partial class SelectionPropertiesFrm : Form
    {
        private SelectionInformation selectionInfo;
        StringBuilder selectionText = new StringBuilder();
        private GerberApertureType apertureType;
        private int apertureNumber = 0;
        private double parameter0 = 0.0;
        private double parameter1 = 0.0;
        private double x, y = 0.0;

        public SelectionPropertiesFrm(SelectionInformation selectionInfo)
        {
            InitializeComponent();
            this.selectionInfo = selectionInfo;
        }

        private void SelectionListFrm_Load(object sender, EventArgs e)
        {
            textBox1.Text = String.Empty;
            selectionText.Append("File: " + selectionInfo.Filename);
            foreach (GerberNet net in selectionInfo.SelectedNetList)
            {
                if (net.ApertureState == GerberApertureState.On)
                {
                    switch (net.Interpolation)
                    {
                        case GerberInterpolation.PolygonAreaStart:
                            selectionText.Append(Environment.NewLine);
                            selectionText.Append("Object type: Polygon");
                            break;

                        case GerberInterpolation.LinearX001:
                        case GerberInterpolation.LinearX01:
                        case GerberInterpolation.LinearX1:
                        case GerberInterpolation.LinearX10:
                            if (net.BoundingBox != null)
                            {
                                selectionText.Append(Environment.NewLine);
                                selectionText.Append(Environment.NewLine);
                                selectionText.Append("Object type: Line" + Environment.NewLine);
                                apertureNumber = net.Aperture;
                                selectionText.Append("  Aperture used: " + "D" + apertureNumber.ToString() + Environment.NewLine);
                                apertureType = selectionInfo.SelectionImage.ApertureArray[apertureNumber].ApertureType;
                                selectionText.Append("  Aperture type: " + apertureType.ToString() + Environment.NewLine);
                                parameter0 = selectionInfo.SelectionImage.ApertureArray[apertureNumber].Parameters[0] * 1000;
                                selectionText.Append("  Diameter: " + parameter0.ToString("0.0") + Environment.NewLine);
                                x = net.StartX * 1000;
                                y = net.StartY * 1000;
                                PointD start = new PointD(x, y);
                                selectionText.Append("  Start: (" + x.ToString("0.0") + ", " + y.ToString("0.0") + ")");
                                selectionText.Append(Environment.NewLine);
                                x = net.StopX * 1000;
                                y = net.StopY * 1000;
                                PointD stop = new PointD(x, y);
                                selectionText.Append("  Stop: (" + x.ToString("0.0") + ", " + y.ToString("0.0") + ")");
                                selectionText.Append(Environment.NewLine);
                                double length = GetLineLength(start, stop);
                                selectionText.Append("  Length: " + length);
                                selectionText.Append(Environment.NewLine);
                                selectionText.Append("  Level Name: ");
                                if (net.Level.LevelName == String.Empty)
                                    selectionText.Append("<Unnamed Level>");

                                else
                                    selectionText.Append(net.Level.LevelName);

                                selectionText.Append(Environment.NewLine);
                                selectionText.Append("  Net Label: ");
                                if (net.Label == String.Empty)
                                    selectionText.Append("<Unlabeled Net>");

                                else
                                    selectionText.Append(net.Label);
                            }
                            break;

                        case GerberInterpolation.ClockwiseCircular:
                        case GerberInterpolation.CounterClockwiseCircular:
                            selectionText.Append(Environment.NewLine);
                            selectionText.Append(Environment.NewLine);
                            selectionText.Append("Object type: Arc" + Environment.NewLine);
                            apertureNumber = net.Aperture;
                            selectionText.Append("  Aperture used: " + "D" + apertureNumber.ToString() + Environment.NewLine);
                            apertureType = selectionInfo.SelectionImage.ApertureArray[apertureNumber].ApertureType;
                            selectionText.Append("  Aperture type: " + apertureType.ToString() + Environment.NewLine);
                            parameter0 = selectionInfo.SelectionImage.ApertureArray[apertureNumber].Parameters[0] * 1000;
                            selectionText.Append("  Diameter: " + parameter0.ToString("0.0") + Environment.NewLine);
                            x = net.StartX * 1000;
                            y = net.StartY * 1000;
                            selectionText.Append("  Start: (" + x.ToString("0.0") + ", " + y.ToString("0.0") + ")");
                            selectionText.Append(Environment.NewLine);
                            x = net.StopX * 1000;
                            y = net.StopY * 1000;
                            selectionText.Append("  Stop: (" + x.ToString("0.0") + ", " + y.ToString("0.0") + ")");
                            selectionText.Append(Environment.NewLine);
                            x = net.CircleSegment.CenterX * 1000;
                            y = net.CircleSegment.CenterY * 1000;
                            selectionText.Append("  Centre: (" + x.ToString("0.0") + ", " + y.ToString("0.0") + ")");
                            selectionText.Append(Environment.NewLine);
                            x = net.CircleSegment.StartAngle;
                            y = net.CircleSegment.EndAngle;
                            selectionText.Append("  Angles [Deg]: (" + x.ToString("0.000") + ", " + y.ToString("0.000") + ")");
                            selectionText.Append(Environment.NewLine);
                            selectionText.Append("  Direction: ");
                            selectionText.Append(net.Interpolation == GerberInterpolation.ClockwiseCircular ? "CW" : "CCW");
                            selectionText.Append(Environment.NewLine);
                            selectionText.Append("  Level Name: ");
                            if (net.Level.LevelName == String.Empty)
                                selectionText.Append("<Unnamed Level>");

                            else
                                selectionText.Append(net.Level.LevelName);

                            selectionText.Append(Environment.NewLine);
                            selectionText.Append("  Net Label: ");
                            if (net.Label == String.Empty)
                                selectionText.Append("<Unlabeled Net>");

                            else
                                selectionText.Append(net.Label);

                            break;
                    }
                }

                if (net.ApertureState == GerberApertureState.Flash)
                {
                    apertureNumber = net.Aperture;
                    selectionText.Append(Environment.NewLine);
                    selectionText.Append(Environment.NewLine);
                    selectionText.Append("Object type: Flashed Aperture" + Environment.NewLine);
                    selectionText.Append("  Aperture used: " + "D" + apertureNumber.ToString() + Environment.NewLine);
                    apertureType = selectionInfo.SelectionImage.ApertureArray[apertureNumber].ApertureType;
                    if (apertureType != GerberApertureType.Macro)
                    {
                        selectionText.Append("  Aperture type: " + apertureType.ToString() + Environment.NewLine);
                        switch (apertureType)
                        {
                            case GerberApertureType.Circle:
                                parameter0 = selectionInfo.SelectionImage.ApertureArray[apertureNumber].Parameters[0] * 1000;
                                selectionText.Append("  Diameter: " + parameter0.ToString("0.0") + Environment.NewLine);
                                break;

                            case GerberApertureType.Rectangle:
                            case GerberApertureType.Oval:
                                parameter0 = selectionInfo.SelectionImage.ApertureArray[apertureNumber].Parameters[0] * 1000;
                                parameter1 = selectionInfo.SelectionImage.ApertureArray[apertureNumber].Parameters[1] * 1000;
                                selectionText.Append("  Dimension: " + parameter0.ToString("0.0") + " x " + parameter1.ToString("0.0") + Environment.NewLine);
                                break;
                        }

                    }

                    else
                    {
                        if (selectionInfo.SelectionImage.ApertureArray[apertureNumber].ApertureMacro != null)
                        {
                            apertureType = selectionInfo.SelectionImage.ApertureArray[apertureNumber].SimplifiedMacroList[0].ApertureType;
                            selectionText.Append("  Aperture type: " + apertureType.ToString() + Environment.NewLine);
                        }
                    }

                    x = net.StopX * 1000;
                    y = net.StopY * 1000;
                    selectionText.Append("  Location: (" + x.ToString("0.0") + ", " + y.ToString("0.0") + ")");
                    selectionText.Append(Environment.NewLine);
                    selectionText.Append("  Level Name: ");
                    if (net.Level.LevelName == String.Empty)
                        selectionText.Append("<Unnamed Level>");

                    else
                        selectionText.Append(net.Level.LevelName);

                    selectionText.Append(Environment.NewLine);
                    selectionText.Append("  Net Label: ");
                    if (net.Label == String.Empty)
                        selectionText.Append("<Unlabeled Net>");

                    else
                        selectionText.Append(net.Label);
                }
            }

            textBox1.Text = selectionText.ToString();
            textBox1.SelectionStart = 0;
            textBox1.SelectionLength = 0;
        }

        private double GetLineLength(PointD start, PointD stop)
        {
            double result = Math.Sqrt(Math.Pow((stop.Y - start.Y), 2) + Math.Pow((stop.X - start.X), 2));
            return Math.Round(result, 1);
        }
    }
}
