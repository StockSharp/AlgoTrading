//+------------------------------------------------------------------+
//| Colibri.mq4
//| Copyright � 2010, Laurent B�ville (LoBev)
//| http://fr-fr.facebook.com/pages/La-Martinique-Fleur-des-Caraibes/13968306412
//+------------------------------------------------------------------+
#property copyright "Copyright � 2010, Laurent B�ville"
#property link      "http://fr-fr.facebook.com/pages/La-Martinique-Fleur-des-Caraibes/13968306412"


#define           n.Tentatives.Connection          5           // try to connect to server 5 times
#define           n.Tentatives.Cl�ture             5           // try to close orders 5 times
#define           Temps.De.R�f�rence               3           // wait 3 Seconds before opening  or closing
#define           Dur�e.Max.Tentative              10          // ea try to send orders during 10 seconds
#define           Add.Slippage                     1           // Slippage
#define           Expiration.Trade.en.heures       48          // Pending orders erased after 48 hours
#define           Dist.Min.Entr.Niv                5           // Minimum distance between levels 
#define           Perte.Max.Par.Jour               0.06        // System is allowed to risk a maximum of 6% of equity every day
#define           Exposition.Maximale              0.1         // You are allowed to risk 10% of equity when taking into account all openend positions

               // ############# GESTION - URGENCE ############# //
extern string     ________1________                = "********* GEST. - URGENCES *********";
extern bool       FERMER.TOUTES.LES.POSES          = false;    // Close all opened orders
extern bool       FERMER.POSES.ACHETEUSES          = false;    // Close all buy orders
extern bool       FERMER.POSES.VENDEUSES           = false;    // Close All sell orders
extern string     Cible                            = "--- Fermeture Cibl�e ---";
extern string     MAGIC.NUM.POUR.FERM.             = "";       // see below
extern bool       FERMER.POSE                      = false;    // Close a specific order by entering its magic number
extern bool       RUN.ON.INIT                      = false;    // Start ea when placed on chart

               // ############# TRADES-PARAMETRES ############# //
extern string     ________2________                = "*********** PARAM.-ORDRES **********";
// Choix du MODE ENTRY :
extern bool       Afficher.Montant.Perte           = false;    // Show the amount of losses
extern string     INFO.ORDRE                       = "0:Rien 1:Comptant 2:Stop 3:Limite 4:Stop Suiv.";
extern int        Type.Ordre                       = 0;        // Order type you want to send (4 types)
extern string     INFO.ORDRE.2                     = "0:Pas d�pendant ; Si ordre m�re ex�cut� -> 1:Ex�cuter  2:Effacer 3:Cl�turer";
extern int        MagicNum.Ordre.M�re              = 0;        // Magic number of mother order
extern int        D�pendance                       = 0;        // Action to do with others orders (Execute/Erase/Close) when mother order is executed
extern string     Mini.Sep                         = "----------";
extern int        Trailing.Stop                    = 0;        // 
extern int        Trail.Entry.Pips                 = 0;        // Level in points at which ea start trailing stops
extern double     Exposition.Au.Risque             = 0.02;     // Risk exposure in %
extern int        Dist.Stop.Min.En.pips            = 50;       // Minimum distance between stop and entry point
extern int        Ecart.Entre.Niveaux              = 2;        // Distance defined between levels
extern string     ________3________                = "-----------------------------------------------------";
extern bool       ACHETER                          = false;    // BUY
extern bool       VENDRE                           = false;    // or SELL

               // ############# GRILLE - OPTIONS ############# //
extern string     ________4________                = "******* CHOIX OPTIONS GRILLE *******";               
extern bool       UTILISATION_GRILLE               = false;    // Security Use Grid : must be set to TRUE
extern string     ________5________                = "-----------------------------------------------------";
extern double     Niveau.De.Protection             = 0;        // Stopping price level 
extern double     Prix.Achat                       = 0;        // Buying price
extern double     Prix.Vente                       = 0;        // Selling Price

extern string     ________6________                = "-----------------------------------------------------";
// Choose calculation type :
extern bool       Grille.Centr�e.?                 = false;    // Level calculated from Grid Center level or by its boundering levels
// if you choose boundering levels calculation mode :
extern double     Grille.Borne.Sup                 = 0;        // Highest level price
extern double     Grille.Borne.Inf                 = 0;        // Lowest level price
// if you choose grid center calculation mode :
extern double     Grille.LC.Niv                    = 0;        // Grid center price
extern double     Grille.LC.Pas                    = 0;        // Levels step in points   
// Maximum levels number :
extern double     Grille.Nbre.Niveaux              = 0;        // Levels number

               // ############# EFFACER - TRACES - ORDRE ############# //
extern string     ________7________                = "****** PROCEDURES NETTOYAGE ******";
extern bool       Clean.Trace.Prise.Profit         = true;     // Clean profits arrows
extern bool       Clean.Trace.Stop.Perte           = true;     // Clean stops arrows             
extern bool       Clean.Trace.Entree.Sortie        = false;    // Clean entry arrows


// ****************************************************************** //
// ******************* CODES MAGIC NUMBER - ORDRES ****************** //
// ****************************************************************** //
// Sens        = Achat : 1 ; Vente : 2
// Type        = Au March� : 0 ; Comptant : 1 ; Stop : 2 ; Limite : 3
//               Stop Suiveur : 4
// Rang        = Rang de l'ordre (< 10)
// D�pendance  = 0 : Pas de d�pendance ; 
//               1 : Effacer si ordre m�re ex�cut�
//               2 : Ouvrir si ordre m�re ex�cut�
//               3 : Cl�turer si ordre m�re ex�cut�
// 
// Calcul du Magic-Number =
// Sens + Type + Rang + D�pendance
//
// Dans OrderComment() : mettre le magic number de l'ordre m�re ou rien
// ***************************************************************** //



double Taille.Pos.Global = 0, Taille.Pos.par.Niv = 0;

// Variables globales
string      Content[10];
string      str.Comment =  "";

//+------------------------------------------------------------------+
//| expert initialization function                                   |
//+------------------------------------------------------------------+
int init()
   {
   //----
      // Initialisation importante : en cas de changement de compte
      GlobalVariableSet("Montant.Journalier.En.Compte", AccountBalance());
      
      // Cr�ation des variables globales en cas de non-existence
      if (!GlobalVariableCheck("$_ACTIVATION_EXPERT_$"))   
         GlobalVariableSet("$_ACTIVATION_EXPERT_$", 0);
      
      // L'expert commence � tourner � l'initialisation
      if (RUN.ON.INIT) start();
   //----
      return(0);
   }
//+------------------------------------------------------------------+
//| expert deinitialization function                                 |
//+------------------------------------------------------------------+
int deinit()
  {
//----
   
//----
   return(0);
  }
//+------------------------------------------------------------------+
//| expert start function                                            |
//+------------------------------------------------------------------+
int start()
   {
   //----

      // :::::::::: Sous Module Stop Suiveurs :::::::::: //
      Stop_Suiveurs();
       
      // :::::::::: Fermeture de positions dans l'urgence :::::::::: // 
      Fermetures();     

       // ******************************************************************* //
      // !!! SECURITE !!!
      if (Perte.Max.Atteinte.?()) return(0);
      // ******************************************************************* //

      // :::::::::: Utilisation Grille :::::::::: //
      if (UTILISATION_GRILLE && Params_Are_Ok_?(Grille.Nbre.Niveaux))
         Prog_Grille_Is_Ok_?(Type.Ordre, Niveau.De.Protection, Grille.Centr�e.?, Grille.Nbre.Niveaux, 
                     Grille.Borne.Sup, Grille.Borne.Inf, Grille.LC.Niv, Grille.LC.Pas);

   //----
      return(0);
   }
//+------------------------------------------------------------------+

// ****************************************************************** //
// ************************ Programme Grille ************************ //
// ****************************************************************** //
bool Prog_Grille_Is_Ok_?(int _Type.Ord, double _Stop.Protect.en.$, bool _Grille.Centr�e.?, 
               int _Nbre.Niv, double _Grille.Borne.Sup, double _Grille.Borne.Inf, 
               double _Grille.LC.Niv, double _Grille.LC.Pas)
   {
      
      double NivPas = 0, Premier.Niv = 0, Adjust.Lot = 0, mult = 0; 
      int sens = 0;

      // :::::::::: Ajustement du Nombre de niveaux :::::::::: //
      if ((!_Grille.Centr�e.? && _Grille.Borne.Sup != 0 && _Grille.Borne.Inf != 0) ||
            (_Grille.Centr�e.? && _Grille.LC.Niv != 0 && _Grille.LC.Pas != 0))
         
         Ajust_Nbre_Niveaux_Max(_Type.Ord, _Stop.Protect.en.$, _Grille.Centr�e.?, _Nbre.Niv,
                        _Grille.Borne.Sup, _Grille.Borne.Inf, _Grille.LC.Niv, _Grille.LC.Pas);         

      // :::::::::: Formattage des tailles de positions :::::::::: //
      // En principe les tailles de position sont calcul�es dans Ajust_Nbre_Niveaux_Max(...)
      Formattage_Tailles_Pos();
    
      // :::::::::: Calcul Pas de Progression entre Niveaux :::::::::: //
      NivPas = Calc_Pas_Progression(_Grille.Centr�e.?, _Nbre.Niv, _Grille.Borne.Sup, _Grille.Borne.Inf, 
                     _Grille.LC.Niv, _Grille.LC.Pas);   

      // :::::::::: Calcul du Premier Niveau par d�faut :::::::::: //                     
      Premier.Niv = Init_Premier_Niveau(_Grille.Centr�e.?, _Nbre.Niv, _Grille.Borne.Sup, _Grille.Borne.Inf, 
                     _Grille.LC.Niv, _Grille.LC.Pas);                  
     
      // :::::::::: Calcul du nombre de mini-lots compl. :::::::::: //
      double Nbre.MiniLots.Compl = MathRound((Taille.Pos.Global - (Taille.Pos.par.Niv * _Nbre.Niv)) /
                           MarketInfo(Symbol(), MODE_LOTSTEP));
     
      // :::::::::: Remplissage de la var. Adjust.Lot :::::::::: //         
      Adjust.Lot = Taille.Pos.par.Niv;
      
      // :::::::::: Calcul du Magic Number :::::::::: //
      if (ACHETER) sens = 1; 
      else if (VENDRE) sens = 2;
      
      int Magic.Num = Calc_Magic_Num(sens, Type.Ordre, D�pendance);

      // --- Filtre sur la longueur maximale du magic number ---
      // Autrement dit : pas plus de 9 ordres par pr�fixe "sens + type"
      if (StringLen(DoubleToStr(Magic.Num, 0)) > 4) return;
            
      // :::::::::: Passer les ordres grille :::::::::: // 
      int Solde.Niv = _Nbre.Niv;
      double Objectif.Niv = Premier.Niv; // Initialisation au premier niveau
      
      if (Condit_Pass_Ord_are_Ok_?() && !FERMER.TOUTES.LES.POSES && !FERMER.POSE)
         {
            // Passer en boucle n ordres tel que n = _Nbre.Niv         
            for (int i = 1; i <= _Nbre.Niv; i++)
               {
                  // R�initialisation de Adjust.Lot
                  Adjust.Lot = Taille.Pos.par.Niv;
                  
                  // Conditions pour augmenter la var. Adjust.Lot   
                  if (Test_Sur_Nbre_Al�at(Nbre.MiniLots.Compl, Solde.Niv)
                           && Nbre.MiniLots.Compl > 0 && Nbre.MiniLots.Compl >= Solde.Niv) 
                     {
                        Adjust.Lot += MarketInfo(Symbol(), MODE_LOTSTEP);
                        Nbre.MiniLots.Compl--; 
                     }
                  
                  // D�cr�mentation pour calcul du nombre de TP restant � ajuster
                  Solde.Niv--;
                  
                  // Avant de passer un ordre s'assurer que c'est possible
                  Attendre_Disponibilit�_pr_Ordre();
                  
                  // Etablir le commentaire de l'ordre
                  str.Comment = Order_Comment();
                 
                  // Ne passer des ordres que si l'expert a �t� activ� manuellement
                  // sinon retour
                  if (GlobalVariableGet("$_ACTIVATION_EXPERT_$") == 1)
                     {   
                        if (mult >= (_Nbre.Niv - 1) &&
                              Succ�s_Ordre_?(mult, Symbol(), Type.Ordre, Adjust.Lot, 
                                    MarketInfo(Symbol(), MODE_SPREAD) + Add.Slippage, 
                                    Niveau.De.Protection, Objectif.Niv, str.Comment, Magic.Num, 
                                    (3600 * Expiration.Trade.en.heures + TimeLocal())))
                                    
                        GlobalVariableSet("$_ACTIVATION_EXPERT_$", 0);
                     }
                  else return(0);
                  
                  // Incr�mentation pour prochain niveau
                  mult++;
                  
                  // Recherche du prochain niveau
                  if (ACHETER) Objectif.Niv += NivPas;
                  if (VENDRE) Objectif.Niv -= NivPas; 
               }
         }
   }


// ****************************************************************** //
// ************************ Commentaire Ordre *********************** //
// ****************************************************************** //
string Order_Comment()
   {
      string Str.Com = StringConcatenate(MagicNum.Ordre.M�re, "#", Trailing.Stop, "#", Trail.Entry.Pips);
      
      // Renvoi de la cha�ne
      return(Str.Com);
   }


// ****************************************************************** //
// ****** Attendre la disponibilit� du serveur pour transaction ***** //
// ****************************************************************** //
void Attendre_Disponibilit�_pr_Ordre()
   {
      int timer1 = TimeLocal(), timer2 = TimeLocal();   
      
      // ----- TEST de disponibilit� ----- //
      while (!IsConnected() || IsTradeContextBusy() || !IsTradeAllowed())
         {
            Sleep(10); // Attendre 10 ms
            timer2 = TimeLocal();
            if (timer2 - timer1 > 2000) break; // Sortie apr�s + de 2 secondes
         }
   }

// ****************************************************************** //
// ************************* Passer un ordre ************************ //
// ****************************************************************** //
bool Succ�s_Ordre_?(int _mult, string _Symbol, int _Type.Ordre, double _Volume, int _Slippage, 
               double _Stoploss, double _Takeprofit, string _Comment = "", int _Magic.Num = 0, 
               datetime _Expiration = 0)
   {
      int err = GetLastError();
      err = 0;
           
      // --- V�rification de la validit� du stop --- //
      if (MathAbs(_Stoploss - (Ask + Bid) / 2) < MarketInfo(Symbol(), MODE_STOPLEVEL) * Point)
         {
            GlobalVariableSet("$_ACTIVATION_EXPERT_$", 0);
            Alert("La distance Stop - Entr�e est insuffisante");
            return(false);
         }

      // --- Rafra�chir les cotations ---
      RefreshRates();
      
      // :::::::::: ACHAT AU MARCHE :::::::::: //
      if (_Type.Ordre == 1 && ACHETER)
         Passer_Un_Ordre(Symbol(), OP_BUY, _Volume, Ask, _Slippage, _Stoploss, _Takeprofit,  
                     _Comment, _Magic.Num, _Expiration, Green);
            
      // :::::::::: VENTE AU MARCHE :::::::::: //
      if (_Type.Ordre == 1 && VENDRE)
         Passer_Un_Ordre(Symbol(), OP_SELLSTOP, _Volume, Bid, _Slippage, _Stoploss, _Takeprofit, 
                     _Comment, _Magic.Num, _Expiration, Red);

      // :::::::::: ACHAT STOP :::::::::: //
      if ((_Type.Ordre == 2 || _Type.Ordre == 4) && ACHETER)
         Passer_Un_Ordre(Symbol(), OP_BUYSTOP, _Volume, Prix.Achat + _mult * Ecart.Entre.Niveaux * Point, 
                  _Slippage, _Stoploss, _Takeprofit, _Comment, _Magic.Num, _Expiration, Teal);
      
      // :::::::::: VENTE STOP :::::::::: //
      if ((_Type.Ordre == 2 || _Type.Ordre == 4) && VENDRE)
         Passer_Un_Ordre(Symbol(), OP_SELLSTOP, _Volume, Prix.Vente - _mult * Ecart.Entre.Niveaux * Point,
                  _Slippage, _Stoploss, _Takeprofit, _Comment, _Magic.Num, _Expiration, Orchid);

      // :::::::::: ACHAT LIMITE :::::::::: //
      if (_Type.Ordre == 3 && ACHETER)
         Passer_Un_Ordre(Symbol(), OP_BUYLIMIT, _Volume, Prix.Achat + _mult * Ecart.Entre.Niveaux * Point, 
                  _Slippage, _Stoploss, _Takeprofit, _Comment, _Magic.Num, _Expiration, Teal);

      // :::::::::: VENTE LIMITE :::::::::: //               
      if (_Type.Ordre == 3 && VENDRE)
         Passer_Un_Ordre(Symbol(), OP_SELLLIMIT, _Volume, Prix.Vente - _mult * Ecart.Entre.Niveaux * Point, 
                  _Slippage, _Stoploss, _Takeprofit, _Comment, _Magic.Num, _Expiration, Orchid);

      
      err = GetLastError();
      
      Print("err : ", GetLastError());
      
      // Renvoi de true par d�faut
      return(true);
   }

// ****************************************************************** //
// ************************* Passer un ordre ************************ //
// ****************************************************************** //
bool Passer_Un_Ordre(string _Symbol, int _Cmd, double _Volume, double _Price, int _Slippage, 
               double _Stoploss, double _Takeprofit, string _Comment = "", int _Magic.Num = 0, 
               datetime _Expiration = 0, color _Arrow.Color = CLR_NONE)
   {
   
      int ticket = -1;
      
      // La plateforme est-elle connect�e ?
      if (!IsConnected()) 
         {
            Print("La plateforme n\'est pas connect�e. IsConnected() == false");
            return(-1);
         }
      
      // L'expert a t-il �t� d�sactiv� ?
      if (IsStopped()) 
         {
            Print("L\'expert a �t� d�sactiv�. IsStopped() == false");
            return(-1);
         }
      
      int cnt = 0; 
      // Attente jusqu'� ce qu'il soit permis de passer un trade
      // dans la limite d'un certain d�lai   
      while (!IsTradeAllowed() && cnt < n.Tentatives.Connection)
         {
            Temps_Attente_Aleatoire();
            cnt ++;
         }
      
      // Une fois le d�lai maximum pass�, trader est-il permis ?
      // Si oui continuer, sinon sortie de la sous-proc�dure
      if(!IsTradeAllowed()) return(-1);
      
      // Calibrage des objectifs : Stop, Prix d'entr�e, Prise de profit
      // selon le format requis par la devise en cours
      _Price       = NormalizeDouble(_Price, Digits);
      _Stoploss    = NormalizeDouble(_Stoploss, Digits);
      _Takeprofit  = NormalizeDouble(_Takeprofit, Digits);
      
      // Passer les ordres
      ticket =OrderSend(_Symbol, _Cmd, _Volume, _Price, _Slippage, _Stoploss, 
                     _Takeprofit,_Comment,_Magic.Num, _Expiration,_Arrow.Color);
      
   }


// ****************************************************************** //
// ********************* Temps Attente Aleatoire ******************** //
// ****************************************************************** //
void Temps_Attente_Aleatoire()
   {
      // Conversion du temps de r�f�rence en dixi�me de secondes
      double Dixi�mes.De.Secondes = MathCeil(Temps.De.R�f�rence / 0.1);
      if (Dixi�mes.De.Secondes <= 0) return;
      
      // D�finition en dixi�me de secondes la dur�e maximum pendant laquelle
      // l'ordre tentera de passer
      int Dur�e.Tentative.dsec = MathRound(Dur�e.Max.Tentative / 0.1);
      
      double p = 1.0 - 1.0 / Dixi�mes.De.Secondes;
      
      // Repos pendant 1 dixi�me de seconde
      Sleep(100);
      
      for (int i = 0; i < Dur�e.Tentative.dsec; i++)
         {
            if (MathRand() >= 32768 * p) 
               {
                  break;
               }
            
            // Repos pendant 1 dixi�me de seconde
            Sleep(100);
         }
   }

// ****************************************************************** //
// ************** Conditions Passage Ordres Remplies ? ************** //
// ****************************************************************** //
bool Condit_Pass_Ord_are_Ok_?()
   {
      double Risque.R�sid. = 0, Margin.Lots.Risk = 0;
      
      // Calcul du risque r�siduel pouvant �tre pris sous forme de lots
      if (OrdersTotal() > 0)      
         {      
            // :::::::::::::::: EXPOSITION ::::::::::::::: //
            for (int i = OrdersTotal(); i >= 0; i--)
               {
                  OrderSelect(i, SELECT_BY_POS, MODE_TRADES);
            
                  // Ne consid�rer que les ordres d�j� en cours
                  if (OrderType() <= OP_SELL) 
                     {
                        Margin.Lots.Risk += MathAbs((OrderOpenPrice() - OrderStopLoss()) / Point) 
                                    * OrderLots() * MarketInfo(Symbol(), MODE_TICKVALUE);
                     }
               }
            // ---
         
            Risque.R�sid. = Exposition.Maximale - (Margin.Lots.Risk / AccountBalance());
         }

      // :::::::::::::::: TEST CONDITION ::::::::::::::: //      
      return(Risque.R�sid. > Exposition.Au.Risque);    
   }

// ****************************************************************** //
// ********************* Test sur Nombre Al�atoire ****************** //
// ****************************************************************** //
bool Test_Sur_Nbre_Al�at(int _Nbre.MiniLots.Compl, int _Nbre.Niv)
   {
      // Alerte sur valeurs incoh�rentes
      if (_Nbre.Niv < _Nbre.MiniLots.Compl)
         Alert("Erreur : _Nbre.Niv < _Nbre.MiniLots.Compl. Le test al�atoire ne peut avoir lieu");
      
      // Initialisation du g�n�rateur de nombre al�atoire
      MathSrand(TimeLocal());
      
      int Nombre.Al�atoire = MathRand();
      double Seuil = 32767 * (_Nbre.MiniLots.Compl / _Nbre.Niv);

      // Renvoi de la valeur du test proprement dit
      return(Nombre.Al�atoire < Seuil);      
   }


// ****************************************************************** //
// ****************** Initialisation Premier Niveau ***************** //
// ****************************************************************** //
double Init_Premier_Niveau(bool _Grille.Centr�e.?, int _Nbre.Niv, double _Grille.Borne.Sup, 
                        double _Grille.Borne.Inf, double _Grille.LC.Niv, double _Grille.LC.Pas)
   {
      // --- CAS : GRILLE NON CENTREE ---
      
      // Achat :
      if (!_Grille.Centr�e.? && ACHETER) return(_Grille.Borne.Inf);

      // Vente :
      if (!_Grille.Centr�e.? && VENDRE) return(_Grille.Borne.Sup);

      
      // --- CAS : GRILLE CENTREE ---   
      
      // Achat :
      if (_Grille.Centr�e.? && ACHETER)
         return(_Grille.LC.Niv - ((_Nbre.Niv - 1) / 2) * _Grille.LC.Niv * Point);
      
      // Vente :
      if (_Grille.Centr�e.? && VENDRE)  
         return(_Grille.LC.Niv + ((_Nbre.Niv - 1) / 2) * _Grille.LC.Niv * Point);  
         
         
      // Renvoi de - 10000 par d�faut
      return(-10000);                        
   }


// ****************************************************************** //
// ***************** Pas de Progression des Niveaux ***************** //
// ****************************************************************** //
double Calc_Pas_Progression(bool _Grille.Centr�e.?, int _Nbre.Niv, double _Grille.Borne.Sup, 
                        double _Grille.Borne.Inf, double _Grille.LC.Niv, double _Grille.LC.Pas)
   {
      double Pas = 0;
      
      // Grille Canal : d�finition du pas de progression
      if (!_Grille.Centr�e.?)
         {
            Pas = (MathAbs(_Grille.Borne.Sup - _Grille.Borne.Inf) 
                                 + MarketInfo(Symbol(), MODE_TICKSIZE)) / _Nbre.Niv;         
         }
      
      // Grille Centr�e : d�finition du pas de progression
      if (_Grille.Centr�e.?)
         {
            Pas = _Grille.LC.Pas * Point;         
         }

      // Formatter la var. Pas
      return(NormalizeDouble(Pas, Digits));
   }

// ****************************************************************** //
// ******************* Formattage Tailles Position******************* //
// ****************************************************************** //
bool Formattage_Tailles_Pos()
   {
      if (MarketInfo(Symbol(), MODE_LOTSTEP) == 0.1)
         {
            if (Taille.Pos.par.Niv >= NormalizeDouble(Taille.Pos.par.Niv, 1))
               Taille.Pos.par.Niv = NormalizeDouble(Taille.Pos.par.Niv, 1);
                       
            if (Taille.Pos.par.Niv < NormalizeDouble(Taille.Pos.par.Niv, 1))
               Taille.Pos.par.Niv = NormalizeDouble(Taille.Pos.par.Niv, 1) 
                              - MarketInfo(Symbol(), MODE_LOTSTEP);
            
            // Renvoi
            return(true);
         }

      if (MarketInfo(Symbol(), MODE_LOTSTEP) == 0.01)
         {
            if (Taille.Pos.par.Niv >= NormalizeDouble(Taille.Pos.par.Niv, 2))
               Taille.Pos.par.Niv = NormalizeDouble(Taille.Pos.par.Niv, 2);
                       
            if (Taille.Pos.par.Niv < NormalizeDouble(Taille.Pos.par.Niv, 2))
               Taille.Pos.par.Niv = NormalizeDouble(Taille.Pos.par.Niv, 2) 
                              - MarketInfo(Symbol(), MODE_LOTSTEP);
            // Renvoi
            return(true);                              
         }
         
      // Renvoi
      return(false);                     
   }


// ****************************************************************** //
// ***************** Ajustement du nombre de niveaux **************** //
// ****************************************************************** //
double Ajust_Nbre_Niveaux_Max(int _Type.Ord, double _Stop.Protect.en.$, bool _Grille.Centr�e.?, 
               int _Nbre.Niv, double _Grille.Borne.Sup, double _Grille.Borne.Inf, 
               double _Grille.LC.Niv, double _Grille.LC.Pas)
   {
      
      double Max.Niv.Th�oriq = 0;
         
      // ----------------------------------------------------------- //
      // ------------------------ SECURITE 1 ----------------------- //
      // ----------------------------------------------------------- //  
      // Ajustement du nombre de niveaux en fonction du point 
      // d'entr�e et des objectifs de profit

      // OBJECTIF CANAL
      if (!_Grille.Centr�e.?)
         {
            // Calcul du nombre max. de niveaux possibles entre les bornes sup�rieures 
            // et inf�rieures, compte tenu de la distance min. autoris�e entre les niveaux
            Max.Niv.Th�oriq = MathAbs(MathFloor((1/Point) * (_Grille.Borne.Sup - _Grille.Borne.Inf) 
                        / Dist.Min.Entr.Niv));
            
            // Test de validit� : Ajustement �ventuel
            if (_Nbre.Niv > Max.Niv.Th�oriq) _Nbre.Niv = Max.Niv.Th�oriq;
         }
      
      // OBJECTIF LIGNE CENTRALE + ORDRE AU MARCHE
      if (_Grille.Centr�e.? && _Type.Ord == 1)
         {
            // Calcul du nombre max. de niveaux autoris�s, compte tenu de la distance entre
            // La ligne centrale de prise de profit et le cours d'entr�e
            Max.Niv.Th�oriq = 2 * MathAbs(MathFloor((1/Point) * (_Grille.LC.Niv - ((Ask+Bid)/2))
                        / Dist.Min.Entr.Niv));
                        
            // Test de validit� : Ajustement �ventuel
            if (_Nbre.Niv > Max.Niv.Th�oriq) _Nbre.Niv = Max.Niv.Th�oriq;                        
         }

      // OBJECTIF LIGNE CENTRALE + (ORDRE STOP ACHAT OU ORDRE LIMITE ACHAT)
      if (  _Grille.Centr�e.? && 
           (_Type.Ord == 2 || _Type.Ord == 3 || (_Type.Ord == 4 && ACHETER && !VENDRE)) && 
            Prix.Achat != 0 && Prix.Vente == 0)
         {
            // Calcul du nombre max. de niveaux autoris�s, compte tenu de la distance entre
            // La ligne centrale de prise de profit et le prix d'achat envisag�
            Max.Niv.Th�oriq = 2 * MathAbs(MathFloor((1/Point) * (_Grille.LC.Niv - Prix.Achat)
                        / Dist.Min.Entr.Niv));     
                        
            // Test de validit� : Ajustement �ventuel
            if (_Nbre.Niv > Max.Niv.Th�oriq) _Nbre.Niv = Max.Niv.Th�oriq;                            
         }

      // OBJECTIF LIGNE CENTRALE + (ORDRE STOP VENTE OU ORDRE LIMITE VENTE)
      if (  _Grille.Centr�e.? && 
           (_Type.Ord == 2 || _Type.Ord == 3 || (_Type.Ord == 4 && !ACHETER && VENDRE)) && 
           Prix.Achat == 0 && Prix.Vente != 0)
         {
            // Calcul du nombre max. de niveaux autoris�s, compte tenu de la distance entre
            // La ligne centrale de prise de profit et le prix de vente envisag�
            Max.Niv.Th�oriq = 2 * MathAbs(MathFloor((1/Point) * (_Grille.LC.Niv - Prix.Vente)
                        / Dist.Min.Entr.Niv));           
                        
            // Test de validit� : Ajustement �ventuel
            if (_Nbre.Niv > Max.Niv.Th�oriq) _Nbre.Niv = Max.Niv.Th�oriq;
         }      
      // ----------------------------------------------------------- // 


      // ----------------------------------------------------------- //
      // ------------------------ SECURITE 2 ----------------------- //
      // ----------------------------------------------------------- //  
      // Si la taille de position envisag�e est trop petite pour �tre 
      // r�partie entre x lignes de niveaux => R�ajuster le nombre de
      // lignes de sortie envisag�es. 
      
      // On calcule ici la taille de position globale
      Taille.Pos.par.Niv = Calc.Taille.Pos.par.Niv(_Type.Ord, _Nbre.Niv, _Stop.Protect.en.$);
      Taille.Pos.Global = _Nbre.Niv * Taille.Pos.par.Niv;

      // Test : condition de validit� portant sur _Nbre.Niv
      if (Taille.Pos.Global < (_Nbre.Niv * MarketInfo(Symbol(), MODE_LOTSTEP)))
         {   
            // Ajustement _Nbre.Niv
            _Nbre.Niv = MathFloor(Taille.Pos.Global / MarketInfo(Symbol(), MODE_LOTSTEP));
            
            // Recalcul des tailles de position global/par Niveau     
            Taille.Pos.par.Niv = MarketInfo(Symbol(), MODE_LOTSTEP);
            Taille.Pos.Global = _Nbre.Niv * Taille.Pos.par.Niv;                
         }
      // ----------------------------------------------------------- //
            
   }

// ****************************************************************** //
// **** Calcul de la taille de position par niveau de la grille ***** //
// ****************************************************************** //
double Calc.Taille.Pos.par.Niv(int _Type.Ord, int _Nbre.Niv, double _Stop.Protect.en.$)
   {
      double dist.Stop.Protect.en.Pips = 0;
      
      // Alerte + Retour si le nombre de niveau = 0
      if (_Nbre.Niv == 0)
         {
            Alert("Erreur : Sp�cifiez un nombre de niveaux > 0 !!!");
            return(0);
         }
         
      // Calcul de la distance du stop au point d'entr�e th�orique   
      if (_Type.Ord == 1)
         dist.Stop.Protect.en.Pips = MathAbs((_Stop.Protect.en.$ - (Ask+Bid)/2)) * (1/Point);
      
      else if((_Type.Ord == 2 || _Type.Ord == 3 || _Type.Ord == 4) && ACHETER)
         dist.Stop.Protect.en.Pips = MathAbs((_Stop.Protect.en.$ - (Prix.Achat + 0.5 * 
                              Ecart.Entre.Niveaux * Point * (_Nbre.Niv - 1)))) 
                                          * (1/Point);
      
      else if((_Type.Ord == 2 || _Type.Ord == 3 || _Type.Ord == 4) && VENDRE)
         dist.Stop.Protect.en.Pips = MathAbs((_Stop.Protect.en.$ - (Prix.Vente - 0.5 * 
                              Ecart.Entre.Niveaux * Point * (_Nbre.Niv - 1)))) 
                                          * (1/Point);         
   
      // Calcul du montant risqu� en $ compte tenu du stop pour 1 lot standard
      double Lot.Margin.en.$ = dist.Stop.Protect.en.Pips * MarketInfo(Symbol(), MODE_TICKVALUE);
      
      // Calcul de la TP pour un niveau
      return((AccountBalance() * Exposition.Au.Risque) / (Lot.Margin.en.$ * _Nbre.Niv));
   }

// ****************************************************************** //
// **************** Calcul du Magic Number appropri� **************** //
// ****************************************************************** //
int Calc_Magic_Num(int _sens, int _type, int _d�pendance)
   {
      // Initialisation
      int Somme = 0;
      int rang = Calc_Rang_Ordre(_sens, _type);
      
      // Calcul Magic Number      
      Somme = StrToInteger(StringConcatenate(_sens, _type, rang, _d�pendance));
      
      // Renvoi de la valeur calcul�e
      return(Somme);
   }

// ****************************************************************** //
// ******************* Calcul du rang de l'ordre ******************** //
// ****************************************************************** //
int Calc_Rang_Ordre(int _sens, int _type)
   {
      int total         = OrdersTotal();
      int rang, num     = 0;
            
      // D�finition du pr�fixe : sens + type
      string Magic.Prefix = StringConcatenate(_sens, _type);
      
      // ...
      string MA.to.Str = "";
      
      // Parcourir l'ensemble des ordres en cours
      for(int i = total; i > 0; i--)
         {
            OrderSelect(i, SELECT_BY_POS, MODE_TRADES);   
            
            // Conversion du magic number s�lectionn� en cha�ne de caract�re
            MA.to.Str = DoubleToStr(OrderMagicNumber(), 0);
            
            // Le pr�fixe en question est reconnu parmi les magic numbers en cours
            if (StringSubstr(MA.to.Str, 0, 2) == Magic.Prefix)   
               {
                  // Place le rang de l'ordre analys� dans une var. temp.
                  num = StrToInteger(StringSubstr(MA.to.Str, 2, 1));
                  
                  // Rafra�chissement de la variable rang ssi n�cessaire
                  if (num > rang)  rang = num; 
               }
         }
      
      // Incr�mentation du rang
      rang++;
      
      // Renvoi de la valeur rang incr�ment� de +1
      return(rang);
   }


// ****************************************************************** //
// ********************** Contr�le Param�trage ********************** //
// ****************************************************************** //
bool Params_Are_Ok_?(int _Nbre.Niv)
   {
      // :::::::::::::::: SECURITE ::::::::::::::: //
      // Alerte + Retour si le nombre de niveau = 0
      if (_Nbre.Niv == 0 && (ACHETER || VENDRE))
         {
            Alert("Erreur : Sp�cifiez un nombre de niveaux > 0 !!!");
            return(false);
         }  
      // :::::::::::::::: SECURITE ::::::::::::::: //   
 
      // :::::::::::::::: RETOUR PAR DEFAUT ::::::::::::::: //  
      return (true);
   }

// ****************************************************************** //
// ****************** Perte Max. Autoris�e par Jour ***************** //
// ****************************************************************** //
bool Perte.Max.Atteinte.?()
   {
      int total = OrdersHistoryTotal();
      double Pertes.Journali�res.Accumul�es = 0, Part.Representative.Pertes = 0;
   
      // Calculer le montant du compte en dollars chaque jour
      // R�actualisation entre 0 et 4h du matin
      if (Hour() < 3 && Hour() >= 0) 
         GlobalVariableSet("Montant.Journalier.En.Compte", AccountBalance());
      
      if (!GlobalVariableCheck("Montant.Journalier.En.Compte") 
            || GlobalVariableGet("Montant.Journalier.En.Compte") == 0)   
         GlobalVariableSet("Montant.Journalier.En.Compte", AccountBalance());
         
      // Initialisation de la variable calculant le % des pertes
      Pertes.Journali�res.Accumul�es = 0;
            
      for(int i = total; i > 0; i--)
         {
            OrderSelect(i,SELECT_BY_POS, MODE_HISTORY);

            if(OrderSymbol() == Symbol() &&  TimeDayOfYear(OrderOpenTime()) == DayOfYear()
                  && OrderProfit() < 0)
               {
                  Pertes.Journali�res.Accumul�es += OrderProfit();
                  Part.Representative.Pertes = Pertes.Journali�res.Accumul�es 
                                          / GlobalVariableGet("Montant.Journalier.En.Compte");
                  
                  if (Part.Representative.Pertes > Perte.Max.Par.Jour) return(true);
               }
         } 
         
         // Renvoi par d�faut :
         return(false);
   }



// ****************************************************************** //
// *************************** Fermetures *************************** //
// ****************************************************************** //
int Fermetures()
   {
      // Fermer l'int�gralit� des positions
      if (FERMER.TOUTES.LES.POSES)     Fermer_Positions();
      
      // Ne fermer que les positions acheteuses
      if (FERMER.POSES.ACHETEUSES)  {     
         Fermer_Positions(0, OP_BUY);
         Fermer_Positions(0, OP_BUYSTOP);
         Fermer_Positions(0, OP_BUYLIMIT);}
      
      // Ne fermer que les positions vendeuses
      if (FERMER.POSES.VENDEUSES)   {      
         Fermer_Positions(0, OP_SELL);
         Fermer_Positions(0, OP_SELLSTOP);
         Fermer_Positions(0, OP_SELLLIMIT); } 
      
      // Ne fermer qu'une seule position bien cibl�e
      if (FERMER.POSE)                 Fermer_Positions(StrToInteger(MAGIC.NUM.POUR.FERM.));     

      // :::::::::: R�initialisation de la variable d'activation de l'ea :::::::::: // 
      if (FERMER.TOUTES.LES.POSES || FERMER.POSES.ACHETEUSES 
            || FERMER.POSES.VENDEUSES ||FERMER.POSE)
         GlobalVariableSet("$_ACTIVATION_EXPERT_$", 0);
   }


// ****************************************************************** //
// ******************* Fermer (Toutes) Position(s) ****************** //
// ****************************************************************** //
int Fermer_Positions(int _Identifiant = 0, int _Fermer.Ordre.Type = 6)
   {
      bool     Ordre.Cl�tur�.?                  = false;
      string   Nom.Arrow.Clean.CodeUn           = "";
      string   Nom.Arrow.Clean.CodeQuatre.Tp    = "";
      string   Nom.Arrow.Clean.CodeQuatre.Stp   = "";
      
      for (int i = OrdersTotal(); i >= 0; i--)
         {
            OrderSelect(i, SELECT_BY_POS, MODE_TRADES);
            
            // Initialisation � false de la variable de fermeture apr�s chaque fermeture
            Ordre.Cl�tur�.? = false;
            
            // *************************************************************** //
            // ---------------- CREATION NOM OBJET A EFFACER ----------------- //
            // *************************************************************** //                        
                           
            // Effacer les objets graphiques associ�s au trade

            Nom.Arrow.Clean.CodeUn = Creation_Nom_Objet_A_Effacer("Arrow", 1, "",
               OrderSymbol(), OrderTicket(), OrderType(), OrderLots(), OrderOpenPrice(),
               OrderTakeProfit(), OrderStopLoss()); 
                           
            Nom.Arrow.Clean.CodeQuatre.Tp = Creation_Nom_Objet_A_Effacer("Arrow", 4, "Tp", 
               OrderSymbol(), OrderTicket(), OrderType(), OrderLots(), OrderOpenPrice(),
               OrderTakeProfit(), OrderStopLoss()); 

            Nom.Arrow.Clean.CodeQuatre.Stp = Creation_Nom_Objet_A_Effacer("Arrow", 4, "Stp", 
               OrderSymbol(), OrderTicket(), OrderType(), OrderLots(), OrderOpenPrice(),
               OrderTakeProfit(), OrderStopLoss()); 
                                                      
            // *************************************************************** //   
            
            // :::::::::::::::: FERMER L'INTEGRALITE DES ORDRES ::::::::::::::: // 
            // :::::::::::::::: OU FERMER UN ORDRE SPECIFIQUE ::::::::::::::: //            
            if ((Symbol() == OrderSymbol() && _Identifiant == 0) ||
                (Symbol() == OrderSymbol() && _Identifiant != 0 && OrderMagicNumber() == _Identifiant))
               {
                  if (
          // ...        
   (OrderType() == OP_BUY        && (_Fermer.Ordre.Type == OP_BUY        || _Fermer.Ordre.Type == 6)) ||
   (OrderType() == OP_SELL       && (_Fermer.Ordre.Type == OP_SELL       || _Fermer.Ordre.Type == 6)) ||
   (OrderType() == OP_BUYSTOP    && (_Fermer.Ordre.Type == OP_BUYSTOP    || _Fermer.Ordre.Type == 6)) ||
   (OrderType() == OP_SELLSTOP   && (_Fermer.Ordre.Type == OP_SELLSTOP   || _Fermer.Ordre.Type == 6)) ||
   (OrderType() == OP_BUYLIMIT   && (_Fermer.Ordre.Type == OP_BUYLIMIT   || _Fermer.Ordre.Type == 6)) ||
   (OrderType() == OP_SELLLIMIT  && (_Fermer.Ordre.Type == OP_SELLLIMIT  || _Fermer.Ordre.Type == 6))
          // ...      
                     )
                     {
                        int incr. = 0;
                        while (!Ordre.Cl�tur�.? && incr. < n.Tentatives.Cl�ture)
                           {
                              incr.++;
                              
                              // Attendre en cas de probl�me : d�connexion ou occupation
                              int timer1 = TimeLocal();
                              int timer2 = TimeLocal();
                              
                              // Attente al�atoire si les conditions ne sont pas r�unies
                              Attendre_Disponibilit�_pr_Ordre();
                              
                              // Rafra�chir cotation
                              RefreshRates();
                              
                              // Tentative de cl�ture
                              if (OrderType() == OP_BUY && OrderType() == OP_SELL)
                                 Ordre.Cl�tur�.? = OrderClose(OrderTicket(),OrderLots(), Bid, MarketInfo(Symbol(), MODE_SPREAD)+10, Violet);
                              
                              if (OrderType() != OP_BUY && OrderType() != OP_SELL)
                                 Ordre.Cl�tur�.? = OrderDelete(OrderTicket(), Violet); 
                              
                              if (!Ordre.Cl�tur�.?) Sleep(150); // Attente si seulement �chec
                              
                           }
                        
                        // ------ EFFACER ------
                        if (Clean.Trace.Entree.Sortie) 
                              Effacer.Objet(Nom.Arrow.Clean.CodeUn);
                        if (Clean.Trace.Prise.Profit) 
                              Effacer.Objet(Nom.Arrow.Clean.CodeQuatre.Tp);
                        if (Clean.Trace.Stop.Perte) 
                              Effacer.Objet(Nom.Arrow.Clean.CodeQuatre.Stp);      
                     }
               }
         }
   }


// ****************************************************************** //
// ******************* D�nomination Objet � Effacer ***************** //
// ****************************************************************** //
string Creation_Nom_Objet_A_Effacer(string _Type.Objet, int _Code.Objet, string _Code.Sous.Objet, 
               string _Devise.Ordre, int _Ticket.Ordre, int _Type.Ordre, double _Quantit�.Lots, 
               double _Prix.Ouverture, double _Prix.Prise.Profit, double _Prix.Stop.Perte)
   {
      // ex : Arrow (code 1) : "#30900169 buy stop 0.03 EURUSDm at 1.2864"
      // ex : Arrow (code 4) : "#31235132 buy stop 0.03 EURUSDm at 1.2658 take profit at 1.325"
      // ex : Arrow (code 4) : "#31010932 buy stop 0.02 EURUSDm at 1.2874 stop loss at 1.2706"
      
      string Nom.Objet.A.Effacer = "";
      
      if (_Type.Objet == "Arrow")   
         {
            // ----- Ajout du ticket de l'ordre ----- 
            Nom.Objet.A.Effacer = StringConcatenate("#", DoubleToStr(_Ticket.Ordre, 0)); 
            
            // ----- Ajout du type de l'ordre ----- 
            
            string Ord.Typ[6] = {" buy ", " sell ", " buy stop ", " sell stop ", " buy limit ", " sell limit "};
            
            Nom.Objet.A.Effacer = StringConcatenate(Nom.Objet.A.Effacer, Ord.Typ[_Type.Ordre]);
            
            // ----- Ajout du volume ----- 
            int decim. = Lots.Decimals();
            string str.Q = DoubleToStr(NormalizeDouble(_Quantit�.Lots, decim.), decim.);
            
            Nom.Objet.A.Effacer = StringConcatenate(Nom.Objet.A.Effacer, str.Q);
            
            // ----- Ajout de la devise d'intervention ----- 
            Nom.Objet.A.Effacer = StringConcatenate(Nom.Objet.A.Effacer, " ", _Devise.Ordre, " ");
            
            // ----- Ajout du prix d'ouverture ----- 
            string str.Pr = DoubleToStr(_Prix.Ouverture, Digits);
            
            Nom.Objet.A.Effacer = StringConcatenate(Nom.Objet.A.Effacer, "at ", str.Pr);
            
            // --- Pr�-Calculs ---
            if (Digits > 5) Print("Trop de chiffres apr�s la virgule. Devise impossible � traiter correctement",
                              "Sous-Module : Creation.Nom.Objet.A.Effacer(...)");
            int add.cf = 0;
                  
            if (Bid >= 0   && Ask < 10)   add.cf = 0; // ex. type : 1.xx.. 
            if (Bid >= 10  && Ask < 100)  add.cf = 1; // ex. type : 12.xx..
            if (Bid >= 100 && Ask < 1000) add.cf = 2; // ex. type : 125.xx.. 
            if (Bid >= 1000) Print("Impossible de continuer : valeur monnaie trop grande (>1000).,", 
                                 "Sous-Module : Creation.Nom.Objet.A.Effacer(...)");                    
            // ---
            
            // ****** CAS CODE 4 ****** 
            if (_Code.Objet == 4 && _Code.Sous.Objet == "Tp")
               {
                  Nom.Objet.A.Effacer = StringConcatenate(Nom.Objet.A.Effacer, " take profit at "); 
                  
                  string str.Tp = "";

                  // D�coupage du nombre str.Tp, dg.Pow chiffres apr�s la virgule
                  str.Tp = DoubleToStr(_Prix.Prise.Profit, Digits);                        
                  
                  for (int i = 1; i <= 5; i++)      
                     if (MathMod(10^Digits * _Prix.Prise.Profit, 10^i) == 0 && Digits > (i-1)) 
                        str.Tp = StringSubstr(str.Tp, 0, add.cf + Digits - ((i-1) + (Digits==i)));
                  
                  Nom.Objet.A.Effacer = StringConcatenate(Nom.Objet.A.Effacer, str.Tp);  
               }

            if (_Code.Objet == 4 && _Code.Sous.Objet == "Stp")
               {
                  Nom.Objet.A.Effacer = StringConcatenate(Nom.Objet.A.Effacer, " stop loss at "); 
                  
                  string str.Stp = "";
                  
                  // D�coupage du nombre str.Stp, dg.Pow chiffres apr�s la virgule
                  str.Stp = DoubleToStr(_Prix.Stop.Perte, Digits);                        
                        
                  for (i = 1; i <= 5; i++)
                     if (MathMod(10^Digits * _Prix.Stop.Perte, 10^i) == 0 && Digits > (i-1)) 
                        str.Stp = StringSubstr(str.Stp, 0, add.cf + Digits - ((i-1) + (Digits==i)));

                  Nom.Objet.A.Effacer = StringConcatenate(Nom.Objet.A.Effacer, str.Stp);  
               }               
         }

      // Renvoi du nom de l'objet
      return(Nom.Objet.A.Effacer);         
   }


// ****************************************************************** //
// ************* Calcul du nombre de d�cimales pour la TP *********** //
// ****************************************************************** //
int Lots.Decimals()
   {
      int Pos.Point = StringFind(MarketInfo(Symbol(), MODE_LOTSTEP), ".");
      // ---   
      return (StringLen(MarketInfo(Symbol(), MODE_LOTSTEP)) - (1 + Pos.Point));
   }

// ****************************************************************** //
// ********************* Fonction Objet � Effacer ******************* //
// ****************************************************************** //
void Effacer.Objet(string _Nom.Objet.A.Effacer)
   {
      for(int i = 0; i < 4; i++)
      {
         if (i==0 && !ObjectDelete(_Nom.Objet.A.Effacer)) _Nom.Objet.A.Effacer = 
            StringSubstr(_Nom.Objet.A.Effacer, 0, StringLen(_Nom.Objet.A.Effacer) - 1);
         
         if (i==1 && !ObjectDelete(_Nom.Objet.A.Effacer)) _Nom.Objet.A.Effacer = 
            StringSubstr(_Nom.Objet.A.Effacer, 0, StringLen(_Nom.Objet.A.Effacer) - 2);
         
         if (i==2 && !ObjectDelete(_Nom.Objet.A.Effacer)) _Nom.Objet.A.Effacer = 
            StringSubstr(_Nom.Objet.A.Effacer, 0, StringLen(_Nom.Objet.A.Effacer) - 3);                        
         
         if (i==3) ObjectDelete(_Nom.Objet.A.Effacer);                           
      }
   }

// ****************************************************************** //
// ************************** Stops Suiveurs ************************ //
// ****************************************************************** //
int Stop_Suiveurs()
   {
      string   D�pendance        = "";
      int      Trail.Entry.Pips  = 0;
      
      // Parcours de l'ensemble des trades en cours
      for (int i = OrdersTotal(); i >= 0; i--)
         {
            OrderSelect(i, SELECT_BY_POS, MODE_TRADES);
            
            // Extraction du num�ro de la d�pendance
            D�pendance = StringSubstr(DoubleToStr(OrderMagicNumber(), 0), 2, 3);  
            
            // Il s'agit d'un stop suiveur ...Et uniquement un STOP
            if (D�pendance == "4" && (OrderType() == OP_BUYSTOP || OrderType() == OP_SELLSTOP)) 
               {
                  // Initialisation du tableau Content[]
                  for (int j = 0; j < ArrayRange(Content, 1) ; j++) Content[j] = "";
                  
                  // Extraction des donn�es de la cha�ne Comment
                  Comment_Extract(OrderComment());
                  
                  // La valeur du Trail.Entry.Pips se trouve dans Content[2]
                  Trail.Entry.Pips  = StrToInteger(Content[2]);
                  
                  // Ajuster le stop suiveur en fonction de cette valeur
                  if (Trail.Entry.Pips != 0)
                     Ajust_Stops_Suiveurs_?(OrderTicket(), Trail.Entry.Pips);
                  else continue;
               }          
            else continue; 
         }
   }

// ****************************************************************** //
// ************ Extraction du commentaire de chaque ordre *********** //
// ****************************************************************** //
// 
// Architecture du commentaire :
// Info1#Info2#...#Info3 (s�parateur = #)
//
// ****************************************************************** //
int Comment_Extract(string _Commentaire)
   {
      // Initialisations
      int      start = 0, pos = 0, n.sep = 0;
      string   chn = "";

      // Extraction du contenu de la cha�ne commentaire dans Content[] 
      // Type commentaire : 0103#456#910     
      while (StringLen(chn) < StringLen(_Commentaire) - n.sep)
         {
            // Lecture de la position du caract�re s�parateur #
            pos = StringFind(_Commentaire, "#", start);
            
            // Extraction de la chaine de caract. comprise entre start et pos
            if (pos != -1)
               Content[n.sep] = StringSubstr(_Commentaire, start, pos - start);
            
            else if (pos == -1 && start > 0)
               Content[n.sep] = StringSubstr(_Commentaire, start, StringLen(_Commentaire) - start);               
            
            else break;
                        
            // Incr�mentation de start � la position qui suit imm�diatement Pos
            start = pos + 1;     
            
            // Incr�mentation indice tableau
            n.sep++; 
            
            // Construction de chn (cha�ne comparative)
            chn = StringConcatenate(chn, Content[n.sep]);   
         }   
   }

// ****************************************************************** //
// ******************* Ajustement Stops Suiveurs ? ****************** //
// ****************************************************************** //
bool Ajust_Stops_Suiveurs_?(int _Order.Ticket, int _Trail.Entry.Pips)
   {
      double bsl = 0, b.tsl.ent = 0, ssl = 0, s.tsl.ent = 0;
      int cnt = 0;

      // Sauvegarde de la distance entre le Stoploss et le point d'entr�e th�orique
      double dStop = MathAbs(OrderOpenPrice() - OrderStopLoss());

      // Rafra�chir les cotations
      RefreshRates();
      
      // S�lectionner l'ordre
      OrderSelect(_Order.Ticket, SELECT_BY_TICKET);       
      
      // Si on cherche � ajuster un stop pending achat
      if (OrderType() == OP_BUYSTOP)
         {
            bsl = _Trail.Entry.Pips * Point; 
            
            // Le point d'entr�e achat doit-il �tre modifi� ?
            if (Bid < (OrderOpenPrice() - bsl))
               {
                  // Ajustement du nouveau point d'entr�e th�orique
                  b.tsl.ent = NormalizeDouble(OrderOpenPrice() - ((OrderOpenPrice() - bsl) - Bid), Digits);
                  
                  // Attente al�atoire si les conditions ne sont pas r�unies
                  Attendre_Disponibilit�_pr_Ordre();
                  
                  // Tentative de modification de l'ordre
                  while (cnt < n.Tentatives.Connection && !OrderModify(_Order.Ticket, b.tsl.ent, 
                              b.tsl.ent - dStop, OrderTakeProfit(), OrderExpiration(), MediumVioletRed))
                     {
                        if (!IsTradeAllowed() || IsTradeContextBusy() || !IsConnected()) 
                           Temps_Attente_Aleatoire(); // Attente
                     }
               }
         }
      
      // Si on cherche � ajuster un stop pending vente
      if (OrderType() == OP_SELLSTOP)  
         {
            ssl = _Trail.Entry.Pips * Point;        

            // Le stop perte doit-il �tre modifi� ?
            if (Ask > (OrderOpenPrice() + ssl))
               {
                  // Ajustement du nouveau point d'entr�e th�orique
                  s.tsl.ent = NormalizeDouble(OrderOpenPrice() + (Ask - (OrderOpenPrice() + ssl)), Digits);

                  // Attente al�atoire si les conditions ne sont pas r�unies
                  Attendre_Disponibilit�_pr_Ordre();

                  // Tentative de modification de l'ordre                  
                  while (cnt < n.Tentatives.Connection && !OrderModify(_Order.Ticket, s.tsl.ent, 
                              s.tsl.ent + dStop, OrderTakeProfit(), OrderExpiration(), MediumVioletRed))
                     {
                        if (!IsTradeAllowed() || IsTradeContextBusy() || !IsConnected()) 
                           Temps_Attente_Aleatoire(); // Attente
                     }
               }            
         }
   }