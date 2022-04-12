using MySql.Data.MySqlClient;
using ErmalTpi.Model;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ErmalTpi.Utilities
{
    public static class DataContext
    {
        // Initialisation de connection sur les DB_Interne (1) & Db_Externe (2)
        public static MySqlConnection InterneConnection => new MySqlConnection(ConfigurationManager.ConnectionStrings["InterneConnection"].ConnectionString);
        public static MySqlConnection ExterneConnection => new MySqlConnection(ConfigurationManager.ConnectionStrings["ExterneConnection"].ConnectionString);
        #region Traitement sur BDD interne avec UID correspondant
        public static User GetInterneUser(string Id)
        {
            // Je déclare la variable UID => TB : no_carte , Entité : no_mifare
            string UID = string.Empty;

            //ObservableCollection<string> data = new ObservableCollection<string>();
            using (var con = InterneConnection)
            {

                // Ouverture de connexion
                con.Open();
                
                // Requete qui va afficher l'UID selon ce que l'utilisateur à mis dans le champs @no_mifare
                string query = "Select no_carte from stock_carte where no_mifare=@no_mifare";

                //Envoi de la commande et des parametre de connexion
                using (var command = new MySqlCommand(query, con))
                {
                    //Ajout du parametre a la command avec le type de string
                    command.Parameters.Add("no_mifare", MySqlDbType.String).Value = Id;

                    //Execute le lecteure
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            //Lecture du UID
                            UID = reader[0].ToString();
                        }
                    }
                }
                //Si l'UID n'est pas NULL ou vide alors
                if (!string.IsNullOrEmpty(UID))
                {
                    //création de la command
                    query = "Select nom,prenom,date_naissance from utilisateur where no_carte=@no_carte";
                    //On récupère l'user dans la DB1
                    using (var command = new MySqlCommand(query, con))
                    {

                        //Ajout du parametre dans la command
                        command.Parameters.Add("no_carte", MySqlDbType.String).Value = UID;
                        //Execute the command reader
                        using (var reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                //Return de user
                                return new User()
                                {
                                    Nom1 = reader.GetString(0),
                                    Prenom1 = reader.GetString(1),
                                };
                            }
                        }
                    }
                }
            }
            return null;
        }
        #endregion
        #region Test si l'user à son pass Covid + de 180 jours
        internal static bool IsUserVacMoreThanOneEightyDays(User user)
        {
            DateTime dateCert = DateTime.Now;
            //Connextion a la DB2
            using (var con = ExterneConnection)
            {
                //Ouverture de connexion
                con.Open();
                var query = "Select date_cert from user_vac where id_user=@id_user";
                using (var command = new MySqlCommand(query, con))
                {
                    //Ajout du parametre a la command
                    command.Parameters.Add("id_user", MySqlDbType.Int64).Value = user.UserId;
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            // On stock la date dans dateCert
                            dateCert = DateTime.Parse(reader[0].ToString());
                            DateTime d1 = DateTime.Now;
                            DateTime d2 = dateCert.Date;

                            TimeSpan t = d1 - d2;
                            if (t.TotalDays >= 180)
                            {
                                return true;
                            }
                        }
                    }
                }
            }
            return false;
        }
        #endregion
         
        #region Affichage sur la DataGrid depuis DB_Externe
        //ObservableCollection<T>(List<T>)	Initialise une nouvelle instance de la classe ObservableCollection<T> qui contient des éléments copiés à partir de la liste spécifiée
        internal static ObservableCollection<User> GetUsers(User user)
        {
            //decalre and initialize user collection
            ObservableCollection<User> users = new ObservableCollection<User>();
            //Get db2 connection
            using (var con = ExterneConnection)
            {
                //Ouverture de la connexion
                con.Open();
                //Je récupère l'ID , nom1, nom2, prenom 1, prenom2 , date de naissance -> sur la table utilisateur avec les paramètre : nom1, prenom1
                string query = "Select id_user,nom1,nom2,prenom1,prenom2,date_naissance from utilisateur where nom1=@nom1 and prenom1=@prenom1";
                //Create command
                using (var command = new MySqlCommand(query, con))
                {
                    //Add parameters to the command
                    command.Parameters.Add("nom1", MySqlDbType.String).Value = user.Nom1;
                    command.Parameters.Add("prenom1", MySqlDbType.String).Value = user.Prenom1;
                    //Execute the reader
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            // J'ajoute tous le utilisateurs dans la collection
                            users.Add(
                                new User()
                                {
                                    UserId = reader.GetInt32(0),  
                                    Nom1 = reader.GetString(1),
                                    Nom2 = reader.GetString(2),
                                    Prenom1 = reader.GetString(3),
                                    Prenom2 = reader.GetString(4),
                                    Date_Naissance = reader.GetDateTime(5)
                                }
                                );
                        }
                    }
                }
            }

            return users;
        }
        #endregion

        #region Recherche Manuelle
        internal static ObservableCollection<User> GetUsersManually(User user)
        {
            //decalre and initialize user collection
            ObservableCollection<User> users = new ObservableCollection<User>();
            using (var con = ExterneConnection)
            {
                //open the connection
                con.Open();
                // Ici c'est le requete avec la recherche manuelle
                // Je sélectionne : id_user,nom1,nom2,prenom1,prenom2,date_naissance -> utilisateur avec les parametre nom1 , prenom1 et la date de naissance
                string query = "Select id_user,nom1,nom2,prenom1,prenom2,date_naissance from utilisateur where nom1=@nom1 and prenom1=@prenom1 and date_naissance=@date_naissance";
                //Create command
                using (var command = new MySqlCommand(query, con))
                {
                    //Add parameters to the command
                    command.Parameters.Add("nom1", MySqlDbType.String).Value = user.Nom1;
                    command.Parameters.Add("prenom1", MySqlDbType.String).Value = user.Prenom1;
                    command.Parameters.Add("date_naissance", MySqlDbType.Date).Value = user.Date_Naissance;
                    //Execute the reader
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            // J'ajoute tous les utilsateurs dans la collection
                            users.Add(
                                new User()
                                {
                                    UserId = reader.GetInt32(0),
                                    Nom1 = reader.GetString(1),
                                    Nom2 = reader.GetString(2),
                                    Prenom1 = reader.GetString(3),
                                    Prenom2 = reader.GetString(4),
                                    Date_Naissance = reader.GetDateTime(5)
                                }
                                );
                        }
                    }
                }
            }

            return users;
        }
        #endregion

    }
}




