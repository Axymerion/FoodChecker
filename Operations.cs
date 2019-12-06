using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FoodChecker
{
    class Operations
    {
        public static List<string> FilterList(List<foodItem> InputList, string KeyString)
        {
            List<string> temp = new List<string>();
            for (int i = 0; i < InputList.Count; i++)
            {
                bool found = false;
                if (KeyString.Length > InputList[i].name.Length)
                {
                    continue;
                }
                for (int j = 0; j <= InputList[i].name.Length - KeyString.Length && !found; j++)
                {
                    int k;
                    for (k = 0; k < KeyString.Length; k++)
                    {
                        if (InputList[i].name[k + j] != KeyString[k]) break;
                    }
                    if (k == KeyString.Length)
                        found = true;
                }
                if (found)
                    temp.Add(InputList[i].name);
            }
            return temp;
        }

    }
}
