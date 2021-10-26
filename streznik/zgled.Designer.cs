namespace streznik{
    partial class Strežnik{
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose( bool disposing ){
            if ( disposing && ( components != null ) )
                components.Dispose();

            base.Dispose( disposing );
        }

        private System.Windows.Forms.TextBox chat;
        private System.Windows.Forms.TextBox log;
        private System.Windows.Forms.TextBox stats;
        private System.Windows.Forms.TextBox connected;
    }
}