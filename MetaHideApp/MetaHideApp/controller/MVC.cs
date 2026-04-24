using MetaHide.model;
using System.Windows.Forms;
using View = MetaHide.view.View;

namespace MetaHide.controller
{
    internal class MVC
    {
        public Model Model { get; set; }
        public Controller Controller { get; set; }
        public View View { get; set; }
        public Form Form { get; set; }

        public MVC()
        {
            Model = new Model();
            Form = new Form();
            View = new View(Model, Form);
            Controller = new Controller(View, Model);
        }

        public void Run()
        {
            Application.Run(Form);
        }
    }
}