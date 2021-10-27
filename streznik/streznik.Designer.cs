namespace streznik{
    partial class Strežnik{
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose( bool disposing ){
            if ( disposing && ( components != null ) )
                components.Dispose();

            base.Dispose( disposing );
        }

        private void InitializeComponent(){
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Strežnik));
            this.chat = new System.Windows.Forms.TextBox();
            this.log = new System.Windows.Forms.TextBox();
            this.stats = new System.Windows.Forms.TextBox();
            this.connected = new System.Windows.Forms.TextBox();
            this.horizontal_split = new System.Windows.Forms.SplitContainer();
            this.vertical_split_left = new System.Windows.Forms.SplitContainer();
            this.stats_lab = new System.Windows.Forms.Label();
            this.connected_lab = new System.Windows.Forms.Label();
            this.vertical_split_right = new System.Windows.Forms.SplitContainer();
            this.log_label = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)(this.horizontal_split)).BeginInit();
            this.horizontal_split.Panel1.SuspendLayout();
            this.horizontal_split.Panel2.SuspendLayout();
            this.horizontal_split.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.vertical_split_left)).BeginInit();
            this.vertical_split_left.Panel1.SuspendLayout();
            this.vertical_split_left.Panel2.SuspendLayout();
            this.vertical_split_left.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.vertical_split_right)).BeginInit();
            this.vertical_split_right.Panel1.SuspendLayout();
            this.vertical_split_right.Panel2.SuspendLayout();
            this.vertical_split_right.SuspendLayout();
            this.SuspendLayout();
            // 
            // chat
            // 
            this.chat.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.chat.Location = new System.Drawing.Point(3, 5);
            this.chat.Name = "chat";
            this.chat.Size = new System.Drawing.Size(672, 22);
            this.chat.TabIndex = 0;
            this.chat.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.chat_KeyPress);
            // 
            // log
            // 
            this.log.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.log.CausesValidation = false;
            this.log.HideSelection = false;
            this.log.Location = new System.Drawing.Point(3, 3);
            this.log.Multiline = true;
            this.log.Name = "log";
            this.log.ReadOnly = true;
            this.log.ScrollBars = System.Windows.Forms.ScrollBars.Both;
            this.log.Size = new System.Drawing.Size(672, 657);
            this.log.TabIndex = 1;
            this.log.TabStop = false;
            this.log.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            // 
            // stats
            // 
            this.stats.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.stats.Cursor = System.Windows.Forms.Cursors.Default;
            this.stats.Enabled = false;
            this.stats.HideSelection = false;
            this.stats.Location = new System.Drawing.Point(3, 3);
            this.stats.Multiline = true;
            this.stats.Name = "stats";
            this.stats.ReadOnly = true;
            this.stats.ScrollBars = System.Windows.Forms.ScrollBars.Horizontal;
            this.stats.ShortcutsEnabled = false;
            this.stats.Size = new System.Drawing.Size(294, 344);
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
            this.connected.Location = new System.Drawing.Point(3, 3);
            this.connected.Multiline = true;
            this.connected.Name = "connected";
            this.connected.ReadOnly = true;
            this.connected.ShortcutsEnabled = false;
            this.connected.Size = new System.Drawing.Size(294, 337);
            this.connected.TabIndex = 3;
            this.connected.TabStop = false;
            // 
            // horizontal_split
            // 
            this.horizontal_split.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.horizontal_split.IsSplitterFixed = true;
            this.horizontal_split.Location = new System.Drawing.Point(12, 12);
            this.horizontal_split.Name = "horizontal_split";
            // 
            // horizontal_split.Panel1
            // 
            this.horizontal_split.Panel1.Controls.Add(this.vertical_split_left);
            // 
            // horizontal_split.Panel2
            // 
            this.horizontal_split.Panel2.Controls.Add(this.vertical_split_right);
            this.horizontal_split.Size = new System.Drawing.Size(982, 697);
            this.horizontal_split.SplitterDistance = 300;
            this.horizontal_split.TabIndex = 4;
            // 
            // vertical_split_left
            // 
            this.vertical_split_left.Dock = System.Windows.Forms.DockStyle.Fill;
            this.vertical_split_left.IsSplitterFixed = true;
            this.vertical_split_left.Location = new System.Drawing.Point(0, 0);
            this.vertical_split_left.Name = "vertical_split_left";
            this.vertical_split_left.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // vertical_split_left.Panel1
            // 
            this.vertical_split_left.Panel1.Controls.Add(this.stats_lab);
            this.vertical_split_left.Panel1.Controls.Add(this.stats);
            // 
            // vertical_split_left.Panel2
            // 
            this.vertical_split_left.Panel2.Controls.Add(this.connected_lab);
            this.vertical_split_left.Panel2.Controls.Add(this.connected);
            this.vertical_split_left.Size = new System.Drawing.Size(300, 697);
            this.vertical_split_left.SplitterDistance = 350;
            this.vertical_split_left.TabIndex = 0;
            this.vertical_split_left.TabStop = false;
            // 
            // stats_lab
            // 
            this.stats_lab.AutoSize = true;
            this.stats_lab.Location = new System.Drawing.Point(3, 3);
            this.stats_lab.Name = "stats_lab";
            this.stats_lab.Size = new System.Drawing.Size(53, 17);
            this.stats_lab.TabIndex = 3;
            this.stats_lab.Text = "STATS";
            // 
            // connected_lab
            // 
            this.connected_lab.AutoSize = true;
            this.connected_lab.Location = new System.Drawing.Point(0, 0);
            this.connected_lab.Name = "connected_lab";
            this.connected_lab.Size = new System.Drawing.Size(94, 17);
            this.connected_lab.TabIndex = 4;
            this.connected_lab.Text = "CONNECTED";
            // 
            // vertical_split_right
            // 
            this.vertical_split_right.Dock = System.Windows.Forms.DockStyle.Fill;
            this.vertical_split_right.IsSplitterFixed = true;
            this.vertical_split_right.Location = new System.Drawing.Point(0, 0);
            this.vertical_split_right.Name = "vertical_split_right";
            this.vertical_split_right.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // vertical_split_right.Panel1
            // 
            this.vertical_split_right.Panel1.Controls.Add(this.log_label);
            this.vertical_split_right.Panel1.Controls.Add(this.log);
            // 
            // vertical_split_right.Panel2
            // 
            this.vertical_split_right.Panel2.Controls.Add(this.chat);
            this.vertical_split_right.Size = new System.Drawing.Size(678, 697);
            this.vertical_split_right.SplitterDistance = 663;
            this.vertical_split_right.TabIndex = 0;
            // 
            // log_label
            // 
            this.log_label.AutoSize = true;
            this.log_label.Location = new System.Drawing.Point(3, 3);
            this.log_label.Name = "log_label";
            this.log_label.Size = new System.Drawing.Size(38, 17);
            this.log_label.TabIndex = 2;
            this.log_label.Text = "LOG";
            // 
            // Strežnik
            // 
            this.ClientSize = new System.Drawing.Size(1006, 721);
            this.Controls.Add(this.horizontal_split);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "Strežnik";
            this.Text = "Strežnik";
            this.horizontal_split.Panel1.ResumeLayout(false);
            this.horizontal_split.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.horizontal_split)).EndInit();
            this.horizontal_split.ResumeLayout(false);
            this.vertical_split_left.Panel1.ResumeLayout(false);
            this.vertical_split_left.Panel1.PerformLayout();
            this.vertical_split_left.Panel2.ResumeLayout(false);
            this.vertical_split_left.Panel2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.vertical_split_left)).EndInit();
            this.vertical_split_left.ResumeLayout(false);
            this.vertical_split_right.Panel1.ResumeLayout(false);
            this.vertical_split_right.Panel1.PerformLayout();
            this.vertical_split_right.Panel2.ResumeLayout(false);
            this.vertical_split_right.Panel2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.vertical_split_right)).EndInit();
            this.vertical_split_right.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        private System.Windows.Forms.TextBox chat;
        private System.Windows.Forms.TextBox log;
        private System.Windows.Forms.TextBox stats;
        private System.Windows.Forms.TextBox connected;
        private System.Windows.Forms.SplitContainer horizontal_split;
        private System.Windows.Forms.SplitContainer vertical_split_left;
        private System.Windows.Forms.SplitContainer vertical_split_right;
        private System.Windows.Forms.Label log_label;
        private System.Windows.Forms.Label stats_lab;
        private System.Windows.Forms.Label connected_lab;
    }
}