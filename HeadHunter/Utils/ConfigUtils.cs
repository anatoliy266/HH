using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Torch;

namespace HeadHunter.Utils
{
    public static class ConfigUtils
    {
        public static T Load<T>(TorchPluginBase plugin, string fileName) where T : new()
        {
            string path = Path.Combine(plugin.StoragePath, fileName);
            T data = new T();
            if (File.Exists(path))
            {
                using (StreamReader streamReader = new StreamReader(path))
                    data = (T)new XmlSerializer(typeof(T)).Deserialize((TextReader)streamReader);
            }
            else
                ConfigUtils.Save<T>(plugin, data, fileName);
            return data;
        }

        public static bool Save<T>(TorchPluginBase plugin, T data, string fileName) where T : new()
        {
            try
            {
                using (StreamWriter streamWriter = new StreamWriter(Path.Combine(plugin.StoragePath, fileName)))
                    new XmlSerializer(typeof(T)).Serialize((TextWriter)streamWriter, (object)data);
                return true;
            }
            catch (Exception ex)
            {
            }
            return false;
        }
    }
}
