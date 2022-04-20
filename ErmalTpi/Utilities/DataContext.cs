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
        #region ConnextionDB -> Interne, Erxterne & Historique
        // Initialisation de connexion sur les DB_Interne (1) & Db_Externe (2)
        public static MySqlConnection InterneConnection => new MySqlConnection(ConfigurationManager.ConnectionStrings["InterneConnection"].ConnectionString);
        public static MySqlConnection ExterneConnection => new MySqlConnection(ConfigurationManager.ConnectionStrings["ExterneConnection"].ConnectionString);
        public static MySqlConnection HistoriqueConnection => new MySqlConnection(ConfigurationManager.ConnectionStrings["HistoriqueConnection"].ConnectionString);
        #endregion
        // -------------------------------------------------------------------------------------------------------------------------------------------------
        // -------------------------------------------------------------------------------------------------------------------------------------------------
        // -------------------------------------------------------------------------------------------------------------------------------------------------
        // Je récupère l'utilisateur dans la DB interne
        #region Cette méthode récupère l'user depuis la db_interne via l'UID

        public static User GetInterneUser(string Id)
        {

            // Je déclare la variable UID => TB : no_carte , Entité : no_mifare
            string UID = string.Empty;

            //ObservableCollection<string> data = new ObservableCollection<string>();
            using (var con = InterneConnection)
            {

                // Ouverture de la la connexion
                con.Open();

                // Requete qui va afficher l'UID selon ce que l'utilisateur à mis dans le champs @no_mifare
                string query = "Select no_carte from stock_carte where no_mifare=@no_mifare";

                //Envoi de la commande et des parametre de connexion
                using (var command = new MySqlCommand(query, con))
                {
                    //Ajout du parametre a la commande
                    command.Parameters.Add("no_mifare", MySqlDbType.String).Value = Id;
                    //Execution du lecteur
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            //Lecture de l'UID
                            UID = reader[0].ToString();
                        }
                    }
                }

                //Si l'UID est pas NULL ou vide alors ->
                if (!string.IsNullOrEmpty(UID))
                {
                    //Je séléctionne le nom , prenom , date de naissance selon le numéro de carte
                    query = "Select nom,prenom,date_naissance from utilisateur where no_carte=@no_carte";
                    //and get user from db1
                    using (var command = new MySqlCommand(query, con))
                    {

                        //Ajout du paramètre a la commande
                        command.Parameters.Add("no_carte", MySqlDbType.String).Value = UID;
                        //Execute la commande de lecture
                        using (var reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                //Return l'utilisateur
                                return new User()
                                {
                                    Nom1 = reader.GetString(0),
                                    Prenom1 = reader.GetString(1),
                                    UID = UID
                                };
                            }
                        }
                    }
                }

            }

            return null;
        }

        internal static (int _capacity, ObservableCollection<User> _users) LoadData()
        {
            List<string> _usersId = new List<string>();
            string _capacity = string.Empty;
            ObservableCollection<User> _users = new ObservableCollection<User>();
            //ObservableCollection<string> data = new ObservableCollection<string>();
            using (var con = HistoriqueConnection)
            {
                // Ouverture de la la connexion
                con.Open();

                // Requete pour 
                string query = "Select * from startedevents limit 1";

                //Envoi de la commande et des parametre de connexion
                using (var command = new MySqlCommand(query, con))
                {

                    //Execution du lecteur
                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                            _capacity = reader[1].ToString();
                    }
                }
                if (_capacity == string.Empty)
                {
                    return (0, new ObservableCollection<User>());
                }
                query = "Select * from historique";
               

                using (var command = new MySqlCommand(query, con))
                {
                    //Execute le reader
                    using (var reader = command.ExecuteReader())
                    {
                        // J'ajoute l'UID , Nom1, Prenom1 , Type d'entrée , date de naissance
                        while (reader.Read())
                        {
                            _users.Add(new User()
                            {
                                UID = reader.GetString(3),
                                Nom1 = reader.GetString(1),
                                Prenom1 = reader.GetString(2),
                                EntryType = reader.GetInt16(5),
                                Date_Naissance = reader.GetDateTime(7)
                            });


                        }
                    }
                }

            }


            return (int.Parse(_capacity), _users);
        }
        #region Cette partie permet d'ajouter , de update la capacité de l'événement
        public static bool InsertOrUpdateCapacity(int capacity)
        {
            try
            {
                using (var con = HistoriqueConnection)
                {
                    //Ouverture de la connexion
                    con.Open();
                    //UPDATE EVENT CAPACITER
                    string query = "UPDATE startedevents SET capacity =@capacity LIMIT 1;";
                    //Création de la commande
                    using (var command = new MySqlCommand("Select * from startedevents limit 1", con))
                    {
                        //Ajouts des paramètres dans la commande
                        using (var reader = command.ExecuteReader())
                        {
                            if (!reader.Read())
                                query = "insert into startedevents (capacity) values(@capacity)";

                        }
                    }
                    //Création de la commande
                    using (var command = new MySqlCommand(query, con))
                    {
                        //Ajouts des paramètres dans la commande
                        command.Parameters.Add("capacity", MySqlDbType.String).Value = capacity;
                        command.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception)
            {
                return false;
            }
            return true;
        }
        #endregion

        #region This method is for getting events
        internal static ObservableCollection<Event> GetEvents()
        {
            //decalre and initialize user collection
            ObservableCollection<Event> events = new ObservableCollection<Event>();
            //Get db2 connection
            using (var con = HistoriqueConnection)
            {
                //Ouverture de la connexion
                con.Open();
                //Requete qui va afficher les événements
                string query = "Select * from tb_event";
                //Ajout du parametre a la command avec le type de string
                using (var command = new MySqlCommand(query, con))
                {
                    //Execute le lecteure
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            // J'ajoute tous le utilisateurs dans la collection
                            events.Add(
                                new Event()
                                {
                                    EventId = reader.GetInt32(0),
                                    EventName = reader.GetString(1),
                                }
                                );
                        }
                    }
                }
            }

            return events;
        }
        #endregion

        #region Cette methode récupère l'utilisateur via id_user depuis user_vac et vérifie son date_cert
        internal static bool IsUserVacMoreThanOneEightyDays(User user)
        {
            DateTime dateCert = DateTime.Now;
            //Connextion a la DB2
            using (var con = ExterneConnection)
            {

                // Ouverture de la la connexion MySql
                con.Open();

                //  Rqs de la date du certificat covid
                string query = "Select date_cert from user_vac where id_user=@id_user";

                //Envoi de la commande et des parametre de connexion
                using (var command = new MySqlCommand(query, con))
                {
                    //Ajout du paramètre a la commande
                    command.Parameters.Add("id_user", MySqlDbType.Int64).Value = user.UserId;
                    //Executtion du lecteur
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            // On stock la date dans dateCert
                            dateCert = DateTime.Parse(reader[0].ToString());
                            //On récupère la date actuelle
                            DateTime d1 = DateTime.Now;
                            //On récupère la date
                            DateTime d2 = dateCert.Date;
                            TimeSpan t = d1 - d2;
                            //Comparaison de la date
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
        #region Affichage sur la DataGrid depuis DB_Extern
        internal static ObservableCollection<User> GetUsers(User user)
        {
            //déclaration et initialisation de la collection d'utilisateurs
            ObservableCollection<User> users = new ObservableCollection<User>();
            //Récupère les user de la db_externe
            using (var con = ExterneConnection)
            {
                //Ouverture de la connexion
                con.Open();
                //Je récupère l'ID , nom1, nom2, prenom 1, prenom2 , date de naissance -> sur la table utilisateur avec les paramètre : nom1, prenom1
                string query = "Select id_user,nom1,nom2,prenom1,prenom2,date_naissance from utilisateur where nom1=@nom1 and prenom1=@prenom1";
                //Création de la commande
                using (var command = new MySqlCommand(query, con))
                {
                    //Ajout du paramètre a la commande
                    command.Parameters.Add("nom1", MySqlDbType.String).Value = user.Nom1;
                    command.Parameters.Add("prenom1", MySqlDbType.String).Value = user.Prenom1;
                    //Execute le reader
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
        #region Cette methode sert a ajouter une nouvelle entré user dans la table historique
        [Obsolete]
        internal static void InsertUser(User user, int eventId, bool isOut = false)
        {
            using (var con = HistoriqueConnection)
            {
                //Ouverture de la connexion
                con.Open();
                // Ici c'est le requete avec la recherche manuelle
                // Je sélectionne : id_user,nom1,nom2,prenom1,prenom2,date_naissance -> utilisateur avec les parametre nom1 , prenom1 et la date de naissance
                string query = "insert into historique (NomUsr,Prenomusr,no_carte" +
                               ",DateHeure,Type_entry,tb_event_idtb_event,Date_Naissance) value(@" +
                               "NomUsr,@Prenomusr,@no_carte,@DateHeure,@Type_entry,@tb_event_idtb_event,@Date_Naissance)";
                //Création de la commande
                using (var command = new MySqlCommand(query, con))
                {
                    //Ajouts des paramètres dans la commande
                    command.Parameters.Add("NomUsr", MySqlDbType.String).Value = user.Nom1;
                    command.Parameters.Add("Prenomusr", MySqlDbType.String).Value = user.Prenom1;
                    command.Parameters.Add("no_carte", MySqlDbType.String).Value = user.UID ?? " ";
                    command.Parameters.Add("DateHeure", MySqlDbType.String).Value = DateTime.Now;
                    command.Parameters.Add("Type_entry", MySqlDbType.String).Value = isOut == false ? 0 : 1;
                    command.Parameters.Add("tb_event_idtb_event", MySqlDbType.Int32).Value = eventId;
                    command.Parameters.Add("Date_Naissance", MySqlDbType.Datetime).Value = user.Date_Naissance;
                    command.ExecuteNonQuery();
                }
            }
        }
        #endregion
    }
}
#endregion
