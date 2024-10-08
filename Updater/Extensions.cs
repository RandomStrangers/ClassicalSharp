﻿/*
    Copyright 2011 MCForge
    
    Author: fenderrock87
    
    Dual-licensed under the    Educational Community License, Version 2.0 and
    the GNU General Public License, Version 3 (the "Licenses"); you may
    not use this file except in compliance with the Licenses. You may
    obtain a copy of the Licenses at
    
    http://www.opensource.org/licenses/ecl2.php
    http://www.gnu.org/licenses/gpl-3.0.html
    
    Unless required by applicable law or agreed to in writing,
    software distributed under the Licenses are distributed on an "AS IS"
    BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express
    or implied. See the Licenses for the specific language governing
    permissions and limitations under the Licenses.
 */
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;

namespace Updater
{
    /// <summary> Converts an object into a string. </summary>
    public delegate string StringFormatter<T>(T value);

    public static class Extensions
    {

        const StringComparison comp = StringComparison.OrdinalIgnoreCase;
        public static bool CaselessEq(this string a, string b) { return a.Equals(b, comp); }
        public static bool CaselessContains(this string a, string b) { return a.IndexOf(b, comp) >= 0; }

        public static bool CaselessContains(this List<string> items, string value)
        {
            foreach (string item in items)
            {
                if (item.Equals(value, comp)) return true;
            }
            return false;
        }

        public static bool CaselessContains(this string[] items, string value)
        {
            for (int i = 0; i < items.Length; i++)
            {
                if (items[i].Equals(value, comp)) return true;
            }
            return false;
        }

        public static bool CaselessRemove(this List<string> items, string value)
        {
            for (int i = 0; i < items.Count; i++)
            {
                if (!items[i].Equals(value, comp)) continue;
                items.RemoveAt(i); return true;
            }
            return false;
        }

        public static int CaselessIndexOf(this List<string> items, string value)
        {
            for (int i = 0; i < items.Count; i++)
            {
                if (items[i].Equals(value, comp)) return i;
            }
            return -1;
        }
    }
}
