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
    /// <summary>
    /// Допметоды для работы с конфигом
    /// </summary>
    public static class ConfigUtils
    {
        /// <summary>
        /// Загрузка конфига из файла
        /// </summary>
        /// <typeparam name="T">Класс обьекта конфигурации</typeparam>
        /// <param name="plugin">Ссылка на текущий плагин</param>
        /// <param name="fileName">Имя файла конфигурации</param>
        /// <returns></returns>
        public static T Load<T>(TorchPluginBase plugin, string fileName) where T : new()
        {
            ///получаем путь к файлу конфига
            string path = Path.Combine(plugin.StoragePath, fileName);
            T data = new T();
            if (File.Exists(path))
            {
                ///если файл конфига существует - читаем с конвертацией в T
                using (StreamReader streamReader = new StreamReader(path))
                    data = (T)new XmlSerializer(typeof(T)).Deserialize((TextReader)streamReader);
            }
            else
                ///если файла нет - создаем(первый запуск торча с плагином)
                ConfigUtils.Save<T>(plugin, data, fileName);
            return data;
        }

        /// <summary>
        /// Метод для сохранения файла конфига
        /// </summary>
        /// <typeparam name="T">Класс обьекта конфигурации</typeparam>
        /// <param name="plugin">Ссылка на текущий плагин</param>
        /// <param name="data">Обьект конфигурации</param>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public static bool Save<T>(TorchPluginBase plugin, T data, string fileName) where T : new()
        {
            try
            {
                ///пишем конфиг в файл
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
