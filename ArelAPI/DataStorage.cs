using System;
using System.Collections.Generic;
using System.IO;
using System.IO.IsolatedStorage;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArelAPI
{
    public static class DataStorage
    {

        //--------------------enregistrer dans un fichier... -----------------------------------------

        public static void saveData(string key, string data)//ecriture normale
        {
            using (IsolatedStorageFileStream stream = new IsolatedStorageFileStream(key, FileMode.Create, IsolatedStorageFile.GetUserStoreForApplication()))
            {
                using (StreamWriter writer = new StreamWriter(stream))
                {
                    writer.Write(data);
                }
            }
        }

        public static string getData(string key)
        {
            string res;
            if (isset(key))
            {
                using (IsolatedStorageFileStream stream = new IsolatedStorageFileStream(key, FileMode.Open, IsolatedStorageFile.GetUserStoreForApplication()))
                {
                    using (StreamReader reader = new StreamReader(stream))
                    {
                        res = reader.ReadToEnd();
                    }
                }
                return res;
            }
            return null;
        }

        public static bool isset(string key)
        {
            IsolatedStorageFile racine = IsolatedStorageFile.GetUserStoreForApplication();
            return racine.FileExists(key);
        }

        public static void clearData()
        {
            IsolatedStorageFile.GetUserStoreForApplication().Dispose();
        }

    }
}
