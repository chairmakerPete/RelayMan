using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace RelayMan {
    public partial class frmMain : Form {

        private static readonly int RelayCount = 8;         // number of relays in the box
        public bool[] Relays = new bool[RelayCount];
        public List<String> CommandQueue = new List<string>();

        public frmMain() {
            InitializeComponent();
            string[] args = Environment.GetCommandLineArgs();
            string[] ComPortNames = null;

            ComPortNames = SerialPort.GetPortNames();
            if ( ComPortNames.Length > 0 ) {
                comboBoxPorts.Items.Clear();

                for ( int i = 0; i < ComPortNames.Length; i++ ) {
                    comboBoxPorts.Items.Add( ComPortNames[i] );
                }
                comboBoxPorts.SelectedIndex = 0;
            }
            else {
                lblNoComPortsFound.Visible = true;
                comboBoxPorts.Enabled = false;
                btnOpen.Enabled = false;
            }
        }

        private void btnOpen_Click( object sender, EventArgs e ) {
            if ( serialPort.IsOpen )
                serialPort.Close();

            serialPort.PortName = comboBoxPorts.SelectedItem.ToString();
            serialPort.Open();
            panel1.Visible = serialPort.IsOpen;

            GetRelayStatus();
        }

        private void GetRelayStatus() {
            if ( serialPort.IsOpen ) {
                serialPort.Write( "S0" );
                serialPort.Write( new byte[] { 13, 10 }, 0, 2 );
            }
        }

        private void UpdateRelayBox() {
            if ( !serialPort.IsOpen )
                return;
            string s;
            for ( int i = 0; i < Relays.Length; i++ ) {
                if ( Relays[i] ) {
                    s = Relays[i] ? "N" : "F" + ( i + 1 ).ToString();
                    serialPort.WriteLine( s );
                    serialPort.Write( new byte[] { 13, 10 }, 0, 2 );
                }
            }
        }

        private string RxString;
        private void serialPort_DataReceived( object sender, SerialDataReceivedEventArgs e ) {
            RxString = serialPort.ReadExisting();
            this.Invoke( new EventHandler( ProcessResponse ) );
        }

        private string LastCommand = string.Empty;
        private void ProcessResponse( object sender, EventArgs e ) {
            tbResponse.AppendText( RxString );
            LastCommand += RxString;
            if ( RxString[RxString.Length - 1] == '#' ) {       // # == end of command response
                CommandQueue.Add( LastCommand );
                LastCommand = string.Empty;
                ShowRelayStatus();
            }
        }

        private void ShowRelayStatus() {
            // get the next command from the queue (FIFO)            
            // if it's an "S0" command, update the relay status, otherwise ignore it
            int status = 0;
            string statusHex;
            if ( CommandQueue.Count > 0 ) {
                if ( CommandQueue[0][0] == 'S' && CommandQueue[0][1] == '0' ) {
                    statusHex = ( CommandQueue[0][4] ).ToString() + ( CommandQueue[0][5] ).ToString();
                    status = Convert.ToInt32( statusHex, 16 );
                    Relays[0] = ( status & 1 ) == 1;
                    Relays[1] = ( status & 2 ) == 2;
                    Relays[2] = ( status & 4 ) == 4;
                    Relays[3] = ( status & 8 ) == 8;
                    Relays[4] = ( status & 16 ) == 16;
                    Relays[5] = ( status & 32 ) == 32;
                    Relays[6] = ( status & 64 ) == 64;
                    Relays[7] = ( status & 128 ) == 128;

                    // update the display
                    // relay 1
                    radioButton1.Checked = Relays[0];
                    radioButton2.Checked = !Relays[0];
                    //relay 2
                    radioButton3.Checked = Relays[1];
                    radioButton4.Checked = !Relays[1];
                    // relay 3
                    radioButton5.Checked = Relays[2];
                    radioButton6.Checked = !Relays[2];
                    // relay 4
                    radioButton7.Checked = Relays[3];
                    radioButton8.Checked = !Relays[3];
                    // relay 5
                    radioButton9.Checked = Relays[4];
                    radioButton10.Checked = !Relays[4];
                    // relay 6
                    radioButton11.Checked = Relays[5];
                    radioButton12.Checked = !Relays[5];
                    // relay 7
                    radioButton13.Checked = Relays[6];
                    radioButton14.Checked = !Relays[6];
                    // relay 8
                    radioButton15.Checked = Relays[7];
                    radioButton16.Checked = !Relays[7];
                }
                else
                    GetRelayStatus();

                CommandQueue.RemoveAt( 0 );
            }
        }

        private void Form1_FormClosing( object sender, FormClosingEventArgs e ) {
            if ( serialPort.IsOpen )
                serialPort.Close();
        }

        private void tbResponse_KeyPress( object sender, KeyPressEventArgs e ) {
            // If the port is closed, don't try to send a character.
            if ( !serialPort.IsOpen )
                return;

            // If the port is Open, declare a char[] array with one element.
            char[] buff = new char[1];

            // Load element 0 with the key character.
            buff[0] = e.KeyChar;

            // Send the one character buffer.
            serialPort.Write( buff, 0, 1 );

            // Set the KeyPress event as handled so the character won't
            // display locally. If you want it to display, omit the next line.

            e.Handled = true;
        }

        private void SendCommand( string command ) {
            if ( serialPort.IsOpen ) {
                serialPort.WriteLine( command );
                serialPort.Write( new byte[] { 13, 10 }, 0, 2 );
            }
        }

        private void btnAllOn_Click( object sender, EventArgs e ) {
            SendCommand( "N0" );
        }

        private void btnAllOff_Click( object sender, EventArgs e ) {
            SendCommand( "F0" );
        }

        private void btnClear_Click( object sender, EventArgs e ) {
            tbResponse.Clear();
        }

        private void radioButton1_Click( object sender, EventArgs e ) {
            SendCommand( "N1" );
        }

        private void radioButton2_Click( object sender, EventArgs e ) {
            SendCommand( "F1" );
        }

        private void radioButton3_Click( object sender, EventArgs e ) {
            SendCommand( "N2" );
        }

        private void radioButton4_Click( object sender, EventArgs e ) {
            SendCommand( "F2" );
        }

        private void radioButton5_Click( object sender, EventArgs e ) {
            SendCommand( "N3" );
        }

        private void radioButton6_Click( object sender, EventArgs e ) {
            SendCommand( "F3" );
        }

        private void radioButton7_Click( object sender, EventArgs e ) {
            SendCommand( "N4" );
        }

        private void radioButton8_Click( object sender, EventArgs e ) {
            SendCommand( "F4" );
        }

        private void radioButton9_Click( object sender, EventArgs e ) {
            SendCommand( "N5" );
        }

        private void radioButton10_Click( object sender, EventArgs e ) {
            SendCommand( "F5" );
        }

        private void radioButton11_Click( object sender, EventArgs e ) {
            SendCommand( "N6" );
        }

        private void radioButton12_Click( object sender, EventArgs e ) {
            SendCommand( "F6" );
        }

        private void radioButton13_Click( object sender, EventArgs e ) {
            SendCommand( "N7" );
        }

        private void radioButton14_Click( object sender, EventArgs e ) {
            SendCommand( "F7" );
        }

        private void radioButton15_Click( object sender, EventArgs e ) {
            SendCommand( "N8" );
        }

        private void radioButton16_Click( object sender, EventArgs e ) {
            SendCommand( "F8" );
        }

    }
}
