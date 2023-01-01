using CloudDrop.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CloudDrop.View.Features
{
    public class FeaturesClass
    {
        public Content content;
        public List<string> allNameFiles;
        public FeaturesClass(Content Content, List<string> AllNameFiles) 
        {
            content = Content;
            allNameFiles = AllNameFiles;
        }
        public void OpenFeatures()
        {
            FeaturesWindow m_window = new FeaturesWindow(content, allNameFiles);
            m_window.Title = content.name;
            m_window.Activate();
        }
    }
}
