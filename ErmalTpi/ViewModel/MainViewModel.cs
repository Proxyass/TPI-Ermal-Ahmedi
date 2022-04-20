using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using ErmalTpi.Model;
using ErmalTpi.Utilities;
using PCSC;
using PCSC.Iso7816;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Media;


namespace ErmalTpi.ViewModel
{
    /// <summary>
    /// This class contains properties that the main View can data bind to.
    /// <para>
    /// Use the <strong>mvvminpc</strong> snippet to add bindable properties to this ViewModel.
    /// </para>
    /// <para>
    /// You can also use Blend to data bind with the tool's support.
    /// </para>
    /// <para>
    /// See http://www.galasoft.ch/mvvm
    /// </para>
    /// </summary>
    public class MainViewModel : ViewModelBase
    {
        /// <summary>
        /// Initializes a new instance of the MainViewModel class.
        /// </summary>
        //Command for button clicks
        //UID à stocker
        public ObservableCollection<User> UsersIn { get; set; }=new ObservableCollection<User>();
        //Total des utilisateurs qui sont sortis
        private int _totalOut=0;


        public int TotalOut
        {
            get { return _totalOut; }
            set { _totalOut = value;RaisePropertyChanged("TotalOut"); }
        }

        private ObservableCollection<Event> _events;

        public ObservableCollection<Event> Events
        {
            get { return _events; }
            set { _events = value; RaisePropertyChanged("Events"); }
        }
        private Event _selectedEvent;

        public Event SelectedEvent
        {
            get { return _selectedEvent; }
            set { _selectedEvent = value; }
        }



        //Commande pour les clics de bouton
        public RelayCommand<string> Command { get; set; }

        //User UID
        private string _uid;

        public string UID
        {
            get { return _uid; }
            set { _uid = value; RaisePropertyChanged("UID"); }
        }

        //Background pour les cas utilisateurs
        private Brush _backColor=Brushes.Transparent;

        public Brush BackColor
        {
            get { return _backColor; }
            set { _backColor = value;RaisePropertyChanged("BackColor"); }
        }

        //Background pour les certificat COVID
        private Brush _CovidColor = Brushes.Transparent;

        public Brush CovidColor
        {
            get { return _CovidColor; }
            set { _CovidColor = value; RaisePropertyChanged("CovidColor"); }
        }


        private string _TextBlockee;

        public string TextBlockee
        {
            get { return _TextBlockee; }
            set { _TextBlockee = value; RaisePropertyChanged("TextBlockee"); }
        }

        private string _OutText;

        public string OutText
        {
            get { return _OutText; }
            set { _OutText = value; RaisePropertyChanged("OutText"); }
        }




        //Couleur pour le carte reader (Background)
        private Brush _readerBackColor = Brushes.Transparent;

        public Brush ReaderBackColor 
        {
            get { return _readerBackColor; }
            set { _readerBackColor = value;RaisePropertyChanged("ReaderBackColor"); }
        }
        //TOus les utilisateurs
        private ObservableCollection<User> _users;
        public ObservableCollection<User> Users
        {
            get { return _users; }
            set { _users = value; RaisePropertyChanged("Users"); }
        }
        //Propriété de la capacité de l'événement
        private int _roomCapacity;

        public int RoomCapacity
        {
            get { return _roomCapacity; }
            set { _roomCapacity = value;RaisePropertyChanged("RoomCapacity"); if (value != 0) DataContext.InsertOrUpdateCapacity(value); }
        }


        #region Selecteduser (Binding -> SelectedUser.{ARGS}
        private User _selectUser;

        public User SelectedUser
        {
            get { return _selectUser; }
            set { _selectUser = value;RaisePropertyChanged("SelectedUser"); }
        }
        #endregion


        public MainViewModel()
        {
            SelectedUser = new User();
            (int _capacity, ObservableCollection<User> _users) = DataContext.LoadData();
            foreach (var item in _users.Distinct())
            {
                if (_users.Where(x => x.Nom1 == item.Nom1 && x.Prenom1 == item.Prenom1 && x.Date_Naissance == item.Date_Naissance).Count() % 2 == 1
                    &&UsersIn.Where(x => x.Nom1 == item.Nom1 && x.Prenom1 == item.Prenom1 && x.Date_Naissance == item.Date_Naissance&&x.EntryType==item.EntryType).FirstOrDefault()==null
                    &&item.EntryType==0)
                {
                    UsersIn.Add(item);
                }
            }
            RaisePropertyChanged("UsersIn");
            TotalOut = _users.Where(x => x.EntryType == 1).Count();
            RoomCapacity = _capacity;
            //Initialisation de l'user
            Users = new ObservableCollection<User>();
            //Initialisation de la command
            Command = new RelayCommand<string>(PerformAction);
            //Lecture de la carte si le lecteur est connecté
            StartReadingCard();
            Events = DataContext.GetEvents();
            // Initialisation du text (Validité)
            TextBlockee = "Validité du certificat";
            OutText = "";
        }

        #region lecture de la carts + détection si il y'a le lecteur connecté ou pas (https://github.com/danm-de/pcsc-sharp/blob/master/Examples/ISO7816-4/Transmit/Program.cs)
        string LastUID = "";
        private void StartReadingCard()
        {
            Task.Run(() =>
            {
                while (true)
                {
                try
                {
                    using (var context = ContextFactory.Instance.Establish(SCardScope.System))
                    {
                        var readerNames = context.GetReaders();
                        // Ici on test si il y'a pas de lecteurs , affichage en rouge
                        if (NoReaderFound(readerNames))
                        {
                            ReaderBackColor = Brushes.Red;
                            MessageBox.Show("Pas de lecteur détecté, veuillez brancher le lecteur ou appeler le service informatique.");
                           return;
                            }
                        // Lecteur détecté affichage en vert
                        if(ReaderBackColor!=Brushes.Green)
                        ReaderBackColor = Brushes.Green;

                        // J'ai constaté avec la console que si il y'a qu'un seul lecteur il a le numéro 0
                        var readerName = readerNames[0];

                        // Test pour vérifié si il y'a plusieurs lecteurs.
                        var readerNamePlus = readerNames.Count();
                            // Alors j'affiche le message + changement de couleurs
                            if (readerNamePlus > 1)
                        {
                            ReaderBackColor = Brushes.Orange;
                            MessageBox.Show("Il y'a plusieurs lecteurs merci de laisser qu'un seul lecteur");
                            }
                        using (var rfidReader = context.ConnectReader(readerName, SCardShareMode.Shared, SCardProtocol.Any))
                        {
                            var apdu = new CommandApdu(IsoCase.Case2Short, rfidReader.Protocol)
                            {
                                CLA = 0xFF,
                                Instruction = InstructionCode.GetData,
                                P1 = 0x00,
                                P2 = 0x00,
                                Le = 0 // We don't know the ID tag size
                            };

                            using (rfidReader.Transaction(SCardReaderDisposition.Leave))
                            {
                                Console.WriteLine("Retrieving the UID .... ");

                                var sendPci = SCardPCI.GetPci(rfidReader.Protocol);
                                var receivePci = new SCardPCI(); // IO returned protocol control information.

                                var receiveBuffer = new byte[256];
                                var command = apdu.ToArray();


                                var bytesReceived = rfidReader.Transmit(
                                    sendPci, // Protocol Control Information (T0, T1 or Raw)
                                    command, // command APDU
                                    command.Length,
                                    receivePci, // returning Protocol Control Information
                                    receiveBuffer,
                                    receiveBuffer.Length); // data buffer

                                var responseApdu =
                                    new ResponseApdu(receiveBuffer, bytesReceived, IsoCase.Case2Short, rfidReader.Protocol);
                                string test = BitConverter.ToString(responseApdu.GetData());
                                // Suppresion des "-" par du vide.
                                test = test.Replace("-", string.Empty);
                                // Appel de la classe Reverser 
                                string id = TraitementUID.ChangementUIDGrouperPardeux(test);
                                    if (id == LastUID)
                                    {
                                        continue;
                                    }
                                    LastUID = id;
                                    Application.Current.Dispatcher.Invoke(() => 
                                    {
                                        var userFound = UsersIn.FirstOrDefault(x => x.UID == id);
                                        if (userFound !=null)
                                        {
                                            UsersIn.Remove(userFound);
                                            TotalOut++;
                                            DataContext.InsertUser(userFound,SelectedEvent.EventId,true);
                                        }
                                        else
                                        {
                                            UID = id;
                                            PerformAction("SearchByUID");
                                        }
                                    });

                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                        if(ex.Message.Contains("The smart card has been removed"))
                        LastUID = string.Empty;
                }


                    Thread.Sleep(1000);
                }
            });
        }
        #endregion
        #region (CAS) Cette commande permet d'effectuer des actions en fonction des boutons pressés par l'utilisateur. (CAS)
        private void PerformAction(string obj)
        {
            switch (obj)
            {
                //Recherche de l'utilisateur par UID
                case "SearchByUID":
                    //Si l'UID est vide alors on affiche le message
                    if (string.IsNullOrEmpty(UID))
                    {
                        MessageBox.Show("Veuillez renseigné l'UID SVP.");
                    }
                    else if (UsersIn.FirstOrDefault(x => x.UID == UID) != null) return;
                    else
                    {
                        //Obtenir l'utilisateur de db_Interne
                        SelectedUser = DataContext.GetInterneUser(UID);
                        if (SelectedUser != null)
                        {
                            //Récupérer les utilisateurs de la base de données et les afficher dans le datagrid
                            GetDB2Users();

                        }
                        else
                        {
                            Users = new ObservableCollection<User>();
                            SelectedUser = new User();
                            //s'il n'y a pas d'utilisateur dans db_Interne alors ont la couleur en rouge.
                            BackColor = Brushes.Red;
                            OutText = "Accèss refuser";
                        }
                    }
                    break;
                    //Recherche de l'utilisateur par nom, prénome et date de naissance
                case "SearchManually":
                    //Test si les champs Nom1 Prenom1 son vide
                    if (string.IsNullOrEmpty(SelectedUser.Nom1)||string.IsNullOrEmpty(SelectedUser.Prenom1))
                    {
                        // Alors on affiche le message suivant
                        MessageBox.Show("Merci de remplir complétement le formulaire de recherche");
                        return;
                    }
                    //Recherche manuelle d'un utilisateur et affichage dans datagrid
                    GetDB2UsersManually();
                    break;
                   //Obtenir la date du panier de l'utilisateur et comparer
                case "GerUserCertDate":

                    CheckCert_Date();
                    break;
                    //Lecture de carte
                case "ReadCard":
                    ReadCard();
                    break;

                default:
                    break;
            }
        }
        #endregion
        #region lecture de la carts + détection si il y'a le lecteur connecté ou pas (https://github.com/danm-de/pcsc-sharp/blob/master/Examples/ISO7816-4/Transmit/Program.cs)
        public void ReadCard()
        {
            try
            {
            using (var context = ContextFactory.Instance.Establish(SCardScope.System))
            {
                var readerNames = context.GetReaders();
                // Ici on test si il y'a pas de lecteurs , affichage en rouge
                if (NoReaderFound(readerNames))
                {
                     ReaderBackColor = Brushes.Red;
                    return ;
                }
                    // Lecteur détecté affichage en vert
                    ReaderBackColor = Brushes.Green;

                    // J'ai constaté avec la console que si il y'a qu'un seul lecteur il a le numéro 0
                    var readerName = readerNames[0];

                    // Test pour vérifié si il y'a plusieurs lecteurs.
                    var readerNamePlus = readerNames.Count();
                    if(readerNamePlus > 1)
                    {
                        ReaderBackColor = Brushes.Orange;
                        MessageBox.Show("Il y'a plusieurs lecteurs merci de laisser qu'un seul lecteur");
                        return ;
                   }
                using (var rfidReader = context.ConnectReader(readerName, SCardShareMode.Shared, SCardProtocol.Any))
                {
                    var apdu = new CommandApdu(IsoCase.Case2Short, rfidReader.Protocol)
                    {
                        CLA = 0xFF,
                        Instruction = InstructionCode.GetData,
                        P1 = 0x00,
                        P2 = 0x00,
                        Le = 0 // We don't know the ID tag size
                    };

                    using (rfidReader.Transaction(SCardReaderDisposition.Leave))
                    {
                        Console.WriteLine("Retrieving the UID .... ");

                        var sendPci = SCardPCI.GetPci(rfidReader.Protocol);
                        var receivePci = new SCardPCI(); // IO returned protocol control information.

                        var receiveBuffer = new byte[256];
                        var command = apdu.ToArray();


                        var bytesReceived = rfidReader.Transmit(
                            sendPci, // Protocol Control Information (T0, T1 or Raw)
                            command, // command APDU
                            command.Length,
                            receivePci, // returning Protocol Control Information
                            receiveBuffer,
                            receiveBuffer.Length); // data buffer

                        var responseApdu =
                            new ResponseApdu(receiveBuffer, bytesReceived, IsoCase.Case2Short, rfidReader.Protocol);

                        Console.WriteLine("SW1: {0:X2}, SW2: {1:X2}\nUid: {2}",
                        responseApdu.SW1, responseApdu.SW2, responseApdu.HasData ? BitConverter.ToString(responseApdu.GetData()) : "No uid received");


                        string test = BitConverter.ToString(responseApdu.GetData());
                        // Suppresion des "-" par du vide.
                        test = test.Replace("-", string.Empty);
                      // Appel de la classe Reverser 
                      UID = TraitementUID.ChangementUIDGrouperPardeux(test);
                    }
                }
            }
               PerformAction("SearchByUID");
            }
            catch (Exception)
            {
            }

        }
        #endregion
        #region Vérification si un lecteur est connecté ou non
        // Ici on compte le nombre de lecteur dispo
        private static bool NoReaderFound(ICollection<string> readerNames) =>
            readerNames == null || readerNames.Count < 1;
        #endregion
        #region Récupération des utilisateurs de la base de données filtrés par nom, prenom et date de naissance.
        private void GetDB2Users()
        {
            //Ont récupère les users
            Users = DataContext.GetUsers(SelectedUser);
            //changer de couleur en conséquence
            ChangeColor();
        }
        #endregion
        #region Obtenir les utilisateurs manuellement
        private void GetDB2UsersManually()
        {
            var userFound = UsersIn.FirstOrDefault(x => x.Nom1 == SelectedUser.Nom1 && x.Nom2 == SelectedUser.Nom2 && x.Date_Naissance == SelectedUser.Date_Naissance);
            if (userFound != null)
            {
                //S'il est déjà présent, alors on le retire
                UsersIn.Remove(userFound);
                DataContext.InsertUser(SelectedUser, SelectedEvent.EventId, false);

                TotalOut++;
                return;
            }
            //Obtenir les utilisateurs manuellement
            Users = DataContext.GetUsersManually(SelectedUser);
            if (Users.Count is 0)
                SelectedUser = new User();
            else
            SelectedUser = Users.First();
            //changement de couleur en conséquence
            ChangeColor();
        }
        #endregion
        #region Changement de couleurs selon le cas
        private void ChangeColor()
        {
            //Si la liste des utilisateurs est vide
            if (!Users.Any())
            {
                BackColor = Brushes.Red;
            }
            //S'il n'y a qu'un seul utilisateur, alors
            else if (Users.Count == 1)
            {
                SelectedUser = Users.First();
                //Obtenir la date de son certificat et vérifier
                CheckCert_Date();

            }
            //s'il y a plus d'un utilisateur, alors laissez l'utilisateur en choisir un.
            else if (Users.Count > 1)
            {
                SelectedUser = Users.First();
                BackColor = Brushes.Orange;
            }
        }
        #endregion
        #region Cette méthode vérifie la date du certificat COVID, si elle est supérieure à 180 jours ou non.
        // Vérification du 
        private void CheckCert_Date()
        {
            //Get whether his vac date is more than 180 days or not
            var isMoretThan80 = DataContext.IsUserVacMoreThanOneEightyDays(SelectedUser);
            //if it is true then the color will be red and his access will be denied
            if(isMoretThan80 == true)
            {
                // Le sons accès refusé
                BackColor = Brushes.Tomato;
                CovidColor = Brushes.Red;
                TextBlockee = "Certificat NON valide";
                System.Media.SoundPlayer playerrr = new System.Media.SoundPlayer(@"C:\Program Files (x86)\sons\denied.wav");
                playerrr.Play();
                OutText = "Entrée refuser";
            }

            else
            {
                //Vérification si l'utilisateur est déjà dedans ou non
                var userFound = UsersIn.FirstOrDefault(x => x.Nom1 == SelectedUser.Nom1 && x.Nom2 == SelectedUser.Nom2 && x.Date_Naissance == SelectedUser.Date_Naissance);
                if (userFound!= null)
                {
                    //Si l'utilisateur est déjà dedans alors on le supprime (SORTI)
                    UsersIn.Remove(userFound);
                    userFound.UID = UID;
                    DataContext.InsertUser(userFound, SelectedEvent.EventId,true);
                    BackColor = Brushes.Tomato;
                    TotalOut++;
                    OutText = "Sorti";
                }
                //Vérifiction de la capacité
                else if (RoomCapacity <= UsersIn.Count)
                {
                    MessageBox.Show("La capacité maximal pour cette événement à été atteint.");
                }
                //  autrement il peut accéder et la couleur sera verte

                else
                {
                    var _user = new User()
                    {
                        Date_Naissance = SelectedUser.Date_Naissance,
                        Nom1 = SelectedUser.Nom1
                ,
                        Nom2 = SelectedUser.Nom2,
                        UID = UID,
                        Prenom1 = SelectedUser.Prenom1,
                        Prenom2 = SelectedUser.Prenom2
                ,
                        UserId = SelectedUser.UserId
                    };
                UsersIn.Add(_user);
                RaisePropertyChanged("UsersIn");
                    DataContext.InsertUser(_user, SelectedEvent.EventId,false);
                // Le sons accès autorisé
                BackColor = Brushes.RoyalBlue;
                CovidColor = Brushes.Green;
                TextBlockee = "Certificat Valide";
                OutText = "Entrée OK";
                System.Media.SoundPlayer playerr = new System.Media.SoundPlayer(@"C:\Program Files (x86)\sons\success.wav");
                playerr.Play();
            }
                }
        }
        #endregion
    }


}
