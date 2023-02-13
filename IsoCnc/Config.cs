/*  Copyright (C) 2022-2023 Patrick H Dussud <Patrick.Dussud@outlook.com>
    *** Acknowledgments to Gerbv Authors and Contributors. ***

    Redistribution and use in source and binary forms, with or without
    modification, are permitted provided that the following conditions
    are met:

    1. Redistributions of source code must retain the above copyright
       notice, this list of conditions and the following disclaimer.
    2. Redistributions in binary form must reproduce the above copyright
       notice, this list of conditions and the following disclaimer in the
       documentation and/or other materials provided with the distribution.
    3. Neither the name of the project nor the names of its contributors
       may be used to endorse or promote products derived from this software
       without specific prior written permission.

    THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND
    ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE
    IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE
    ARE DISCLAIMED.  IN NO EVENT SHALL THE COPYRIGHT OWNER OR CONTRIBUTORS BE LIABLE
    FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL
    DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS
    OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION)
    HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT
    LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY
    OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF
    SUCH DAMAGE.
 */
using System;
using System.Collections.Generic;
using System.IO;

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
                if (line.Length == 0)
                    continue;
                if (comment_start_span.IndexOf(line[0]) >= 0)
                    continue;
                int idx = line.IndexOf(':');
                if (idx >= 1)
                {

                    string key = line.Substring(0, idx).Trim().ToString();
                    if (dict.ContainsKey(key))
                        throw new ParseException("the same configuration appears more than once in the config file");
                    string val = line.Substring(idx + 1).Trim();
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

        public static bool UpdateConfig(string path, ConfigDictionary update)
        {
            TextReader tf = null;
            TextWriter uf = null;
            try
            {
                tf = File.OpenText(path ?? cd + "\\config.ini");
                HashSet<string> keys = new HashSet<string>();
                var lines = new List<string>();
                ReadOnlySpan<char> comment_start_span = comment_start;
                while (true)
                {
                    string line = tf.ReadLine();
                    if (line == null)
                        break;
                    line = line.Trim();
                    if ((line.Length != 0) && (comment_start_span.IndexOf(line[0]) < 0))
                    {
                        int idx = line.IndexOf(':');
                        if (idx >= 1)
                        {

                            string key = line.Substring(0, idx).Trim().ToString();
                            if (update.ContainsKey(key))
                            {
                                keys.Add(key);
                                var val = "";
                                update.get_key(key, ref val, false);
                                line = (key + " : " + val);

                            }
                        }
                    }
                    lines.Add(line);
                }
                tf.Close();
                tf = null;
                //add all remaining keys
                foreach (var keyvalue in update.dict)
                {
                    if (!keys.Contains(keyvalue.Key))
                    {
                        lines.Add(keyvalue.Key + " : " + keyvalue.Value);
                    }
                }
                //rewrite the file 
                uf = new StreamWriter(new FileStream(path ?? cd + "\\config.ini", FileMode.Truncate, FileAccess.Write, FileShare.None));
                foreach (var line in lines)
                {
                    uf.WriteLine(line);
                }
            }
            catch (FileNotFoundException)
            {
                return false;
            }
            finally
            {
                tf?.Close();
                uf?.Close();
            }
            return true;
        }

    }

    public class ConfigDictionary
    {
        internal readonly Dictionary<string, string> dict;
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
            string val = "";
            bool found = get_key(key, ref val, optional);
            if (found)
            {
                val = val.Replace("\\n", "\n");
                val = val.Replace("\\r", "\r");
                value = val.Replace("\\t", "\t");
            }
        }

        internal bool get_key(string key, ref string value, bool optional)
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