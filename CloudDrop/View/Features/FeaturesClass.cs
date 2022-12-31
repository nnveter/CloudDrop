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
        public FeaturesClass(Content Content) 
        {
            content = Content;
        }
        public void OpenFeatures()
        {
            FeaturesWindow m_window = new FeaturesWindow(content);
            m_window.Title = content.name;
            m_window.Activate();
        }
    }
}
