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
    public partial class Form1 : Form {

        private readonly int RelayCount = 8;

        public class Relay {
            public int RelayID { get; set; }
            public bool IsClosed { get; set; }
        }

        public List<Relay> Relays = new List<Relay>();


        public Form1() {
            InitializeComponent();
            string[] args = Environment.GetCommandLineArgs();
            string[] ComPortsNames = null;

            ComPortsNames = SerialPort.GetPortNames();
            comboBoxPorts.Items.Clear();

            for ( int i = 0; i < ComPortsNames.Length; i++ ) {
                comboBoxPorts.Items.Add( ComPortsNames[i] );
            }
            comboBoxPorts.SelectedIndex = 0;

        }

        private void Form1_Load( object sender, EventArgs e ) {
            string[] ComPortsNames = null;

            ComPortsNames = SerialPort.GetPortNames();
            comboBoxPorts.Items.Clear();

            for ( int i = 0; i < ComPortsNames.Length; i++ ) {
                comboBoxPorts.Items.Add( ComPortsNames[i] );
            }
            comboBoxPorts.SelectedIndex = 0;
        }

        private void btnOpen_Click( object sender, EventArgs e ) {

            serialPort.PortName = comboBoxPorts.SelectedItem.ToString();
            serialPort.Open();
            panel1.Visible = serialPort.IsOpen;

            Relays.Clear();
            for ( int i = 0; i < RelayCount; i++ ) {
                Relays.Add( new Relay { RelayID = i, IsClosed = false } );
            }

        }

        private void TurnOnRelay( object sender, EventArgs e ) {
            object o = ( sender as RadioButton ).Tag;
        }

        private string RxString;

        private void serialPort_DataReceived( object sender, SerialDataReceivedEventArgs e ) {
            RxString = serialPort.ReadExisting();
            this.Invoke( new EventHandler( DisplayText ) );
        }

        private void DisplayText( object sender, EventArgs e ) {
            tbResponse.AppendText( RxString );
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

        private void btnAllOn_Click( object sender, EventArgs e ) {
            if ( serialPort.IsOpen ) {
                serialPort.WriteLine( "N0" );
                serialPort.Write( new byte[] { 13, 10 }, 0, 2 );
            }
        }

        private void btnAllOff_Click( object sender, EventArgs e ) {
            if ( serialPort.IsOpen ) {
                serialPort.WriteLine( "F0" );
                serialPort.Write( new byte[] { 13, 10 }, 0, 2 );
            }
        }

    }
}
