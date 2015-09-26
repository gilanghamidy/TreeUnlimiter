using System;
using System.IO;
using System.Xml.Serialization;
using UnityEngine;

namespace TreeUnlimiter
{
    public class Configuration
    {

        public bool DebugLogging = false;
        public byte DebugLoggingLevel = 0;
        public bool UseNoWindEffects = false;

        public static void Serialize(string filename, Configuration config)
        {
            var serializer = new XmlSerializer(typeof(Configuration));
            try
            {
                using (var writer = new StreamWriter(filename))
                {
                    serializer.Serialize(writer, config);
                }
            }
            catch (Exception ex1)
            {
                Debug.Log("[TreeUnlimter:ConfiguationSeralize] Had a problem saving the config file  Error: " + ex1.Message.ToString());
            }
        }


        public static Configuration Deserialize(string filename)
        {
            var serializer = new XmlSerializer(typeof(Configuration));

            try
            {
                using (var reader = new StreamReader(filename))
                {
                    var config = (Configuration)serializer.Deserialize(reader);
                    return config;
                }
            }
            catch (Exception ex1)
            {
                Debug.Log("[TreeUnlimter:ConfiguationDeseralize] Could not find configuration file, a new one will be generated." + ex1.Message.ToString());
            }

            return null;
        }
    }
}
