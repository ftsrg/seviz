/*
 * SEViz - Symbolic Execution VIsualiZation
 *
 * SEViz is a tool, which can support the test generation process by
 * visualizing the symbolic execution in a directed graph.
 *
 * Budapest University of Technology and Economics (BME)
 *
 * Authors: Dávid Honfi <david.honfi@inf.mit.bme.hu>, Zoltán Micskei
 * <micskeiz@mit.bme.hu>, András Vörös <vori@mit.bme.hu>
 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace SEViz.Viewer.Controls
{
    /// <summary>
    /// Interaction logic for MessageBox.xaml - http://stackoverflow.com/questions/2796470/wpf-create-a-dialog-prompt
    /// </summary>
    public partial class InputMessageBox : Window
    {
        public enum InputType
        {
            Text,
            Password
        }

        private InputType _inputType = InputType.Text;

        public InputMessageBox(string question, string title, string defaultValue = "")
        {
            InitializeComponent();
            this.Loaded += new RoutedEventHandler(PromptDialog_Loaded);
            txtQuestion.Text = question;
            Title = title;
            txtResponse.Text = defaultValue;

        }

        void PromptDialog_Loaded(object sender, RoutedEventArgs e)
        {

                txtResponse.Focus();
        }

        public static string Prompt(string question, string title, string defaultValue = "")
        {
            var inst = new InputMessageBox(question, title, defaultValue);
            inst.ShowDialog();
            if (inst.DialogResult == true)
                return inst.ResponseText;
            return null;
        }

        public string ResponseText
        {
            get
            {
                    return txtResponse.Text;
            }
        }

        private void btnOk_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
