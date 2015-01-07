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

        // command line parameter properties
        private int comPort { get; set; }
        private int relay { get; set; }
        private int duration { get; set; }
        private bool setOn { get; set; }
        private int delay { get; set; }
        private bool runFromCmdLine { get; set; }

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

            DisplayCmdLinArgs( args );
            if ( ParseCmdLineArgs( args ) ) {
                runFromCmdLine = true;
            }
        }

        private void RunCmdLineArgs() {
            // pause for delay seconds if specified
            if ( delay > 0 ) {
                System.Threading.Thread.Sleep( delay * 1000 );
            }

            // grab the com port
            if ( serialPort.IsOpen ) {
                serialPort.Close();
            }
            serialPort.PortName = "COM" + comPort.ToString();
            serialPort.Open();

            // fire command
            SendCommand( ( setOn ? "N" : "F" ) + relay.ToString() );

            // hang on for duration seconds
            if ( duration > 0 ) {
                System.Threading.Thread.Sleep( duration * 1000 );
                SendCommand( ( setOn ? "F" : "N" ) + relay.ToString() );   // toggle command previously set
            }
        }

        private bool ParseCmdLineArgs( string[] args ) {
            // parse args
            // looking for following key words: 
            /*  relay=n (where n in range 1 - 8)
             *  set=on/off 
             *  duration=nn (where nn = number of seconds)
             *  comport=nn (where nn = com port number to use)
             *  allon (switch all relays on) 
             *  alloff (switch all relays off)
             *  delay=nn (delay for nn seconds before running commands)
             */

            foreach ( string arg in args ) {
                if ( arg.ToLower().Contains( "comport=" ) ) {
                    comPort = Convert.ToInt32( arg.Substring( arg.LastIndexOf( "=" ) + 1 ) );
                }

                if ( arg.ToLower().Contains( "relay=" ) ) {
                    relay = Convert.ToInt32( arg.Substring( arg.LastIndexOf( "=" ) + 1 ) );
                }

                if ( arg.ToLower().Contains( "duration=" ) ) {
                    duration = Convert.ToInt32( arg.Substring( arg.LastIndexOf( "=" ) + 1 ) );
                }

                if ( arg.ToLower().Contains( "set=" ) ) {
                    setOn = arg.Substring( arg.LastIndexOf( "=" ) + 1 ) == "on";
                }

                if ( arg.ToLower().Contains( "delay=" ) ) {
                    delay = Convert.ToInt32( arg.Substring( arg.LastIndexOf( "=" ) + 1 ) );
                }

            }
            return ( comPort > 0 && relay > 0 );

        }

        private void DisplayCmdLinArgs( string[] args ) {
            if ( args.Length > 0 ) {
                for ( int i = 0; i < args.Length; i++ ) {
                    listBoxCmdLineParams.Items.Add( "Param " + i.ToString() + ":  " + args[i] );
                }
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
            // sends "S0" to the relay box which will cause it to respond with a status code of two bytes representing a hex value
            // this is decoded when the message response comes in via the DataReceived delegate and its dependents
            if ( serialPort.IsOpen ) {
                serialPort.Write( "S0" );
                serialPort.Write( new byte[] { 13, 10 }, 0, 2 );
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
                tbResponse.AppendText( "\r\n" );
                ShowRelayStatus();
            }
        }

        private void ShowRelayStatus() {
            int status;
            string statusHex;

            // get the next command from the queue (FIFO)                        
            if ( CommandQueue.Count > 0 ) {
                // if it's an "S0" command, update the relay status, otherwise ignore it
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
                else {
                    // if a command has been processed, force a status update
                    GetRelayStatus();
                }
                // take this command off the queue and consider it dealt with
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

        private void btnClear_Click( object sender, EventArgs e ) {
            tbResponse.Clear();
        }

        // individual buttons handlers section
        private void btnAllOn_Click( object sender, EventArgs e ) {
            SendCommand( "N0" );
        }

        private void btnAllOff_Click( object sender, EventArgs e ) {
            SendCommand( "F0" );
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

        private void frmMain_Load( object sender, EventArgs e ) {
            if ( runFromCmdLine ) {
                RunCmdLineArgs();
                serialPort.Close();
                Application.Exit();
            }
        }


    }
}
