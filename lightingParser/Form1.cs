﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace lightingParser
{
    public partial class Form1 : Form
    {
        string LCDString;
        public GR2400IPInterface lightingInterface;
        BindingSource bs;

        delegate void SetTextCallback(string text);

        public Form1()
        {
            InitializeComponent();
        }

        public void MessageReceived(object sender, EventArgs e)
        {

        }

        private void Form1_Load(object sender, EventArgs e)
        {
            LCDString = "                                                                                                                                                                                                                                                                                                                                                                                                              ";
            lightingInterface = new GR2400IPInterface();
            lightingInterface.NewMessage += MessageReceived;
            bs = new BindingSource();
            bs.DataSource = lightingInterface.LightingDataLog;
            dataListBox.DataSource = bs;
        }

        bool checkForMatch(byte[]array1 , byte[]array2, int start)
        {
            if (array1.Length < (start + array2.Length))
                return false;

            for (int i = 0; i < array2.Length; i++)
                if (array1[start + i] != array2[i])
                    return false;

                return true;
        }

        //Data format: "aa:bb:cc:dd:"
        private string LCDtext(byte[] hexData)
        {
            int horiz = 0;
            int position = 0;
            byte[] startMarker = {0x33,0x38,0x30,0x31};
            string ret = "";
            while(true)
            {
                if (checkForMatch(hexData,startMarker, position))
                {
                    position += startMarker.Length;

                    string char1b = Convert.ToChar(hexData[position]).ToString();
                    string char2b = Convert.ToChar(hexData[position+1]).ToString();
                    string newstringb = char1b + char2b;

                    int x = Convert.ToUInt16(newstringb, 16);
                    ret += "X: " + x.ToString() + " ";

                    char1b = Convert.ToChar(hexData[position+4]).ToString(); ;
                    char2b = Convert.ToChar(hexData[position+5]).ToString();;
                    newstringb = char1b + char2b;

                    x = Convert.ToUInt16(newstringb, 16);
                    ret += "L: " + x.ToString() + " ";

                    horiz = x * 16;

                    position += 6;
                    while(true)
                    {
                        
                        int nextByte = hexData[position];
                        position++;
                        int nextByte2; 
                        if (nextByte == 0x0d)
                        {
                            ret = ret.Substring(0, ret.Length - 1); //last char is a checksum
                            ret += Environment.NewLine + "--------" + Environment.NewLine;
                            break;
                        }
                        else
                        {
                            nextByte2 = hexData[position];
                            position += 1;

                            string char1 = Convert.ToChar(nextByte).ToString();
                            string char2 = Convert.ToChar(nextByte2).ToString();
                            string newstring = char1 + char2;

                            char newChar = Convert.ToChar( Convert.ToUInt16(newstring ,16) );
                            if (newChar == 14)
                                newChar = ' ';
                            if (newChar < 32 || newChar > 126)
                                newChar = '*';
                            if(newChar != 0)
                             ret += newChar;
                            StringBuilder sb = new StringBuilder(LCDString);
                            sb[horiz] = newChar;
                            LCDString = sb.ToString();
                            
                        }
                        horiz++;
                    }
                }
                else
                {
                    position += 1;
                }
                if (position >= hexData.Length)
                {
                    return ret;
                }
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            lightingInterface.QueryLCDData();
        }

        private void updateLCDBox()
        {
            int at=0;

            textBox3.Text = "";
            for (int i = 0; i < LCDString.Length; i++)
            {

                if ((i % 22) != 0)
                {
                    textBox3.Text = textBox3.Text + LCDString.Substring(at, 1);
                    at++;
                }
                else
                    textBox3.Text = textBox3.Text + Environment.NewLine;
                
            }
        }

        private void ScrollUpBtn_Click(object sender, EventArgs e)
        {
            lightingInterface.ScrollUp();
        }

        private void TabUpBtn_Click(object sender, EventArgs e)
        {
            lightingInterface.TabUp();
        }

        private void ScrollDownBtn_Click(object sender, EventArgs e)
        {
            lightingInterface.ScrollDown();
        }

        private void EnterBtn_Click(object sender, EventArgs e)
        {
            lightingInterface.Enter();
        }

        private void ExitBtn_Click(object sender, EventArgs e)
        {
            lightingInterface.Exit();
        }

        private void DeleteBtn_Click(object sender, EventArgs e)
        {
            lightingInterface.Delete();
        }

        private void HelpBtn_Click(object sender, EventArgs e)
        {
            lightingInterface.Help();
        }

        private void QueryIDBtn_Click(object sender, EventArgs e)
        {
            lightingInterface.QueryID((int)QueryIDNumeric.Value);
            QueryIDNumeric.Value++;
        }

        private void listBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            dataTitlelbl.Text = ((sender as ListBox).SelectedItem as dataLog).Title;
            dataDescriptionTextBox.Text = ((sender as ListBox).SelectedItem as dataLog).Description + " - " +  ((sender as ListBox).SelectedItem as dataLog).Time.ToString();

            if (((sender as ListBox).SelectedItem as dataLog).ToPC != null)
                dataTextBox.Text = BitConverter.ToString(((sender as ListBox).SelectedItem as dataLog).ToPC) + Environment.NewLine + Environment.NewLine + System.Text.Encoding.ASCII.GetString(((sender as ListBox).SelectedItem as dataLog).ToPC);
            if (((sender as ListBox).SelectedItem as dataLog).FromPC != null)
                dataTextBox.Text = BitConverter.ToString(((sender as ListBox).SelectedItem as dataLog).FromPC) + Environment.NewLine + Environment.NewLine + System.Text.Encoding.ASCII.GetString(((sender as ListBox).SelectedItem as dataLog).FromPC);
        }

        private void dataTextBox_TextChanged(object sender, EventArgs e)
        {

        }

        private void QueryRelayBtn_Click(object sender, EventArgs e)
        {
            lightingInterface.QueryRelay((int)RelayQueryIDNumeric.Value,(int) RelayNumeric.Value - 1);
            RelayNumeric.Value++;
        }

        private void button2_Click(object sender, EventArgs e)
        {

                
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            RelayListView.Items.Clear();
            foreach (RelayC r in lightingInterface.DiscoveredRelays.ToArray())
            {
                RelayListView.Items.Add(r.Id.ToString() + " - " + (r.Relay + 1).ToString());
                if (r.RelayState == RelayStateT.Off)
                    RelayListView.Items[RelayListView.Items.Count - 1].BackColor = Color.Red;
                if (r.RelayState == RelayStateT.On)
                    RelayListView.Items[RelayListView.Items.Count - 1].BackColor = Color.Green;
                if (r.RelayState == RelayStateT.Unknown)
                    RelayListView.Items[RelayListView.Items.Count - 1].BackColor = Color.Yellow;
            }
        }

        private void BusScan_Tick(object sender, EventArgs e)
        {
            lightingInterface.QueryID((int)QueryIDNumeric.Value);
            QueryIDNumeric.Value++;
            if (QueryIDNumeric.Value == 40)
                BusScan.Enabled = false;
        }

        private void button2_Click_1(object sender, EventArgs e)
        {
            lightingInterface.changeParameter((int)nameIDslider.Value);
            nameIDslider.Value++;
        }

        private void button3_Click(object sender, EventArgs e)
        {
            lightingInterface.requestData();
        }

        private void button4_Click(object sender, EventArgs e)
        {
            lightingInterface.changeGetID((int)busIDNumeric.Value);
        }
    }
}
