﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Controls;
using BSky.Controls;
using BSky.Interfaces.Model;
using BSky.Statistics.Common;
using System.Xml;
using System.Windows;
using BSky.Interfaces.Controls;
using System.Text.RegularExpressions;
using BSky.Controls.XmlDecoder.Model;
using BSky.Controls.Dialogs;
using System.Windows.Input;

namespace BSky.XmlDecoder
{
    /// <summary>
    /// Helper file to evaluate variable names to strings.
    /// </summary>
    public static class OutputHelper
    {
        private static Dictionary<string, object> GlobalList = new Dictionary<string, object>();
        public static int TotalOutputTables { get; set; }

        public static void AddGlobalObject(string macro, object obj)
        {
            if (GlobalList.ContainsKey(macro))
            {
                GlobalList.Remove(macro);
            }
            GlobalList[macro] = obj;
        }

        public static void DeleteGlobalObject(string macro)
        {
            if (GlobalList.ContainsKey(macro))
                GlobalList.Remove(macro);
        }

        public static object GetGlobalMacro(string Macro, string variablename)
        {
            if (!GlobalList.ContainsKey(Macro))
            {
                return null;
            }
            if (string.IsNullOrEmpty(variablename))
            {
                return GlobalList[Macro];
            }
            return EvaluateValue(GlobalList[Macro] as DependencyObject, variablename);
        }

        private static Dictionary<string, string> MacroList;

        public static string ExpandMacro(string Macro)
        {
            if (Macro == null)
                return "";
            if (MacroList.ContainsKey(Macro))
                return MacroList[Macro];
            else
                return Macro;
        }

        public static void UpdateMacro(string Macro, string value)
        {
            MacroList[Macro] = value;
        }

        public static void DeleteMacro(string Macro)
        {
            if (MacroList.ContainsKey(Macro))
                MacroList.Remove(Macro);
        }

        public static string ExpandParam(string datasource, string delimiter)
        {
            string output = string.Empty;

            List<string> lst = OutputHelper.GetList(datasource, string.Empty, false);

            if (lst == null || lst.Count == 0)
            {
                lst = OutputHelper.GetFactors(OutputHelper.ExpandMacro(datasource));
            }
            if (lst != null)
            {
                foreach (string str in lst)
                {
                    if (!string.IsNullOrEmpty(output))
                        output += delimiter + str;
                    else
                        output += str;
                }
            }
            return output;
        }

        //Added by Aaron 12/11/2013
        //This function is called for every 
        //Added by Aaron 12/11/2013
        //This function is called for every 
        public static string EvaluateValue(DependencyObject obj, string objname) // Group Variable shoul also be added to this function
        {
            //13Sep2013 col name and col object /// Starts
            int paramtype = 0; // 0 -> default, 1-> col name, 2-> col type
            bool datasetnameprefix = false;

            // Modified by Aaron 12/29/2015
            //This to support the use of one control in multiple lines of syntax
            //just like how the target control when used in multi-way anova
            //needs to return var1*var2 with * being the seperator and var1,var2 with , being the seperator //for Anova and plotMeans respectively
            bool forceUseCommas = false;

            //Aaron Commented code below on 10/01/2013
            //Reason is how variables are subsituted is controlled through the subsititue settings dialog
            //  if (objname.IndexOf("#") == 0 && objname.LastIndexOf("#") == (objname.Length - 1)) //its #target#. Col name
            // {
            //     paramtype = 1;
            // }
            // else if (objname.IndexOf("$") == 0 && objname.LastIndexOf("$") == (objname.Length - 1))//its $target$. Col obj
            // {
            //     paramtype = 2;
            // }

            string currentDataset = ExpandMacro("%DATASET%");
            string activemodel = ExpandMacro("%MODEL%");
            //objname = objname.Replace('$', ' ').Replace('#', ' ').Trim();

            //Added by Aaron 12/11/2013
            //In the name of a control, I look for $
            //If found, I ignore the setting of how the value in the control should be constructed and append a dataset name
            //and a $ sign to the variable name
            //This works only for BSKyVariable and BSkyGrouping Variable

            bool boolTilda = false;
            bool overrideSep = false;
            string oriSepCharacter = "";
            string newSepCharacter = "";
            bool prefixEachVariable = false;
            string newPrefixCharacter = "";

            if (objname.Contains('@'))
            {
                //I need objname to get the control from the canvas. Hence I need to remove the $
                objname = objname.Replace('@', ' ').Replace('#', ' ').Trim();
                boolTilda = true;
            }

            if (objname.Contains('&'))
            {
                // oriSubstituteSettings = list.SubstituteSettings;
                // oriSepCharacter = list.SepCharacter;
                int indexofAmbersand = objname.IndexOf("&");
                //  list.SubstituteSettings = list.SubstituteSettings.Replace("UseComma", "UseSeperator").Trim();
                int indexofNewSep = indexofAmbersand + 1;
                // list.SepCharacter = objname[indexofNewSep].ToString();
                newSepCharacter = objname[indexofNewSep].ToString();
                // string stringToReplace = "";
                // stringToReplace = "&" + list.SepCharacter;
                //I need objname to get the control from the canvas. Hence I need to remove the $
                objname = objname.Replace('&', ' ').Replace('#', ' ').Trim();
                objname = objname.Replace(newSepCharacter[0], ' ').Replace('#', ' ').Trim();
                overrideSep = true;
            }

            if (objname.Contains('$'))
            {
                datasetnameprefix = true;
                //I need objname to get the control from the canvas. Hence I need to remove the $
                //07/24/28 Commented the line below and inseted the one below the line below
                // objname = objname.Replace('$', ' ').Replace('#', ' ').Trim();
                objname = objname.Replace('$', ' ').Trim();
            }

            //Added by Aaron 02/11/2014
            //This code was added to support displaying the variables selected in a listbox control in the format
            //gender =dataset$gender, income =dataset$income
            //If a control name is prefixed by !, we will always display variables in that control in the format
            //gender =dataset$gender, income =dataset$income
            bool custDispFormat = false;

            if (objname.Contains('!'))
            {
                custDispFormat = true;
                //I need objname to get the control from the canvas. Hence I need to remove the $
                objname = objname.Replace('!', ' ').Replace('#', ' ').Trim();
            }

            if (objname.Contains('#'))
            {
                forceUseCommas = true;
                //I need objname to get the control from the canvas. Hence I need to remove the $
                objname = objname.Replace('#', ' ').Trim();
            }
			
            //Added by Aaron 07/27/2018
            //Handling  -var1,-var2 for reshape, see line 707
            //we only have to do this when variable names are not enclosed in "", and when variable names are not prefixed by a dataset 
            if (objname.Contains('^'))
            {
                prefixEachVariable = true;
                //I need objname to get the control from the canvas. Hence I need to remove the $
                int indexofCarrot = objname.IndexOf("^");
                //  list.SubstituteSettings = list.SubstituteSettings.Replace("UseComma", "UseSeperator").Trim();
                int indexofPrefix = indexofCarrot + 1;
                // list.SepCharacter = objname[indexofNewSep].ToString();
                newPrefixCharacter = objname[indexofPrefix].ToString();
                // string stringToReplace = "";
                // stringToReplace = "&" + list.SepCharacter;
                //I need objname to get the control from the canvas. Hence I need to remove the $
                objname = objname.Replace('^', ' ').Replace('#', ' ').Trim();
                objname = objname.Replace(newPrefixCharacter[0], ' ').Replace('#', ' ').Trim();
                prefixEachVariable =true;
            }

            //13Sep2013 col name and col object//// ends

            FrameworkElement fe = obj as FrameworkElement;
            FrameworkElement element = fe.FindName(objname) as FrameworkElement;

            if (element == null)
                return string.Empty;

            IBSkyInputControl ctrl = element as IBSkyInputControl;
            string result = string.Empty;

            if (typeof(BSkyGroupingVariable).IsAssignableFrom(element.GetType()))
            {
                string vals = string.Empty;
                DragDropList list = null;
                BSkyGroupingVariable groupVar = element as BSkyGroupingVariable;

                foreach (object child in groupVar.Children)
                {
                    if (child is SingleItemList)
                        list = child as DragDropList;
                }

                if (list != null)
                {
                    foreach (object o in list.Items)
                    {
                        if (datasetnameprefix == true)
                        {
                            if (list.SubstituteSettings.Contains("Enclosed")) vals += "'" + currentDataset + "$" + o.ToString() + "'";
                            else vals += currentDataset + "$" + o.ToString();
                        }
                        else if (custDispFormat == true)
                        {
                            // if (list.SubstituteSettings.Contains("Enclosed")) vals += "'" + currentDataset + "$" + o.ToString() + "'";
                            vals += o.ToString() + " = " + currentDataset + "$" + o.ToString();
                        }
                        else
                        {
                            if (list.SubstituteSettings.Contains("NoPrefix"))
                            {
                                if (list.SubstituteSettings.Contains("Enclosed")) vals += "'" + o.ToString() + "'";
                                else vals += o.ToString();
                            }
                            //Added by Aaron 02/12/2014
                            //handles the case of var1=dataset$var1
                            else if (list.SubstituteSettings.Contains("CustomFormat"))
                            {
                                if (list.SubstituteSettings.Contains("Enclosed")) vals += "'" + o.ToString() + " = " + currentDataset + "$" + o.ToString() + "'";
                                else vals += o.ToString() + " = " + currentDataset + "$" + o.ToString();
                            }
                            else
                            // Added by Aaron 10/26/2013
                            // This is the case where the prefix is enabled
                            {
                                if (list.SubstituteSettings.Contains("Enclosed")) vals += "'" + currentDataset + "$" + o.ToString() + "'";
                                else vals += currentDataset + "$" + o.ToString();
                            }
                        }
                        if (list.SubstituteSettings.Contains("UsePlus")) vals += "+";
                        if (list.SubstituteSettings.Contains("UseComma")) vals += ",";
                        if (list.SubstituteSettings.Contains("UseSeperator")) vals += list.SepCharacter;
                    }
                    if (list.SubstituteSettings.Contains("UseComma")) vals = vals.TrimEnd(',');
                    if (list.SubstituteSettings.Contains("UsePlus")) vals = vals.TrimEnd('+');
                    if (list.SubstituteSettings.Contains("UseSeperator"))
                    {
                        if (list.SepCharacter != null)
                        {
                            if (list.SepCharacter != string.Empty)
                            {
                                char charsToTrim = list.SepCharacter[0];
                                vals = vals.TrimEnd(list.SepCharacter.ToCharArray());
                                //  vals = vals.TrimEnd(charsToTrim);
                                //  vals.TrimEnd(list.SepCharacter as chars[]);
                            }
                        }
                    }
                }

                //if (ctrl.Syntax == "%%VALUE%%")
                //{
                //    vals = ctrl.Syntax.Replace("%%VALUE%%", vals);
                //    return vals;
                //}
                //else return ctrl.Syntax;

                if (vals == string.Empty | vals == null)
                {
                    if (ctrl.Syntax == "%%VALUE%%")
                    {
                        //vals = txt.Text;
                        result = ctrl.Syntax.Replace("%%VALUE%%", string.Empty);
                    }
                    else
                        result = ctrl.Syntax;

                    return result;
                }
                else
                {
                    if (list.SubstituteSettings.Contains("CreateArray")) vals = "c(" + vals + ")";
                    if (list.SubstituteSettings.Contains("Brackets")) vals = "(" + vals + ")";
                    if (list.PrefixTxt != string.Empty)
                    {
                        vals = list.PrefixTxt + vals;
                    }

                    //09Jul2015
                    //if (ctrl.Syntax == "%%VALUE%%")
                    //{
                    //    vals = ctrl.Syntax.Replace("%%VALUE%%", vals);
                    //    return vals;
                    //}
                    //else return ctrl.Syntax;

                    return vals;
                }

            }

            else if (typeof(BSkyAggregateCtrl).IsAssignableFrom(element.GetType()))
            {
                // datasetnameprefix = true;
                string vals = string.Empty;
                DragDropListForSummarize list = null;
                BSkyAggregateCtrl groupVar = element as BSkyAggregateCtrl;
                bool displayCounts = false;
                CheckBox temp = null;
                TextBox tb = null;
                string[] tokens = null;
                int nooftokens = 0;
                int iterator = 0;

                foreach (object child in groupVar.Children)
                {
                    if (child is DragDropListForSummarize)
                        list = child as DragDropListForSummarize;
                    if (child is CheckBox)
                    {
                        temp = child as CheckBox;
                        displayCounts = temp.IsChecked.Value;
                    }
                    if (child is TextBox)
                    {
                        tb = child as TextBox;
                        if (tb.Text != "")
                            tokens = tb.Text.Split(',');
                    }

                }
                if (displayCounts) iterator = 1;
                else iterator = 0;
                if (tokens == null)
                    nooftokens = 0;
                else
                    nooftokens = tokens.Length;
                // list.SubstituteSettings = "NoPrefix|UseComma|StringPrefix|Brackets";
                // list.PrefixTxt = "%>% summarize";
                if (list != null)
                {
                    foreach (DataSourceVariable o in list.Items)
                    {
                        if (datasetnameprefix == true)
                        {
                            if (list.SubstituteSettings.Contains("Enclosed")) vals += "'" + currentDataset + "$" + o.ToString() + "'";
                            else vals += currentDataset + "$" + o.ToString();
                        }
                        else if (custDispFormat == true)
                        {
                            // if (list.SubstituteSettings.Contains("Enclosed")) vals += "'" + currentDataset + "$" + o.ToString() + "'";
                            vals += o.ToString() + " = " + currentDataset + "$" + o.ToString();
                        }
                        else
                        {
                            if (list.SubstituteSettings.Contains("NoPrefix"))
                            {
                                if (list.SubstituteSettings.Contains("Enclosed")) vals += "'" + o.ToString() + "'";
                                else
                                {
                                    if (nooftokens > 0)
                                    {
                                        if (iterator < nooftokens)
                                        {
                                            //na.rm=TRUE should not be applied to n_distinct
                                            if (!o.XName.Contains("n_distinct"))
                                            {
                                                vals = vals + tokens[iterator] + "=" + o.XName.Replace(")", ",na.rm =TRUE)");
                                            }
                                            else
                                            {
                                                vals = vals + tokens[iterator] + "=" + o.XName;
                                            }
                                            iterator = iterator + 1;
                                        }
                                        //Case where iterator == nooftokens as all tokens are used
                                        //We create a default variable name e.g. mean_mpg instead of the default mean(mpg)
                                        else
                                        {
                                            //na.rm=TRUE should not be applied to n_distinct
                                            if (!o.XName.Contains("n_distinct"))
                                            {
                                                vals += o.XName.Replace("(", "_").Replace(")", "") + "=" + o.XName.Replace(")", ",na.rm =TRUE)");
                                            }
                                            else
                                            {
                                                vals += o.XName.Replace("(", "_").Replace(")", "") + "=" + o.XName;
                                            }
                                        }
                                    }
                                    //No tokens specified

                                    else
                                    {
                                        //na.rm=TRUE should not be applied to n_distinct
                                        if (!o.XName.Contains("n_distinct"))
                                        {
                                            //We create a default variable name e.g. mean_mpg instead of the default mean(mpg)
                                            vals += o.XName.Replace("(", "_").Replace(")", "") + "=" + o.XName.Replace(")", ",na.rm =TRUE)");
                                        }
                                        else
                                        {
                                            vals += o.XName.Replace("(", "_").Replace(")", "") + "=" + o.XName;
                                        }
                                    }
                                }
                            }
                            //Added by Aaron 02/12/2014
                            //handles the case of var1=dataset$var1
                            else if (list.SubstituteSettings.Contains("CustomFormat"))
                            {
                                if (list.SubstituteSettings.Contains("Enclosed")) vals += "'" + o.ToString() + " = " + currentDataset + "$" + o.ToString() + "'";
                                else vals += o.ToString() + " = " + currentDataset + "$" + o.ToString();
                            }
                            else
                            // Added by Aaron 10/26/2013
                            // This is the case where the prefix is enabled
                            {
                                if (list.SubstituteSettings.Contains("Enclosed")) vals += "'" + currentDataset + "$" + o.ToString() + "'";
                                else vals += currentDataset + "$" + o.ToString();
                            }
                        }
                        if (list.SubstituteSettings.Contains("UsePlus")) vals += "+";
                        if (list.SubstituteSettings.Contains("UseComma")) vals += ",";
                        if (list.SubstituteSettings.Contains("UseSeperator")) vals += list.SepCharacter;
                    }
                    if (list.SubstituteSettings.Contains("UseComma")) vals = vals.TrimEnd(',');
                    if (list.SubstituteSettings.Contains("UsePlus")) vals = vals.TrimEnd('+');
                    if (list.SubstituteSettings.Contains("UseSeperator"))
                    {
                        if (list.SepCharacter != null)
                        {
                            if (list.SepCharacter != string.Empty)
                            {
                                char charsToTrim = list.SepCharacter[0];
                                vals = vals.TrimEnd(list.SepCharacter.ToCharArray());
                                //  vals = vals.TrimEnd(charsToTrim);
                                //  vals.TrimEnd(list.SepCharacter as chars[]);
                            }
                        }
                    }
                }

                //if (ctrl.Syntax == "%%VALUE%%")
                //{
                //    vals = ctrl.Syntax.Replace("%%VALUE%%", vals);
                //    return vals;
                //}
                //else return ctrl.Syntax;

                //02Oct2016 following 'if' is commented and the 'else' block is used but 'else' is commented.
                // This is done because target box may or may not have any value. And in both th cases it should 
                // execute the 'else' block (without 'else' keyword) below.
                ////if (vals == string.Empty | vals == null)
                ////{
                ////    if (ctrl.Syntax == "%%VALUE%%")
                ////    {
                ////        vals = txt.Text;
                ////        result = ctrl.Syntax.Replace("%%VALUE%%", string.Empty);
                ////    }
                ////    else
                ////        result = ctrl.Syntax;

                ////    return result;
                ////}
                ////else
                {
                    if (displayCounts)
                    {
                        if (nooftokens > 0)
                            vals = tokens[0] + "=" + "n(), " + vals;
                        else
                            vals = "Count" + "=" + "n(), " + vals;
                    }
                    if (list.SubstituteSettings.Contains("CreateArray")) vals = "c(" + vals + ")";

                    if (list.SubstituteSettings.Contains("Brackets")) vals = "(" + vals + ")";
                    if (list.PrefixTxt != string.Empty)
                    {
                        vals = list.PrefixTxt + vals;
                    }

                    //09Jul2015
                    //if (ctrl.Syntax == "%%VALUE%%")
                    //{
                    //    vals = ctrl.Syntax.Replace("%%VALUE%%", vals);
                    //    return vals;
                    //}
                    //else return ctrl.Syntax;

                    return vals;
                }

            }

            else if (typeof(BSkySortCtrl).IsAssignableFrom(element.GetType()))
            {
                // datasetnameprefix = true;
                string vals = string.Empty;
                DragDropListForSummarize list = null;
                BSkySortCtrl groupVar = element as BSkySortCtrl;

                foreach (object child in groupVar.Children)
                {
                    if (child is DragDropListForSummarize)
                        list = child as DragDropListForSummarize;
                }
                // list.SubstituteSettings = "NoPrefix|UseComma|StringPrefix|Brackets";
                // list.PrefixTxt = "%>% summarize";
                if (list != null)
                {
                    foreach (DataSourceVariable o in list.Items)
                    {
                        if (datasetnameprefix == true)
                        {
                            if (list.SubstituteSettings.Contains("Enclosed")) vals += "'" + currentDataset + "$" + o.ToString() + "'";
                            else vals += currentDataset + "$" + o.ToString();
                        }
                        else if (custDispFormat == true)
                        {
                            // if (list.SubstituteSettings.Contains("Enclosed")) vals += "'" + currentDataset + "$" + o.ToString() + "'";
                            vals += o.ToString() + " = " + currentDataset + "$" + o.ToString();
                        }
                        else
                        {
                            if (list.SubstituteSettings.Contains("NoPrefix"))
                            {
                                if (list.SubstituteSettings.Contains("Enclosed")) vals += "'" + o.ToString() + "'";
                                else
                                {
                                    vals = vals + o.XName;
                                }
                            }
                            //Added by Aaron 02/12/2014
                            //handles the case of var1=dataset$var1
                            else if (list.SubstituteSettings.Contains("CustomFormat"))
                            {
                                if (list.SubstituteSettings.Contains("Enclosed")) vals += "'" + o.ToString() + " = " + currentDataset + "$" + o.ToString() + "'";
                                else vals += o.ToString() + " = " + currentDataset + "$" + o.ToString();
                            }
                            else
                            // Added by Aaron 10/26/2013
                            // This is the case where the prefix is enabled
                            {
                                if (list.SubstituteSettings.Contains("Enclosed")) vals += "'" + currentDataset + "$" + o.ToString() + "'";
                                else vals += currentDataset + "$" + o.ToString();
                            }
                        }
                        if (list.SubstituteSettings.Contains("UsePlus")) vals += "+";
                        if (list.SubstituteSettings.Contains("UseComma")) vals += ",";
                        if (list.SubstituteSettings.Contains("UseSeperator")) vals += list.SepCharacter;
                    }
                    if (list.SubstituteSettings.Contains("UseComma")) vals = vals.TrimEnd(',');
                    if (list.SubstituteSettings.Contains("UsePlus")) vals = vals.TrimEnd('+');
                    if (list.SubstituteSettings.Contains("UseSeperator"))
                    {
                        if (list.SepCharacter != null)
                        {
                            if (list.SepCharacter != string.Empty)
                            {
                                char charsToTrim = list.SepCharacter[0];
                                vals = vals.TrimEnd(list.SepCharacter.ToCharArray());
                                //  vals = vals.TrimEnd(charsToTrim);
                                //  vals.TrimEnd(list.SepCharacter as chars[]);
                            }
                        }
                    }
                }

                //if (ctrl.Syntax == "%%VALUE%%")
                //{
                //    vals = ctrl.Syntax.Replace("%%VALUE%%", vals);
                //    return vals;
                //}
                //else return ctrl.Syntax;

                if (vals == string.Empty | vals == null)
                {
                    if (ctrl.Syntax == "%%VALUE%%")
                    {
                        //vals = txt.Text;
                        result = ctrl.Syntax.Replace("%%VALUE%%", string.Empty);
                    }
                    else
                        result = ctrl.Syntax;

                    return result;
                }
                else
                {
                    if (list.SubstituteSettings.Contains("CreateArray")) vals = "c(" + vals + ")";

                    if (list.SubstituteSettings.Contains("Brackets")) vals = "(" + vals + ")";
                    if (list.PrefixTxt != string.Empty)
                    {
                        vals = list.PrefixTxt + vals;
                    }

                    //09Jul2015
                    //if (ctrl.Syntax == "%%VALUE%%")
                    //{
                    //    vals = ctrl.Syntax.Replace("%%VALUE%%", vals);
                    //    return vals;
                    //}
                    //else return ctrl.Syntax;

                    return vals;
                }

            }

            //Added by Aaron 12/27/2013
            //Changed typeof(ListBox) to type of BSkyVariableList
            //Added by Aaron 05/29/2014
            //changed typeof(BSkyVariableList).IsAssignableFrom(element.GetType()) to line below
            else if (typeof(BSkySourceList).IsAssignableFrom(element.GetType()) || typeof(BSkyTargetList).IsAssignableFrom(element.GetType())) // mostly colname should be in listbox rather than any other control
            {
                string vals = string.Empty;
                DragDropList list = element as DragDropList;
                string oriSubstituteSettings = "";
                oriSubstituteSettings = list.SubstituteSettings;
                

                if (boolTilda)
                {
                    oriSubstituteSettings = list.SubstituteSettings;
                    list.SubstituteSettings = "NoPrefix|UseComma|Enclosed";

                }
             
                if (overrideSep)
                {
                    oriSubstituteSettings = list.SubstituteSettings;
                    oriSepCharacter = list.SepCharacter;
                    list.SubstituteSettings = list.SubstituteSettings.Replace("UseComma", "UseSeperator").Trim();
                    list.SepCharacter = newSepCharacter;

                }

                if (list != null)
                {
                    foreach (object o in list.Items)
                    {
                        //       if(paramtype==1 || paramtype==0 ) //13Sep2013 if-else added. old logic had just one line which is under 'if'
                        //         vals += "'" + o.ToString() + "',";
                        //   else if(paramtype==2)
                        //     vals += currentDataset+"$" + o.ToString() + ",";

                        if (datasetnameprefix == true)
                        {
                            if (list.SubstituteSettings.Contains("Enclosed")) vals += "'" + currentDataset + "$" + o.ToString() + "'";
                            else vals += currentDataset + "$" + o.ToString();
                        }
                        else if (custDispFormat == true)
                        {
                            // if (list.SubstituteSettings.Contains("Enclosed")) vals += "'" + currentDataset + "$" + o.ToString() + "'";
                            vals += o.ToString() + " = " + currentDataset + "$" + o.ToString();
                        }
                        else
                        {
                            if (list.SubstituteSettings.Contains("NoPrefix"))
                            {
                                if (list.SubstituteSettings.Contains("Enclosed")) vals += "'" + o.ToString() + "'";
                                else
                                {
//Added by Aaron 07/27/2018
//This is the only place where I need to pass -var1,-var2
                                    if (!prefixEachVariable)
                                        vals += o.ToString();
                                    else vals = vals+newPrefixCharacter + o.ToString();
                                }
                            }
                            //Added by Aaron 02/12/2014
                            //handles the case of var1=dataset$var1
                            else if (list.SubstituteSettings.Contains("CustomFormat"))
                            {
                                if (list.SubstituteSettings.Contains("Enclosed")) vals += "'" + o.ToString() + " = " + currentDataset + "$" + o.ToString() + "'";
                                else vals += o.ToString() + " = " + currentDataset + "$" + o.ToString();
                            }
                            else
                            // Added by Aaron 10/26/2013
                            // This is the case where the prefix is enabled
                            {
                                if (list.SubstituteSettings.Contains("Enclosed")) vals += "'" + currentDataset + "$" + o.ToString() + "'";
                                else vals += currentDataset + "$" + o.ToString();
                            }
                        }

                        if (list.SubstituteSettings.Contains("UsePlus")) vals += "+";
                        if (list.SubstituteSettings.Contains("UseComma")) vals += ",";
                        //if (list.SubstituteSettings.Contains("UseSeperator")) vals += list.SepCharacter;
                        // Modified by Aaron 12/29/2015
                        //This to support the use of one control in multiple lines of syntax
                        //just like how the target control when used in multi-way anova
                        //needs to return var1*var2 with * being the seperator and var1,var2 with , being the seperator //for Anova and plotMeans respectively

                        if (list.SubstituteSettings.Contains("UseSeperator"))
                        {
                            if (forceUseCommas == true)
                            {
                                vals += ",";
                            }
                            else

                                vals += list.SepCharacter;
                        }
                    }
                    // vals = vals.TrimEnd(',');
                    if (list.SubstituteSettings.Contains("UseComma")) vals = vals.TrimEnd(',');
                    if (list.SubstituteSettings.Contains("UsePlus")) vals = vals.TrimEnd('+');
                    if (list.SubstituteSettings.Contains("UseSeperator"))
                    {
                        //if (list.SepCharacter != null)
                        //{
                        //    if (list.SepCharacter != string.Empty)
                        //    {
                        //        char charsToTrim = list.SepCharacter[0];
                        //        vals = vals.TrimEnd(list.SepCharacter.ToCharArray());
                        //        //  vals = vals.TrimEnd(charsToTrim);
                        //        //  vals.TrimEnd(list.SepCharacter as chars[]);
                        //    }
                        //}
                        if (list.SepCharacter != null)
                        {
                            // Modified by Aaron 12/29/2015
                            //This to support the use of one control in multiple lines of syntax
                            //just like how the target control when used in multi-way anova
                            //needs to return var1*var2 with * being the seperator and var1,var2 with , being the seperator //for Anova and plotMeans respectively

                            if (forceUseCommas == true)
                            {
                                vals = vals.TrimEnd('+');
                            }
                            else if (list.SepCharacter != string.Empty)
                            {
                                char charsToTrim = list.SepCharacter[0];
                                vals = vals.TrimEnd(list.SepCharacter.ToCharArray());
                                //  vals = vals.TrimEnd(charsToTrim);
                                //  vals.TrimEnd(list.SepCharacter as chars[]);
                            }
                        }
                    }
                }



                if (overrideSep == true)
                {
                    list.SubstituteSettings = oriSubstituteSettings;
                    list.SepCharacter = oriSepCharacter;
                }

                if (boolTilda == true)
                {
                    list.SubstituteSettings = oriSubstituteSettings;

                }


                if (vals == string.Empty | vals == null)
                {
                    if (ctrl.Syntax == "%%VALUE%%")
                    {
                        //vals = txt.Text;
                        result = ctrl.Syntax.Replace("%%VALUE%%", vals);
                    }
                    else
                        result = ctrl.Syntax;

                    return result;
                }
                else
                {
                    if (list.SubstituteSettings.Contains("CreateArray")) vals = "c(" + vals + ")";
                    if (list.SubstituteSettings.Contains("Brackets")) vals = "(" + vals + ")";
                    if (list.PrefixTxt != string.Empty)
                    {
                        vals = list.PrefixTxt + vals;
                    }

                    if (ctrl.Syntax == "%%VALUE%%")
                    {
                        vals = ctrl.Syntax.Replace("%%VALUE%%", vals);
                        return vals;
                    }
                    else return ctrl.Syntax;
                }

            }

            if (typeof(BSkyListBoxwBorderForDatasets).IsAssignableFrom(element.GetType())) // mostly colname should be in listbox rather than any other control
            {
                string vals = string.Empty;
                BSkyListBoxwBorderForDatasets list = element as BSkyListBoxwBorderForDatasets;

                if (list != null)
                {
                    foreach (DatasetDisplay o in list.Items)
                    {
                        //       if(paramtype==1 || paramtype==0 ) //13Sep2013 if-else added. old logic had just one line which is under 'if'
                        //         vals += "'" + o.ToString() + "',";
                        //   else if(paramtype==2)
                        //     vals += currentDataset+"$" + o.ToString() + ",";

                        //if (datasetnameprefix == true)
                        //{
                        //    if (list.SubstituteSettings.Contains("Enclosed")) vals += "'" + currentDataset + "$" + o.ToString() + "'";
                        //    else vals += currentDataset + "$" + o.ToString();
                        //}
                        //else if (custDispFormat == true)
                        //{
                        //    // if (list.SubstituteSettings.Contains("Enclosed")) vals += "'" + currentDataset + "$" + o.ToString() + "'";
                        //    vals += o.ToString() + " = " + currentDataset + "$" + o.ToString();
                        //}

                        if (list.SubstituteSettings.Contains("Enclosed")) vals += "'" + o.Name + "'";
                        else vals += o.Name;

                        //Added by Aaron 02/12/2014
                        //handles the case of var1=dataset$var1
                        //else if (list.SubstituteSettings.Contains("CustomFormat"))
                        //{
                        //    if (list.SubstituteSettings.Contains("Enclosed")) vals += "'" + o.ToString() + " = " + currentDataset + "$" + o.ToString() + "'";
                        //    else vals += o.ToString() + " = " + currentDataset + "$" + o.ToString();
                        //}
                        //else
                        //// Added by Aaron 10/26/2013
                        //// This is the case where the prefix is enabled
                        //{
                        //    if (list.SubstituteSettings.Contains("Enclosed")) vals += "'" + currentDataset + "$" + o.ToString() + "'";
                        //    else vals += currentDataset + "$" + o.ToString();
                        //}

                        if (list.SubstituteSettings.Contains("UsePlus")) vals += "+";
                        if (list.SubstituteSettings.Contains("UseComma")) vals += ",";
                        if (list.SubstituteSettings.Contains("UseSeperator")) vals += list.SepCharacter;
                    }
                    // vals = vals.TrimEnd(',');
                    if (list.SubstituteSettings.Contains("UseComma")) vals = vals.TrimEnd(',');
                    if (list.SubstituteSettings.Contains("UsePlus")) vals = vals.TrimEnd('+');
                    if (list.SubstituteSettings.Contains("UseSeperator"))
                    {
                        if (list.SepCharacter != null)
                        {
                            if (list.SepCharacter != string.Empty)
                            {
                                char charsToTrim = list.SepCharacter[0];
                                vals = vals.TrimEnd(list.SepCharacter.ToCharArray());
                                //  vals = vals.TrimEnd(charsToTrim);
                                //  vals.TrimEnd(list.SepCharacter as chars[]);
                            }
                        }
                    }
                }

                if (vals == string.Empty | vals == null)
                {
                    if (ctrl.Syntax == "%%VALUE%%")
                    {
                        //vals = txt.Text;
                        result = ctrl.Syntax.Replace("%%VALUE%%", vals);
                    }
                    else
                        result = ctrl.Syntax;

                    return result;
                }
                else
                {
                    if (list.SubstituteSettings.Contains("CreateArray")) vals = "c(" + vals + ")";
                    if (list.PrefixTxt != string.Empty)
                    {
                        vals = list.PrefixTxt + vals;
                    }

                    if (ctrl.Syntax == "%%VALUE%%")
                    {
                        vals = ctrl.Syntax.Replace("%%VALUE%%", vals);
                        return vals;
                    }
                    else return ctrl.Syntax;
                }

            }
            // Added12/27/2013 by Aaron
            //Added code below to support BSkyListBox
            //else if ((typeof(ListBox).IsAssignableFrom(element.GetType())))
            //{
            //    ListBox lst = element as ListBox;
            //    string vals = string.Empty;
            //    foreach (string str in lst.SelectedItems)
            //        vals = vals + str+",";
            //    vals = vals.TrimEnd(',');
            //    result = ctrl.Syntax.Replace("%%VALUE%%", vals);
            //    return result;
            //}

            else if (typeof(BSkyListBox).IsAssignableFrom(element.GetType()) || typeof(BSkyMasterListBox).IsAssignableFrom(element.GetType())) // mostly colname should be in listbox rather than any other control
            {
                string vals = string.Empty;
                CtrlListBox list = element as CtrlListBox;

                if (list != null)
                {
                    foreach (object o in list.SelectedItems)
                    {
                        //       if(paramtype==1 || paramtype==0 ) //13Sep2013 if-else added. old logic had just one line which is under 'if'
                        //         vals += "'" + o.ToString() + "',";
                        //   else if(paramtype==2)
                        //     vals += currentDataset+"$" + o.ToString() + ",";

                        if (list.SubstituteSettings.Contains("Enclosed")) vals += "'" + o.ToString() + "'";
                        else vals += o.ToString();

                        //Added by Aaron 02/12/2014
                        //handles the case of var1=dataset$var1
                        if (list.SubstituteSettings.Contains("UsePlus")) vals += "+";
                        if (list.SubstituteSettings.Contains("UseComma")) vals += ",";
                    }
                    // vals = vals.TrimEnd(',');
                    if (list.SubstituteSettings.Contains("UseComma")) vals = vals.TrimEnd(',');
                    if (list.SubstituteSettings.Contains("UsePlus")) vals = vals.TrimEnd('+');
                }
                if (ctrl.Syntax == "%%VALUE%%")
                {
                    vals = ctrl.Syntax.Replace("%%VALUE%%", vals);
                    return vals;
                }
                else return ctrl.Syntax;
            }

            //07/13/2014
            //Aaron
            //Added support for the following use cases
            //1. an option portion of the syntax subset=var1>60|var2=10
            //In above case if nothing is entered in textbox, the optional part of syntax is not created
            //2. Added support for an optional control that creates a character array of all the labels that will be used
            //to generate values for a binned variable. If 3 bins are chosen and the labels of the bins are 20-30,30-40,40-50, then we create a array as c("20-30","30-40","40-50")
            else if (typeof(BSkyTextBox).IsAssignableFrom(element.GetType()))
            {
                BSkyTextBox txt = element as BSkyTextBox;
                string vals = string.Empty;

                //Added by Aaron 09/07/2014
                //Added this code to amke sure that an empty string is returned if the textbox is disabled
                if (txt.Enabled == false)
                {
                    if (ctrl.Syntax == "%%VALUE%%")
                    {
                        vals = "";
                        result = ctrl.Syntax.Replace("%%VALUE%%", vals);
                    }
                    else
                        result = ctrl.Syntax;

                    return result;
                }

                currentDataset = ExpandMacro("%DATASET%");
                activemodel = ExpandMacro("%MODEL%");
                //Aaron 07/12/2014
                // This line ensures that the prefix text gets returned only when a valid value  is entered in the textbox
                //This ensures that an empty string retured when text is empty
                //This allows you to create an optional syntax sub-string like subset=var1>10
                if (txt.Text == string.Empty | txt.Text == null)
                {
                    if (ctrl.Syntax == "%%VALUE%%")
                    {
                        vals = txt.Text;
                        result = ctrl.Syntax.Replace("%%VALUE%%", vals);
                    }
                    else
                        result = ctrl.Syntax;

                    return result;
                }
                if (txt.SubstituteSettings != null)
                {
                    if (txt.SubstituteSettings.Contains("TextAsIs"))
                    {
                        //vals += currentDataset + "$" + o.ToString();
                        vals += txt.Text;
                    }
                    if (txt.SubstituteSettings.Contains("PrefixByDatasetName"))
                    {
                        //vals += currentDataset + "$" + o.ToString();
                        vals += currentDataset + "$" + txt.Text;
                    }
                    else if (txt.SubstituteSettings.Contains("CreateArray"))
                    {
                        string[] strs = txt.Text.Split(',');

                        for (int i = 0; i < strs.Length; i++)
                        {
                            strs[i] = "'" + strs[i] + "'";
                        }
                        vals = "c(" + string.Join(",", strs) + ")";
                    }
                    if (txt.SubstituteSettings.Contains("Brackets")) vals = "(" + vals + ")";
                    //having the if below allows you to create a prefix for an enclosed string
                    if (txt.SubstituteSettings.Contains("PrefixByString"))
                    {
                        // vals += txt.PrefixTxt + txt.Text;
                        vals = txt.PrefixTxt + vals;
                    }
                }
                //Case where you don't have to prefix text by dataset name and you don't have to create an array or enclose it
                else vals = txt.Text;
                if (ctrl.Syntax == "%%VALUE%%")
                {
                    result = ctrl.Syntax.Replace("%%VALUE%%", vals);
                    // if (!txt.RequiredSyntax) vals = vals + ",";
                }
                else
                {
                    result = ctrl.Syntax;
                    // if (!txt.RequiredSyntax) vals = vals + ",";
                }

                return result;
            }

            else if (typeof(CheckBox).IsAssignableFrom(element.GetType()))
            {
                BSkyCheckBox txt = element as BSkyCheckBox;

                if (txt.IsChecked.HasValue && txt.IsChecked.Value)
                {
                    if (ctrl.Syntax == "%%VALUE%%") return "TRUE";
                    else return ctrl.Syntax;
                }
                else
                {
                    if (txt.uncheckedsyntax != null)
                        return txt.uncheckedsyntax;
                    else return "";
                }
            }
            else if (typeof(RadioButton).IsAssignableFrom(element.GetType()))
            {
                RadioButton txt = element as RadioButton;
                result = txt.IsChecked.HasValue && txt.IsChecked.Value ? "TRUE" : "FALSE";

                //Added by Aaron
                //Made the changes below to support the aggregate command
                //If there is a string in syntax e.g. MEAN, if the radio button is selected, MEAN will be passed
                //If unselected, nothing or empty string will be passed
                //This will allow me to create syntax as follows
                //FUN ={Mean}{Sum}{txt2} where Mean, Sum are both radio buttons that are part of a group "rd"
                //Note: That txt2 is a text box control that returns a value only when the "other" radio button is enabled
                //"Other" is a part of the group rd
                //If the other radio button is selected, we pass the value in the textbox txt2
                //Other is not represented as only when other radio button  is selected that the string in txt2 is passed

                if (result == "TRUE")
                {
                    if (ctrl.Syntax == "%%VALUE%%")
                    {
                        return ctrl.Syntax.Replace("%%VALUE%%", result);
                    }
                    else if (ctrl.Syntax == "%DATASET%")
                    {
                        return currentDataset;
                    }
                    else if (ctrl.Syntax == "%MODEL%")
                    {
                        return activemodel;
                    }
                    else return ctrl.Syntax;
                }

                else return "";
                //result = ctrl.Syntax.Replace("%%VALUE%%", result);
                //return result;
            }
            else if (typeof(BSkyRadioGroup).IsAssignableFrom(element.GetType()))
            {
                // Aaron 12/24/2012
                // The radio buttons belonging to the radio group are either placed directly on the canvas (without using the dialog builder
                // that creates the radio buttons) or they are placed on the stackpanel that the radiogroup contains (The content property of the radiogroup
                // will point to the stack panel when we use the dialog builder)
                BSkyRadioGroup txt = element as BSkyRadioGroup;

                StackPanel stkpanl = txt.Content as StackPanel;
                //We are checking the count of the stack panel. Positive count indicates that radio buttons have been placed on stack panel
                if (stkpanl.Children.Count != 0)
                {
                    return txt.Value;
                }
                else
                {
                    BSkyCanvas parentofRdGrp = UIHelper.FindVisualParent<BSkyCanvas>(txt);

                    //System.Windows.FrameworkElement element1 = parent1.FindName(value) as System.Windows.FrameworkElement;
                    foreach (UIElement child in parentofRdGrp.Children)
                    {
                        if (child.GetType().Name == "BSkyRadioButton")
                        {
                            BSkyRadioButton rdBtn = child as BSkyRadioButton;

                            if (rdBtn.GroupName == objname)
                            {
                                if (rdBtn.IsChecked.HasValue && rdBtn.IsChecked.Value)
                                {
                                    if (rdBtn.Syntax == "%%VALUE%%") return "TRUE";

                                    else return rdBtn.Syntax;
                                }
                            }
                        }
                    }
                }
                //System.Windows.FrameworkElement element1 = parent1.FindName(value) as System.Windows.FrameworkElement;
                //if (element1 != null)
                //{
                //    BSkyRadioGroup radioGroup = element1 as BSkyRadioGroup;
                //    StackPanel stack1 = radioGroup.Content as StackPanel;
                //    stack1.Children.Add(this);
                //}
                //return txt.Value;
            }

            else if (typeof(BSkyEditableComboBox).IsAssignableFrom(element.GetType()))
            {
                string txtwithprefix = "";

                BSkyEditableComboBox txt = element as BSkyEditableComboBox;

                if (txt.Syntax != "%%VALUE%%")
                {
                    if (txt.prefixSelectedValue != "")
                    {
                        txt.Syntax = txt.prefixSelectedValue + txt.Syntax;
                    }
                    return txt.Syntax;
                }
                else if (txt.Text == "") return txt.NothingSelected;
                else if (txt.Syntax == "%%VALUE%%")
                {
                    txtwithprefix = txt.Text;
                    if (txt.prefixSelectedValue != "")
                    {
                        txtwithprefix = txt.prefixSelectedValue + txtwithprefix;
                    }
                    return txtwithprefix;
                }

            }
            else if (typeof(BSkyNonEditableComboBox).IsAssignableFrom(element.GetType()))
            {
                BSkyNonEditableComboBox txt = element as BSkyNonEditableComboBox;

                if (txt.Syntax != "%%VALUE%%") return txt.Syntax;
                else if (txt.SelectedValue == null) return txt.NothingSelected;
                else if (txt.Syntax == "%%VALUE%%") return txt.SelectedValue as string;

            }
            return string.Empty;
        }

        #region For Syntax Editor
        //SynEditor set values back to dialog UI elements
        public static void SetValueFromSynEdt(DependencyObject obj, string objname, string args)
        {
            FrameworkElement fe = obj as FrameworkElement;
            FrameworkElement element = fe.FindName(objname) as FrameworkElement;

            if (element == null)//28Mar2013
            {
                MessageBox.Show(BSky.GlobalResources.Properties.Resources.CtrlNotInXaml + " " + objname +
                    ".\n " + BSky.GlobalResources.Properties.Resources.CtrlNameUsedInSyntax,
                    BSky.GlobalResources.Properties.Resources.UndefCtrl, MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            IBSkyInputControl ctrl = element as IBSkyInputControl;
            string result = string.Empty;
            //// take two strings and then try to replace
            ////bsky.one.sm.t.test(vars =c({SelectedVars}), mu={testValue}, conf.level=0.89, datasetname ='{%DATASET%}', missing=0)
            ////bsky.one.sm.t.test(vars =c('tg0'), mu=3, conf.level=0.89, datasetname ='Dataset1', missing=0)

            if (typeof(ListBox).IsAssignableFrom(element.GetType()))
            {
                string vals = string.Empty;
                //17Jan2014 string[] arr = args != null ? stringToArray(args) : null;//{"tg0","tg1","tg2"}; //10Sep2013 modified for NA
                DataSourceVariable[] dsv = args != null ? stringToDSV(args) : null;
                //ListBox list = element as ListBox;
                //list.ItemsSource = dsv;// arr;
                BSkyTargetList list = element as BSkyTargetList;
                list.ItemsSource = null;
                if (dsv != null)//Fix for if any list box in dialog is left empty
                {
                    foreach (DataSourceVariable svar in dsv)
                    {
                        list.Items.Add(svar);
                    }
                }
            }
            else if (typeof(TextBox).IsAssignableFrom(element.GetType()))
            {
                TextBox txt = element as TextBox;
                txt.Text = args;// "123";
            }
            else if (typeof(CheckBox).IsAssignableFrom(element.GetType()))
            {
                CheckBox txt = element as CheckBox;

                if (args.Equals("TRUE"))
                    txt.IsChecked = true;
                else
                    txt.IsChecked = false;
                ////result = txt.IsChecked.HasValue && txt.IsChecked.Value ? "TRUE" : "FALSE";
                ////result = ctrl.Syntax.Replace("%%VALUE%%", result);
                ////return result;
            }
            else if (typeof(RadioButton).IsAssignableFrom(element.GetType()))
            {
                RadioButton txt = element as RadioButton;

                if (args.Equals("TRUE"))
                    txt.IsChecked = true;
                else
                    txt.IsChecked = false;
                ////result = txt.IsChecked.HasValue && txt.IsChecked.Value ? "TRUE" : "FALSE";
                ////result = ctrl.Syntax.Replace("%%VALUE%%", result);
                ////return result;
            }
            if (typeof(BSkyRadioGroup).IsAssignableFrom(element.GetType()))
            {
                BSkyRadioGroup txt = element as BSkyRadioGroup;
                //txt.Value = args;
                ////return txt.Value;
            }

            ////return string.Empty;
        }

        private static string[] stringToArray(string str)
        {
            str = str.Replace("c(", " ").Replace(')', ' ').Trim(); // c( must not have space in between

            MatchCollection vars = Regex.Matches(str, "[A-Za-z0-9_.]+");
            int size = vars.Count;
            string[] arr = new string[size];
            int i = 0;

            foreach (Match m in vars)
            {
                arr[i++] = m.ToString();
                //Console.Write(" " + m.ToString());
            }
            return arr;
        }

        //17Jan2014 String to DataSourceVariable
        private static DataSourceVariable[] stringToDSV(string str)
        {
            str = str.Replace("c(", " ").Replace(')', ' ').Trim(); // c( must not have space in between

            MatchCollection vars = Regex.Matches(str, "[A-Za-z0-9_.]+");
            int size = vars.Count;
            DataSourceVariable[] arr = new DataSourceVariable[size];
            int i = 0;

            foreach (Match m in vars)
            {
                DataSourceVariable dsv = GetDataSourceVariable(m.ToString());
                arr[i++] = dsv != null ? dsv : new DataSourceVariable() { Name = m.ToString() };//03Apr2014
                //arr[i++] = new DataSourceVariable() { Name = m.ToString() };
                //Console.Write(" " + m.ToString());
            }
            return arr;
        }

        //Get datasourcevariable with all attributes like MEASURE, TYPE etc..
        private static DataSourceVariable GetDataSourceVariable(string variablename)
        {
            List<DataSourceVariable> lstdsv = null;

            if (AnalyticsData.DataSource != null)
                lstdsv = AnalyticsData.DataSource.Variables;
            else if (AnalyticsData.Result != null && AnalyticsData.Result.Datasource != null)
                lstdsv = AnalyticsData.Result.Datasource.Variables;

            if (lstdsv == null)
                return null;

            //01Feb2017 Instead of var.Name I changed it to var.RName to fix filter_. issue with cross tab.
            var variable = from var in lstdsv
                           where var.RName == variablename
                           select var;
            DataSourceVariable dv = variable.FirstOrDefault();

            if (dv != null)
            {
                return dv;
            }
            else
            {
                return null;
            }
        }

        public static void getArgumentSetDictionary(string bskcommand, Dictionary<string, string> varValuePair)
        {
            //"bsky.one.sm.t.test(vars=c('tg0','tg2','tg3'),mu=3,conf.level=0.89,datasetname='Dataset1',missing=0)";
            //working for command @"[A-Za-z0-9._]+=( ?(c\()(['A-Za-z0-9._,]+[\)]?)|([A-Za-z0-9._]+))";
            string pattern = @"[A-Za-z0-9._]+=( ?(c\()(['A-Za-z0-9._,{}%#$]+[\)]?)|(['A-Za-z0-9._{}%]+))";
            MatchCollection mc = Regex.Matches(bskcommand, pattern);

            foreach (Match m in mc)
            {
                Console.WriteLine("Index : " + m.Index + "   String : " + m.ToString());
                SplitAndAddToDictionary(m.ToString(), varValuePair);
            }
        }

        private static void SplitAndAddToDictionary(string str, Dictionary<string, string> vvp)
        {
            int endlen = str.IndexOf('=');
            string key = str.Substring(0, endlen).Trim();
            //10Sep2013 handle NA, replace null in its place
            string val = string.Empty;

            if (!str.Contains("'NA'") && str.Contains("NA")) // if there is NA which is R NA and not any value 'NA'
                val = null;
            else
                val = str.Substring(endlen).Replace('=', ' ').Replace("c(", " ").Replace(')', ' ').Trim();
            vvp.Add(key, val);
        }

        public static void MergeTemplateCommandDictionary(Dictionary<string, string> template, Dictionary<string, string> command, Dictionary<string, string> merged)
        {
            string tval = null;
            string cval = null;
            Dictionary<string, string>.KeyCollection kc = template.Keys;

            foreach (string k in kc)
            {
                template.TryGetValue(k, out tval);
                command.TryGetValue(k, out cval);
                if (!tval.Equals(cval))//if they are unequal.( Add only some values which are diff )
                {
                    //27Mar2014 if '{ctrl name}' is there but not just value like 3.5
                    //No need to substitute when there is value provided directly.
                    if (tval.Contains("{") && tval.Contains("}")) //just this if is added, following line were already there
                    {
                        if (cval != null)//10sep2013 mod for NA. if else added. Earlier there was just following stmt here
                            merged.Add(tval.Replace('{', ' ').Replace('}', ' ').Replace("'", " ").Replace('#', ' ').Replace('$', ' ').Trim(), cval.Trim());
                        else
                            merged.Add(tval.Replace('{', ' ').Replace('}', ' ').Replace("'", " ").Replace('#', ' ').Replace('$', ' ').Trim(), cval);//cval is null here so no Trim()
                    }
                }
            }
        }
        #endregion

        // private static readonly Regex re = new Regex(@"\{([^\}]+)\}", RegexOptions.Compiled);
        //Added by Aaron 06/18/2014
        //Added to handle {{ }} sourrounding control names
        private static readonly Regex re = new Regex(@"\{\{([^\}]+)\}\}", RegexOptions.Compiled);

        //The function below replaces the control name by the values in the commmand syntax

        public static string GetCommand(string commandformat, DependencyObject obj)
        {
            string output = re.Replace(commandformat,
                            delegate (Match match)
                            {
                                //Aaron 12/11/2013
                                //Not sure how the class match is constructed but every control name in the dialog that is 
                                //referenced in the syntax command i.e. every thing enclosed in curly braces 
                                //is contained in the match.Groups[1].Value structure
                                //So line of code below is called for every string contained in the curly brace e.g. {source}
                                //in the command syntax
                                string matchedText = match.Groups[1].Value;
                                //Aaron 12/11/2013
                                //The function below gets the values we are going to replace the control name in the syntax command by
                                //For example the control named source is reference in the command as {source}. In the command syntax
                                //source gets replaced by var1,var2
                                //The function below gets the string to replace the control name in the command syntax
                                return GetParam(obj, matchedText);
                            });
            //Added by Aaron 09/01/2013
            //A variable list control does not have to return a value. For example the layers variable of a crosstab can be empty. 
            //in this case layers returns "". This gets replaced in the command syntax as layers = c("")
            //What R understands is layers =NA
            // output = output.Replace("c()", "NA");
            output = handleLayersInCrosstabs(output); // This removes all lines with c()

            output = RemoveParametersWithNoValuesInCommandSyntax(output);
            output = FixExtraCommasInCommandSyntax(output);//14Jul2014
            return output;
        }

        //Added by Aaron 09/01/2013
        //A variable list control does not have to return a value. For example the layers variable of a crosstab can be empty. 
        //in this case layers returns "". This gets replaced in the command syntax as layers = c("")
        //What R understands is layers =NA

        //Added by Aaron 06/16/2015
        //Cross tab does not expect to see layers =c(). We can remove layers completely and crosstab runs correctly
        //I have tested the above. The code that generates the table causes a problem. THe R code runs correctly when x is store, y is overall. But the XML fails
        //To recreate
        //  BSky_Multiway_Cross_Tab = BSkyCrossTable(x=c('store'),y=c('overall'),datasetname='Dataset2',chisq = FALSE,prop.r=FALSE,prop.c=FALSE,resid=FALSE,sresid=FALSE,expected=FALSE,asresid=FALSE)
        //BSkyFormat(BSky_Multiway_Cross_Tab) fails
        private static string handleLayersInCrosstabs(string inputtext)
        {
            string pattern = @"[A-Za-z0-9_.]+\s*=\s*c\(\)";
            //  string pattern = @"\s*=\s*c\(\)";
            bool str = Regex.IsMatch(inputtext, pattern);
            MatchCollection mc = null;

            if (str)
            {
                mc = Regex.Matches(inputtext, pattern);

                foreach (Match m in mc)
                {
                    inputtext = Regex.Replace(inputtext, pattern, "", RegexOptions.None);
                }
            }
            return inputtext;
        }

        private static string FixExtraCommasInCommandSyntax(string inputtext) //14Jul2014 remove extra commas from command
        {
            //Step 1.// Pattern to match in phase 1
            string pattern = @"\s*\,+\s*";//@"\s*[\,]\s*[\,]"; //set of adjecent commas(eg. ,,, , ,,,,, are 3 sets)

            //// Finding pattern and replacing it with something
            bool str = Regex.IsMatch(inputtext, pattern);
            MatchCollection mc = null;
            MatchCollection mcsubstring = null;

            while (str)
            {
                mc = Regex.Matches(inputtext, pattern);
                if (mc.Count > 0)
                {
                    inputtext = Regex.Replace(inputtext, pattern, ",", RegexOptions.None);
                }
                str = Regex.IsMatch(inputtext, @"\s*\,\s*\,");//look if, after replacement, adjacent commas are present 
            }

            //Step 2.// Pattern to match in phase 2
            pattern = @"\s*[\,]\s*[\)]"; // comma and closing ')'

            //// Finding pattern and replacing it with something
            str = Regex.IsMatch(inputtext, pattern);
            mc = Regex.Matches(inputtext, pattern);
            if (mc.Count > 0)
            {
                inputtext = Regex.Replace(inputtext, pattern, ")", RegexOptions.None);
            }

            //Step 3.// Pattern to match in phase 3
            pattern = @"[\(]\s*[\,]\s*"; // opening '(' and comma

            //// Finding pattern and replacing it with something
            str = Regex.IsMatch(inputtext, pattern);
            mc = Regex.Matches(inputtext, pattern);
            if (mc.Count > 0)
            {
                inputtext = Regex.Replace(inputtext, pattern, "(", RegexOptions.None);
            }

            //Aaron 07/09/2015
            //stripping out facet_grid(.~.)
            pattern = @"\+\s*facet_grid\(\s*\.\s*~\s*\.\s*\)";
            str = Regex.IsMatch(inputtext, pattern);
            mc = Regex.Matches(inputtext, pattern);
            if (mc.Count > 0)
            {
                inputtext = Regex.Replace(inputtext, pattern, "", RegexOptions.None);
            }

            //Handling the challenge posed by plotMeans
            //The syntax below shows the problem with plot means
            //test123 <- summarySE(Dataset2,measurevar="hp",groupvars=c("transmission",""))
            //ggplot(test123,aes(x=transmission,y=hp)) + geom_errorbar(aes(ymin=hp-se,ymax=hp+se),width=.1) +geom_line() +geom_point() 
            //the issue is that summarySE requireds "" around the grouping variable which is optional but 
            //ggplot does not
            //with the "" in the groupvars in the summarySE, the summarySE fails.
            //What we will do is look for ,"" in c() and strip it out

            //This looks for c("XXX","")
            pattern = @"c\s*\(\s*\""[A-Za-z]+\""\,\""\""\s*\)";

            //This looks for ,""
            string replacepattern = @"\,\""\""";
            string replacestring = null;
            str = Regex.IsMatch(inputtext, pattern);
            mc = Regex.Matches(inputtext, pattern);
            string matchstring = null;

            foreach (Match match in mc)
            {
                matchstring = match.Value;
                mcsubstring = Regex.Matches(matchstring, replacepattern);
                //The replacestring strips out the ,"" from c("transmission","")
                replacestring = Regex.Replace(matchstring, replacepattern, "", RegexOptions.None);
                //This replaces c("XXX","") with c("XXX")
                inputtext = Regex.Replace(inputtext, pattern, replacestring, RegexOptions.None);
                // Console.WriteLine("Found '{0}' at position {1}",
                //                match.Value, match.Index);
            }

            return (inputtext);
        }

        private static string RemoveParametersWithNoValuesInCommandSyntax(string inputtext) //14Jul2014 remove extra commas from command
        {
            //Looking for pattern ,prefix=\"\"
            //BSkyFARes <-BSkyFactorAnalysis(vars=c('mpg','engine','horse','weight','accel'), factors=2, screeplot =FALSE,rotation=\"none\", saveScores =FALSE, prefixForScores=\"\",dataset=\"Dataset2\")
            string pattern = @"\,\s*[A-Za-z0-9_.]+\s*=\s*\""\"""; //18Jun2016 \s* added in the begining becuase there may be zero or more spaces after comma.
            //// Finding pattern and replacing it with something
            bool str = Regex.IsMatch(inputtext, pattern);
            MatchCollection mc = null;

            if (str) //No need of while here. before 18Jun2016 -> while (str)
            {
                mc = Regex.Matches(inputtext, pattern);
                if (mc.Count > 0)
                {
                    //inputtext = Regex.Replace(inputtext, pattern, "", RegexOptions.None); //before 18Jun2016
                    //18Jun2016. Following is added in place of above the statement. The reason being we dont want to remove 
                    // ,replacement="" (with comma) from our Compute dialog expression. Other than this we remove all other parameter
                    // those do not have any value, like ,prefix=""
                    inputtext = Regex.Replace(inputtext, pattern,
                    delegate (Match match)
                    {
                        string v = match.ToString();

                        if (!v.Contains("replacement=\"\"")) // if (!v.Equals(",replacement=\"\"")) // comma is a issue for Equals. There may b zero or more spaces after comma
                            return "";
                        else
                            return v;
                    });
                }
                str = Regex.IsMatch(inputtext, pattern);//look for more of the same pattern  
            }

            //Looking for pattern prefix=\"\"
            pattern = @"[A-Za-z0-9_.]+\s*=\s*\""\""";

            str = Regex.IsMatch(inputtext, pattern);
            if (str) //No need of while here. before 18Jun2016 -> while (str)
            {
                mc = Regex.Matches(inputtext, pattern);
                if (mc.Count > 0)
                {
                    //inputtext = Regex.Replace(inputtext, pattern, "", RegexOptions.None); //before 18Jun2016
                    //18Jun2016. Following is added in place of above the statement. The reason being we dont want to remove 
                    // replacement=""  (without comma)  from our Compute dialog expression. Other than this we remove all other parameter
                    // those do not have any value, like prefix=""
                    inputtext = Regex.Replace(inputtext, pattern,
                    delegate (Match match)
                    {
                        string v = match.ToString();

                        if (!v.Equals("replacement=\"\""))
                            return "";
                        else
                            return v;
                    });
                }
                str = Regex.IsMatch(inputtext, pattern);
            }

            //Looking for pattern prefix=,
            // state_choropleth(BSkyDfForMap, title=, legend=, zoom=)"
            //Above is in state map
            pattern = @"[A-Za-z0-9_.]+\s*=\s*,";
            //// Finding pattern and replacing it with something
            str = Regex.IsMatch(inputtext, pattern);
            mc = null;
            while (str)
            {
                mc = Regex.Matches(inputtext, pattern);
                if (mc.Count > 0)
                {
                    inputtext = Regex.Replace(inputtext, pattern, "", RegexOptions.None);
                }
                str = Regex.IsMatch(inputtext, pattern);//look for more of the same pattern  
            }

            //Looking for pattern 'prefix=)'
            //Example string is "BSkyDfForMap =data.frame(region=Dataset2[,c(\"region\")], value =Dataset2[,c(\"value\")])\nstate_choropleth(BSkyDfForMap,   zoom=)"
            pattern = @"[A-Za-z0-9_.]+\s*=\s*\)";
            //// Finding pattern and replacing it with something
            str = Regex.IsMatch(inputtext, pattern);
            mc = null;
            while (str)
            {
                mc = Regex.Matches(inputtext, pattern);
                if (mc.Count > 0)
                {
                    inputtext = Regex.Replace(inputtext, pattern, ")", RegexOptions.None);
                }
                str = Regex.IsMatch(inputtext, pattern);//look for more of the same pattern  
            }

            //Added(but not used) by Anil:10May2017 to remove occurrences of 'var=\n' from syntax. 
            //Pattern is same as the above case but no closing ')' , instead a new-line character is present.
            //Looking for pattern: prefix=
            //Example string is "fromidx ="
            ////pattern = @"[A-Za-z0-9_.]+\s*=\s*\n";
            //////// Finding pattern and replacing it with something
            ////str = Regex.IsMatch(inputtext, pattern);
            ////mc = null;
            ////while (str)
            ////{
            ////    mc = Regex.Matches(inputtext, pattern);
            ////    if (mc.Count > 0)
            ////    {
            ////        inputtext = Regex.Replace(inputtext, pattern, "", RegexOptions.None);
            ////    }
            ////    str = Regex.IsMatch(inputtext, pattern);//look for more of the same pattern  
            ////}

            //Added by Aaron 06/09/2015
            //handles +xlab() ,+ylab()+ggtitle() fot ggplots
            //when these strings are found they habe to be removed as they generate an error
            //However +coord_flip(), +theme_bw() etc should not be replaced

            pattern = @"\+\s*[A-Za-z0-9_.]+\s*\(\s*\""\""\)";
            str = Regex.IsMatch(inputtext, pattern);
            mc = null;

            if (str)
            {
                mc = Regex.Matches(inputtext, pattern);

                foreach (Match m in mc)
                {
                    if (m.Value.Contains("xlab") || m.Value.Contains("ylab") || m.Value.Contains("ggtitle"))
                    {
                        inputtext = Regex.Replace(inputtext, pattern, "", RegexOptions.None);
                    }
                }

            }

            return (inputtext);
        }

        public static string GetParam(DependencyObject obj, string paramname)
        {
            string tempVal = string.Empty;

            if (OutputHelper.ExpandMacro(paramname) != paramname)
            {
                tempVal = OutputHelper.ExpandMacro(paramname);
            }
            else
            {
                tempVal = EvaluateValue(obj, paramname);
            }

            return tempVal;
        }

        public static string[] GetParams(DependencyObject obj, string commandformat)
        {
            string[] splits = commandformat.Split(new string[] { "##" }, StringSplitOptions.None);
            string[] param = splits.Skip(1).ToArray();
            string[] paramvalues = new string[param.GetLength(0)];
            int i = 0;

            foreach (string s in param)
            {
                string tempVal = string.Empty;

                if (OutputHelper.ExpandMacro(s) != s)
                {
                    tempVal = OutputHelper.ExpandMacro(s);
                }
                else
                {
                    tempVal = EvaluateValue(obj, s);
                }
                paramvalues[i++] = tempVal;
            }
            return paramvalues;
        }

        private static void GetSplitsDataMatrix(string[] splits, int lstIndex, int datanumber, string[,] matrix, bool[] visibleRows)
        {
            if (lstIndex == splits.Count() - 1)
            {
                for (int index = 0; index < GetFactors(splits[lstIndex]).Count(); ++index)
                {
                    int tempIndex = (dataMatrixIndex) * TotalOutputTables + datanumber;
                    XmlDocument doc = AnalyticsData.Result.Data;
                    XmlNode rownode = doc.SelectSingleNode(string.Format("/Root/UATableList/UADoubleMatrix[{0}]/rows", tempIndex));

                    if (rownode == null)
                    {
                        visibleRows = new bool[1];
                        matrix = new string[1, 1];
                        return;
                    }

                    int rows = rownode.ChildNodes.Count;
                    XmlNode temp = doc.SelectSingleNode(string.Format("/Root/UATableList/UADoubleMatrix[{0}]/rows/row/columns", tempIndex));
                    int cols = temp.ChildNodes.Count;

                    XmlNode metadata = doc.SelectSingleNode(string.Format("/Root/UATableList/Metadata/crosstab/UADoubleMatrix/rows", tempIndex));

                    if (metadata != null)
                    {
                        rows = metadata.ChildNodes.Count;
                    }

                    int j = 0;
                    int rownodecounter = 0;

                    for (int i = 0; i < rows; ++i)
                    {
                        XmlNode node = rownode.ChildNodes[rownodecounter];

                        if (metadata != null)
                        {
                            XmlNode tempm = metadata.ChildNodes[i].SelectSingleNode("./columns/column[2]");

                            if (tempm.InnerText == "0")
                            {
                                continue;
                            }
                        }
                        visibleRows[i + dataMatrixIndex * rows] = true;
                        j = 0;
                        foreach (XmlNode cnode in node.FirstChild.ChildNodes)
                        {
                            matrix[i + dataMatrixIndex * rows, j] = cnode.InnerText;
                            j++;
                        }
                        rownodecounter++;
                    }
                    dataMatrixIndex++;
                }
            }
            else
            {
                for (int index = 0; index < GetFactors(splits[lstIndex]).Count(); ++index)
                {
                    GetSplitsDataMatrix(splits, lstIndex + 1, datanumber, matrix, visibleRows);
                }
            }

        }

        private static int dataMatrixIndex = 1;

        public static string[,] GetFootnotes(int datanumber)
        {
            //string[,] matrix = new string[1, 5];
            //matrix[0, 0] = "Foot notes from code";
            //return matrix;
            XmlDocument doc = AnalyticsData.Result.Data;
            string[,] matrix = null;
            XmlNode metadata = doc.SelectSingleNode(string.Format("/Root/UATableList/Metadata[@tablenumber={0}]/normal/UADoubleMatrix/rows", datanumber));

            if (metadata == null)
            {
                return matrix;
            }
            else
            {
                int rows = metadata.ChildNodes.Count;
                XmlNode temp = doc.SelectSingleNode(string.Format("/Root/UATableList/Metadata[@tablenumber={0}]/normal/UADoubleMatrix/rows/row/columns", datanumber));
                int cols = temp.ChildNodes.Count;
                matrix = new string[rows, cols];
                for (int i = 0; i < rows; ++i)
                {
                    for (int j = 0; j < cols; ++j)
                    {
                        XmlNode tempm = metadata.ChildNodes[i].SelectSingleNode(string.Format("./columns/column[{0}]", j + 1));
                        matrix[i, j] = tempm.InnerText;
                    }
                }
            }
            return matrix;
        }

        public static string[,] GetNotes()//use DOM and get uasummary strings
        {
            //string notes = string.Empty;
            string innertxt = string.Empty;
            XmlDocument doc = AnalyticsData.Result.Data;

            if (doc == null) return null;
            //listnode may be null if UASummary is not returned from R function
            XmlNode listnode = doc.SelectSingleNode(string.Format("/Root/UASummary/UAList"));
            int notescount = listnode != null ? listnode.ChildNodes.Count : 0; // 1 replaced by 0 on 03jul2013
            string[] rowheaders = { "Filepath", "Active Dataset", "?", "?", "?", "Total Vars", "Command", "User.Self", "Elapsed", "?", "?" };//must be equal to notescount
            string[,] uasummary = new string[notescount, 2];

            if (notescount == 0) uasummary = null; //03Jul2013
            if (listnode != null)
            {
                for (int j = 0; j < notescount; ++j)
                { //headers can be set as an attribute in DOM of same UAString.Right now its hardcoded above.

                    XmlNode tempm = listnode.SelectSingleNode(string.Format("./UAString[{0}]", j + 1));
                    innertxt = tempm.InnerText;
                    if (innertxt == null || innertxt.Trim().Length < 1)
                        innertxt = "-none-";
                    //notes = notes + innertxt + "\n";
                    uasummary[j, 0] = rowheaders[j];
                    uasummary[j, 1] = innertxt;
                }
            }
            //return notes;
            return uasummary;
        }

        public static int FlexGridMaxCells;
        //public static int FlexGridMaxRows;
        //public static int FlexGridMaxCols;
        //returns the 2D matrix that contains the data
        public static string[,] GetDataMatrix(int datanumber, out bool[] visiblerows)
        {
            XmlDocument doc = AnalyticsData.Result.Data;

            if (doc == null)//24Dec2015 doc could be null when command fails. App should not crash is its null.
            {
                visiblerows = null;
                return null;
            }
            // Following XML string will only look for UADoubleMatrix so a temp fix is done in RService to make UAIntMatrix to UADoubleMatrix 09Jan2013
            XmlNode rownode = doc.SelectSingleNode(string.Format("/Root/UATableList/UADoubleMatrix[{0}]/rows", datanumber));

            if (rownode == null)
            {
                visiblerows = new bool[1];
                return null;
                //29Apr2014 return new string[1, 1];
            }

            int rows = rownode.ChildNodes.Count;
            XmlNode temp = doc.SelectSingleNode(string.Format("/Root/UATableList/UADoubleMatrix[{0}]/rows/row/columns", datanumber));
            int cols = temp.ChildNodes.Count;

            #region Setting Max Rows ( or Max Cols ) to process for painting in grid.
            int configtotalcells = FlexGridMaxCells; //right now this value is assumed as the number of cells to be shown in Flexgrid
            int actualtotalcell = rows * cols;
            int customMaxRows = configtotalcells / cols;
            //int customMaxRows = FlexGridMaxRows;//set it from options settings in OutputReader
            //int customMaxCols = FlexGridMaxCols;//set it from options settings in OutputReader
            if (actualtotalcell > configtotalcells)// || cols > customMaxCols)
            {
                //Mouse.OverrideCursor = null;
                LargeResultWarningWindow lrww = new LargeResultWarningWindow(rows, customMaxRows, configtotalcells);
                //lrww.RowCount = customMaxRows;//set max row for message
                lrww.ShowDialog();
                //Mouse.OverrideCursor = System.Windows.Input.Cursors.Wait;
                string selectedoption = lrww.KeyPressed;//out of Full Part and Cancel

                switch (selectedoption)
                {
                    case "Full": //show full table
                        break;
                    case "Part": //show partial data
                        rows = customMaxRows;
                        //cols = customMaxCols;
                        break;
                    default:  //cancel out;

                        string[,] cancelmatrix = new string[1, 1];
                        cancelmatrix[0, 0] = "Abort";
                        //cancelmatrix[0, 1] = "";
                        visiblerows = null;
                        return cancelmatrix;
                }
                //Mouse.OverrideCursor = null;
            }

            #endregion
            XmlNode metadata = doc.SelectSingleNode(string.Format("/Root/UATableList/Metadata/crosstab/UADoubleMatrix/rows", datanumber));

            if (metadata != null)
            {
                rows = metadata.ChildNodes.Count;
            }

            if ((AnalyticsData.DataSource != null) && OutputHelper.GetGlobalMacro(string.Format("GLOBAL.{0}.SPLIT", AnalyticsData.DataSource.Name), "Comparegroups") == "TRUE")
            {
                List<string> splitVars = OutputHelper.GetList(string.Format("GLOBAL.{0}.SPLIT.SplitsVars", AnalyticsData.DataSource.Name), "", false);
                int totalvars = 1;

                foreach (string str in splitVars)
                {
                    totalvars = GetFactors(str).Count * totalvars;
                }

                int totalrows = totalvars * rows;
                string[,] matrix = new string[totalrows, cols];
                visiblerows = new bool[totalrows];
                dataMatrixIndex = 0;
                GetSplitsDataMatrix(splitVars.ToArray(), 0, datanumber, matrix, visiblerows);
                return matrix;
            }
            else
            {
                string[,] matrix = new string[rows, cols];
                visiblerows = new bool[rows];

                int j = 0;
                int rownodecounter = 0;

                for (int i = 0; i < rows; ++i)
                {
                    XmlNode node = rownode.ChildNodes[rownodecounter];

                    if (metadata != null)
                    {
                        XmlNode tempm = metadata.ChildNodes[i].SelectSingleNode("./columns/column[2]");

                        if (tempm.InnerText == "0")
                        {
                            continue;
                        }
                    }
                    //visiblerows[i] = true;//comm by Anil19feb2012
                    j = 0;
                    foreach (XmlNode cnode in node.FirstChild.ChildNodes)
                    {
                        if (!cnode.InnerText.Equals("NA"))//if cond. added by Anil 19Feb2012
                            visiblerows[i] = true;
                        if (cnode.InnerText.Contains("bskyCurrentDatasetSplitSliceObj"))//For changing slice object name with current dataset name
                        {
                            string currentDataset = ExpandMacro("%DATASET%");
                            matrix[i, j] = cnode.InnerText.Replace("bskyCurrentDatasetSplitSliceObj", currentDataset);  //currentDataset;
                        }
                        else
                        {
                            matrix[i, j] = cnode.InnerText;
                        }
                        j++;
                    }
                    rownodecounter++;
                }
                return matrix;
            }
        }

        

        /// Get list of colnames. BSkyReturnStructure$Tables[[]]$columnNames
        public static List<string> GetKeepRemoveColNames(int datanumber)
        {
            List<string> ColNames = new List<string>();
            string innertxt = string.Empty;
            XmlDocument doc = AnalyticsData.Result.Data;

            if (doc == null) return null;
            //listnode may be null if $columnNames is not present in BSkyReturnStructure
            //11Jun2017 if there is just one colname in $columnNames then UAString appears instead of UAStringList and 
            // <row> will not appear as its a single item. So data is kept directly inside <UAString> tags for single value.

            //First get the tag inside of ColNames tag. It could be NULL, 1 or 1+
            XmlNode colNameNode = doc.SelectSingleNode(string.Format("/Root/UATableList/ColNames[@tablenumber={0}]", datanumber));

            if (colNameNode != null) //find if 1 or 1+ childnodes
            {
                XmlNode UAStrORList = colNameNode.SelectSingleNode(string.Format("UAStringList"));//is it a list. 1+ $columnNames?

                if (UAStrORList != null)//it is probably 1+ $columnName
                {
                    XmlNode listnode = doc.SelectSingleNode(string.Format("/Root/UATableList/ColNames[@tablenumber={0}]/UAStringList", datanumber));
                    int colcount = listnode != null ? listnode.ChildNodes.Count : 0; // 1 replaced by 0 on 03jul2013

                    if (listnode != null)
                    {
                        for (int j = 0; j < colcount; ++j)
                        { //headers can be set as an attribute in DOM of same UAString.Right now its hardcoded above.

                            XmlNode tempm = listnode.SelectSingleNode(string.Format("row[{0}]", j + 1));
                            innertxt = tempm.InnerText;
                            if (innertxt == null || innertxt.Trim().Length < 1)
                                innertxt = "-none-";

                            ColNames.Add(innertxt);
                        }
                    }
                }
                else //it is probably 1 $columnName
                {
                    UAStrORList = colNameNode.SelectSingleNode(string.Format("UAString"));// it is a single value in $columnNames?
                    if (UAStrORList != null)//its probably 1 $columnName
                    {
                        XmlNode singlenode = doc.SelectSingleNode(string.Format("/Root/UATableList/ColNames[@tablenumber={0}]/UAString", datanumber));
                        int colcount = singlenode != null ? singlenode.ChildNodes.Count : 0; // 1 replaced by 0 on 03jul2013

                        if (singlenode != null)
                        {
                            innertxt = UAStrORList.InnerText;
                            if (innertxt == null || innertxt.Trim().Length < 1)
                                innertxt = "-none-";

                            ColNames.Add(innertxt);
                        }
                    }
                }
            }
            else //if no ColNames tag is in DOM. Means do nothing with cols. Keep them as they are.(no remove no keep list)
            {
                return null;
            }
            return ColNames;
        }

        ////for getting error messages from metadata in DOM. AD 02Mar2012

        public static MetadataTable GetFullMetadataTable(int datanumber, string metadatatabletype)
        {
            XmlDocument doc = AnalyticsData.Result.Data;
            MetadataTable mat = null;
            XmlNode metadata = doc.SelectSingleNode(string.Format("/Root/UATableList/Metadata[@tablenumber={0}]", datanumber));

            if (metadata == null || metadatatabletype == null)
            {
                return mat;
            }
            else
            {
                int rows = 0;
                XmlNode temp = doc.SelectSingleNode(string.Format("/Root/UATableList/Metadata[@tablenumber={0}]/{1}/UAList", datanumber, metadatatabletype));

                if (temp == null)
                    return mat;

                int cols = temp.ChildNodes.Count;

                for (int i = 0; i < cols; i++)
                {
                    if (rows < temp.ChildNodes.Item(i).ChildNodes.Count)
                        rows = temp.ChildNodes.Item(i).ChildNodes.Count;
                }

                mat = new MetadataTable();
                mat.Metadatatable = new MetadataTableRow[rows]; // allocating required number of rows in metadatatable
                for (int k = 0; k < rows; k++)//creating rows objects
                {
                    mat.Metadatatable[k] = new MetadataTableRow();
                }
                for (int i = 0; i < cols; ++i)
                {
                    for (int j = 0; j < temp.ChildNodes.Item(i).ChildNodes.Count; ++j)
                    {
                        MetadataTableRow mtr = mat.Metadatatable[j]; // working in one row of the table

                        string s = temp.ChildNodes.Item(i).ChildNodes.Item(j).InnerText;

                        if (s != null && !s.Trim().Equals("."))
                        {
                            switch (i)
                            {
                                case 0:
                                    mtr.VarIndex = Convert.ToInt16(s);
                                    break;
                                case 1:
                                    switch (s)
                                    {
                                        case "-1":
                                            s = "Error";
                                            break;
                                        case "-2":
                                            s = "Critical Error";
                                            break;
                                        case "1":
                                            s = "Warning";
                                            break;
                                        case "2": // Footer
                                            s = "Footer";
                                            break;
                                        default:
                                            s = "";
                                            break;
                                    }
                                    mtr.InfoType = s;
                                    break;
                                case 2:
                                    mtr.VarName = s;
                                    break;
                                case 3:
                                    mtr.DataTableRow = Convert.ToInt16(s);
                                    break;
                                case 4:
                                    mtr.StartCol = Convert.ToInt16(s);
                                    break;
                                case 5:
                                    mtr.EndCol = Convert.ToInt16(s);
                                    break;
                                case 6:
                                    mtr.BSkyMsg = s;
                                    break;
                                case 7:
                                    mtr.RMsg = s;
                                    break;
                                default:
                                    break;
                            }
                        }
                        else
                        {
                            mtr.VarIndex = 0;
                            mtr.InfoType = "-";
                            mtr.VarName = "-";
                            mtr.DataTableRow = 0;
                            mtr.StartCol = 0;
                            mtr.EndCol = 0;
                            mtr.BSkyMsg = "-";
                            mtr.RMsg = "-";
                        }

                    }//for row
                }//for col
            }
            return mat;
        }

        //reading other metadata tables. ie. metadatatable[2] and [3]
        public static string[,] GetMetaData2(int datanumber, string metadatatabletype)//for getting error messages from metadata in DOM. AD 02Mar2012
        {
            XmlDocument doc = AnalyticsData.Result.Data;
            string[,] matrix = null;
            XmlNode rownode = doc.SelectSingleNode(string.Format("/Root/UATableList/Metadata[@tablenumber={0}]/{1}/UADoubleMatrix/rows", datanumber, metadatatabletype));

            if (rownode == null)
            {
                return matrix;
            }

            int rows = rownode.ChildNodes.Count;
            XmlNode temp = doc.SelectSingleNode(string.Format("/Root/UATableList/Metadata[@tablenumber={0}]/{1}/UADoubleMatrix/rows/row/columns", datanumber, metadatatabletype));
            int cols = temp.ChildNodes.Count;

            matrix = new string[rows, cols];
            int j = 0;
            int rownodecounter = 0;

            for (int i = 0; i < rows; ++i)
            {
                XmlNode node = rownode.ChildNodes[rownodecounter];
                j = 0;
                foreach (XmlNode cnode in node.FirstChild.ChildNodes)
                {
                    matrix[i, j] = cnode.InnerText;
                    j++;
                }
                rownodecounter++;
            }
            return matrix;
        }

        //Returns a string telling the type of metadata///  like: crosstab / normal /( normal for chisq)
        public static string findMetaDataType(int datanumber)
        {
            XmlDocument doc = AnalyticsData.Result.Data;
            XmlNode normal = doc.SelectSingleNode(string.Format("/Root/UATableList/Metadata[@tablenumber={0}]/normal", datanumber));
            XmlNode crosstab = doc.SelectSingleNode(string.Format("/Root/UATableList/Metadata[@tablenumber={0}]/crosstab1", datanumber));
            XmlNode crosstab2 = doc.SelectSingleNode(string.Format("/Root/UATableList/Metadata[@tablenumber={0}]/crosstab2", datanumber));

            if (normal != null)
            {
                return "normal";
            }
            if (crosstab != null)
                return "crosstab1";
            if (crosstab2 != null)
                return "crosstab2";
            return null;
        }

        public static string[,] GetBSkyErrorsWarning(int datanumber, string metadatatabletype) //25Jun2013 for errors and warning outside analytic return results.
        {
            XmlDocument doc = AnalyticsData.Result.Data;

            if (doc == null) return null;

            string[,] matrix = null;
            XmlNode metadata = null;
            XmlNode errwarncount = doc.SelectSingleNode("/Root/UATableList/BSkyErrorWarn");

            if (errwarncount == null)
                return null;
            if (datanumber == 0) //25jun2013 if we do not know what is the table number for BAkyErrorWarning.
            {
                string tno = doc.SelectSingleNode("/Root/UATableList/BSkyErrorWarn/@tablenumber").Value;

                if (tno != null)
                    datanumber = Int16.Parse(tno);

                metadata = doc.SelectSingleNode(string.Format("/Root/UATableList/BSkyErrorWarn[@tablenumber={0}]", datanumber));//tcount
            }
            else
            {
                metadata = doc.SelectSingleNode(string.Format("/Root/UATableList/BSkyErrorWarn[@tablenumber={0}]", datanumber)); //before 25jun2013 this was the only line there was no 'if' above
            }

            // Table not found in DOM 
            if (metadata == null || metadatatabletype == null)
            {
                return matrix;
            }
            else// Table found in DOM and datanumber is already set
            {
                int rows = 0;
                XmlNode temp = doc.SelectSingleNode(string.Format("/Root/UATableList/BSkyErrorWarn[@tablenumber={0}]/{1}/UAList", datanumber, metadatatabletype));

                if (temp == null)
                    return matrix;

                int cols = temp.ChildNodes.Count;

                for (int i = 0; i < cols; i++)
                {
                    if (rows < temp.ChildNodes.Item(i).ChildNodes.Count)
                        rows = temp.ChildNodes.Item(i).ChildNodes.Count;
                }

                //use commented code to get all the info, instead of just getting RMsg and User Msg
                matrix = new string[rows, 3];//matrix = new string[rows, cols];

                int k = 0;

                for (int i = 0; i < cols; ++i)
                {
                    if ((i == 1) || (i == 6) || (i == 7))
                    {
                        for (int j = 0; j < temp.ChildNodes.Item(i).ChildNodes.Count; ++j)
                        {
                            //XmlNode tempm = metadata.ChildNodes[i].SelectSingleNode(string.Format("./normal/UAString[{0}]", j + 1));////fix this 02Mar2012
                            string s = temp.ChildNodes.Item(i).ChildNodes.Item(j).InnerText;

                            if (s != null && !s.Trim().Equals("."))
                            {
                                if (i == 1)
                                {
                                    switch (s)
                                    {
                                        case "-1":
                                            s = "Error:";
                                            break;
                                        case "-2":
                                            s = "Critical Error:";
                                            break;
                                        case "1":
                                            s = "Warning:";
                                            break;
                                        default:
                                            s = "";
                                            break;
                                    }
                                }
                                else if (i == 3)//row
                                { s = "Row " + s; }
                                else if (i == 4)//start
                                { s = "From col " + s; }
                                else if (i == 5)//ends
                                { s = "To col " + s; }

                                if (i == 6) s = "User Msg: " + s;

                                //use commented code to get all the info, instead of just getting RMsg and User Msg
                                if ((i == 1) || (i == 6) || (i == 7)) matrix[j, k] = s;//matrix[j, i] = s;
                            }
                            else
                            {
                                //use commented code to get all the info, instead of just getting RMsg and User Msg
                                if ((i == 1) || (i == 6) || (i == 7)) matrix[j, k] = "";//matrix[j, i] = "";//space char
                            }
                        }//row for
                        k++;
                    }//if
                }//col for
            }
            return matrix;
        }

        public static int GetStatsTablesCount() //23Aug2013 Stat result table count
        {
            int count = 0;
            XmlDocument doc = AnalyticsData.Result.Data;

            if (doc == null) return 0;

            XmlNodeList usrrescount = doc.SelectSingleNode("/Root/UATableList").ChildNodes;//29Apr2014

            if (usrrescount == null)
                return 0;

            count = doc.SelectSingleNode("/Root/UATableList").ChildNodes.Count;//29Apr2014
            return count;
        }

        public static int GetUserTablesCount() // User's result table count
        {
            int count = 0;
            XmlDocument doc = AnalyticsData.Result.Data;

            if (doc == null)
                return 0;

            XmlNode usrrescount = doc.SelectSingleNode("/Root/UATableList/UserResult/UserData");

            if (usrrescount == null)
                return 0;

            count = doc.SelectNodes("/Root/UATableList/UserResult/UserData").Count;
            return count;
        }

        //01May2014 Get table header from DOM if any
        public static string GetBSkyStatTableHeader(int datanumber)
        {
            string tabletitle = string.Empty;
            XmlDocument doc = AnalyticsData.Result.Data;
            XmlNode tablehead = null;//doc.SelectSingleNode(string.Format("/Root/UATableList/UADoubleMatrix"), datanumber);

            XmlNodeList xnl = doc.SelectSingleNode("/Root/UATableList").ChildNodes;//29Apr2014

            if (xnl != null && xnl.Count > 0)
                tablehead = xnl[datanumber - 1];// datanumber =1 that mean I need find index 0
            if (tablehead != null)
            {
                if (tablehead.SelectSingleNode("tableheader") != null)
                    tabletitle = tablehead.SelectSingleNode("tableheader").InnerText.Replace("&gt;", ">").Replace("&lt;", "<").Replace("&ge;", ">=").Replace("&le;", "<=");
            }
            return tabletitle;
        }

        //23Aug2013 For stat results. like row col header if needed add one more param for table title.
        public static object GetBSkyStatResults(int datanumber, out string restype, out string[] colHeaders, out string[] rowHeaders, out string slicename)
        {
            restype = "";
            colHeaders = null; 
            rowHeaders = null; 
            slicename = string.Empty;

            bool[] visibleRows; // not using it yet
            /// Find col headers row headers and title(if exits)
            XmlDocument doc = AnalyticsData.Result.Data;
            XmlNode rowcolhead = null;//doc.SelectSingleNode(string.Format("/Root/UATableList/UADoubleMatrix"), datanumber);

            XmlNodeList xnl = doc.SelectSingleNode("/Root/UATableList").ChildNodes;//29Apr2014

            if (xnl != null && xnl.Count > 0)
                rowcolhead = xnl[datanumber - 1];// datanumber =1 that mean I need find index 0
            if (rowcolhead != null)
            {
                if (rowcolhead.SelectSingleNode("colheaders") != null)
                    colHeaders = rowcolhead.SelectSingleNode("colheaders").InnerText.Replace("&gt;", ">").Replace("&lt;", "<").Replace("&ge;", ">=").Replace("&le;", "<=").Split(','); //comma separated string to Array
                if (rowcolhead.SelectSingleNode("rowheaders") != null)
                    rowHeaders = rowcolhead.SelectSingleNode("rowheaders").InnerText.Replace("&gt;", ">").Replace("&lt;", "<").Replace("&ge;", ">=").Replace("&le;", "<=").Split(','); //comma separated string to Array
                if (rowcolhead.SelectSingleNode("slicename") != null)
                    slicename = rowcolhead.SelectSingleNode("slicename").InnerText.Replace("&gt;", ">").Replace("&lt;", "<").Replace("&ge;", ">=").Replace("&le;", "<=");
            }

            //01Feb2016 substitute long slice name with datasetname in row/col header
            ChangeSlinameToDatasetNameInHeader(ref colHeaders);
            ChangeSlinameToDatasetNameInHeader(ref rowHeaders);

            slicename = SetSliceComponentOrder(slicename);
            // Table title if exists /// no code written for this yet

            string[,] BSkyStatData = GetDataMatrix(datanumber, out visibleRows);// table data
            return BSkyStatData;
        }

        //01Feb2016 Replace long slice name with current dataset name in flexgrid table headers
        private static void ChangeSlinameToDatasetNameInHeader(ref string[] header)
        {
            if (header == null)
                return;

            string currentDataset = ExpandMacro("%DATASET%");

            for (int i = 0; i < header.Length; i++)
            {
                if (header[i].Contains("bskyCurrentDatasetSplitSliceObj"))//For changing slice object name with current dataset name
                {
                    header[i] = header[i].Replace("bskyCurrentDatasetSplitSliceObj", currentDataset);  //currentDataset;
                }
            }
        }

        //04Sep2013 this function is only meant to be called from GetBSkyStatResults
        public static string SetSliceComponentOrder(string slicename)
        {
            int indexoffirstcomma = slicename.IndexOf(',');
            int indexofseccomma = slicename.IndexOf(',', indexoffirstcomma + 1);

            if (indexoffirstcomma < 1 || indexofseccomma < 1) 
                return string.Empty;

            string itrcount = slicename.Substring(indexoffirstcomma + 1, indexofseccomma - indexoffirstcomma - 1);//parts[1];
            string spliton = slicename.Substring(indexofseccomma + 1);

            return "Split Info : " + spliton.Trim() + ". " + itrcount.Trim() + ".";
        }

        public static object GetBSkyResults(int datanumber, out string restype, out string[] colHeaders, out string[] rowHeaders) //25Jun2013 for reslts those are at the end of return structure. May contain string, tables, array.
        {
            XmlDocument doc = AnalyticsData.Result.Data;
            colHeaders = null;
            rowHeaders = null;
            XmlNode metadata = null;
            XmlNode nextlvl = null;
            restype = "unknown";
            if (datanumber < 1)
                return null;
            metadata = doc.SelectSingleNode(string.Format("/Root/UATableList/UserResult/UserData[@tablenumber={0}]", datanumber)); //before 25jun2013 this was the only line there was no 'if' above
            if (metadata.HasChildNodes)
            {
                nextlvl = metadata.FirstChild;
            }
            // Table not found in DOM 
            if (metadata == null)
            {
                return null;
            }
            else if (nextlvl.Name == "UAString") //UAString
            {
                restype = "string";
                if (nextlvl.InnerText == null)
                    return null;
                return nextlvl.InnerText;
            }
            else if (nextlvl.Name == "UAStringList") //string array
            {
                restype = "stringlist";
                if (nextlvl.InnerText == null)
                    return null;
                string[] slist = new string[nextlvl.ChildNodes.Count];
                int ind = 0;

                foreach (XmlNode n in nextlvl.ChildNodes)
                {
                    slist[ind++] = n.InnerText;
                }
                return slist;
            }

            else if (nextlvl.Name == "UADataFrame" || nextlvl.Name == "UADoubleMatrix") // .... data.frame and matrix
            {
                restype = (nextlvl.Name == "UADataFrame") ? "dataframe" : "matrix";
                colHeaders = nextlvl.SelectSingleNode("colheaders").InnerText.Split(','); //comma separated string to Array
                rowHeaders = nextlvl.SelectSingleNode("rowheaders").InnerText.Split(','); //comma separated string to Array
                if (colHeaders != null && colHeaders.Length == 1 && colHeaders[0].Trim().Length == 0)
                    colHeaders = null;
                if (rowHeaders != null && rowHeaders.Length == 1 && rowHeaders[0].Trim().Length == 0)
                    rowHeaders = null;
                return getMatrix(nextlvl.SelectSingleNode("rows"));
            }
            else if (nextlvl.Name == "UAList" || nextlvl.Name == "UAIntList")// UAList  .... 
            {
                restype = "matrix";
                colHeaders = nextlvl.SelectSingleNode("colheaders").InnerText.Split(','); //comma separated string to Array
                rowHeaders = nextlvl.SelectSingleNode("rowheaders").InnerText.Split(','); //comma separated string to Array
                if (colHeaders != null && colHeaders.Length == 1 && colHeaders[0].Trim().Length == 0)
                    colHeaders = null;
                if (rowHeaders != null && rowHeaders.Length == 1 && rowHeaders[0].Trim().Length == 0)
                    rowHeaders = null;
                return getListToMatrix(nextlvl.SelectSingleNode("rows"));
            }
            return null;
        }

        // converts same sized two or more lists to matrix /// data.frame
        public static string[,] getListToMatrix(XmlNode grandparentfromleaf) //grand parent from leaf node is to be passed
        {
            int rows = 0;
            string[,] matrix = null;
            int cols = grandparentfromleaf.ChildNodes.Count;

            if (cols == 0)
                return matrix;
            for (int i = 0; i < cols; i++)
            {
                if (rows < grandparentfromleaf.ChildNodes.Item(i).ChildNodes.Count)
                    rows = grandparentfromleaf.ChildNodes.Item(i).ChildNodes.Count;
            }

            matrix = new string[rows, cols];
            for (int i = 0; i < cols; ++i)
            {
                for (int j = 0; j < grandparentfromleaf.ChildNodes.Item(i).ChildNodes.Count; ++j)
                {
                    string s = grandparentfromleaf.ChildNodes.Item(i).ChildNodes.Item(j).InnerText;

                    if (s != null && !s.Trim().Equals("."))
                    {
                        matrix[j, i] = s;
                    }
                    else
                    {
                        matrix[j, i] = "";//space char
                    }
                }
            }
            return matrix;
        }

        // creates matrix out of row/col DOM structure
        public static string[,] getMatrix(XmlNode xmlrows) // rows contains multi row. each row contains multi cols
        {
            string[,] matrix = null;
            int rows = xmlrows.ChildNodes.Count;
            int cols = xmlrows.FirstChild.FirstChild.ChildNodes.Count; // rows > row > columns > col1..col3...colN

            matrix = new string[rows, cols];
            for (int i = 0; i < rows; ++i)
            {
                XmlNode row = xmlrows.ChildNodes[i];

                for (int j = 0; j < cols; ++j)
                {
                    string s = row.FirstChild.ChildNodes.Item(j).InnerText;

                    if (s != null && !s.Trim().Equals("."))
                    {
                        matrix[i, j] = s;
                    }
                    else
                    {
                        matrix[i, j] = "";//space char
                    }
                }
            }
            return matrix;
        }

        public static void Reset()
        {
            if (MacroList == null)
                MacroList = new Dictionary<string, string>();
            else
                MacroList.Clear();
        }

        public static AnalyticsData AnalyticsData
        {
            get;
            set;
        }

        public static event EventHandler FactorsNeeded;

        public static List<string> GetList(string listname, string varname, bool factors)
        {
            if (factors)
            {
                string strvar = OutputHelper.ExpandMacro(varname);
                return OutputHelper.GetFactors(strvar);
            }

            if (AnalyticsData == null || AnalyticsData.InputElement == null)
            {
                return null;
            }
            object obj = AnalyticsData.InputElement.FindName(listname);

            if (obj == null)
            {
                string[] parts = listname.Split('.');

                if (parts[0] == "GLOBAL")
                {
                    FrameworkElement fe = GetGlobalMacro(parts[0] + "." + parts[1] + "." + parts[2], string.Empty) as FrameworkElement;
                    obj = fe.FindName(parts[3]);
                }
            }
            if (obj != null)
            {
                List<string> temp = new List<string>();

                if (typeof(ListBox).IsAssignableFrom(obj.GetType()))//if its ListBox. Old code moved from above 'if'
                {
                    ListBox list = obj as ListBox;

                    if (list != null)
                    {
                        temp.Clear();
                        foreach (object o in list.Items)
                        {
                            temp.Add(o.ToString());
                        }
                    }
                }
                if (typeof(TextBox).IsAssignableFrom(obj.GetType()))//05Mar2013 if its TextBox. New code
                {
                    TextBox tb = obj as TextBox;

                    if (tb != null)
                    {
                        temp.Clear();
                        temp.Add(tb.Text);
                    }
                }
                return temp;
            }
            if (obj == null)//for error messages. 29Nov2012
            {

            }
            return null;
        }

        public static List<string> GetFactors(string variablename)
        {
            //26Apr2016 Crosstab fix for variable name having special chars eg. filter_.
            // var.Name is changed to var.RName
            var variable = from var in AnalyticsData.DataSource.Variables
                           where var.RName == variablename
                           select var;
            DataSourceVariable dv = variable.FirstOrDefault();

            if (dv != null)
            {
                List<string> temp = new List<string>();
                temp.AddRange(dv.Values);
                temp.Remove("<NA>");// (".");//remove NAs. Anil 29Feb2012
                return temp;
            }
            else
            {
                List<string> temp = new List<string>();
                temp.Add(variablename + "X");
                temp.Add(variablename + "Y");
                return temp;
            }
        }

        public static bool Evaluate(string conditon)
        {
            if (OutputHelper.ExpandMacro(conditon) != conditon)
            {
                bool isTrue = false;

                if (bool.TryParse(OutputHelper.ExpandMacro(conditon), out isTrue))
                {
                    return isTrue;
                }
                else
                    return false;
            }
            if (AnalyticsData == null || AnalyticsData.InputElement == null)
            {
                return true;
            }

            ///// Checking multiple &&  or multiplpe || condition ////
            string condtype = CheckMulticondition(conditon);

            if (condtype.Length > 0) // multi condition exists
            {
                return GetMultiConditionResult(conditon);
            }
            else // single condition
            {
                return GetUIElementResult(conditon);
            }
            //return true;
        }

        //Right now only && or || condition can be checked. Mix of && and || is not entertained.
        public static string CheckMulticondition(string condition)//18Sep2013
        {
            string conditiontype = string.Empty; // no multi condition

            if ((condition.Contains("AND") && !condition.Contains("OR")))
            {
                conditiontype = "&&";
            }
            else if ((condition.Contains("OR") && !condition.Contains("AND")))
            {
                conditiontype = "||";
            }
            else if ((condition.Contains("OR") && condition.Contains("AND")))
            {
                conditiontype = "&|";
            }
            return conditiontype;
        }

        private static bool GetMultiConditionResult(string mcondition)
        {
            string tempstr = " " + mcondition.Replace("AND", " & ").Replace("OR", " | ") + " ";
            string[] operands; // queue of operands
            //string[] operators; // queue of operators
            char[] separators = { '&', '|' };
            operands = tempstr.Split(separators);
            bool UIresult;

            foreach (string opr in operands)
            {
                UIresult = GetUIElementResult(opr);
                // replacing each UI element names with true/false based on UIresult
                tempstr = tempstr.Replace(opr.Trim(), UIresult.ToString());
            }

            //adding spaces around operators
            tempstr = tempstr.Replace("&", " & ").Replace("|", " | "); // adding spaces again as ealier ones should already be gone.


            do
            {
                tempstr = tempstr.Replace("  ", " ");//replace multispace by single space
            } while (tempstr.Contains("  "));

            // now evaluate true & true to true. Basically replace in mcondition.
            //// & processor
            do
            {
                tempstr = tempstr.Replace("True & True", "True").Replace("True & False", "False").Replace("False & True", "False").Replace("False & False", "False");
            } while (tempstr.Contains('&'));
            //// | processor
            do
            {
                tempstr = tempstr.Replace("True | True", "True").Replace("True | False", "True").Replace("False | True", "True").Replace("False | False", "False");
            } while (tempstr.Contains('|'));

            if (tempstr.Trim().Equals("True"))
                return true;
            else
                return false;
        }

        public static bool GetUIElementResult(string condition)
        {
            object obj = AnalyticsData.InputElement.FindName(condition.Trim()); // trim important here

            if (obj != null && typeof(CheckBox).IsAssignableFrom(obj.GetType()))
            {
                CheckBox chkbox = obj as CheckBox;
                return chkbox.IsChecked.HasValue ? chkbox.IsChecked.Value : false;
            }

            else if (obj != null && typeof(RadioButton).IsAssignableFrom(obj.GetType()))
            {
                RadioButton txt = obj as RadioButton;
                return txt.IsChecked.HasValue ? txt.IsChecked.Value : false;
            }

            else if (obj != null && typeof(ListBox).IsAssignableFrom(obj.GetType()))
            {
                ListBox lb = obj as ListBox;
                return lb.Items.Count > 0 ? true : false;
            }
            else if (condition != null && condition.Trim().Length == 0) // for condition="".// think and modify if needed
            {
                return true;
            }

            return false; // no such element in dialog
        }
    }
}