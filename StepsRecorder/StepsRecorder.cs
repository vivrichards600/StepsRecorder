using Microsoft.Win32;
using System;
using System.Configuration;
using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Forms;

namespace StepsRecorder
{
    public partial class StepsRecorder : Form
    {
        public StepsRecorder()
        {
            InitializeComponent();
        }

        String IEVersion = "latest";
        Boolean RecordElementPosition = false; // needs work
        String user = "User 1";
        Boolean outputUser = true;

        /// <summary>
        /// Load settings from App.config
        /// </summary>
        void LoadSettings()
        {
            try
            {
                // urls to auto load in combobox to navigate to
                var listOfUrls = ConfigurationSettings.AppSettings.Get("StartupUrls").Split(',');
                foreach (String url in listOfUrls)
                {
                    toolStripNavigateComboBox.Items.Add(url);
                }
                toolStripNavigateComboBox.SelectedIndex = 0;

                //set browser window tab names - can customise these in app settings to change actors in test steps script
                tabControl2.TabPages[0].Text = ConfigurationSettings.AppSettings.Get("BrowserTab1") != "" ? ConfigurationSettings.AppSettings.Get("BrowserTab1") : "User";
                user = tabControl2.TabPages[0].Text;
                tabControl2.TabPages[1].Text = ConfigurationSettings.AppSettings.Get("BrowserTab2") != "" ? ConfigurationSettings.AppSettings.Get("BrowserTab2") : "User";

                // Allow user to specify version of IE - defaults to latest
                IEVersion = ConfigurationSettings.AppSettings.Get("IEVersion");
                setSpecifiedIEVersion();

                //--------------------------------------------
                //* NEEDS WORK **
                // Record where element is - top, middle, bottom, left, right. Switched off by default (needs more work)
                RecordElementPosition = Convert.ToBoolean(ConfigurationSettings.AppSettings.Get("RecordElementPosition"));
            }
            catch
            {
                throw new Exception("Problem loading StepsRecorder config file. Please make sure it exists and that the appropriate values are input!");
            }

        }

        /// <summary>
        /// Set version of IE to run - will default to use latest if not specified
        /// or IE7 if it cannot recognise user setting
        /// </summary>
        void setSpecifiedIEVersion()
        {

            int BrowserVer, RegVal;
            if (IEVersion == "latest" || IEVersion == "")
            {
                //check latest version of IE on machine and set registry to use that version
                // get the installed IE version
                using (WebBrowser Wb = new WebBrowser())
                    BrowserVer = Wb.Version.Major;

                // set the appropriate IE version
                if (BrowserVer >= 11)
                    RegVal = 11001;
                else if (BrowserVer == 10)
                    RegVal = 10001;
                else if (BrowserVer == 9)
                    RegVal = 9999;
                else if (BrowserVer == 8)
                    RegVal = 8888;
                else
                    RegVal = 7000;
            }
            else
            {
                // attempt to use specified IE version
                if (IEVersion == "11")
                    RegVal = 11001;
                else if (IEVersion == "10")
                    RegVal = 10001;
                else if (IEVersion == "9")
                    RegVal = 9999;
                else if (IEVersion == "8")
                    RegVal = 8888;
                else
                    RegVal = 7000; //defaults to IE7
            }

            // set the actual key
            RegistryKey Key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Internet Explorer\Main\FeatureControl\FEATURE_BROWSER_EMULATION", true);
            Key.SetValue(System.Diagnostics.Process.GetCurrentProcess().ProcessName + ".exe", RegVal, RegistryValueKind.DWord);
            Key.Close();
        }

        Boolean mouseDownAdded1 = false;
        Boolean mouseDownAdded2 = false;

        void Document_MouseDown(object sender, HtmlElementEventArgs e)
        {
            WebBrowser browser = new WebBrowser();
            if (tabControl2.SelectedIndex == 0)
            {
                browser = webBrowser1;
            }
            else
            {
                browser = webBrowser2;
            }
            if (recordSteps == true)
            {
                if (e.MouseButtonsPressed == MouseButtons.Left)
                {
                    HtmlElement element = null;
                    element = browser.Document.GetElementFromPoint(e.ClientMousePosition);

                    if (element != null)
                    {
                        //vertical Y , horizontal X
                        var elementYPosition = ""; // position of element from top of page
                        var elementXPosition = ""; // position of element from left of page

                        if (RecordElementPosition == true)
                        {
                            //get position from top
                            if (e.ClientMousePosition.Y > (browser.Document.Window.Size.Height / 4) * 2)
                            {
                                //toward bottom
                                elementYPosition = "bottom";
                            }
                            else if (e.ClientMousePosition.Y < (browser.Document.Window.Size.Height / 2))
                            {
                                //toward top
                                elementYPosition = "top";
                            }
                            else
                            {
                                //near middle
                                elementYPosition = "";//"middle";
                            }
                            //get position from left
                            if (e.ClientMousePosition.X > (browser.Document.Window.Size.Width / 4) * 2)
                            {
                                //toward right
                                elementXPosition = "right";
                            }
                            else if (e.ClientMousePosition.X < (browser.Document.Window.Size.Width / 2))
                            {
                                //toward left
                                elementXPosition = "left";
                            }
                            else
                            {
                                //near middle
                                elementXPosition = "";//"middle";
                            }

                        }
                        AddTestStep(element, elementXPosition, elementYPosition);
                    }
                }

                if (e.MouseButtonsPressed == MouseButtons.Right)
                {
                    //vertical Y , horizontal X
                    HtmlElement element = browser.Document.GetElementFromPoint(e.ClientMousePosition);
                    if (element != null)
                    {
                        var elementYPosition = ""; // position of element from top of page
                        var elementXPosition = ""; // position of element from top of page

                        if (RecordElementPosition == true)
                        {
                            //get position from top
                            if (e.ClientMousePosition.Y > (browser.Document.Window.Size.Height / 3) * 2)
                            {
                                //toward bottom
                                elementYPosition = "bottom";
                            }
                            else if (e.ClientMousePosition.Y < (browser.Document.Window.Size.Height / 3))
                            {
                                //toward top
                                elementYPosition = "top";
                            }
                            else
                            {
                                //near middle
                                elementYPosition = "";// "middle";
                            }

                            //get position from left
                            if (e.ClientMousePosition.X > (browser.Document.Window.Size.Width / 3) * 2)
                            {
                                //toward right
                                elementXPosition = "right";
                            }
                            else if (e.ClientMousePosition.X < (browser.Document.Window.Size.Width / 3))
                            {
                                //toward left
                                elementXPosition = "left";
                            }
                            else
                            {
                                //near middle
                                elementXPosition = "";// "middle";
                            }
                        }
                        AddExpectedResult(element, elementXPosition, elementYPosition);
                    }
                }
            }

        }

        void ScrollToRecordedStepsEnd()
        {
            // set the current caret position to the end
            stepsList.SelectionStart = stepsList.Text.Length;
            // scroll it automatically
            stepsList.ScrollToCaret();
        }

        void GenerateAndAddExpectedTextStep(String elementType, String innerText, String positionY, String positionX)
        {
            if (RecordElementPosition == true)
            {
                stepsList.AppendText(string.Format("{4}{Expected Result{4}{5} {0} toward {2} {3} of page contains text '{1}'{4}{4}", elementType, innerText, positionY, positionX, Environment.NewLine, user));
            }
            else
            {
                stepsList.AppendText(string.Format("{2}Expected Result{2}{3} {0} contains text '{1}'{2}{2}", elementType, innerText, Environment.NewLine, user));
            }
        }

        void AddExpectedResult(HtmlElement element, String elementXPosition, String elementYPosition)
        {
           // whether or not to output the actor in the test steps script
            outputUser = true;

            //ensure bullet list for new line is off for expected result
            stepsList.SelectionBullet = false;

            //check which browser window is recording the result
            WebBrowser browser = new WebBrowser();
            if (tabControl2.SelectedIndex == 0)
            {
                browser = webBrowser1;
            }
            else
            {
                browser = webBrowser2;
            }

            var elementType = "";
            switch (element.TagName.ToLower())
            {
                case "a":
                    elementType = "Link";
                    GenerateAndAddExpectedTextStep(elementType, element.InnerText, elementYPosition, elementXPosition);
                    break;
                case "h1":
                    elementType = "page heading";
                    GenerateAndAddExpectedTextStep(elementType, element.InnerText, elementYPosition, elementXPosition);
                    break;
                case "h2":
                    elementType = "page heading";
                    GenerateAndAddExpectedTextStep(elementType, element.InnerText, elementYPosition, elementXPosition);
                    break;
                case "h3":
                    elementType = "page heading";
                    GenerateAndAddExpectedTextStep(elementType, element.InnerText, elementYPosition, elementXPosition);
                    break;
                case "h4":
                    elementType = "page heading";
                    GenerateAndAddExpectedTextStep(elementType, element.InnerText, elementYPosition, elementXPosition);
                    break;
                case "h5":
                    elementType = "page heading";
                    GenerateAndAddExpectedTextStep(elementType, element.InnerText, elementYPosition, elementXPosition);
                    break;
                case "h6":
                    elementType = "page heading";
                    GenerateAndAddExpectedTextStep(elementType, element.InnerText, elementYPosition, elementXPosition);
                    break;
                case "p":
                    elementType = "page text";
                    GenerateAndAddExpectedTextStep(elementType, element.InnerText, elementYPosition, elementXPosition);
                    break;
                case "span":
                    elementType = "page";
                    GenerateAndAddExpectedTextStep(elementType, element.InnerText, elementYPosition, elementXPosition);
                    break;
                case "label":
                    elementType = "Label";
                    GenerateAndAddExpectedTextStep(elementType, element.InnerText, elementYPosition, elementXPosition);
                    break;
                case "div":
                    elementType = "page text";
                    GenerateAndAddExpectedTextStep(elementType, element.InnerText, elementYPosition, elementXPosition);
                    break;


                case "img":
                    elementType = "Image";
                    mshtml.IHTMLImgElement img = element.DomElement as mshtml.IHTMLImgElement;
                    GenerateAndAddExpectedTextStep(elementType, img.alt, elementYPosition, elementXPosition);
                    break;
                case "input":
                    //check if regular input or a button!
                    try
                    {
                        mshtml.IHTMLInputElement input = element.DomElement as mshtml.IHTMLInputElement;
                        if (input.type.ToLower() == "text")
                        {
                            elementType = "Input";

                            // Check if the selected element has an associated label we can use
                            var inputLabel = "";
                            var inputLabels = browser.Document.GetElementsByTagName("label");
                            foreach (HtmlElement lbl in inputLabels)
                            {
                                //check if element has an ID so we can obtain a label for it
                                if (element.Id != null)
                                {
                                    //check if the current label for attribute matches one for our element
                                    String forLabel = lbl.OuterHtml.Replace("\"", "");
                                    if (forLabel.Contains("for=" + element.Id))
                                    {
                                        inputLabel = lbl.OuterText;

                                    }
                                }
                            }

                            string inputValue = "";
                            if (input.value == null)
                            {
                                inputValue = input.name;
                            }
                            else
                            {
                                inputValue = input.value;
                            }

                            if (RecordElementPosition == true)
                            {

                                stepsList.AppendText(string.Format("{5}Expected Result{5}'{6} {0}' {1} toward {3} {4} of page contains text '{2}'{5}{5}", inputLabel, elementType, inputValue, elementYPosition, elementXPosition, Environment.NewLine, user));
                            }
                            else
                            {
                                stepsList.AppendText(string.Format("{3}Expected Result{3}{4} '{0}' {1} contains text '{2}'{3}{3}", inputLabel, elementType, inputValue, Environment.NewLine, user));

                            }
                        }

                        else
                        {
                            elementType = "Button";
                            mshtml.IHTMLInputButtonElement inputButton = element.DomElement as mshtml.IHTMLInputButtonElement;

                            //need to clean up - check if field is a password field!
                            if (input.type.ToLower() == "password")
                            {
                                elementType = "Input";
                            }



                            // Check if the selected element has an associated label we can use
                            var inputLabel = "";
                            var inputLabels = browser.Document.GetElementsByTagName("label");
                            foreach (HtmlElement lbl in inputLabels)
                            {
                                //check if element has an ID so we can obtain a label for it
                                if (element.Id != null)
                                {
                                    //check if the current label for attribute matches one for our element
                                    String forLabel = lbl.OuterHtml.Replace("\"", "");
                                    if (forLabel.Contains("for=" + element.Id))
                                    {
                                        inputLabel = lbl.OuterText;

                                    }
                                }
                            }

                            string inputValue = "";
                            if (input.value == null)
                            {
                                inputValue = input.name;
                            }
                            else
                            {
                                inputValue = input.value;
                            }
                            if (RecordElementPosition == true)
                            {
                                stepsList.AppendText(string.Format("{4}Expected Result{4}{5} {0} toward {2} {3} of page contains text '{1}'{4}{4}", elementType, inputValue, elementYPosition, elementXPosition, Environment.NewLine, user.ToUpper()));
                            }
                            else
                            {
                                //    stepsList.AppendText(string.Format("Expected Result{2}{3} {0} contains text '{1}'{2}{2}", elementType, inputValue, Environment.NewLine, user));
                                if (inputLabel == "")
                                {
                                    inputLabel = input.name;
                                }
                                stepsList.AppendText(string.Format("{3}Expected Result{3}{4} '{0}' {1} contains text '{2}'{3}{3}", inputLabel, elementType, inputValue, Environment.NewLine, user));
                            }
                        }
                    }
                    catch { }

                    elementType = "Input";
                    break;
                case "button":
                    elementType = "Button";
                    mshtml.IHTMLElement button = element.DomElement as mshtml.IHTMLElement;
                    if (RecordElementPosition == true)
                    {
                        stepsList.AppendText(string.Format("{4}Expected Result{4}{5} {0} toward {2} {3} of page contains text '{1}'{4}{4}", elementType, button.innerHTML, elementYPosition, elementXPosition, Environment.NewLine, user));
                    }
                    else
                    {
                        stepsList.AppendText(string.Format("{2}Expected Result{2}{3} {0} contains text '{1}'{2}{2}", elementType, button.innerHTML, Environment.NewLine, user));
                    }
                    break;
                case "select":
                    elementType = "Select list";
                    mshtml.IHTMLSelectElement select = element.DomElement as mshtml.IHTMLSelectElement;
                    mshtml.HTMLOptionElement option = (mshtml.HTMLOptionElement)select.item(select.selectedIndex, null);

                    // use select list associated label name
                    // Check if the selected element has an associated label we can use
                    var elementLabel = "";
                    var labels = browser.Document.GetElementsByTagName("label");
                    foreach (HtmlElement lbl in labels)
                    {
                        //check if element has an ID so we can obtain a label for it
                        if (element.Id != null)
                        {
                            //check if the current label for attribute matches one for our element
                            String forLabel = lbl.OuterHtml.Replace("\"", "");
                            if (forLabel.Contains("for=" + element.Id))
                            {
                                elementLabel = lbl.OuterText;
                            }
                        }
                    }

                    var selectValue = "";
                    if (select.selectedIndex != 0)
                    {
                        selectValue = option.text;
                    }
                    if (selectValue != "")
                    {
                        if (RecordElementPosition == true)
                        {
                            stepsList.AppendText(string.Format("{5}Expected Result{5}{6} '{0}' {1} toward {3} {4} of page contains option '{2}'{5}{5}", elementLabel, elementType, selectValue, elementYPosition, elementXPosition, Environment.NewLine, user));
                            stepsList.AppendText("\n");
                        }
                        else
                        {
                            stepsList.AppendText(string.Format("{3}Expected Result{3}{4} '{0}' {1} contains option '{2}'{3}{3}", elementLabel, elementType, selectValue, Environment.NewLine, user));
                            stepsList.AppendText("\n");
                        }
                    }
                    break;
            }
            ScrollToRecordedStepsEnd();
        }

        void AddTestStep(HtmlElement element, String elementXPosition, String elementYPosition)
        {


            stepsList.SelectionBullet = true;
            WebBrowser browser = new WebBrowser();
            if (tabControl2.SelectedIndex == 0)
            {
                browser = webBrowser1;
            }
            else
            {
                browser = webBrowser2;
            }

            //on, in, to
            String intent = "on";

            //value of input field
            var inputValue = "";

            // value of select list
            var selectValue = "";

            // Check what type of element has been selected
            var elementType = "";

            switch (element.TagName.ToLower())
            {
                case "input":
                    elementType = "input field";
                    intent = "in";
                    try
                    {
                        mshtml.IHTMLInputElement input = element.DomElement as mshtml.IHTMLInputElement;
                        inputValue = input.value;

                    }
                    catch { }

                    break;
                case "select":
                    elementType = "select list";
                    intent = "on";
                    try
                    {
                        mshtml.IHTMLSelectElement select = element.DomElement as mshtml.IHTMLSelectElement;
                        mshtml.HTMLOptionElement option = (mshtml.HTMLOptionElement)select.item(select.selectedIndex, null);
                        if (select.selectedIndex != 0)
                        {
                            selectValue = option.text;
                        }
                    }
                    catch { }
                    break;
                case "a":
                    elementType = "link";
                    intent = "on";
                    break;
                case "button":
                    intent = "on";
                    elementType = "Button";
                    break;

                case "img":
                    intent = "on";
                    elementType = "Slide";
                    break;
            }

            // Check if the selected element has an associated label we can use
            var elementLabel = "";
            var labels = browser.Document.GetElementsByTagName("label");
            foreach (HtmlElement lbl in labels)
            {
                //check if element has an ID so we can obtain a label for it
                if (element.Id != null)
                {
                    //check if the current label for attribute matches one for our element
                    //@'for=\"name"'
                    String forLabel = lbl.OuterHtml.Replace("\"", "");
                    if (forLabel.Contains("for=" + element.Id))
                    {
                        elementLabel = lbl.OuterText;

                    }
                }
            }

            //get image alt text
            // vScreen products use images for slides!!
            if (elementType == "Slide")
            {
                mshtml.IHTMLImgElement img = element.DomElement as mshtml.IHTMLImgElement;
                //GenerateAndAddExpectedTextStep(elementType, img.alt, elementYPosition, elementXPosition);


                //BUG: Currently outputs i.e. **USER** even if we're not recording a valid action
                if (outputUser == true)
                {
                    stepsList.SelectionBullet = false;
                    //record which user is recording an action
                    stepsList.AppendText("**" + user.ToUpper() + "**" + Environment.NewLine);
                    //we only want to record the user when recording a new list of steps before an expected result
                    outputUser = false;
                }
                stepsList.SelectionBullet = true;
                stepsList.AppendText(string.Format("Click {0} '{1}' {2}{4}", intent, img.alt, elementType, user.ToUpper(), Environment.NewLine));
            }

            // get text for link
            if (elementType == "link")
            {
                elementLabel = element.InnerText;
                intent = "on";
            }

            // check if we're dealing with a an input button/submit/reset
            if (elementType == "input field" && elementLabel == "")
            {
                try
                {
                    mshtml.IHTMLInputButtonElement button = element.DomElement as mshtml.IHTMLInputButtonElement;
                    elementType = "button";
                    intent = "on";
                    elementLabel = button.value;
                    if (elementLabel == null)
                    {
                        elementLabel = button.name;
                    }
                }
                catch (Exception ex)
                {
                    intent = "in";
                    elementType = "input";

                }
            }

            if (element.TagName == "BUTTON")
            {
                mshtml.IHTMLElement button = element.DomElement as mshtml.IHTMLElement;


                //BUG: Currently outputs i.e. **USER** even if we're not recording a valid action
                if (outputUser == true)
                {
                    stepsList.SelectionBullet = false;
                    //record which user is recording an action
                    stepsList.AppendText("**" + user.ToUpper() + "**" + Environment.NewLine);
                    //we only want to record the user when recording a new list of steps before an expected result
                    outputUser = false;
                }
                stepsList.SelectionBullet = true;
                stepsList.AppendText(string.Format("Click {0} '{1}' {2}{4}", intent, button.innerHTML, elementType, user.ToUpper(), Environment.NewLine));

            }

            //check type of input
            if (element.TagName == "SELECT")
            {
                if (!string.IsNullOrWhiteSpace(selectValue))
                {

                    //BUG: Currently outputs i.e. **USER** even if we're not recording a valid action
                    if (outputUser == true)
                    {
                        stepsList.SelectionBullet = false;
                        //record which user is recording an action
                        stepsList.AppendText("**" + user.ToUpper() + "**" + Environment.NewLine);
                        //we only want to record the user when recording a new list of steps before an expected result
                        outputUser = false;
                    }
                    stepsList.SelectionBullet = true;
                    stepsList.AppendText(string.Format("Select '{0}'{2}", selectValue, user.ToUpper(), Environment.NewLine));
                }
                else
                {

                    //BUG: Currently outputs i.e. **USER** even if we're not recording a valid action
                    if (outputUser == true)
                    {
                        stepsList.SelectionBullet = false;
                        //record which user is recording an action
                        stepsList.AppendText("**" + user.ToUpper() + "**" + Environment.NewLine);
                        //we only want to record the user when recording a new list of steps before an expected result
                        outputUser = false;
                    }
                    stepsList.SelectionBullet = true;
                    if (RecordElementPosition == true)
                    {
                        // we found the label associated to the selected input and it doesn't have a value input
                        stepsList.AppendText(string.Format("Click {0} '{1}' {2} toward {4} {5} of page{6}", intent, elementLabel, elementType, user.ToUpper(), elementYPosition, elementXPosition, Environment.NewLine));
                    }
                    else
                    {
                        // we found the label associated to the selected input and it doesn't have a value input
                        stepsList.AppendText(string.Format("Click {0} '{1}' {2}{4}", intent, elementLabel, elementType, user.ToUpper(), Environment.NewLine));
                    }
                }
            }
            else
            {
                //check if input has a value
                if (string.IsNullOrWhiteSpace(inputValue))
                {
                    if (!string.IsNullOrWhiteSpace(elementLabel) && !string.IsNullOrWhiteSpace(elementType))
                    {

                        //BUG: Currently outputs i.e. **USER** even if we're not recording a valid action
                        if (outputUser == true)
                        {
                            stepsList.SelectionBullet = false;
                            //record which user is recording an action
                            stepsList.AppendText("**" + user.ToUpper() + "**" + Environment.NewLine);
                            //we only want to record the user when recording a new list of steps before an expected result
                            outputUser = false;
                        }

                        stepsList.SelectionBullet = true;
                        if (RecordElementPosition == true)
                        {
                            // we found the label associated to the selected input and it doesn't have a value input
                            stepsList.AppendText(string.Format("Click {0} '{1}' {2} toward {4} {5} of page{6}", intent, elementLabel, elementType, user.ToUpper(), elementYPosition, elementXPosition, Environment.NewLine));
                            //stepsList.AppendText(string.Format("**{3}**{6}Click {0} '{1}' {2} toward {4} {5} of page{6}", intent, elementLabel, elementType, user.ToUpper(), elementYPosition, elementXPosition, Environment.NewLine));
                        }
                        else
                        {
                            // we found the label associated to the selected input and it doesn't have a value input
                            stepsList.AppendText(string.Format("Click {0} '{1}' {2}{4}", intent, elementLabel, elementType, user.ToUpper(), Environment.NewLine));
                            //stepsList.AppendText(string.Format("**{3}**{4}Click {0} '{1}' {2}{4}", intent, elementLabel, elementType, user.ToUpper(), Environment.NewLine));
                        }
                    }
                }
                else
                {
                    if (elementType == "button")
                    {

                        //BUG: Currently outputs i.e. **USER** even if we're not recording a valid action
                        if (outputUser == true)
                        {
                            stepsList.SelectionBullet = false;
                            //record which user is recording an action
                            stepsList.AppendText("**" + user.ToUpper() + "**" + Environment.NewLine);
                            //we only want to record the user when recording a new list of steps before an expected result
                            outputUser = false;
                        }
                        stepsList.SelectionBullet = true;
                        if (RecordElementPosition == true)
                        {
                            // we found the label associated to the selected input and it doesn't have a value input
                            stepsList.AppendText(string.Format("Click {0} '{1}' {2} toward {4} {5} of page{6}", intent, elementLabel, elementType, user.ToUpper(), elementYPosition, elementXPosition, Environment.NewLine));
                        }
                        else
                        {
                            // we found the label associated to the selected input and it doesn't have a value input
                            stepsList.AppendText(string.Format("Click {0} '{1}' {2}{4}", intent, elementLabel, elementType, user.ToUpper(), Environment.NewLine));
                        }
                    }
                    else
                    {

                        //BUG: Currently outputs i.e. **USER** even if we're not recording a valid action
                        if (outputUser == true)
                        {
                            stepsList.SelectionBullet = false;
                            //record which user is recording an action
                            stepsList.AppendText("**" + user.ToUpper() + "**" + Environment.NewLine);
                            //we only want to record the user when recording a new list of steps before an expected result
                            outputUser = false;
                        }
                        stepsList.SelectionBullet = true;
                        // we found the label associated to the selected input
                        stepsList.AppendText(string.Format("Input '{0}'{2}", inputValue, user.ToUpper(), Environment.NewLine));
                    }

                }
            }

            stepsList.SelectionBullet = false;
            ScrollToRecordedStepsEnd();
        }


        private void toolStripButton3_Click(object sender, EventArgs e)
        {
            if (tabControl2.SelectedIndex == 0)
            {
                //browser 1
                webBrowser1.Navigate(toolStripNavigateComboBox.Text);
                if (recordSteps == true)
                {
                    // record web page change
                    stepsList.SelectionBullet = false;
                    stepsList.AppendText("**" + user.ToUpper() + "**" + Environment.NewLine);
                    stepsList.SelectionBullet = true;
                    stepsList.AppendText("Navigate to " + toolStripNavigateComboBox.Text + Environment.NewLine);
                    stepsList.SelectionBullet = false;
                }
            }
            else
            {
                //browser 2
                webBrowser2.Navigate(toolStripNavigateComboBox.Text);
                if (recordSteps == true)
                {
                    // record web page change
                    stepsList.SelectionBullet = false;
                    stepsList.AppendText("**" + user.ToUpper() + "**" + Environment.NewLine);
                    stepsList.SelectionBullet = true;
                    stepsList.AppendText("Navigate to " + toolStripNavigateComboBox.Text + Environment.NewLine);
                    stepsList.SelectionBullet = false;
                }
            }
        }

        private void toolStripButton1_Click(object sender, EventArgs e)
        {
            stepsList.AppendText(string.Format("{1}{0}", Environment.NewLine, DateTime.Now.ToString("dd MMMM yyyy HH:mm")));
            stepsList.AppendText(string.Format("Browser: IE{0}{1}{1}", webBrowser1.Version.Major, Environment.NewLine));
            recordSteps = true;
            this.Text = "Steps Recorder [Recording]";
        }
        Boolean recordSteps = false;

        private void toolStripButton2_Click(object sender, EventArgs e)
        {
            //paused button pressed
            recordSteps = false;
            this.Text = "Steps Recorder [Paused]";
        }

        private void toolStripButton4_Click(object sender, EventArgs e)
        {
            //reset steps text
            stepsList.Text = "";
        }

        private void NavCombo_KeyDown(object sender, KeyEventArgs e)
        {
            try
            {
                if (e.KeyValue == 13)
                {
                    if (tabControl2.SelectedIndex == 0)
                    {
                        webBrowser1.Navigate(toolStripNavigateComboBox.Text);
                        if (recordSteps == true)
                        {
                            // record web page change
                            stepsList.SelectionBullet = false;
                            stepsList.AppendText("**" + user.ToUpper() + "**" + Environment.NewLine);
                            stepsList.SelectionBullet = true;
                            stepsList.AppendText("Navigate to " + toolStripNavigateComboBox.Text + Environment.NewLine);
                            stepsList.SelectionBullet = false;
                        }
                    }
                    else
                    {
                       webBrowser2.Navigate(toolStripNavigateComboBox.Text);
                        if (recordSteps == true)
                        {
                            // record web page change
                            stepsList.SelectionBullet = false;
                            stepsList.AppendText("**" + user.ToUpper() + "**" + Environment.NewLine);
                            stepsList.SelectionBullet = true;
                            stepsList.AppendText("Navigate to " + toolStripNavigateComboBox.Text + Environment.NewLine);
                            stepsList.SelectionBullet = false;
                        }
                    }
                }
            }
            catch { }
            
            ScrollToRecordedStepsEnd();
        }

        private static Bitmap bmpScreenshot;
        private static Graphics gfxScreenshot;
        void Document_DoubleClick(object sender, EventArgs e)
        {
            if (recordSteps == true)
            {
                WebBrowser browser = new WebBrowser();
                if (tabControl2.SelectedIndex == 0)
                {
                    browser = webBrowser1;
                }
                else
                {
                    browser = webBrowser2;
                }

                // Set the bitmap object to the size of the screen
                //bmpScreenshot = new Bitmap(Screen.PrimaryScreen.Bounds.Width, Screen.PrimaryScreen.Bounds.Height, PixelFormat.Format32bppArgb);
                bmpScreenshot = new Bitmap(browser.Bounds.Width, browser.Bounds.Height, PixelFormat.Format32bppArgb);
                // Create a graphics object from the bitmap
                gfxScreenshot = Graphics.FromImage(bmpScreenshot);

                Point browserLocation = browser.PointToScreen(Point.Empty);

                // Take the screenshot from the upper left corner to the right bottom corner
                //gfxScreenshot.CopyFromScreen(Screen.PrimaryScreen.Bounds.X, Screen.PrimaryScreen.Bounds.Y, 0, 0, Screen.PrimaryScreen.Bounds.Size, CopyPixelOperation.SourceCopy);
                gfxScreenshot.CopyFromScreen(browserLocation.X, browserLocation.Y + 15, 0, 0, browser.Bounds.Size, CopyPixelOperation.SourceCopy);
                // Save the screenshot to the specified path that the user has chosen

                // ATTEMPT RESIZE
                int x = bmpScreenshot.Width / 2;
                int y = bmpScreenshot.Height / 2;

                bmpScreenshot = resizeImage(bmpScreenshot, new Size(x, y));
                Clipboard.Clear();
                Clipboard.SetDataObject(bmpScreenshot);
                stepsList.AppendText("\n");
                stepsList.Paste();
                stepsList.AppendText("\n \n");

                bmpScreenshot.Dispose();

                ScrollToRecordedStepsEnd();
            }
        }

        public static Bitmap resizeImage(Image imgToResize, Size size)
        {
            return new Bitmap(imgToResize, size);
        }

        private void tabControl_Click(object sender, EventArgs e)
        {
            outputUser = true;

            // Get the 'actor' names from the tabs - if not set revert to defaults!
            if (tabControl2.SelectedIndex == 0)
            {
                user = tabControl2.TabPages[0].Text != "" ? tabControl2.TabPages[0].Text : "User 1";
            }
            else
            {
                user = tabControl2.TabPages[0].Text != "" ? tabControl2.TabPages[1].Text : "User 2";
            }
        }

        private void webBrowser1_DocumentCompleted(object sender, WebBrowserDocumentCompletedEventArgs e)
        {
            try
            {
                webBrowser1.Document.Body.AttachEventHandler("ondblclick", Document_DoubleClick);
                if (mouseDownAdded1 == false)
                {
                    webBrowser1.Document.MouseDown += new HtmlElementEventHandler(Document_MouseDown);
                    mouseDownAdded1 = true;
                }
            }
            catch { }

            ScrollToRecordedStepsEnd();
        }

        private void webBrowser2_DocumentCompleted(object sender, WebBrowserDocumentCompletedEventArgs e)
        {
            try
            {
                webBrowser2.Document.Body.AttachEventHandler("ondblclick", Document_DoubleClick);
                if (mouseDownAdded2 == false)
                {
                    webBrowser2.Document.MouseDown += new HtmlElementEventHandler(Document_MouseDown);
                    mouseDownAdded2 = true;
                }
            }
            catch { }

            ScrollToRecordedStepsEnd();
        }

        private void StepsRecorderLoad(object sender, EventArgs e)
        {
            LoadSettings();
        }

        private void label1_DoubleClick(object sender, EventArgs e)
        {
            AboutBox about = new AboutBox();
            about.ShowDialog();
        }
    }
}
