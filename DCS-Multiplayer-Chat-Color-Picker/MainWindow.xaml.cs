using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using LsonLib;
using Microsoft.Win32;

//https://github.com/rstarkov/LsonLib
//https://github.com/xceedsoftware/wpftoolkit/wiki/ColorPicker
//http://hex2rgba.devoth.com/

/*
Welcome to BAKA (Best Alternative Kolor App)! (Formerly named DCS Multiplayer Chat Color Picker.) 
This application will allow you to pick your favorite colors for the DCS chat in Multiplayer.
You will also be able to reset the colors.
You can also choose "designer" colors.
Desinger Color Schemes:
Colorblind
Rainbow
Monica Helms
You can also save presets.

Storyboard:
-User opens downloads the app
-User double clicks the .exe
-The app pops up
-User picks the reference file (likely dcs.exe)
-User presses ok
-App pre-loads the default colors into the boxes if there wasnt a save file located
-User selects their fave colors
-User clicks Save Colors
    -the colors are written to the backup file
    -the colors are modded into the MP chat file
    -There is some sort of visual confirmation
    -after saving, the preset list name will reset for visual confirmation
-User selects a preset from the dropdown and then pressed load Preset
    -the program will load those colors into the color boxes but will not save

==================================================================== 

Lua file struture for the save folder:
DcsMultiplayerChatColorPicker = {
	["userFileLocation"] = "G:|Games|DCS World OpenBeta|bin|DCS.exe",
	["Presets"] = {
		["presetName_1"] = {
			["color_1"] = "#FF1E88E5",
			["color_2"] = "#FFD81B60",
			["color_3"] = "#FFFFFFFF",
			["color_4"] = "#FFFFC107",
			["color_5"] = "#FF004D40",
		},
		["presetName_2"] = {
			["color_1"] = "#FFFF8C00",
			["color_2"] = "#FFFFEF00",
			["color_3"] = "#FF00811F",
			["color_4"] = "#FF0044FF",
			["color_5"] = "#FF760089",
		},
		["presetName_3"] = {
			["color_1"] = "#FF55CDFC",
			["color_2"] = "#FFF7A8B8",
			["color_3"] = "#FFFEFEFE",
			["color_4"] = "#FFF7A8B8",
			["color_5"] = "#FF55CDFC",
		},
	},
}

=========================================================

Color Sets:
Default:
eBlueText - 0x0808dbff
eRedTest - 0xfd5151ff
eWhiteText - 0xffffffff
eOrangeText - 0xff8800ff
eYellowText - 0xfbb941ff
 */

namespace DCS_Multiplayer_Chat_Color_Picker
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        string appPath = System.AppDomain.CurrentDomain.BaseDirectory;//gets the path of were the utility is running
        string settingsPath;
        string appName = "BestAlternativeKolorApp";
        public MainWindow()
        {
            InitializeComponent();
            //settingsPath = appPath + @"/DCS-Multilayer-Chat-Color-Picker-Settings/Settings.txt";//where the settings will be saved
            settingsPath = appPath + appName + @"/Settings.txt";//where the settigns will be saved
            LoadDefaultColors();//load the default colors at first so blank string errors can be avoided
            DisableAllButtons();
            CheckForSaveFile();
        }

        
        private void CheckForSaveFile()
        {
           if (File.Exists(settingsPath))//if the safe file exists, load it
            {
                var savedSettings = LsonVars.Parse(File.ReadAllText(settingsPath));//put the contents of the settings file into a lua read

                selected_selectDcsExe_string = savedSettings[appName]["userFileLocation"].GetString();

                if (selected_selectDcsExe_string.Length > 2)//i just chose two just because
                {
                    selected_selectDcsExe_string = selected_selectDcsExe_string.Replace('|', '\\');
                    Console.WriteLine("DEBUG: The Saved Settings userFileLocation is: " + selected_selectDcsExe_string);
                    textBlock_selectDcsExe.Text = selected_selectDcsExe_string;
                    isDcsExeSelected = true;
                    GeneratePathsFromDcsExePath();
                    textBlock_selectDcsExe.BorderBrush = Brushes.LightGreen;//visual feedback
                    ReadTheLua();
                    ApplyLuaColors();
                    EnableAllButtons();
                }
            }
            else//if the save file does not exist, make one
            {
                Console.WriteLine("DEBUG: Did not find the Setting file");

                Directory.CreateDirectory(appPath + appName);//creates the save folder
                                                                                                   //https://docs.microsoft.com/en-us/dotnet/api/system.io.streamwriter?redirectedfrom=MSDN&view=netcore-3.1
                                                                                                   
                //write the following in the text file
                string[] defaultExportString = {
                appName + " = ",
                //"DcsMultiplayerChatColorPicker = ",
                "{",
                "   [\"userFileLocation\"] = \"\",",
                "   [\"Presets\"] = ",
                "   {",
                "       [\"presetName_1\"] = ",
                "       {",
                "           [\"color_1\"] = \"#FF1E88E5\",",
                "           [\"color_2\"] = \"#FFD81B60\",",
                "           [\"color_3\"] = \"#FFFFFFFF\",",
                "           [\"color_4\"] = \"#FFFFC107\",",
                "           [\"color_5\"] = \"#FF004D40\",",
                "       },",
                "       [\"presetName_2\"] =",
                "       {",
                "           [\"color_1\"] = \"#FFFF8C00\",",
                "           [\"color_2\"] = \"#FFFFEF00\",",
                "           [\"color_3\"] = \"#FF00811F\",",
                "           [\"color_4\"] = \"#FF0044FF\",",
                "           [\"color_5\"] = \"#FF760089\",",
                "       },",
                "       [\"presetName_3\"] =",
                "       {",
                "           [\"color_1\"] = \"#FF55CDFC\",",
                "           [\"color_2\"] = \"#FFF7A8B8\",",
                "           [\"color_3\"] = \"#FFFEFEFE\",",
                "           [\"color_4\"] = \"#FFF7A8B8\",",
                "           [\"color_5\"] = \"#FF55CDFC\",",
                "       },",
                "   },",
                "}",
            };
                    System.IO.File.WriteAllLines(settingsPath, defaultExportString);
            }

           //load presets
           dropDownButton_loadPreset.ItemsSource = new List<string> {"Current", "Default","Option 1", "Option 2", "Option 3" };
            dropDownButton_loadPreset.SelectedIndex = 0;
        }

        private void DisableAllButtons()
        {
            //button_loadPreset.IsEnabled = false;
            button_saveColors.IsEnabled = false;
            dropDownButton_loadPreset.IsEnabled = false;
            //textBlock_savePreset.IsEnabled = false;

            colorPicker_blueCoalition.IsEnabled = false;
            colorPicker_redCoalition.IsEnabled = false;
            colorPicker_spectators.IsEnabled = false;
            colorPicker_serverMessages.IsEnabled = false;
            colorPicker_selfSay.IsEnabled = false;
        }
        private void EnableAllButtons()
        {
            //button_loadPreset.IsEnabled = true;
            button_saveColors.IsEnabled = true;
            dropDownButton_loadPreset.IsEnabled = true;
            //textBlock_savePreset.IsEnabled = true;

            colorPicker_blueCoalition.IsEnabled = true;
            colorPicker_redCoalition.IsEnabled = true;
            colorPicker_spectators.IsEnabled = true;
            colorPicker_serverMessages.IsEnabled = true;
            colorPicker_selfSay.IsEnabled = true;
        }

        private void LoadDefaultColors()
        {
            //this was written before i knew how to do the hex conversions
            colorPicker_blueCoalition.SelectedColor = Color.FromArgb(255, 8, 8, 219);//blue
            colorPicker_redCoalition.SelectedColor = Color.FromArgb(255, 253, 81, 81);//red
            colorPicker_spectators.SelectedColor = Color.FromArgb(255, 255, 255, 255);//white
            colorPicker_serverMessages.SelectedColor = Color.FromArgb(255, 255, 136, 0);//orange
            colorPicker_selfSay.SelectedColor = Color.FromArgb(255, 251, 185, 65);//yellow
        }

        string selected_selectDcsExe_string;
        private void Button_selectDcsExe_Click(object sender, RoutedEventArgs e)
        {
            //this is where the user will select some specific file
            //after the file is selected, this app will then find the correct file for modification
            //the name of that file is \DCS World OpenBeta\MissionEditor\modules\dialogs\mul_chat.dlg

            //Have the select dialog pop up
            OpenFileDialog openFileDialog_selectDcsExe = new OpenFileDialog();
            openFileDialog_selectDcsExe.InitialDirectory = "C:\\Program Files\\Eagle Dynamics\\DCS World\\bin\\";//likely not necessary, but it may help
            openFileDialog_selectDcsExe.Filter = "Application files (*.exe)|*.exe";//pick an exe only
            //openFileDialog_selectDcsExe.RestoreDirectory = true;//sure, but not necessary
            openFileDialog_selectDcsExe.Title = "Select DCS.exe (Hint: C:\\Install Location\\bin\\DCS.exe";//hints for all kinds of installs
            //the user picks their dcs-updater.exe
            if (openFileDialog_selectDcsExe.ShowDialog() == true)
            {
                var selected_selectDcsExe = openFileDialog_selectDcsExe.FileName;
                selected_selectDcsExe_string = selected_selectDcsExe.ToString();
                //see the "options.lua" check for info on how this works
                if (selected_selectDcsExe.IndexOf("DCS.exe", 0, StringComparison.CurrentCultureIgnoreCase) != -1)//check to make sure that the file they pick is the correct one
                {
                    //the user selected the correct correct file
                    //if the file is the correct one, try to make all of the other file paths that are related
                    //to that part of the folder system
                    GeneratePathsFromDcsExePath();
                    textBlock_selectDcsExe.Text = selected_selectDcsExe;
                    
                    textBlock_selectDcsExe.BorderBrush = Brushes.LightGreen;//visual feedback
                    //save the location of the exe to the settings file here
                    var savedSettings = LsonVars.Parse(File.ReadAllText(settingsPath));//put the contents of mul_char.dlg into a lua read

                    string tempString = selected_selectDcsExe_string.Replace('\\', '|');//this prevents a lua read error 

                    savedSettings[appName]["userFileLocation"] = tempString;//save the user location
                    File.WriteAllText(settingsPath, LsonVars.ToString(savedSettings)); // serialize back to a file
                }
                else
                {
                    //the user did not select the correct file
                    MessageBox.Show("You picked: " + selected_selectDcsExe + ". This is not the correct file. Please try again.");
                    //textBlock_selectDcsExe.Text = null;
                    
                }
            }
        }

        string dcsInstallDirectory;
        string dcsMulChatDirectoryAndName_string;
        bool isDcsExeSelected;

        private void GeneratePathsFromDcsExePath()
        {
            //generate paths here
            //the name of that file is \DCS\MissionEditor\modules\dialogs\mul_chat.dlg
            //init the strings first
            dcsInstallDirectory = Path.GetFullPath(Path.Combine(selected_selectDcsExe_string, @"..\..\"));
            dcsMulChatDirectoryAndName_string = Path.Combine(dcsInstallDirectory, @"MissionEditor\modules\dialogs\mul_chat.dlg");
            //MessageBox.Show(dcsInstallDirectory);//results in something like "C:/ProgramFiles/DCS"

            //check to make sure that the file exists
            if (File.Exists(dcsMulChatDirectoryAndName_string))
            {
                textBlock_selectDcsExe.Text = selected_selectDcsExe_string;
                //MessageBox.Show("Your DCS.exe Version: " + myFileVersionInfo.FileVersion);//debugging
                isDcsExeSelected = true;
                ReadTheLua();
                ApplyLuaColors();
                EnableAllButtons();
            }
            else
            {
                MessageBox.Show("Based on your DCS.exe location, the necssary file needed for this app could not be found." +
                    " Please ensure that '\\DCS\\MissionEditor\\modules\\dialogs\\mul_chat.dlg' exists and try again.");
                isDcsExeSelected = false;
            }


        }

        private void ApplyLuaColors()
        {
            //MessageBox.Show("The colors form the dlg file are: "
            //   + "\r\n" + "Blue: " + eBlueText_ValueFromDlg
            //   + "\r\n" + "Red: " + eRedText_ValueFromDlg
            //   + "\r\n" + "Yellow: " + eYellowText_ValueFromDlg
            //   + "\r\n" + "White: " + eWhiteText_ValueFromDlg
            //   + "\r\n" + "Orange: " + eOrangeText_ValueFromDlg
            //   );
            //https://stackoverflow.com/questions/2109756/how-do-i-get-the-color-from-a-hexadecimal-color-code-using-net
            //this assigns the blue coalition color as the converted color of the string that
            //resulted in the conversion of the dcs version of hex (0x123456) to the colorpicker of hex (#345612)
            colorPicker_blueCoalition.SelectedColor =
                (Color)ColorConverter.ConvertFromString(Convert_DcsHex_to_ColorpickerHex(eBlueText_ValueFromDlg));
            colorPicker_redCoalition.SelectedColor =
                (Color)ColorConverter.ConvertFromString(Convert_DcsHex_to_ColorpickerHex(eRedText_ValueFromDlg));
            colorPicker_selfSay.SelectedColor =
                (Color)ColorConverter.ConvertFromString(Convert_DcsHex_to_ColorpickerHex(eYellowText_ValueFromDlg));
            colorPicker_spectators.SelectedColor =
                (Color)ColorConverter.ConvertFromString(Convert_DcsHex_to_ColorpickerHex(eWhiteText_ValueFromDlg));
            colorPicker_serverMessages.SelectedColor =
                (Color)ColorConverter.ConvertFromString(Convert_DcsHex_to_ColorpickerHex(eOrangeText_ValueFromDlg));

        }

        string eBlueText_ValueFromDlg;
        string eRedText_ValueFromDlg;
        string eYellowText_ValueFromDlg;
        string eWhiteText_ValueFromDlg;
        string eOrangeText_ValueFromDlg;


        private void ReadTheLua()
        {
            var mulChatLuaText = LsonVars.Parse(File.ReadAllText(dcsMulChatDirectoryAndName_string));//put the contents of mul_char.dlg into a lua read
            //put the five colors into 5 different variables
            //the following should result in "0x0808dbff" for the default file
            eBlueText_ValueFromDlg = mulChatLuaText["dialog"]["children"]["pNoVisible"]["children"]["eBlueText"]
                ["skin"]["states"]["released"][2]["text"]["color"].GetString();
            //MessageBox.Show("The color form the dlg file is: " + eBlueText_ValueFromDlg);//result in "0x0808dbff"
            eRedText_ValueFromDlg = mulChatLuaText["dialog"]["children"]["pNoVisible"]["children"]["eRedText"]
                ["skin"]["states"]["released"][2]["text"]["color"].GetString();
            eYellowText_ValueFromDlg = mulChatLuaText["dialog"]["children"]["pNoVisible"]["children"]["eYellowText"]
                ["skin"]["states"]["released"][2]["text"]["color"].GetString();
            eWhiteText_ValueFromDlg = mulChatLuaText["dialog"]["children"]["pNoVisible"]["children"]["eWhiteText"]
                ["skin"]["states"]["released"][2]["text"]["color"].GetString();
            eOrangeText_ValueFromDlg = mulChatLuaText["dialog"]["children"]["pNoVisible"]["children"]["eOrangeText"]
                ["skin"]["states"]["released"][2]["text"]["color"].GetString();

        }

        private void WriteTheLua()
        {
            var mulChatLuaText = LsonVars.Parse(File.ReadAllText(dcsMulChatDirectoryAndName_string));//put the contents of mul_char.dlg into a lua read
           
            mulChatLuaText["dialog"]["children"]["pNoVisible"]["children"]
                ["eBlueText"]["skin"]["states"]["released"][2]["text"]
                ["color"] = Convert_ColorpickerHex_to_DcsHex(colorPicker_blueCoalition.SelectedColor.ToString());

            mulChatLuaText["dialog"]["children"]["pNoVisible"]["children"]
                ["eRedText"]["skin"]["states"]["released"][2]["text"]
                ["color"] = Convert_ColorpickerHex_to_DcsHex(colorPicker_redCoalition.SelectedColor.ToString());

            mulChatLuaText["dialog"]["children"]["pNoVisible"]["children"]
               ["eYellowText"]["skin"]["states"]["released"][2]["text"]
               ["color"] = Convert_ColorpickerHex_to_DcsHex(colorPicker_selfSay.SelectedColor.ToString());

            mulChatLuaText["dialog"]["children"]["pNoVisible"]["children"]
              ["eWhiteText"]["skin"]["states"]["released"][2]["text"]
              ["color"] = Convert_ColorpickerHex_to_DcsHex(colorPicker_spectators.SelectedColor.ToString());

            mulChatLuaText["dialog"]["children"]["pNoVisible"]["children"]
              ["eOrangeText"]["skin"]["states"]["released"][2]["text"]
              ["color"] = Convert_ColorpickerHex_to_DcsHex(colorPicker_serverMessages.SelectedColor.ToString());


            File.WriteAllText(dcsMulChatDirectoryAndName_string, LsonVars.ToString(mulChatLuaText)); // serialize back to a file

        }

        private void Button_selectDcsExe_rightUp(object sender, MouseButtonEventArgs e)
        {
            //string testString = colorPicker_blueCoalition.SelectedColor.ToString();
            //MessageBox.Show(testString);//results in [#][Alpha][red][green][blue]
            textBlock_selectDcsExe.Text = "";
        }

        private void ColorPicker_blueCoalition_selectedColorChanged(object sender, RoutedPropertyChangedEventArgs<Color?> e)
        {
            //blue coalition messages are seen in this color
            //string testString = colorPicker_blueCoalition.SelectedColor.ToString();
            //MessageBox.Show(testString);//results in [#][Alpha][red][green][blue]
        }

        private void ColorPicker_redCoalition_selectedColorChanged(object sender, RoutedPropertyChangedEventArgs<Color?> e)
        {
            //red coalition messages are seen in this color
        }

        private void ColorPicker_spectators_selectedColorChanged(object sender, RoutedPropertyChangedEventArgs<Color?> e)
        {
            //spectator messages are this color
        }

        private void ColorPicker_serverMessages_selectedColorChanged(object sender, RoutedPropertyChangedEventArgs<Color?> e)
        {
            //server messages are this color
        }

        private void ColorPicker_selfSay_selectedColorChanged(object sender, RoutedPropertyChangedEventArgs<Color?> e)
        {
            //this color is for messages coming from the player
        }

        private void Button_defaultColors_Click(object sender, RoutedEventArgs e)
        {
            //clicking this button will make the colors all default. You will still need to save.
            
        }

        string color_blueColation;
        string color_redColation;
        string color_spectators;
        string color_serverMessages;
        string color_selfSay;

        private void Button_saveColors_Click(object sender, RoutedEventArgs e)
        {
            Action_saveColors_Button();
            //MessageBox.Show("Saved");
            dropDownButton_loadPreset.SelectedItem = "Current";
        }

        private void Action_saveColors_Button()
        {
            //this is where the colors are saved to a backup file and exoprted to the DCS file

            //put the names of the colors in a string.
            //this will result in [#][Alpha][red][green][blue]
          
            color_blueColation = colorPicker_blueCoalition.SelectedColor.ToString();
            color_redColation = colorPicker_redCoalition.SelectedColor.ToString();
            color_spectators = colorPicker_spectators.SelectedColor.ToString();
            color_serverMessages = colorPicker_serverMessages.SelectedColor.ToString();
            color_selfSay = colorPicker_selfSay.SelectedColor.ToString();

            SaveAllOfTheData();
            WriteTheLua();
        }

        private string Convert_ColorpickerHex_to_DcsHex(string colorString)
        {
            //input will be    [#][Alpha][red][green][blue] , example #FF34eb77
            //output should be [0x][red][green][blue][Alpha], example 0x34eb77ff
            string alphaPart = colorString.Substring(1,2);
            string redGreenBluePart = colorString.Substring(3, 6);
            colorString = ("0x" + redGreenBluePart + alphaPart);
            return colorString;
        }

        private string Convert_DcsHex_to_ColorpickerHex(string colorString)
        {
            //input will be [0x][red][green][blue][Alpha], example 0x34eb77ff
            //output should be    [#][Alpha][red][green][blue] , example #FF34eb77

            string alphaPart = colorString.Substring(8, 2);
            string redGreenBluePart = colorString.Substring(2, 6);
            colorString = ("#" + alphaPart + redGreenBluePart);
            return colorString;
        }

        string[] defaultExportString;

        private void SaveAllOfTheData()
        {
            
            //this will be called when all the data needs to be saved. 
            //Most likely only when the user clicks the save button
            //nevermind. we are going to have 5 presets. 
            //string[] contents = { "hi" };
            //string FileLocation = @"G:\Games\DCS World OpenBeta\MissionEditor\modules\dialogs\testArea\masterSave.txt";
            //System.IO.File.WriteAllLines(FileLocation, contents);
            //var masterSaveFile = LsonVars.Parse(File.ReadAllText(FileLocation));

            //masterSaveFile["dialog"]["children"]["pNoVisible"]["children"]
            // ["eOrangeText"]["skin"]["states"]["released"][2]["text"]
            // ["color"] = Convert_ColorpickerHex_to_DcsHex(colorPicker_serverMessages.SelectedColor.ToString());
            var savedSettings = LsonVars.Parse(File.ReadAllText(settingsPath));//put the contents of mul_char.dlg into a lua read

            string tempString = selected_selectDcsExe_string.Replace('\\', '|');//this prevents a lua read error 

            savedSettings[appName]["userFileLocation"] = tempString;//save the user location
            //case for which preset was enabled and which to overrite, if any

            switch (dropDownButton_loadPreset.SelectedItem.ToString())
            {
                case "Option 1"://if Option 1 was in the box when the save button was pressed
                    savedSettings[appName]["Presets"]["presetName_1"]["color_1"] 
                        = (colorPicker_blueCoalition.SelectedColor.ToString());

                    savedSettings[appName]["Presets"]["presetName_1"]["color_2"] 
                        = (colorPicker_redCoalition.SelectedColor.ToString());

                    savedSettings[appName]["Presets"]["presetName_1"]["color_3"] 
                        = (colorPicker_spectators.SelectedColor.ToString());

                    savedSettings[appName]["Presets"]["presetName_1"]["color_4"] 
                        = (colorPicker_selfSay.SelectedColor.ToString());

                    savedSettings[appName]["Presets"]["presetName_1"]["color_5"] 
                        = (colorPicker_serverMessages.SelectedColor.ToString());

                    break;

                case "Option 2":
                    savedSettings[appName]["Presets"]["presetName_2"]["color_1"]
                        = (colorPicker_blueCoalition.SelectedColor.ToString());

                    savedSettings[appName]["Presets"]["presetName_2"]["color_2"]
                        = (colorPicker_redCoalition.SelectedColor.ToString());

                    savedSettings[appName]["Presets"]["presetName_2"]["color_3"]
                        = (colorPicker_spectators.SelectedColor.ToString());

                    savedSettings[appName]["Presets"]["presetName_2"]["color_4"]
                        = (colorPicker_selfSay.SelectedColor.ToString());

                    savedSettings[appName]["Presets"]["presetName_2"]["color_5"]
                        = (colorPicker_serverMessages.SelectedColor.ToString());

                    break;

                case "Option 3":
                    savedSettings[appName]["Presets"]["presetName_3"]["color_1"]
                        = (colorPicker_blueCoalition.SelectedColor.ToString());

                    savedSettings[appName]["Presets"]["presetName_3"]["color_2"]
                        = (colorPicker_redCoalition.SelectedColor.ToString());

                    savedSettings[appName]["Presets"]["presetName_3"]["color_3"]
                        = (colorPicker_spectators.SelectedColor.ToString());

                    savedSettings[appName]["Presets"]["presetName_3"]["color_4"]
                        = (colorPicker_selfSay.SelectedColor.ToString());

                    savedSettings[appName]["Presets"]["presetName_3"]["color_5"]
                        = (colorPicker_serverMessages.SelectedColor.ToString());
                    break;

                    //the rest shouldnt do anything to the backup file
                case "Default":
                    break;
                case "Presets":
                    break;
            }

            File.WriteAllText(settingsPath, LsonVars.ToString(savedSettings)); // serialize back to a file

        }

        private void Button_savePreset_Click(object sender, RoutedEventArgs e)
        {
            //this may be able to be rolled into "save". Just have a condition that if the name
            //box has anything in it, then the preset will be both saved and applied. The
            //name wiill be put into the database and the name box will be cleared
        }

        private void Button_loadPreset_Click(object sender, RoutedEventArgs e)
        {

        }

        private void dropDownButton_loadPreset_selectionChanged(object sender, SelectionChangedEventArgs e)
        {
            switch(dropDownButton_loadPreset.SelectedItem.ToString())
            {
                case "Option 1":
                    LoadPreset1Colors();
                    break;
                case "Option 2":
                    LoadPreset2Colors();
                    break;
                case "Option 3":
                    LoadPreset3Colors();
                    break;
                case "Default":
                    LoadDefaultColors();
                    break;

                case "Current":
                    LoadCurrentColors();
                    break;
            }
        }

        private void LoadCurrentColors()
        {
            Console.WriteLine(isDcsExeSelected.ToString());
            if (isDcsExeSelected == true)
            {
            ReadTheLua();
            ApplyLuaColors();
            }
        }

        private void LoadPreset3Colors()
        {
            var savedSettings = LsonVars.Parse(File.ReadAllText(settingsPath));//put the contents of the settings file into a lua read

            //the selected color will equal the string that represents the lua section
            //colorPicker_blueCoalition.SelectedColor =
            //     (Color)ColorConverter.ConvertFromString("#FF0808DB");
            colorPicker_blueCoalition.SelectedColor =
                 (Color)ColorConverter.ConvertFromString(savedSettings[appName]["Presets"]["presetName_3"]["color_1"].GetString());
            colorPicker_redCoalition.SelectedColor =
                 (Color)ColorConverter.ConvertFromString(savedSettings[appName]["Presets"]["presetName_3"]["color_2"].GetString());
            colorPicker_spectators.SelectedColor =
                 (Color)ColorConverter.ConvertFromString(savedSettings[appName]["Presets"]["presetName_3"]["color_3"].GetString());
            colorPicker_selfSay.SelectedColor =
                 (Color)ColorConverter.ConvertFromString(savedSettings[appName]["Presets"]["presetName_3"]["color_4"].GetString());
            colorPicker_serverMessages.SelectedColor =
                 (Color)ColorConverter.ConvertFromString(savedSettings[appName]["Presets"]["presetName_3"]["color_5"].GetString());
            //Console.WriteLine(savedSettings[appName]["Presets"]["presetName_1"]["color_1"].ToString());

        }

        private void LoadPreset2Colors()
        {
            var savedSettings = LsonVars.Parse(File.ReadAllText(settingsPath));//put the contents of the settings file into a lua read

            //the selected color will equal the string that represents the lua section
            //colorPicker_blueCoalition.SelectedColor =
            //     (Color)ColorConverter.ConvertFromString("#FF0808DB");
            colorPicker_blueCoalition.SelectedColor =
                 (Color)ColorConverter.ConvertFromString(savedSettings[appName]["Presets"]["presetName_2"]["color_1"].GetString());
            colorPicker_redCoalition.SelectedColor =
                 (Color)ColorConverter.ConvertFromString(savedSettings[appName]["Presets"]["presetName_2"]["color_2"].GetString());
            colorPicker_spectators.SelectedColor =
                 (Color)ColorConverter.ConvertFromString(savedSettings[appName]["Presets"]["presetName_2"]["color_3"].GetString());
            colorPicker_selfSay.SelectedColor =
                 (Color)ColorConverter.ConvertFromString(savedSettings[appName]["Presets"]["presetName_2"]["color_4"].GetString());
            colorPicker_serverMessages.SelectedColor =
                 (Color)ColorConverter.ConvertFromString(savedSettings[appName]["Presets"]["presetName_2"]["color_5"].GetString());
            //Console.WriteLine(savedSettings[appName]["Presets"]["presetName_1"]["color_1"].ToString());

        }

        private void LoadPreset1Colors()
        {

            var savedSettings = LsonVars.Parse(File.ReadAllText(settingsPath));//put the contents of the settings file into a lua read

            //the selected color will equal the string that represents the lua section
            //colorPicker_blueCoalition.SelectedColor =
            //     (Color)ColorConverter.ConvertFromString("#FF0808DB");
            colorPicker_blueCoalition.SelectedColor =
                 (Color)ColorConverter.ConvertFromString(savedSettings[appName]["Presets"]["presetName_1"]["color_1"].GetString());
            colorPicker_redCoalition.SelectedColor =
                 (Color)ColorConverter.ConvertFromString(savedSettings[appName]["Presets"]["presetName_1"]["color_2"].GetString());
            colorPicker_spectators.SelectedColor =
                 (Color)ColorConverter.ConvertFromString(savedSettings[appName]["Presets"]["presetName_1"]["color_3"].GetString());
            colorPicker_selfSay.SelectedColor =
                 (Color)ColorConverter.ConvertFromString(savedSettings[appName]["Presets"]["presetName_1"]["color_4"].GetString());
            colorPicker_serverMessages.SelectedColor =
                 (Color)ColorConverter.ConvertFromString(savedSettings[appName]["Presets"]["presetName_1"]["color_5"].GetString());
            //Console.WriteLine(savedSettings[appName]["Presets"]["presetName_1"]["color_1"].ToString());


        }

        private void titleBar_leftButtonDown(object sender, MouseButtonEventArgs e)
        {
            //this moves the window when the titlebar is clicked and held down
            //I made the custom title bar
            if (e.ChangedButton == MouseButton.Left)
                DragMove();
        }



        private void button_close_click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
