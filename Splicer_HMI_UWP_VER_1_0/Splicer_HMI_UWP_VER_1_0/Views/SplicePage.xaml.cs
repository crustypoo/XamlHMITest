using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace Splicer_HMI_UWP_VER_1_0
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class SplicePage : Page
    {
        public SplicePage()
        {
            this.InitializeComponent();
            this.page_state_initializations();
            this.initialize_tvalues();
            this.initialize_jvalues();
            this.initialize_tensionUI();
            this.initialize_jogUI();
        }

        // Tension Control Vars
        private double t1_sp = 0;
        private double t2_sp = 0;
        private double t3_sp = 0;

        private double t1_act = 34.7;
        private double t2_act = 34;
        private double t3_act = 34;

        private bool t1active;
        private bool t2active;
        private bool t3active;

        // Jog Control Vars
        private double j1_sp = 0;
        private double j2_sp = 0;
        private double j3_sp = 0;

        private double j1_prev_sp;
        private double j2_prev_sp;
        private double j3_prev_sp;

        private bool j1_active;
        private bool j2_active;
        private bool j3_active;

        // Max Vars
        private double t_max = 60;
        private double v_max = 20;

        /******************************************************************************************
         * Pivot Controls
         * forward/back navigation
         ******************************************************************************************/
        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            if (rootPivot.SelectedIndex > 0)
            {
                // If not at the first item, go back to the previous one.
                rootPivot.SelectedIndex -= 1;
            }
            else
            {
                // The first PivotItem is selected, so loop around to the last item.
                rootPivot.SelectedIndex = rootPivot.Items.Count - 1;
            }
        }

        private void NextButton_Click(object sender, RoutedEventArgs e)
        {
            if (rootPivot.SelectedIndex < rootPivot.Items.Count - 1)
            {
                // If not at the last item, go to the next one.
                rootPivot.SelectedIndex += 1;
            }
            else
            {
                // The last PivotItem is selected, so loop around to the first item.
                rootPivot.SelectedIndex = 0;
            }
        }

        /*******************************************************************************************
         * Page variable initializations
         *******************************************************************************************/
        // Initialize values of Tension Zones
        private void initialize_tvalues()
        {
            // SP initializations
            t1sptextblock.Text = t1_sp.ToString();
            t2sptextblock.Text = t2_sp.ToString();
            t3sptextblock.Text = t3_sp.ToString();
            t1textbox.Text = t1_sp.ToString();
            t2textbox.Text = t2_sp.ToString();
            t3textbox.Text = t3_sp.ToString();
            t1slider.Value = t1_sp;
            t2slider.Value = t2_sp;
            t3slider.Value = t3_sp;

            t1slider.Maximum = t_max;
            t2slider.Maximum = t_max;
            t3slider.Maximum = t_max;

            // Monitor Initializations
            t1actualtextblock.Text = ((int)t1_act).ToString();
            t2actualtextblock.Text = ((int)t2_act).ToString();
            t3actualtextblock.Text = ((int)t3_act).ToString();
        }

        // Initialize values for Jog Control 
        private void initialize_jvalues()
        {
            daxis_textbox.Text = j1_sp.ToString();
            oaxis_textbox.Text = j2_sp.ToString();
            taxis_textbox.Text = j3_sp.ToString();

            daxis_vel_setting.Text = j1_sp.ToString();
            oaxis_vel_setting.Text = j2_sp.ToString();
            taxis_vel_setting.Text = j3_sp.ToString();

            daxis_slider.Value = j1_sp;
            oaxis_slider.Value = j2_sp;
            taxis_slider.Value = j3_sp;

            daxis_slider.Maximum = v_max;
            oaxis_slider.Maximum = v_max;
            taxis_slider.Maximum = v_max;

            j1_prev_sp = j1_sp;
            j2_prev_sp = j2_sp;
            j3_prev_sp = j3_sp;
        }

        // Initializations for halt system
        private void page_state_initializations()
        {
            resume_toggle.IsChecked = true;
            halt_toggle.IsChecked = false;
        }


        /*******************************************************************************************
         * Message Box Prompts
         *******************************************************************************************/
        // Message Dialog - textbox input is non-numerical
        private async void display_dialog_textbox()
        {
            var dialog = new Windows.UI.Popups.MessageDialog("Attention! " +
                "Please enter a valid decimal number!");
            dialog.Commands.Add(new Windows.UI.Popups.UICommand { Label = "Okay", Id = 0 });
            await dialog.ShowAsync();
        }

        // Message Dialog - Tension zone closure out of order
        private async void display_dialog_tensionoutoforder(int zone)
        {
            var dialog = new Windows.UI.Popups.MessageDialog("Attention! " +
                "Please ensure that tension zone " + zone + " is deactivated first!");
            dialog.Commands.Add(new Windows.UI.Popups.UICommand { Label = "Okay", Id = 0 });
            await dialog.ShowAsync();
        }

        private async void display_dialog_multipleaxes(int axis)
        {
            string axis_name = "DRIVE";
            if (axis == 1)
                axis_name = "DRIVE";
            else if (axis == 2)
                axis_name = "ORIGIN";
            else if (axis == 3)
                axis_name = "TARGET";

            var dialog = new Windows.UI.Popups.MessageDialog("Attention! Please ensure the "
                + axis_name + " AXIS is disabled first! User limited to one axis operation.");
            dialog.Commands.Add(new Windows.UI.Popups.UICommand { Label = "Okay", Id = 0 });
            await dialog.ShowAsync();
        }

        private async void display_dialog_enabletension(int zone)
        {
            var dialog = new Windows.UI.Popups.MessageDialog("Attention! " +
                "Please ensure that tension zone " + zone + " is activated first!");
            dialog.Commands.Add(new Windows.UI.Popups.UICommand { Label = "Okay", Id = 0 });
            await dialog.ShowAsync();
        }

        // Machine Configuration Page

        /******************************************************************************************
         * Tension Zone Controls 
         * Slider, plus/minus, textbox, toggle
         *****************************************************************************************/
        // Tension Zone UI initializations
        private void initialize_tensionUI()
        {
            disable_tension_ui(2);
            disable_tension_ui(3);

            t1active = false;
            t2active = false;
            t3active = false;
        }

        // Disable tension zone UI elements
        private void disable_tension_ui(int zone)
        {
            if (zone == 1)
            {
                t1slider.IsEnabled = false;
                t1textbox.IsEnabled = false;
                t1plusbutton.IsEnabled = false;
                t1minusbutton.IsEnabled = false;
                t1_toggle.IsEnabled = false;
            }
            else if (zone == 2)
            {
                t2slider.IsEnabled = false;
                t2textbox.IsEnabled = false;
                t2plusbutton.IsEnabled = false;
                t2minusbutton.IsEnabled = false;
                t2_toggle.IsEnabled = false;
            }
            else if (zone == 3)
            {
                t3slider.IsEnabled = false;
                t3textbox.IsEnabled = false;
                t3plusbutton.IsEnabled = false;
                t3minusbutton.IsEnabled = false;
                t3_toggle.IsEnabled = false;
            }
        }

        // Enable tension zone UI elements
        private void enable_tension_ui(int zone)
        {
            if (zone == 1)
            {
                t1slider.IsEnabled = true;
                t1textbox.IsEnabled = true;
                t1plusbutton.IsEnabled = true;
                t1minusbutton.IsEnabled = true;
                t1_toggle.IsEnabled = true;
            }
            else if (zone == 2)
            {
                t2slider.IsEnabled = true;
                t2textbox.IsEnabled = true;
                t2plusbutton.IsEnabled = true;
                t2minusbutton.IsEnabled = true;
                t2_toggle.IsEnabled = true;
            }
            else if (zone == 3)
            {
                t3slider.IsEnabled = true;
                t3textbox.IsEnabled = true;
                t3plusbutton.IsEnabled = true;
                t3minusbutton.IsEnabled = true;
                t3_toggle.IsEnabled = true;
            }
        }

        // T1 Value Change
        private void t1_slide_valuechanged(object sender, RoutedEventArgs e)
        {
            t1_sp = t1slider.Value;
            t1sptextblock.Text = ((int)t1_sp).ToString();
            t1textbox.Text = t1_sp.ToString();
        }
        // T2 Value Change
        private void t2_slide_valuechanged(object sender, RoutedEventArgs e)
        {
            t2_sp = t2slider.Value;
            t2sptextblock.Text = ((int)t2_sp).ToString();
            t2textbox.Text = t2_sp.ToString();
        }
        // T3 Value Change
        private void t3_slide_valuechanged(object sender, RoutedEventArgs e)
        {
            t3_sp = t3slider.Value;
            t3sptextblock.Text = ((int)t3_sp).ToString();
            t3textbox.Text = t3_sp.ToString();
        }

        // Modify T1 value w/ TextBox
        private void t1_textboxchanged(object sender, RoutedEventArgs e)
        {
            int sp_tmp;
            if (!Double.TryParse(t1textbox.Text, out t1_sp))
            {
                //t1textbox.Text = t1sptextblock.Text;
            }
            else
            {
                if (t1_sp < 0)
                {
                    t1_sp = 0;
                }
                else if (t1_sp > t_max)
                {
                    t1_sp = t_max;
                }
                sp_tmp = (int)t1_sp;
                t1sptextblock.Text = sp_tmp.ToString();
                t1slider.Value = t1_sp;
            }
        }
        // Modify T2 value w/ TextBox
        private void t2_textboxchanged(object sender, RoutedEventArgs e)
        {
            int sp_tmp;
            if (!Double.TryParse(t2textbox.Text, out t2_sp))
            {
                //t2textbox.Text = t2sptextblock.Text;
            }
            else
            {
                if (t2_sp < 0)
                {
                    t2_sp = 0;
                }
                else if (t2_sp > t_max)
                {
                    t2_sp = t_max;
                }
                sp_tmp = (int)t2_sp;
                t2sptextblock.Text = sp_tmp.ToString();
                t2slider.Value = t2_sp;
            }
        }
        // Modify T3 value w/ TextBox
        private void t3_textboxchanged(object sender, RoutedEventArgs e)
        {
            int sp_tmp;
            if (!Double.TryParse(t3textbox.Text, out t3_sp))
            {
                //t3textbox.Text = t3sptextblock.Text;
            }
            else
            {
                if (t3_sp < 0)
                {
                    t3_sp = 0;
                }
                else if (t3_sp > t_max)
                {
                    t3_sp = t_max;
                }
                sp_tmp = (int)t3_sp;
                t3sptextblock.Text = sp_tmp.ToString();
                t3slider.Value = t3_sp;
            }
        }

        // Modify T1 value w/ Slider
        private void t1_slidermanipulated(object sender, RoutedEventArgs e)
        {
            t1_sp = t1slider.Value;
            t1sptextblock.Text = ((int)t1_sp).ToString();
            t1textbox.Text = ((int)t1_sp).ToString();
        }
        // Modify T2 value w/ Slider
        private void t2_slidermanipulated(object sender, RoutedEventArgs e)
        {
            t2_sp = t2slider.Value;
            t2sptextblock.Text = ((int)t2_sp).ToString();
            t2textbox.Text = ((int)t2_sp).ToString();
        }
        // Modify T3 value w/ Slider
        private void t3_slidermanipulated(object sender, RoutedEventArgs e)
        {
            t3_sp = t3slider.Value;
            t3sptextblock.Text = ((int)t3_sp).ToString();
            t3textbox.Text = ((int)t3_sp).ToString();
        }

        // Increase T1 value w/ plus button
        private void t1plus_click(object sender, RoutedEventArgs e)
        {
            int sp_tmp;
            if (t1_sp < 0)
            {
                t1_sp = 0;
            }
            else if (t1_sp <= t_max - 0.1 && t1_sp >= 0)
            {
                t1_sp += 0.1;
            }
            else
            {
                t1_sp = t_max;
            }
            sp_tmp = (int)t1_sp;
            t1sptextblock.Text = sp_tmp.ToString();
            t1textbox.Text = t1_sp.ToString();
            t1slider.Value = t1_sp;
        }
        // Increase T2 value w/ plus button
        private void t2plus_click(object sender, RoutedEventArgs e)
        {
            int sp_tmp;
            if (t2_sp < 0)
            {
                t2_sp = 0;
            }
            else if (t2_sp <= t_max - 0.1 && t2_sp >= 0)
            {
                t2_sp += 0.1;
            }
            else
            {
                t2_sp = t_max;
            }
            sp_tmp = (int)t2_sp;
            t2sptextblock.Text = sp_tmp.ToString();
            t2textbox.Text = t2_sp.ToString();
            t2slider.Value = t2_sp;
        }
        // Increase T3 value w/ plus button
        private void t3plus_click(object sender, RoutedEventArgs e)
        {
            int sp_tmp;
            if (t3_sp < 0)
            {
                t3_sp = 0;
            }
            else if (t3_sp <= t_max - 0.1 && t3_sp >= 0)
            {
                t3_sp += 0.1;
            }
            else
            {
                t3_sp = t_max;
            }
            sp_tmp = (int)t3_sp;
            t3sptextblock.Text = sp_tmp.ToString();
            t3textbox.Text = t3_sp.ToString();
            t3slider.Value = t3_sp;
        }

        // decrease T1 value w/ minus button
        private void t1minus_click(object sender, RoutedEventArgs e)
        {
            int sp_tmp;
            if (t1_sp >= 0.1)
                t1_sp -= 0.1;
            else if (t1_sp > 0 && t1_sp < 0.1)
                t1_sp = 0;
            sp_tmp = (int)t1_sp;
            t1sptextblock.Text = sp_tmp.ToString();
            t1textbox.Text = t1_sp.ToString();
            t1slider.Value = t1_sp;
        }
        // decrease T2 value w/ minus button
        private void t2minus_click(object sender, RoutedEventArgs e)
        {
            int sp_tmp;
            if (t2_sp >= 0.1)
                t2_sp -= 0.1;
            else if (t2_sp > 0 && t2_sp < 0.1)
                t2_sp = 0;
            sp_tmp = (int)t2_sp;
            t2sptextblock.Text = sp_tmp.ToString();
            t2textbox.Text = t2_sp.ToString();
            t2slider.Value = t2_sp;
        }
        // decrease T3 value w/ minus button
        private void t3minus_click(object sender, RoutedEventArgs e)
        {
            int sp_tmp;
            if (t3_sp >= 1)
                t3_sp -= 0.1;
            else if (t3_sp > 0 && t3_sp < 0.1)
                t3_sp = 0;
            sp_tmp = (int)t3_sp;
            t3sptextblock.Text = sp_tmp.ToString();
            t3textbox.Text = t3_sp.ToString();
            t3slider.Value = t3_sp;
        }


        private void t1_togglechecked(object sender, RoutedEventArgs e)
        {
            t1active = true;
            enable_tension_ui(2);
            if (t2active == true)
                enable_tension_ui(3);
            // TODO
        }
        private void t2_togglechecked(object sender, RoutedEventArgs e)
        {
            t2active = true;
            if (t1active == true)
                enable_tension_ui(3);
            // TODO
        }
        private void t3_togglechecked(object sender, RoutedEventArgs e)
        {
            t3active = true;
            // TODO
        }

        private void t1_toggleunchecked(object sender, RoutedEventArgs e)
        {
            if (t3active == true)
            {
                t1_toggle.IsChecked = true;
                display_dialog_tensionoutoforder(3);
            }
            else
            {
                t1active = false;
                disable_tension_ui(3);
                // TODO
            }
        }
        private void t2_toggleunchecked(object sender, RoutedEventArgs e)
        {
            if (t3active == true)
            {
                t2_toggle.IsChecked = true;
                display_dialog_tensionoutoforder(3);
            }
            else
            {
                t2active = false;
                disable_tension_ui(3);
                // TODO
            }
        }
        private void t3_toggleunchecked(object sender, RoutedEventArgs e)
        {
            t3active = false;
            // TODO
        }

        /**********************************************************************************************
        / Jog Control Event Handlers
        / Slider, Plus/Minus, textbox
        /*********************************************************************************************/

        // Jog control ui initializations
        public void initialize_jogUI()
        {
            j1_active = false;
            j2_active = false;
            j3_active = false;

            disable_jog_controls(1);
            disable_jog_controls(2);
            disable_jog_controls(3);

            daxis_toggle.IsOn = false;
            oaxis_toggle.IsOn = false;
            taxis_toggle.IsOn = false;
        }

        public void disable_jog_controls(int axis)
        {
            if (axis == 1)
            {
                daxis_textbox.IsEnabled = false;
                daxis_plusbutton.IsEnabled = false;
                daxis_minusbutton.IsEnabled = false;
                daxis_slider.IsEnabled = false;
                daxis_buttonforward.IsEnabled = false;
                daxis_buttonreverse.IsEnabled = false;
                daxis_toggleforward.IsEnabled = false;
                daxis_togglereverse.IsEnabled = false;
            }
            else if (axis == 2)
            {
                oaxis_textbox.IsEnabled = false;
                oaxis_plusbutton.IsEnabled = false;
                oaxis_minusbutton.IsEnabled = false;
                oaxis_slider.IsEnabled = false;
                oaxis_buttonforward.IsEnabled = false;
                oaxis_buttonreverse.IsEnabled = false;
                oaxis_toggleforward.IsEnabled = false;
                oaxis_togglereverse.IsEnabled = false;
            }
            else if (axis == 3)
            {
                taxis_textbox.IsEnabled = false;
                taxis_plusbutton.IsEnabled = false;
                taxis_minusbutton.IsEnabled = false;
                taxis_slider.IsEnabled = false;
                taxis_buttonforward.IsEnabled = false;
                taxis_buttonreverse.IsEnabled = false;
                taxis_toggleforward.IsEnabled = false;
                taxis_togglereverse.IsEnabled = false;
            }
        }

        public void enable_jog_controls(int axis)
        {
            if (axis == 1)
            {
                daxis_textbox.IsEnabled = true;
                daxis_plusbutton.IsEnabled = true;
                daxis_minusbutton.IsEnabled = true;
                daxis_slider.IsEnabled = true;
                daxis_buttonforward.IsEnabled = true;
                daxis_buttonreverse.IsEnabled = true;
                daxis_toggleforward.IsEnabled = true;
                daxis_togglereverse.IsEnabled = true;
            }
            else if (axis == 2)
            {
                oaxis_textbox.IsEnabled = true;
                oaxis_plusbutton.IsEnabled = true;
                oaxis_minusbutton.IsEnabled = true;
                oaxis_slider.IsEnabled = true;
                oaxis_buttonforward.IsEnabled = true;
                oaxis_buttonreverse.IsEnabled = true;
                oaxis_toggleforward.IsEnabled = true;
                oaxis_togglereverse.IsEnabled = true;
            }
            else if (axis == 3)
            {
                taxis_textbox.IsEnabled = true;
                taxis_plusbutton.IsEnabled = true;
                taxis_minusbutton.IsEnabled = true;
                taxis_slider.IsEnabled = true;
                taxis_buttonforward.IsEnabled = true;
                taxis_buttonreverse.IsEnabled = true;
                taxis_toggleforward.IsEnabled = true;
                taxis_togglereverse.IsEnabled = true;
            }
        }

        private void daxis_activated(object sender, RoutedEventArgs e)
        {
            if (daxis_toggle.IsOn == true)
            {
                if (j2_active == false && j3_active == false && t1active == true)
                {
                    j1_active = true;
                    enable_jog_controls(1);
                }
                else
                {
                    if (j2_active == true)
                    {
                        display_dialog_multipleaxes(2);
                    }
                    else if (j3_active == true)
                    {
                        display_dialog_multipleaxes(3);
                    }
                    else if (t1active == false)
                    {
                        display_dialog_enabletension(1);
                    }
                    daxis_toggle.IsOn = false;
                    j1_active = false;
                }
            }
            else
            {
                j1_active = false;
                disable_jog_controls(1);
            }
        }

        private void oaxis_activated(object sender, RoutedEventArgs e)
        {
            if (oaxis_toggle.IsOn == true)
            {
                if (j1_active == false && j3_active == false && t1active == false && t3active == false)
                {
                    j2_active = true;
                    enable_jog_controls(2);
                }
                else
                {
                    if (j1_active == true)
                    {
                        display_dialog_multipleaxes(1);
                    }
                    else if (j3_active == true)
                    {
                        display_dialog_multipleaxes(3);
                    }
                    else if (t3active == true)
                    {
                        display_dialog_tensionoutoforder(3);
                    }
                    else if (t1active == true)
                    {
                        display_dialog_tensionoutoforder(1);
                    }
                    oaxis_toggle.IsOn = false;
                    j2_active = false;
                }
            }
            else
            {
                j2_active = false;
                disable_jog_controls(2);
            }
        }

        private void taxis_activated(object sender, RoutedEventArgs e)
        {
            if (taxis_toggle.IsOn == true)
            {
                if (j1_active == false && j2_active == false && t3active == false)
                {
                    j3_active = true;
                    enable_jog_controls(3);
                }
                else
                {
                    if (j1_active == true)
                    {
                        display_dialog_multipleaxes(1);
                    }
                    else if (j2_active == true)
                    {
                        display_dialog_multipleaxes(2);
                    }
                    else if (t3active == true)
                    {
                        display_dialog_tensionoutoforder(3);
                    }
                    taxis_toggle.IsOn = false;
                    j3_active = false;
                }
            }
            else
            {
                j3_active = false;
                disable_jog_controls(3);
            }
        }

        // text box input for jog velocity
        private void daxis_textboxtextchanged(object sender, RoutedEventArgs e)
        {
            int sp_tmp;
            if (!Double.TryParse(daxis_textbox.Text, out j1_sp))
            {
                display_dialog_textbox();
            }
            else
            {
                if (j1_sp < 0)
                {
                    j1_sp = 0;
                }
                else if (j1_sp > v_max)
                {
                    j1_sp = v_max;
                }
                sp_tmp = (int)j1_sp;
                daxis_vel_setting.Text = sp_tmp.ToString();
                daxis_slider.Value = j1_sp;
            }
        }
        private void oaxis_textboxtextchanged(object sender, RoutedEventArgs e)
        {
            int sp_tmp;
            if (!Double.TryParse(oaxis_textbox.Text, out j2_sp))
            {
                // TODO
            }
            else
            {
                if (j2_sp < 0)
                {
                    j2_sp = 0;
                }
                else if (j2_sp > v_max)
                {
                    j2_sp = v_max;
                }
                sp_tmp = (int)j2_sp;
                oaxis_vel_setting.Text = sp_tmp.ToString();
                oaxis_slider.Value = j2_sp;
            }
        }
        private void taxis_textboxtextchanged(object sender, RoutedEventArgs e)
        {
            int sp_tmp;
            if (!Double.TryParse(taxis_textbox.Text, out j3_sp))
            {
                // TODO
            }
            else
            {
                if (j3_sp < 0)
                {
                    j3_sp = 0;
                }
                else if (j3_sp > v_max)
                {
                    j3_sp = v_max;
                }
                sp_tmp = (int)j3_sp;
                taxis_vel_setting.Text = sp_tmp.ToString();
                taxis_slider.Value = j3_sp;
            }
        }

        // plus button increment for jog velocity
        private void daxis_plusbuttonclick(object sender, RoutedEventArgs e)
        {
            int sp_tmp;
            if (j1_sp < 0)
            {
                j1_sp = 0;
            }
            else if (j1_sp <= v_max - 0.1 && j1_sp >= 0)
            {
                j1_sp += 0.1;
            }
            else
            {
                j1_sp = v_max;
            }
            sp_tmp = (int)j1_sp;
            daxis_vel_setting.Text = sp_tmp.ToString();
            daxis_textbox.Text = j1_sp.ToString();
            daxis_slider.Value = j1_sp;
            j1_prev_sp = j1_sp;
        }
        private void oaxis_plusbuttonclick(object sender, RoutedEventArgs e)
        {
            int sp_tmp;
            if (j2_sp < 0)
            {
                j2_sp = 0;
            }
            else if (j2_sp <= v_max - 0.1 && j2_sp >= 0)
            {
                j2_sp += 0.1;
            }
            else
            {
                j2_sp = v_max;
            }
            sp_tmp = (int)j2_sp;
            oaxis_vel_setting.Text = sp_tmp.ToString();
            oaxis_textbox.Text = j2_sp.ToString();
            oaxis_slider.Value = j2_sp;
            j2_prev_sp = j2_sp;
        }
        private void taxis_plusbuttonclick(object sender, RoutedEventArgs e)
        {
            int sp_tmp;
            if (j3_sp < 0)
            {
                j3_sp = 0;
            }
            else if (j3_sp <= v_max - 0.1 && j3_sp >= 0)
            {
                j3_sp += 0.1;
            }
            else
            {
                j3_sp = v_max;
            }
            sp_tmp = (int)j3_sp;
            taxis_vel_setting.Text = sp_tmp.ToString();
            taxis_textbox.Text = j3_sp.ToString();
            taxis_slider.Value = j3_sp;
            j3_prev_sp = j3_sp;
        }

        // minus button increment for jog velocity
        private void daxis_minusbuttonclick(object sender, RoutedEventArgs e)
        {
            int sp_tmp;
            if (j1_sp >= 0.1)
                j1_sp -= 0.1;
            else if (j1_sp > 0 && j1_sp < 0.1)
                j1_sp = 0;
            sp_tmp = (int)j1_sp;
            daxis_vel_setting.Text = sp_tmp.ToString();
            daxis_textbox.Text = j1_sp.ToString();
            daxis_slider.Value = j1_sp;
            j1_prev_sp = j1_sp;
        }
        private void oaxis_minusbuttonclick(object sender, RoutedEventArgs e)
        {
            int sp_tmp;
            if (j2_sp >= 0.1)
                j2_sp -= 0.1;
            else if (j2_sp > 0 && j2_sp < 0.1)
                j2_sp = 0;
            sp_tmp = (int)j2_sp;
            oaxis_vel_setting.Text = sp_tmp.ToString();
            oaxis_textbox.Text = j2_sp.ToString();
            oaxis_slider.Value = j2_sp;
            j2_prev_sp = j2_sp;
        }
        private void taxis_minusbuttonclick(object sender, RoutedEventArgs e)
        {
            int sp_tmp;
            if (j3_sp >= 0.1)
                j3_sp -= 0.1;
            else if (j3_sp > 0 && j3_sp < 0.1)
                j3_sp = 0;
            sp_tmp = (int)j3_sp;
            taxis_vel_setting.Text = sp_tmp.ToString();
            taxis_textbox.Text = j3_sp.ToString();
            taxis_slider.Value = j3_sp;
            j3_prev_sp = j3_sp;
        }

        // slider input for jog velocity 
        private void daxis_sliderchange(object sender, RoutedEventArgs e)
        {
            j1_sp = daxis_slider.Value;
            daxis_vel_setting.Text = ((int)j1_sp).ToString();
            daxis_textbox.Text = j1_sp.ToString();
            j1_prev_sp = j1_sp;
        }
        private void oaxis_sliderchange(object sender, RoutedEventArgs e)
        {
            j2_sp = oaxis_slider.Value;
            oaxis_vel_setting.Text = ((int)j2_sp).ToString();
            oaxis_textbox.Text = j2_sp.ToString();
            j2_prev_sp = j2_sp;
        }
        private void taxis_sliderchange(object sender, RoutedEventArgs e)
        {
            j3_sp = taxis_slider.Value;
            taxis_vel_setting.Text = ((int)j3_sp).ToString();
            taxis_textbox.Text = j3_sp.ToString();
            j3_prev_sp = j3_sp;
        }

        // Runtime Page
        /*****************************************************************************************************
         * System Halt Control Panel
         *****************************************************************************************************/

        private void disable_panelactivation_toggles(int panel_id)
        {
            switch (panel_id)
            {
                case 1:
                    daxis_toggle.IsEnabled = false;
                    break;
                case 2:
                    oaxis_toggle.IsEnabled = false;
                    break;
                case 3:
                    taxis_toggle.IsEnabled = false;
                    break;
            }
        }

        private void enable_panelactivation_toggles(int panel_id)
        {
            switch (panel_id)
            {
                case 1:
                    daxis_toggle.IsEnabled = true;
                    break;
                case 2:
                    oaxis_toggle.IsEnabled = true;
                    break;
                case 3:
                    taxis_toggle.IsEnabled = true;
                    break;
            }
        }

        private void halt_activated(object sender, RoutedEventArgs e)
        {
            halt_toggle.IsChecked = false;
            halt_toggle.IsEnabled = false;
            resume_toggle.IsEnabled = true;

            // disable control panels
            t1_toggle.IsChecked = false;
            t2_toggle.IsChecked = false;
            t3_toggle.IsChecked = false;

            daxis_toggle.IsOn = false;
            oaxis_toggle.IsOn = false;
            taxis_toggle.IsOn = false;

            disable_tension_ui(1);
            disable_tension_ui(2);
            disable_tension_ui(3);

            disable_jog_controls(1);
            disable_jog_controls(2);
            disable_jog_controls(3);

            disable_panelactivation_toggles(1);
            disable_panelactivation_toggles(2);
            disable_panelactivation_toggles(3);
        }

        private void resume_activated(object sender, RoutedEventArgs e)
        {
            resume_toggle.IsChecked = false;
            resume_toggle.IsEnabled = false;
            halt_toggle.IsEnabled = true;

            enable_tension_ui(1);
            enable_panelactivation_toggles(1);
            enable_panelactivation_toggles(2);
            enable_panelactivation_toggles(3);
        }



    }
}
