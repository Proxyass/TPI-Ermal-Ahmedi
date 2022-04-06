using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ErmalTpi.Utilities
{
    internal class TraitementUID
    {
        // Cette class permet de faire le reverse de l'UID pour allez faire une recherche sur la DB avec le bon code UID
        // J'ai trouver cette class sur internet : {lien}

        public static string ChangementUIDGrouperPardeux(string val)
        {
            if (string.IsNullOrEmpty(val?.Trim()))
                return null;

            var n1 = string.Empty;
            for (var i = 0; i < val.Count(); i++)
            {
                if (i > 0 && (i % 2) == 0)
                    n1 += "|";
                n1 += val[i];
            }

            var n2 = n1.Split('|').Reverse();
            return string.Join("", n2);

        }
    }
}
