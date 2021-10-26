using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace streznik
{
    public partial class Strežnik : Form
    {
        public Strežnik()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Strežnik));
            this.chat = new System.Windows.Forms.TextBox();
            this.log = new System.Windows.Forms.TextBox();
            this.stats = new System.Windows.Forms.TextBox();
            this.connected = new System.Windows.Forms.TextBox();
            this.SuspendLayout();
            // 
            // chat
            // 
            this.chat.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.chat.Location = new System.Drawing.Point(406, 687);
            this.chat.Name = "chat";
            this.chat.Size = new System.Drawing.Size(588, 22);
            this.chat.TabIndex = 0;
            // 
            // log
            // 
            this.log.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.log.Enabled = false;
            this.log.HideSelection = false;
            this.log.Location = new System.Drawing.Point(406, 12);
            this.log.Multiline = true;
            this.log.Name = "log";
            this.log.ReadOnly = true;
            this.log.ShortcutsEnabled = false;
            this.log.Size = new System.Drawing.Size(588, 670);
            this.log.TabIndex = 1;
            this.log.TabStop = false;
            // 
            // stats
            // 
            this.stats.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.stats.Cursor = System.Windows.Forms.Cursors.Default;
            this.stats.Enabled = false;
            this.stats.HideSelection = false;
            this.stats.Location = new System.Drawing.Point(12, 12);
            this.stats.Multiline = true;
            this.stats.Name = "stats";
            this.stats.ReadOnly = true;
            this.stats.ScrollBars = System.Windows.Forms.ScrollBars.Horizontal;
            this.stats.ShortcutsEnabled = false;
            this.stats.Size = new System.Drawing.Size(388, 357);
            this.stats.TabIndex = 2;
            this.stats.TabStop = false;
            // 
            // connected
            // 
            this.connected.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.connected.Enabled = false;
            this.connected.HideSelection = false;
            this.connected.Location = new System.Drawing.Point(12, 375);
            this.connected.Multiline = true;
            this.connected.Name = "connected";
            this.connected.ReadOnly = true;
            this.connected.ShortcutsEnabled = false;
            this.connected.Size = new System.Drawing.Size(388, 334);
            this.connected.TabIndex = 3;
            this.connected.TabStop = false;
            // 
            // Strežnik
            // 
            this.ClientSize = new System.Drawing.Size(1006, 721);
            this.Controls.Add(this.connected);
            this.Controls.Add(this.stats);
            this.Controls.Add(this.log);
            this.Controls.Add(this.chat);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "Strežnik";
            this.Text = "Strežnik";
            this.ResumeLayout(false);
            this.PerformLayout();

        }
    }
}
