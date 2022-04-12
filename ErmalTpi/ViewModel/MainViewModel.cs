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
        public RelayCommand<string> Command { get; set; }

        //User UID
        private string _uid;

        public string UID
        {
            get { return _uid; }
            set { _uid = value; RaisePropertyChanged("UID"); }
        }

        private string _TextBlockee;

        public string TextBlockee
        {
            get { return _TextBlockee; }
            set { _TextBlockee = value; RaisePropertyChanged("TextBlockee"); }
        }
        

        //Couleur de fond pour l'utilisateur
        private Brush _backColor = Brushes.Transparent;


        public Brush BackColor
        {
            get { return _backColor; }
            set { _backColor = value; RaisePropertyChanged("BackColor"); }
        }


       
        //Couleur de fond du lecteur de carte
        private Brush _readerBackColor = Brushes.Transparent;

        public Brush ReaderBackColor
        {
            get { return _readerBackColor; }
            set { _readerBackColor = value; RaisePropertyChanged("ReaderBackColor"); }


        }
        //All users
        private ObservableCollection<User> _users;
        public ObservableCollection<User> Users
        {
            get { return _users; }
            set { _users = value; RaisePropertyChanged("Users"); }
        }

        //Utilisateur sélectionné
        private User _selectUser;

        public User SelectedUser
        {
            get { return _selectUser; }
            set { _selectUser = value; RaisePropertyChanged("SelectedUser"); }
        }

        #region Initialisation vue
        public MainViewModel()
        {
            //Initialisation de l'user
            Users = new ObservableCollection<User>();
            //Initialisation de la command
            Command = new RelayCommand<string>(PerformAction);
            //Lecture de la carte si le lecteur est connecté
            ReadCard();
            // Initialisation du text (Validité)
            TextBlockee = "Validité du certificat";
        }
        #endregion

        #region Recherche via UID
        private void PerformAction(string obj)
        {
            switch (obj)
            {
                //Recherche de l'user par son UID
                case "SearchByUID":
                    //Si l'UID est NULL ou vide alors -> affichage du message
                    if (string.IsNullOrEmpty(UID))
                    {
                        MessageBox.Show("Veuillez renseigné l'UID SVP.");
                    }
                    else
                    {
                        //Recupération de l'user dans la DB1
                        SelectedUser = DataContext.GetInterneUser(UID);
                        if (SelectedUser != null)
                        {
                            //Recupération de l'user dans la DB2 (Db_Externe) et -> affichage dans la datagrid
                            GetDB2Users();

                        }
                        else
                        {
                            //Si il n'y a pas d'utilisateur dans la DB1 (Db_Interne) -> changement de couleur en orange
                            BackColor = Brushes.Orange;
                            
                        }
                    }
                    break;
                case "SearchManually":
                    if (string.IsNullOrEmpty(SelectedUser.Nom1) || string.IsNullOrEmpty(SelectedUser.Prenom1))
                    {
                        MessageBox.Show("Merci de remplir complétement le formulaire de recherche");
                        return;
                    }
                    //Search user manually and show in datagrid
                    GetDB2UsersManually();
                    // Success
                    break;
                //Get user cart_date and compare
                case "GerUserCertDate":
                    CheckCert_Date();
                    break;

                //Lecture de la carte
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
                        return;
                    }
                    // Lecteur détecté affichage en vert
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

                        return;
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



        private static bool NoReaderFound(ICollection<string> readerNames) =>
                readerNames == null || readerNames.Count < 1;
        #endregion


        #region Get users manually
        private void GetDB2UsersManually()
        {
            //Get users manually
            Users = DataContext.GetUsersManually(SelectedUser);

            if (Users.Count is 0)
                SelectedUser = new User();
            //change color accordingly
            ChangeColor();
        }
        #endregion

        private void ChangeColor()
        {
            //Si la liste des utilisateurs est vide
            if (!Users.Any())
            {
                BackColor = Brushes.Red;
            }
            //Si il n'y a qu'un seul utilisateur alors
            else if (Users.Count == 1)
            {
                //Check la date et vérification
                CheckCert_Date();
            }
            //Si il y'a + d'un (1) utilisteur dispo , alors on laisse l'utilisateur séléctionner un (1)
            else if (Users.Count > 1)
            {
                BackColor = Brushes.Orange; 
            }
        }
        #region Get users from db2 filtered by nom,prenom and date
        private void GetDB2Users()
        {
            //Get users
            Users = DataContext.GetUsers(SelectedUser);
            //change color accordingly
            ChangeColor();
        }
        #endregion
        #region This method check for vac date whether it is more than 180 days or not
        // Vérification du 
        private void CheckCert_Date()
        {
            //On récupère l'infos sur la date de : date_cert et on test si elle est  à plus de 180 jours ou pas.
            var isMoretThan80 = DataContext.IsUserVacMoreThanOneEightyDays(SelectedUser);
            //Si c'est vrai (true) alors la couleur va etre rouge est les accès seront refusé
            if (isMoretThan80 == true)
            {
                BackColor = Brushes.Red;
                TextBlockee = "Certificat NON valide";
                System.Media.SoundPlayer playerrr = new System.Media.SoundPlayer(@"C:\Program Files (x86)\sons\denied.wav");
                playerrr.Play();
            }
            //  otherwise he can access and the color will be green
            else
            {
                BackColor = Brushes.Green;
                TextBlockee = "Certificat Valide";
                System.Media.SoundPlayer playerr = new System.Media.SoundPlayer(@"C:\Program Files (x86)\sons\success.wav");
                playerr.Play();
            }
        }
        #endregion
        

    }
}
