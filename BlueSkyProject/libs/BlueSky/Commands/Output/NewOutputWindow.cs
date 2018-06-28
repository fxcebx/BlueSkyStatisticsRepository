﻿using BSky.Lifetime;
using BlueSky.CommandBase;
using BSky.Interfaces.Interfaces;

namespace BlueSky.Commands.Output
{
    class NewOutputWindow : BSkyCommandBase
    {
        protected override void OnPreExecute(object param)
        {
        }

        protected override void OnExecute(object param)
        {
            ////OutputWindow ow = (LifetimeService.Instance.Container.Resolve<IOutputWindow>()) as OutputWindow;
            //////IUIController controller = (LifetimeService.Instance.Container.Resolve<IUIController>();
            //Window1 window = LifetimeService.Instance.Container.Resolve<Window1>();//App's main window

            ///// Get the reference of the output window container  /////
            OutputWindowContainer owc = (LifetimeService.Instance.Container.Resolve<IOutputWindowContainer>()) as OutputWindowContainer;
            IOutputWindow iow = new OutputWindow(); // create new output window
            ///  add new output window to the window container. This window will become active window
            owc.AddOutputWindow(iow);

            //owc.ActiveOutputWindow = iow;//setting as default
            //Window temp = iow as Window;
            //temp.Owner = window;
            //temp.Show();

        }

        protected override void OnPostExecute(object param)
        {
        }

        ////Send executed command to output window. So, user will know what he executed
        //protected override void SendToOutputWindow(string command, string title)//13Dec2013
        //{
        //    #region Get Active output Window
        //    //////// Active output window ///////
        //    OutputWindowContainer owc = (LifetimeService.Instance.Container.Resolve<IOutputWindowContainer>()) as OutputWindowContainer;
        //    OutputWindow ow = owc.ActiveOutputWindow as OutputWindow; //get currently active window
        //    #endregion
        //    ow.AddMessage(command, title);
        //}

    }
}
