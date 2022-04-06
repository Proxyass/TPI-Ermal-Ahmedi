using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ErmalTpi.Model
{
    public class User
    {
        public int UserId { get; set; }
        public string Nom1 { get; set; }
        public string Nom2 { get; set; }
        public string Prenom1 { get; set; }
        public string Prenom2 { get; set; }
        public DateTime Date_Naissance { get; set; } = DateTime.Now;
    }
}
