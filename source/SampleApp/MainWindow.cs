// =============================================================================
//  MainWindow.cs
//
//  Written in 2022 by Dairoku Sekiguchi (sekiguchi at acm dot org)
//
//  To the extent possible under law, the author(s) have dedicated all copyright
//  and related and neighboring rights to this software to the public domain worldwide.
//  This software is distributed without any warranty.
//
//  You should have received a copy of the CC0 Public Domain Dedication along with
//  this software. If not, see <http://creativecommons.org/publicdomain/zero/1.0/>.
// =============================================================================

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SampleApp
{
    public partial class MainWindow : Form
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void MainWindow_Load(object sender, EventArgs e)
        {
            mImageView.AllocateImageBuffer(512, 512, false);
            byte[] image = mImageView.GetImageBuffer();
            int idx = 0;
            for (int y = 0; y < 512; y++)
                for (int x = 0; x < 512; x++)
                {
                    image[idx] = (byte)(x ^ y);
                    idx++;
                }
            mImageView.MarkAsImageModified();
        }
    }
}
