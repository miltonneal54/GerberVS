using Cnc;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Web;

namespace Config
{
    public class ParseException : Exception
    {
        public ParseException(string message) : base(message) { }
    }
    public class Configuration
    {
        static string cd = Environment.CurrentDirectory;
        private static char[] comment_start = { '#', '/', '+', '-' };
        protected static void read_config_lines(TextReader tf, ConfigDictionary dict)
        {
            ReadOnlySpan<char> comment_start_span = comment_start;
            while (true)
            {
                string line = tf.ReadLine();
                if (line == null)
                    return;
                line = line.Trim();
                if (comment_start_span.IndexOf(line[0]) >= 0)
                    continue;
                int idx = line.IndexOf(':');
                if (idx >= 1)
                {

                    string key = line.Substring(0, idx).Trim().ToString();
                    if (dict.ContainsKey(key))
                        throw new ParseException("the same configuration appears more than once in the config file");
                    string val = line.Substring(idx + 1).Trim().ToString();
                    dict.Add(key, val);
                }
            }
        }
        public static ConfigDictionary ReadConfig(string path)
        {
            TextReader config_file = null;

            try
            {
                config_file = new StreamReader(path??cd+"\\config.ini");
                ConfigDictionary config_dict = new ConfigDictionary();
                read_config_lines(config_file, config_dict);
                return config_dict;



            }
            catch (FileNotFoundException)
            {
            }
            finally
            {
                if (config_file != null)
                    config_file.Close();
            }
            return null;
        }

    }

    public class ConfigDictionary
    {
        private readonly Dictionary<string, string> dict;
        public ConfigDictionary()
        {
            dict = new Dictionary<string, string>();
        }
        public bool ContainsKey(string s)
        {
            return dict.ContainsKey(s);
        }
        public void Add(string key, string val)
        {
            dict.Add(key, val);
        }
        public void IntGetValue(string key, ref int value, bool optional = false)
        {
            string val = "";
            if (get_key(key, ref val, optional))
            {
                try
                {
                    value = Int32.Parse(val);
                }
                catch (Exception)
                {
                    var pe = new ParseException(String.Format("Value: {0} is not a valid integer value for setting {1}", val, key));
                    throw pe;
                }
            }
        }
        public void DoubleGetValue(string key, ref double value, bool optional = false)
        {
            string val = "";
            if (get_key(key, ref val, optional))
            {
                try
                {
                    value = Double.Parse(val);
                }
                catch (Exception)
                {
                    var pe = new ParseException(String.Format("Value: {0} is not a valid floating point value for setting {1}", val, key));
                    throw pe;
                }
            }
        }
        public void BooleanGetValue(string key, ref bool value, bool optional = false)
        {
            string val = "";
            if (get_key(key, ref val, optional))
            {
                try
                {
                    value = Boolean.Parse(val);
                }
                catch (Exception)
                {
                    var pe = new ParseException(String.Format("Value: {0} is not a valid Boolean value for setting {1}", val, key));
                    throw pe;
                }
            }
        }
        public void string_get_value(string key, ref string value, bool optional = false)
        {
            get_key(key, ref value, optional);
        }

        private bool get_key(string key, ref string value, bool optional)
        {
            string val = "";
            try
            {
                if (optional && !dict.ContainsKey(key))
                {
                    return false;
                }
                val = dict[key];
                value = val;
                return true;
            }
            catch (KeyNotFoundException)
            {
                var pe = new ParseException(String.Format("setting {0} is missing from config.ini", key));
                throw pe;
            }
        }
    }
}