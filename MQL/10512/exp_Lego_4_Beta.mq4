//+-------------------------------------------------------------------+
//|                                          Build_Own_Strategy!.mq4  |
//|                                     � 2007  v0 - AutoGrapf.dp.ua  | // It was shared for MQL4 book. So, there are no any reserved copyrights except open source since beginning.
//|                          v1 2010 Seletsky R.V. aka Romari         | // Also shared on MQL4.com . R is for Roman... I guess.
//|                                          v2,v3 - by 26_994        | // All are shared.
//|                          v4 2011 Yakov Gloodun aka 26_994         |// Yes, it's my name. And sorry me for my English - my mother teaches it.
//+-------------------------------------------------------------------+
//-----------------------------------------------------------------------------------------------------------+
//                                       ���������� �� ���������� - � ����                                   |
//-----------------Paramethers--------------------------------------------------------------------- 1 -------+
// �������� - ����� ���������� ������� �� MQL4.com ���-������ � ��������� "10.0" �� ������� ���������� (����� ��������������! ����� ��������)
extern   string   Instructsia                =  "...����� � ������";
extern   string   ShiftTunes           =  "���� (���). �� ��� ����������, ����� 2�� � ����������������";
extern double GeneralShift = 0; // 0 - ���������� ���, 1 - ��������.
extern string Implemented_Indicators         = "��������� ����������. 12 ����. �������������";
extern string WARNING1                       = "�������� ���������� ������������� � �������";
extern string WARNING2                       = "���������, ��� ���������� ���������� ���������,";
extern string WARNING3                       = "����� ��������� ����� ���������";
extern string Note_����                      = " ������� ������� ���������� ���������� ����� ��� ����-�� ������";
extern string The_List_Of                    = " ---- ---- ---- ---- ---- ---- ---- ---- ---- ---- ---- ---- ----";
extern string Indicators1                    = " ��������� ���������� ��������� ����! ����� ������ � ���������! ";
extern string Indicators2                    = " ����������� - �������� ����� �������� ��� ��������! ";
extern string Indicators3                    = " ��������� ����� ���� - ����������� �������� ����. ";

extern string Indicators4                    = " ";

extern   string  Indicator_1     = "2MA (MACD 0-line-mode ��)";
extern  bool  otkr_MA      =false;
extern  bool  zakr_MA      =false;  // ����������� _����_ ����������. ����� ��� MACD-"������� �����".
extern double MA1          =4;
extern double MA2          =67;
extern double Shift_1      =1;  // ���� - ��� ���, �� ������� ��������� ��������. 1 - ����������������, 0 - �������, 3 - ��� ����� �����... 
extern double Shift_2      =1; 
extern double MA_type      =1; //0 - Simple, 1 - Expotential, 2 - Smoothed, 3 - Linear Weighed. Most people are using EMA.
extern   string   Indicator_2       =  "�������� - �������������� ����������.";
extern  bool   otkr_Stoh   =false;// 
extern  bool   zakr_Stoh   =false;
extern double per_K=5;
extern double per_D=3;
extern double slow=3;
extern double zoneBUY=20;
extern double zoneSELL=80; //������ ���� - ����� �� ����� ����������� ������ �� �����������.

extern   string   Indicator_3          =  "---- Awesome (std) ----"; // �������� std, ����������, standard. ��� ��������, ��� ���������... �� ������������ ����. �������. ���� � ����� ������������ ������ ���� ������ �� �����.
extern  bool   otkr_AO    =false;
extern  bool   zakr_AO    =false;
extern   string     StdDev         =  "--- StdDev (std) ----"; //�� �� �����, ��� � ������ ����������� - ������ � ���. ����.
extern  bool   otkr_StdDev=false; 
extern  bool   zakr_StdDev=false; 
extern double StdDevMAPeriod =14;
extern double StdDevMAShift =0; 
extern double StdDevLimitToOpen   =0.0025;
extern double StdDevLimitToClose  =0.0025;// ��������� ���� - �� ������ ��������� ToClose ����, ��� ToOpen.
extern   string   Indicator_4         =  "---- Demarker (std) ---- ";
extern  bool   otkr_Dema   =false;
extern  bool   zakr_Dema   =false;
extern double DeMa_period=14;
extern double niz_ur=0.3;
extern double verx_ur=0.7;
extern   string   Indicator_5              =  "---- CCI (std) ---- ";
extern  bool   otkr_CCI    =false;
extern  bool   zakr_CCI    =false;
extern double CCI_period=14;
extern double CCI_Level=100;    // What, if we want 95?
extern   string   Indicator_6              =  "---- RSI (std) ----";
extern  bool   otkr_RSI   =false;
extern  bool   zakr_RSI   =false;
extern double RSI_Period =14;
extern double RSI_High=70;
extern double RSI_Low=30;
extern   string   Indicator_7             =  "---- MACD (std) ---- ";
extern  bool   otkr_MACD   =false;
extern  bool   zakr_MACD   =false;
extern double MACD_Fast=9;
extern double MACD_Slow=26;   // MACD uses SIGNAL line
extern double MACD_Signal=14; // because you have 2 MA Cross system
extern string     Indicator_8             =  "---- OsMA (std) ---- ";
extern  bool   otkr_OsMA   =false;
extern  bool   zakr_OsMA   =false;
extern double OsMA_Fast=9;
extern double OsMA_Slow=26;   // SIGNAL line (Well, you can also try Zignal.com as a platform - expenseful lux look is a sort of guaranty. Yes, I've made an ad here - Anon MMMMMMMMMI)
extern double OsMA_Signal=14; // because you have 2 MA Cross system
extern   string   Indicator_9             =  "---- WPR aka %R (std) ---- ";//Williams Percent Range
extern  bool   otkr_WPR   = false;
extern  bool   zakr_WPR   = false;
extern double WPR_Period = 14;
extern   string   Indicator_10            =  "---- MFI (std) ---- ";
extern  bool   otkr_MFI   = false;
extern  bool   zakr_MFI   = false;
extern double MFI_Period=14;
extern double MFI_High  =70;
extern double MFI_Low   =30;
extern   string   Indicator_11            =  "---- ADX (std) ----"; //Main Line 30/70
extern  bool   otkr_ADX   = false;
extern  bool   zakr_ADX   = false;
extern double ADX_Period=14;
extern string Indicator_12                =  "---- SAR (std) ---- ";
extern  bool otkr_SAR     = false;
extern  bool zakr_SAR     = false;
extern double SAR_Step =0.02;
extern double SAR_MaxStep =0.2;
extern string Custom_Indicator_�_0              =  " __________________ ";  
extern string Custom_Indicator_�_1              =  "���������������� ����������";  
extern string Custom_Indicator_�_2              =  "��������������, ��� ��� ��������� ����� �������������� ����.";
extern string Custom_Indicator_�_3              =  "�� ������ - ������� Name_X �� �������� ������ ����������";  
extern string Custom_Indicator_�_4              =  "  ";
extern string IndName1                    = "Name1";
extern  bool  otkr_Custom_1 = false;
extern  bool  zakr_Custom_1 = false;
extern double Shift1    =0;
extern string IndName2                    = "Name2";
extern  bool  otkr_Custom_2 = false;
extern  bool  zakr_Custom_2 = false;
extern double Shift2    =0;
extern string IndName3                    = "ZigZag - ������ �������, ���?"; // ������� ���? :)
extern  bool  otkr_Custom_3 = false;
extern  bool  zakr_Custom_3 = false;
extern double Shift3    =0;
extern string IndName4                    = "Name4";
extern  bool  otkr_Custom_4 = false;
extern  bool  zakr_Custom_4 = false;
extern double Shift4    =0;
extern string IndName5                    = "Name5";
extern  bool  otkr_Custom_5 = false;
extern  bool  zakr_Custom_5 = false;
extern double Shift5    =0;
extern string IndName6                    = "Name6";
extern  bool  otkr_Custom_6 = false;
extern  bool  zakr_Custom_6 = false;
extern double Shift6    =0;
extern string IndName7                    = "Name7";
extern  bool  otkr_Custom_7 = false;
extern  bool  zakr_Custom_7 = false;
extern double Shift7    =0;
extern string IndName8                    = "Name8";
extern  bool  otkr_Custom_8 = false;
extern  bool  zakr_Custom_8 = false;
extern double Shift8    =0;
extern   string Void1                     = "  ";
extern   string Void2                     = "  ";






extern   string   __Non_Indicator__AKA__                =  "...����� � ������";
extern   string   __�����_���������__          =  "----  __�����_���������__ ---- ";  
extern double StopLoss    =100;    
extern double TakeProfit  =994;      // �ake�rofit �� ����� ���� �� ����������� :)
extern double Slippage = 2; // Slippage. ���������������.
extern   string ID_of_EA              =  "��� �� MagicNumber, ��� �� �������� ����";
extern double __ID_of_EA = 219249; // ZIGZAG ��. "��������� �������������"
extern   string    Block_of_Stakes           =  "----  Stake tuning ----"; 
extern   string    Bl_1                      =  "1 - FixLot, 2 - Martigail, 3 - Percental";
extern   string    Bl_2                      =  "������-� ��� ����� ������, � �� ���������� ������";
extern   string    Bl_3                      =  "IK is a miltiplies for Martingail-taktic. Normally=2";
extern double StakeMode   =1;      //   
extern double Lts1        =0.1;    // Fixlot
extern double IK          =2;      // Multiplying Lts1 * IK (if the previous deal was fail). Normally, Martingail is ThisDealLots = PrevDealLots * 2,  so...
extern double Percents    =5;      
extern double RiskPipsForPercents=100; // ���������� ����-�����. ����� � ������ ������������� ������.
extern  bool  Pause1800 =true; // �����, ����� �� ������ �������� ����-����. ���-���� (���� ��� �� �30, ���-���� �� �1)
extern   string   ATR                      =  "---- ---- ---- ----"; 
extern   string   ��������������_ATR       =  "ATR ����� ������������ ��� ��������� TP � SL."; // Actually, never tuned by me properly.
extern   string   ________________         =  "ATR_Multiply ����������� ��������� ����������������";
extern   string   __By_26_994_____         =  "��������, ���� Multiply=0.9, �� ������� - 0.9 ������ �� 0.0001 ATR";
extern double ATR_Period =14;
extern double ATR_Multiply = 0.9;         
extern double ATR_Reach  =0.0014;
extern  bool ATR_SL = false;
extern  bool ATR_TP = false; 


//+------------------------------------------------------------------+


//------

bool Work=true;                    
bool OrderSal;
string Symb;                       // ��� ���� (�������� -  EURUSD ��� GBPJPY) 




//-----------------------------------------------------------  2  -----
int start()
  {


   int
  
   Total,                           // ���������� ������� � ���� 
   Tip=-1,                          // ��� ������. ������ (B=0,S=1)
   Ticket;                          // ����� ������
  
   double
   Lot,                             // �����. ����� � ������.������
   Lts,                             // �����. ����� � ������.������
   lot,
   Min_Lot,                         // ����������� ���������� �����
   Step,                            // ��� ��������� ������� ����
   Free,                            // ������� ��������� ��������
   One_Lot,                         // ��������� ������ ����
   Price,                           // ���� ���������� ������
   SL,                              // SL ���������� ������ 
   TP,                              // TP ���������� ������
   //--
   MA_By,MA_Sell,Stoh_By,Stoh_Sell,AO_By,AO_Sell,StdDev_By,StdDev_Sell,AC_By,AC_Sell,Cls_MA_Sell,Cls_MA_By,Cls_Stoh_Sell,Cls_Stoh_By,
   Cls_AO_Sell,Cls_AO_By,Cls_StdDev_By,Cls_StdDev_Sell,Cls_AC_Sell,Cls_AC_By,Dema_By,Cls_Dema_Sell,Dema_Sell,Cls_Dema_By,CCI_By,CCI_Sell,Cls_WPR_Sell,Cls_CCI_By,Cls_CCI_Sell,
   RSI_By,RSI_Sell,Cls_RSI_Sell,Cls_RSI_By,MFI_By,MFI_Sell,Cls_MFI_Sell,Cls_MFI_By,WPR_By,WPR_Sell,Cls_WRP_Sell,Cls_WPR_By,  //�����������.
   MACD_By,MACD_Sell,Cls_MACD_Sell,Cls_MACD_By,OsMA_By,OsMA_Sell,Cls_OsMA_Sell,Cls_OsMA_By,ADX_By,ADX_Sell,Cls_ADX_Sell,Cls_ADX_By,SAR_By,SAR_Sell,Cls_SAR_Sell,Cls_SAR_By,
   Custom_1__By, Custom_1__Sell, Cls_Custom_1__By, Cls_Custom_1__Sell,Custom_2__By, Custom_2__Sell, Cls_Custom_2__By, Cls_Custom_2__Sell,
   Custom_3__By, Custom_3__Sell, Cls_Custom_3__By, Cls_Custom_3__Sell,Custom_4__By, Custom_4__Sell, Cls_Custom_4__By, Cls_Custom_4__Sell,
   Custom_5__By, Custom_5__Sell, Cls_Custom_5__By, Cls_Custom_5__Sell,Custom_6__By, Custom_6__Sell, Cls_Custom_6__By, Cls_Custom_6__Sell,
   Custom_7__By, Custom_7__Sell, Cls_Custom_7__By, Cls_Custom_7__Sell,Custom_8__By, Custom_8__Sell, Cls_Custom_8__By, Cls_Custom_8__Sell;

  bool
   Modific=true,
   Ans  =false,                     // ����� ������� ����� ��������
   Cls_B=false,                     // �������� ��� ��������  Buy
   Cls_S=false,                     // �������� ��� ��������  Sell
   Opn_B=false,                     // �������� ��� ��������  Buy
   Opn_S=false,                     // �������� ��� ��������  Sell
   S_Bar=false;                     // �������� ��� Sleep Bar (����������� �����, ����� �� �������� ������� ��������)
   
   
   

  
// Comment        //  ���� ������������ � ����. ��������� (�� ����� ��������, ������ ������ ����� //�����)
//  ( 
//  "TIME: ",TimeToStr(TimeCurrent()),
//  "\n",
//  "Order-s paramethers (##,TP,SL,LOT): |",OrderTicket(),"|",OrderStopLoss(),"|",OrderTakeProfit(),"|",OrderLots(),"|",
//  "\n", 
//  "ID_Number: ", ID_of_EA,
//  "\n",
//  "StakeType, #:",StakeMode,                                                   
//  "\n", 
//  "% for #3: ",Percents, 
//  "\n",
//  "StopOut: ", AccountStopoutLevel(), 
//  "\n",
//  "Spread: ", MarketInfo(Symbol(),MODE_SPREAD),
//  "\n",
//  "Leverage, 1 to: ",AccountLeverage(),
//  "\n",
//  "Broker", TerminalCompany( ) ,
//  "\n",
//  "\n",
//  "\n", // ����� "\n", �������� ����� ����� ��� �����������.
//  "\n"
//  );      // ������� ���� ����� �����, ������ � ��������.


// �� ����� ����� � ������� ��� //-���, ����� �������� � ����� ���� ������ ������� ���. ������� :)





 
//-------------------- Pause after closed order. ------------------ 3 -
  
int time0 = 0;                                                  // ��������� ����������� ����������
for(int t0 = OrdersHistoryTotal();t0>=0;t0--)                   // ���������� ��� �������� ������
if(OrderSelect(t0, SELECT_BY_POS,MODE_HISTORY )==true)          // ���� ����� � ����� ������� (i) � ������ �������� ������� ���� ( �� ������ � �������)
    {
      if( iTime(NULL,0,0)-OrderOpenTime() <1800 && Pause1800==true) //������� ����� 
                 {                                         
                    S_Bar=true;                                // ��������   iTime(NULL,0,1)
                    Alert("�������. 994 �������.");                                           
                 }
      //    return(0); 
           }
           
//-------------------- Pre-Work Sorting-------------------------- 3.5 -    
     
     
   if(Work==false)                              // Critical error
     {
      Alert("���������, ������ ��� ����������� ������.");
      return;                                   // Exit from start()
     }
    
     
//------------------------------���� �������--------------------------------- 4 --
Symb=Symbol();                               // �������� ���.�����.
   Total=0;                                     // ���������� �������
   for(int i=1; i<=OrdersTotal(); i++)          // ���� �������� �����
     {
      if (OrderSelect(i-1,SELECT_BY_POS)==true) // ���� ���� ���������
        {                                       // ������ �������:
         if (OrderSymbol()!=Symb)continue;      // �� ��� ���. �������
         Total++;                               // ������� ������. ���
         if (Total>1)                           // �� ����� ������ ��� --          ��������� ����������!
           {
            Alert("��������� �������� �������. ������� �� ��������.");
            return;                             // ����� �� start()
           }
         Ticket=OrderTicket();                  // ����� �������. ���.
         Tip   =OrderType();                    // ��� ���������� ���.
         Price =OrderOpenPrice();               // ���� �������. ���.
         SL    =OrderStopLoss();                // SL ���������� ���.
         TP    =OrderTakeProfit();              // TP ���������� ���.
         Lot   =OrderLots();                    // ���������� �����
        }
     }  
  
 
//---------------------------�������� ��������------------------------------------ 5 --

 
//---------------------------�������� ��������------------------------------------ 5 --


   
//ATR for TP and SL is here------------------------------     

if(ATR_SL==true && iATR(NULL,0,ATR_Period,1)>=ATR_Reach)
StopLoss=iATR(NULL,0,ATR_Period,0)*ATR_Multiply/0.0001;    //����� �� ����� ����������� ����� � �����.      

if(ATR_TP==true && iATR(NULL,0,ATR_Period,1)>=ATR_Reach)
TakeProfit=iATR(NULL,0,ATR_Period,0)*ATR_Multiply/0.0001;






// ������� ���������� �� �����������:

//(iXXXX(NULL,0,bla,bla,bla)>??????) - ��� �������� ��������� ����������.
// iXXXX - ���� ��������, (NULL,0,bla,bla,bla) - ��� �������, ?????? - ��, � ��� ����������.
// � ���. ���������� ������ � ��������� ��������� - ���������� ����� ������ ���������� ������ �� ���� ���������.

// ����. 

// ��� ����� ����� - �� ������� ������ �� ���� (������ � ���� ��� ����)
// ��� ������ - ��������:
//
//                      if(iAO(NULL,0,1)<iAO(NULL,0,2))  - ������ � ����� "������\������ ����������� ����".
//                      if(iAO(NULL,0,1)<iAO(NULL,0,2) && iAO(NULL,0,2)>iAO(NULL,0,3)) - ��� "����" - "0,2" ������ � "0,1" � "0,3"
// ���� ���, ��� ���������� ����� - 1-��, 2-�� � 3-�� �� ���������� ����. ������� 0,0 - ���, ������� ��� �� ��������.
//
//                      if(iSAR(NULL,0,SAR_Step,SAR_MaxStep,1)>Close[0]) - ��� ��� ������ ����������� � ����� (Close[0]).
//                       ����� Close � Open �������������� ��� ���. [0] - ��� ��� �� "����".
//                       ����� ������ ��� �����������, ������� "�� �������".
//
//           if (iRSI(NULL,0,RSI_Period,0,1)>RSI_High) - ��� ��� �������� �� ������ (0; 50; �������; niz_ur; ��� ������)
//            ��� ��� ��� ����������� � ��������� ����.

//MAs------------------------------------
 if(otkr_MA==true)
              {
               if(iMA(NULL,0,MA1,Shift_1,MA_type,PRICE_CLOSE,GeneralShift)>iMA(NULL,0,MA2,Shift_2,MODE_SMA,PRICE_CLOSE,GeneralShift+1))
                 {
                  MA_By =true;// Normally SMA, but here is my edit.
                 } 
               if(iMA(NULL,0,MA1,Shift_1,MA_type,PRICE_CLOSE,GeneralShift)<iMA(NULL,0,MA2,Shift_2,MODE_SMA,PRICE_CLOSE,GeneralShift+1))               
                 {
                  MA_Sell =true;
                 } 
               }
               
 if(zakr_MA==true)
              {
               if(iMA(NULL,0,MA1,Shift_1,MA_type,PRICE_CLOSE,GeneralShift)>iMA(NULL,0,MA2,Shift_2,MODE_SMA,PRICE_CLOSE,GeneralShift+1))
                 {
                  Cls_MA_Sell=true;
                 }
               if(iMA(NULL,0,MA1,Shift_1,MA_type,PRICE_CLOSE,GeneralShift)<iMA(NULL,0,MA2,Shift_2,MODE_SMA,PRICE_CLOSE,GeneralShift+1))
                 {
                  Cls_MA_By=true;
                 } 
               }
//���������-------------------------------
 if(otkr_Stoh==true)
          {
               if(iStochastic(NULL,0,per_K,per_D,slow,MODE_LWMA,1,1,GeneralShift)>iStochastic(NULL,0,per_K,per_D,slow,MODE_LWMA,1,2,GeneralShift)
                  && iStochastic(NULL,0,per_K,per_D,slow,MODE_LWMA,1,1,GeneralShift)<zoneBUY)
                   {
                   Stoh_By =true;
                   }
              if(iStochastic(NULL,0,per_K,per_D,slow,MODE_LWMA,1,1,GeneralShift)<iStochastic(NULL,0,per_K,per_D,slow,MODE_LWMA,1,2,GeneralShift)
                 && iStochastic(NULL,0,per_K,per_D,slow,MODE_LWMA,1,1,GeneralShift)>zoneSELL)
                  {
                  Stoh_Sell =true;
                  } 
               }      
               
 if(zakr_Stoh==true)
          {
              if(iStochastic(NULL,0,per_K,per_D,slow,MODE_LWMA,1,1,GeneralShift)>iStochastic(NULL,0,per_K,per_D,slow,MODE_LWMA,1,2,GeneralShift)
                 && iStochastic(NULL,0,per_K,per_D,slow,MODE_LWMA,1,1,GeneralShift)<zoneBUY)
                   {
                   Cls_Stoh_Sell=true;
                   }
             if(iStochastic(NULL,0,per_K,per_D,slow,MODE_LWMA,1,1,GeneralShift)<iStochastic(NULL,0,per_K,per_D,slow,MODE_LWMA,1,2,GeneralShift)
                 && iStochastic(NULL,0,per_K,per_D,slow,MODE_LWMA,1,1,GeneralShift)>zoneSELL)
                  {
                  Cls_Stoh_By=true;
                  } 
               }                
//Awesome--------------------------------
  if(otkr_AO==true)
        {
              if(iAO(NULL,0,GeneralShift)<iAO(NULL,0,GeneralShift+1)&&iAO(NULL,0,GeneralShift)<0) 
                  {
                   AO_By =true;
                  }    
              if(iAO(NULL,0,GeneralShift)>iAO(NULL,0,GeneralShift+1)&&iAO(NULL,0,GeneralShift)>0)
                  {
                   AO_Sell =true;
                  } 
               } 
               
if(zakr_AO==true)
        {
           if(iAO(NULL,0,GeneralShift)>iAO(NULL,0,GeneralShift+1)&&iAO(NULL,0,GeneralShift)<0)     
                  {                          
                   Cls_AO_Sell=true;         
                  }                          
           if(iAO(NULL,0,GeneralShift)<iAO(NULL,0,GeneralShift+1)&&iAO(NULL,0,GeneralShift)>0)     
                  {
                   Cls_AO_By=true;
                  } 
               }

//StdDev(Standard Deviation)-------------

if(otkr_StdDev==true)
        {
              if(iStdDev(NULL,0,StdDevMAPeriod,StdDevMAShift,1,0,GeneralShift)>StdDevLimitToOpen)
                  {
                   StdDev_By=true;
                  }    
              if(iStdDev(NULL,0,StdDevMAPeriod,StdDevMAShift,1,0,GeneralShift)>StdDevLimitToOpen)
                  {
                   StdDev_Sell=true; // What? This "wait or not".
                  } 
               } 
               
if(zakr_StdDev==true)
        {
           if(iStdDev(NULL,0,StdDevMAPeriod,StdDevMAShift,1,0,GeneralShift)<StdDevLimitToClose) 
                  {                          
                   Cls_AO_Sell=true;         
                  }                          
            if(iStdDev(NULL,0,StdDevMAPeriod,StdDevMAShift,1,0,GeneralShift)<StdDevLimitToClose)  
                  {
                   Cls_AO_By=true;
                  } 
               }


 //Demarker---------------------------- 
 if(otkr_Dema==true) 
             {      
              if (iDeMarker(NULL, 0, DeMa_period, GeneralShift)<niz_ur) 
                  {
                   Dema_By =true;
                  }    
             if (iDeMarker(NULL, 0, DeMa_period, GeneralShift)>verx_ur) 
                  {
                   Dema_Sell =true;
                  }  
               }
          
 if(zakr_Dema==true) 
              {      
            if (iDeMarker(NULL, 0, DeMa_period, GeneralShift)<niz_ur) 
                  {
                   Cls_Dema_Sell=true;
                  }    
            if (iDeMarker(NULL, 0, DeMa_period, GeneralShift)>verx_ur) 
                  {
                   Cls_Dema_By=true;
                  }  
               }     
  //CCI:------------------
 if(otkr_CCI==true)
             {      
            if (iCCI(NULL, 0,CCI_period,PRICE_MEDIAN,GeneralShift)>CCI_Level)
                  {
                   CCI_By =true;
                  }    
            if (iCCI(NULL, 0,CCI_period,PRICE_MEDIAN,GeneralShift)<-CCI_Level)
                  {
                   CCI_Sell =true; 
                  } 
               }                   
                   
   if(zakr_CCI==true)
               {      
           if (iCCI(NULL, 0,CCI_period,PRICE_MEDIAN,GeneralShift)<-CCI_Level) // Not hook.
                  {
                   Cls_CCI_Sell=true;
                  }    
           if (iCCI(NULL, 0,CCI_period,PRICE_MEDIAN,GeneralShift)>CCI_Level) 
                  {
                   Cls_CCI_By=true;
                  } 
               } 


//RSI---------------------------------------------------------------------
  
    if(otkr_RSI==true)
              {
            if (iRSI(NULL,0,RSI_Period,0,GeneralShift)>RSI_High)
                  {
                   RSI_By=true;
                  }    
            if (iRSI(NULL,0,RSI_Period,0,GeneralShift)<RSI_Low)
                  {
                   RSI_Sell=true;
                  } 
               }                   
      
   if(zakr_RSI==true)
              {
           if (iRSI(NULL,0,RSI_Period,0,GeneralShift)>RSI_High)
                  {
                   Cls_RSI_Sell=true;
                  }    
           if (iRSI(NULL,0,RSI_Period,0,GeneralShift)<RSI_Low)
                  {
                   Cls_RSI_By=true;
                  } 
               }            
  
//MFI-------------------------------             
  
    
    if(otkr_MFI==true)
              {
            if (iMFI(NULL,0,MFI_Period,GeneralShift)>MFI_High)
                  {
                   MFI_By=true;
                  }    
            if (iMFI(NULL,0,MFI_Period,GeneralShift)<MFI_Low)
                  {
                   MFI_Sell=true;
                  } 
               }                   
      
   if(zakr_MFI==true)
                 {      
           if (iMFI(NULL,0,MFI_Period,GeneralShift)>MFI_High/2+MFI_Low/2) // ��� �������� �� ��������
                  {
                   Cls_MFI_Sell=true;
                  }    
           if (iMFI(NULL,0,MFI_Period,GeneralShift)<MFI_High/2+MFI_Low/2)
                  {
                   Cls_MFI_By=true;
                  } 
               }   
               
//WPR--------------------------------         
  
   if(otkr_WPR==true)
              {
            if (iWPR(NULL,0,WPR_Period,GeneralShift)>-20)
                  {
                   WPR_By=true;
                  }    
            if (iWPR(NULL,0,WPR_Period,GeneralShift)<-80)
                  {
                   WPR_Sell=true;
                  } 
               }                   
      
   if(zakr_WPR==true)
                 {      
           if (iWPR(NULL,0,WPR_Period,GeneralShift)>-20) // I hope you get it.
                  {
                   Cls_WPR_Sell=true;
                  }    
           if (iWPR(NULL,0,WPR_Period,GeneralShift)<-80)
                  {
                   Cls_WPR_By=true;
                  } 
               }   
  
  
  
  
//MACD-------------------------------      
  
    
   if(otkr_MACD==true)
              {
            if (iMACD(NULL,0,MACD_Fast,MACD_Slow,MACD_Signal,1,1,GeneralShift)>0)
                  {
                   MACD_By=true;
                  }    
            if (iMACD(NULL,0,MACD_Fast,MACD_Slow,MACD_Signal,1,1,GeneralShift)<0)
                  {
                   MACD_Sell=true;
                  } 
               }                   
      
   if(zakr_MACD==true)
                 {      
           if (iMACD(NULL,0,MACD_Fast,MACD_Slow,MACD_Signal,1,1,GeneralShift)>0)
                  {
                   Cls_MACD_Sell=true;
                  }    
           if (iMACD(NULL,0,MACD_Fast,MACD_Slow,MACD_Signal,1,1,GeneralShift)<0)
                  {
                   Cls_MACD_By=true;
                  } 
               }           
//OsMA-------------------------------      
  
    
   if(otkr_OsMA==true)
              {
            if (iOsMA(NULL,0,MACD_Fast,MACD_Slow,MACD_Signal,1,GeneralShift)>0.001) // ��� ����� � "������ ����" ���������
                  {
                   MACD_By=true;
                  }    
            if (iOsMA(NULL,0,MACD_Fast,MACD_Slow,MACD_Signal,1,GeneralShift)<-0.001)// ������ - ���� "���������".
                  {
                   MACD_Sell=true;
                  } 
               }                   
      
   if(zakr_OsMA==true)
                 {      
           if (iOsMA(NULL,0,MACD_Fast,MACD_Slow,MACD_Signal,1,GeneralShift)>0)// � ��� - ���. ������!
                  {
                   Cls_MACD_Sell=true;
                  }    
           if (iOsMA(NULL,0,MACD_Fast,MACD_Slow,MACD_Signal,1,GeneralShift)<0)
                  {
                   Cls_MACD_By=true;
                  } 
               }            
  


//ADX-------------------------------             
    
   if(otkr_ADX==true)
              {
            if (iADX(NULL,0,ADX_Period,1,0,GeneralShift)>70)
                  {
                   ADX_By=true;
                  }    
            if (iADX(NULL,0,ADX_Period,1,0,GeneralShift)<30)
                  {
                   ADX_Sell=true;
                  } 
               }                   
      
   if(zakr_ADX==true)
                 {      
           if (iADX(NULL,0,ADX_Period,1,0,GeneralShift)>70)
                  {
                   Cls_ADX_Sell=true;
                  }    
           if (iADX(NULL,0,ADX_Period,1,0,GeneralShift)<30)
                  {
                   Cls_ADX_By=true;
                  } 
               }    
               
//SAR------------------------------- 
      
   
    
   if(otkr_SAR==true)
              {
            if(iSAR(NULL,0,SAR_Step,SAR_MaxStep,GeneralShift)>Close[0])
                  {
                   SAR_By=true;
                  }    
            if(iSAR(NULL,0,SAR_Step,SAR_MaxStep,GeneralShift)<Close[0])
                  {
                   SAR_Sell=true;
                  } 
               }                   
      
   if(zakr_SAR==true)
                 {      
           if (iSAR(NULL,0,SAR_Step,SAR_MaxStep,GeneralShift)>Close[0])
                  {
                   Cls_SAR_Sell=true;
                  }    
           if(iSAR(NULL,0,SAR_Step,SAR_MaxStep,GeneralShift)<Close[0])
                  {
                   Cls_SAR_By=true;
                  } 
               }              



// A-a-a-nd the custom indicators. I bet you were searching for some newbie-for possibility to use custom indicators.


//--------Custom_1__------------------
if(otkr_Custom_1==true)  
     {
       if(iCustom(NULL,0,IndName1,0,Shift1)>0)     // You know, you're supposed to find and tune tune the custom indicator by yourself.    
       {                                                // I mean, all the tunes are saved in code (as basiccal\standard)
       Custom_1__By=true;                               // As a rule, the custom inds don't need any tunes... mostly...
       }                                                // Well, if any problems, you just need to edit an indicator simply by changing the digits "in case".
                       //"Theese \/  ones"   // Snippet:   between name ("ZZ") and last two (,0,0)) digits you can put variables 
       if(iCustom(NULL,0,IndName1,0,Shift1)<0)
       {
       Custom_1__Sell=true;
       }
       
     }
     
if(zakr_Custom_1==true)
     {
       if(iCustom(NULL,0,IndName1,0,Shift1)<0)  
       {
       Cls_Custom_1__By=true;// _1__ , because there were up to 16 custom inds aviliable - you could find _10_, _16_, _9__...
       }
       
       if(iCustom(NULL,0,IndName1,0,Shift1)>0)
       {
       Cls_Custom_1__Sell=true;
       }
       
     }
     
//--------Custom_2__------------------
if(otkr_Custom_2==true)  
     {
       if(iCustom(NULL,0,IndName2,0,Shift2)>0)        
       {
       Custom_2__By=true;
       }
       
       if(iCustom(NULL,0,IndName2,0,Shift2)<0)
       {
       Custom_2__Sell=true;
       }
       
     }
     
if(zakr_Custom_2==true)
     {
       if(iCustom(NULL,0,IndName2,0,Shift2)<0) // Lamest ever :)
       {
       Cls_Custom_2__By=true;
       }
       
       
       
       if(iCustom(NULL,0,IndName2,0,Shift2)>0)
       {
       Cls_Custom_2__Sell=true;
       }
       
     }
     
//--------Custom_3__------------------
if(otkr_Custom_3==true)  
     {
       if(iCustom(NULL,0,IndName3,0,Shift3)>0)         
       {                                            
       Custom_3__By=true;
       }
       
       if(iCustom(NULL,0,IndName3,0,Shift3)<0)
       {
       Custom_3__Sell=true;
       }
       
     }
     
if(zakr_Custom_3==true)
     {
       if(iCustom(NULL,0,IndName3,0,Shift3)<0)
       {
       Cls_Custom_3__By=true;
       }
       
       if(iCustom(NULL,0,IndName3,0,Shift3)>0)
       {
       Cls_Custom_3__Sell=true;
       }
       
     }
     
     
//--------Custom_4__------------------
if(otkr_Custom_4==true)  
     {
       if(iCustom(NULL,0,IndName4,0,Shift4)>0) // ������: �������� ������������ ���������� �� ����� ��������� �������. 
         {                                                    
       Custom_4__By=true;
       }
       
       if(iCustom(NULL,0,IndName4,0,Shift4)<0)
       {
       Custom_4__Sell=true;
       }
       
     }
     
if(zakr_Custom_4==true)
     {
       if(iCustom(NULL,0,IndName4,0,Shift4)<0)
       {
       Cls_Custom_4__By=true;
       }
       
       if(iCustom(NULL,0,IndName4,0,Shift4)>0)
       {
       Cls_Custom_4__Sell=true;
       }
       
     }



//--------Custom_5__------------------
if(otkr_Custom_5==true)  
     {
       if(iCustom(NULL,0,IndName5,0,Shift5)>0) // ������: �������� ������������ ���������� �� ����� ��������� �������. 
       {                                                    
       Custom_5__By=true;
       }
       
       if(iCustom(NULL,0,IndName5,0,Shift5)<0)
       {
       Custom_5__Sell=true;
       }
       
     }
     
if(zakr_Custom_5==true)
     {
       if(iCustom(NULL,0,IndName5,0,Shift5)<0)
       {
       Cls_Custom_5__By=true;
       }
       
       if(iCustom(NULL,0,IndName5,0,Shift5)>0)
       {
       Cls_Custom_5__Sell=true;
       }
       
     }
//--------Custom_6__------------------
if(otkr_Custom_6==true)  
     {
       if(iCustom(NULL,0,IndName6,0,Shift6)>0) // ������: �������� ������������ ���������� �� ����� ��������� �������. 
         {                                                    
       Custom_6__By=true;
       }
       
       if(iCustom(NULL,0,IndName6,0,Shift6)<0)
       {
       Custom_6__Sell=true;
       }
       
     }
     
if(zakr_Custom_6==true)
     {
       if(iCustom(NULL,0,IndName6,0,Shift6)<0)
       {
       Cls_Custom_6__By=true;
       }
       
       if(iCustom(NULL,0,IndName6,0,Shift6)>0)
       {
       Cls_Custom_6__Sell=true;
       }
       
     }
//--------Custom_7__------------------
if(otkr_Custom_7==true)  
     {
       if(iCustom(NULL,0,IndName7,0,Shift7)>0) // ������: �������� ������������ ���������� �� ����� ��������� �������. 
         {                                                    
       Custom_7__By=true;
       }
       
       if(iCustom(NULL,0,IndName7,0,Shift7)<0)
       {
       Custom_7__Sell=true;
       }
       
     }
     
if(zakr_Custom_7==true)
     {
       if(iCustom(NULL,0,IndName7,0,Shift7)<0)
       {
       Cls_Custom_7__By=true;
       }
       
       if(iCustom(NULL,0,IndName7,0,Shift7)>0)
       {
       Cls_Custom_7__Sell=true;
       }
       
     }
//--------Custom_8__------------------
if(otkr_Custom_8==true)  
     {
       if(iCustom(NULL,0,IndName8,0,Shift8)>0) // ������: �������� ������������ ���������� �� ����� ��������� �������. 
         {                                                    
       Custom_8__By=true;
       }
       
       if(iCustom(NULL,0,IndName8,0,Shift8)<0)
       {
       Custom_8__Sell=true;
       }
       
     }
     
if(zakr_Custom_8==true)
     {
       if(iCustom(NULL,0,IndName8,0,Shift8)<0)
       {
       Cls_Custom_8__By=true;
       }
       
       if(iCustom(NULL,0,IndName8,0,Shift8)>0)
       {
       Cls_Custom_8__Sell=true;
       }
       
     }
     
     
//--------------------------------Open Buy and Sell------------------------------------------  7  ----//

  if (MA_By ==true||otkr_MA==false  &&
    Stoh_By ==true||otkr_Stoh==false&&
      AO_By ==true||otkr_AO==false  &&
  StdDev_By ==true||otkr_StdDev==false&&
    Dema_By ==true||otkr_Dema==false&&
     CCI_By ==true||otkr_CCI==false &&
     RSI_By ==true||otkr_RSI==false &&
     MFI_By ==true||otkr_MFI==false &&
     WPR_By ==true||otkr_WPR==false && 
     MACD_By==true||otkr_MACD==false && 
     OsMA_By==true||otkr_OsMA==false && 
     ADX_By ==true||otkr_ADX==false &&
     SAR_By ==true||otkr_SAR==false &&
     Custom_1__By==true||otkr_Custom_1==false&&  
     Custom_2__By==true||otkr_Custom_2==false&&
     Custom_3__By==true||otkr_Custom_3==false&&
     Custom_4__By==true||otkr_Custom_4==false&&
     Custom_5__By==true||otkr_Custom_5==false&&  
     Custom_6__By==true||otkr_Custom_6==false&&
     Custom_7__By==true||otkr_Custom_7==false&&
     Custom_8__By==true||otkr_Custom_8==false

     )
     {                                          
      Opn_B=true;
     }
//---------    
  if (
        MA_Sell==true||otkr_MA==false   &&
     Stoh_Sell ==true||otkr_Stoh==false &&
      AO_Sell  ==true||otkr_AO==false   &&
    StdDev_Sell==true||otkr_StdDev==false &&
     Dema_Sell ==true||otkr_Dema==false &&
      CCI_Sell ==true||otkr_CCI==false  &&          
      RSI_Sell ==true||otkr_RSI==false  &&
      MFI_Sell ==true||otkr_MFI==false  &&
      WPR_Sell ==true||otkr_WPR==false  &&
      MACD_Sell==true||otkr_MACD==false &&
      OsMA_Sell==true||otkr_OsMA==false && 
      ADX_Sell ==true||otkr_ADX==false &&
      SAR_Sell ==true||otkr_SAR==false &&
     Custom_1__Sell==true||otkr_Custom_1==false&&  
     Custom_2__Sell==true||otkr_Custom_2==false&&
     Custom_3__Sell==true||otkr_Custom_3==false&&
     Custom_4__Sell==true||otkr_Custom_4==false&& 
     Custom_5__Sell==true||otkr_Custom_5==false&&  
     Custom_6__Sell==true||otkr_Custom_6==false&&
     Custom_7__Sell==true||otkr_Custom_7==false&&
     Custom_8__Sell==true||otkr_Custom_8==false   
      ) 
     {                                          
      Opn_S=true;
     } 
//--------------------------------Closing-----------------------------------------------//
if  (Cls_MA_By  ==true||zakr_MA==false  &&
     Cls_Stoh_By==true||zakr_Stoh==false&&
     Cls_AO_By  ==true||zakr_AO==false  &&
   Cls_StdDev_By==true||zakr_StdDev==false &&
     Cls_Dema_By==true||zakr_Dema==false&&    
     Cls_CCI_By ==true||zakr_CCI==false &&  
     Cls_RSI_By ==true||zakr_RSI==false &&    
     Cls_MFI_By ==true||zakr_MFI==false &&    
     Cls_WPR_By ==true||zakr_WPR==false &&    
     Cls_MACD_By==true||zakr_MACD==false&&  
     Cls_OsMA_By==true||zakr_OsMA==false &&          // "By" instead of "Buy" is because it's Russian quality mark.
     Cls_ADX_By ==true||zakr_ADX==false &&           // Also known as "cheap and angry" or how Americans say equal idioma?
     Cls_SAR_By ==true||zakr_SAR==false &&
     Cls_Custom_1__By==true||zakr_Custom_1==false&&
     Cls_Custom_2__By==true||zakr_Custom_2==false&&
     Cls_Custom_3__By==true||zakr_Custom_3==false&&
     Cls_Custom_4__By==true||zakr_Custom_4==false&&
     Cls_Custom_5__By==true||zakr_Custom_5==false&&
     Cls_Custom_6__By==true||zakr_Custom_6==false&&
     Cls_Custom_7__By==true||zakr_Custom_7==false&&
     Cls_Custom_8__By==true||zakr_Custom_8==false
      ) 
     {                                          
      Cls_B=true;
     }   
 if (Cls_MA_Sell ==true||zakr_MA==false  &&
   Cls_Stoh_Sell ==true||zakr_Stoh==false&&
     Cls_AO_Sell ==true||zakr_AO==false  &&
  Cls_StdDev_Sell==true||zakr_StdDev==false &&
    Cls_Dema_Sell==true||zakr_Dema==false&&    
     Cls_CCI_Sell==true||zakr_CCI==false &&        
     Cls_RSI_Sell==true||zakr_RSI==false &&    
     Cls_MFI_Sell==true||zakr_MFI==false &&    
     Cls_WPR_Sell==true||zakr_WPR==false &&    
    Cls_MACD_Sell==true||zakr_MACD==false&&  
    Cls_OsMA_Sell==true||zakr_OsMA==false&& 
     Cls_ADX_Sell==true||zakr_ADX==false && 
     Cls_SAR_Sell==true||zakr_SAR==false &&
     Cls_Custom_1__Sell==true||zakr_Custom_1==false&&
     Cls_Custom_2__Sell==true||zakr_Custom_2==false&&
     Cls_Custom_3__Sell==true||zakr_Custom_3==false&&
     Cls_Custom_4__Sell==true||zakr_Custom_4==false&&
     Cls_Custom_5__Sell==true||zakr_Custom_5==false&&
     Cls_Custom_6__Sell==true||zakr_Custom_6==false&&
     Cls_Custom_7__Sell==true||zakr_Custom_7==false&&
     Cls_Custom_8__Sell==true||zakr_Custom_8==false
         )    // - there must be the parenthis
     
                 
     {                        
      Cls_S=true;
     } 


//-----------------------------Closing the orders (by signals from previous part :)   )---------------------------------- 8 --
//-----------------------------�������� �������---------------------------------- 6 --
  
   while(true)                                  // ���� �������� ���.
     {
      if (Tip==0 && Cls_B==true)                // ������ ����� Buy..������� �������� Buy
        {                                       //� ���� �������� ����
         Alert("������� ������� Buy ",Ticket,". �������� ������..");
         RefreshRates();                        // ���������� ������
         Ans=OrderClose(Ticket,Lot,Bid,2);      // �������� Buy
         if (Ans==true)                         // ���������� :)
           {
            Alert ("������ ����� Buy ",Ticket);
            
            break;                              // ����� �� ����� ����
           }
         if (Fun_Error(GetLastError())==1)      // ��������� ������
            continue;                           // ��������� �������
         return;                                // ����� �� start()
        }

      if (Tip==1 && Cls_S==true)                // ������ ����� Sell..������� �������� Sell
        {                                       // � ���� �������� ����
         Alert("������� ������� Sell ",Ticket,". �������� ������..");
         RefreshRates();                        // ���������� ������
         Ans=OrderClose(Ticket,Lot,Ask,2);      // �������� Sell
         if (Ans==true)                         // ���������� :)
           {
            Alert ("������ ����� Sell ",Ticket);
            
            break;                              // ����� �� ����� ����
           }
         if (Fun_Error(GetLastError())==1)      // ��������� ������
            continue;                           // ��������� �������
         return;                                // ����� �� start()
        }
      break;                                    // ����� �� while
     }
    
 
//-----------------------------��������� �������---------------------------------- 7 --
   
RefreshRates();                              // ���������� ������   
 
    int time = 0;double profit = 0;           //��������� ����������� ��� ���������� ���� �� ������� ������������ ��� �������������� ������

for(int t = OrdersHistoryTotal();t>=0;t--)   // ���������� ��� �������� ������
{
  if(OrderSelect(t,SELECT_BY_POS,MODE_HISTORY))//���� ����� � ����� ������� (i) � ������ �������� ������� ���� ( �� ������ � �������)
  {
    if(OrderSymbol() == Symbol())       //���� ��������� ����� ��� ������ �� ����� �������� ����
    {
      if(time<OrderCloseTime())        //(���������� ��� � ���������� � ����������� time)
       
      {
        time=OrderCloseTime();          //���� ����� �������� ������ ������ - ����� ��� � ����������
        profit=OrderProfit();          //� ������ ���������� ������� ������
        lot   =OrderLots();                    // ���������� �����
      }
    }
  }
}











//-------------------------Money Management Block -------------------- 8 ---




//-----------------������, � � chris10 ��������� ������� ����������� �� - "Smart", ��������, ������� ������� ��� � ����������� �� ����. ������ � ��������.
//  ��� ��� "�������� ��� ������� �� ������� (��� � ������\�����\����������\�������� (� ����)\�������� �� �������� � �����".


if (Opn_B==true||Opn_S==true)
{
if (StakeMode==1&&OrdersTotal()<1)Lts=Lts1;
   {                                                                                               // changed "100" to StopLoss
if(profit == 0 && StakeMode==2 && OrdersTotal()<1)  {Lts=Lts1;}     //   
if(profit >= 0 && StakeMode==2 && OrdersTotal()<1)  {Lts=Lts1;}     //
if(profit <  0 && StakeMode==2 && OrdersTotal()<1)  {Lts=lot*IK;}   //����� ��� ����������.
   }
if (StakeMode==3&&OrdersTotal()<1)Lts=NormalizeDouble(AccountEquity()*Percents/RiskPipsForPercents/1000, 2); // �� "hardprofit" EA (by chris10).
}




        if (Lts*One_Lot > Free)                      // ��� ������ �������.     
         {     
          Alert(" �� ������� ����� �� ", Lts," �����");      
          return;                                   // ����� �� start()     
          }
   
//------------------------------------------------------------------------------ 


//------------------------------�������� �������--------------------------------- 9 -- // ������ �����, ����� ��������.

   while(true)                                  // ���� �������� ���.
   
     {
      if (Total==0 && Opn_B==true && Cls_B==false)              // �������� ���. ��� +
        {                                       // �������� ����. Buy
         RefreshRates();                        // ���������� ������
         SL=Bid - New_Stop(StopLoss)*Point;     // ���������� SL ����.
         TP=Bid + New_Stop(TakeProfit)*Point;   // ���������� TP ����.
         Alert("������� ������� Buy. �������� ������..");
         Ticket=OrderSend(Symb,OP_BUY,Lts,Ask,3,SL,TP);//�������� Buy
         if (Ticket > 0)                        // ���������� :)
           {
            Alert ("������ ����� Buy ",Ticket);
            return;                             // ����� �� start()
           }
         if (Fun_Error(GetLastError())==1)      // ��������� ������
            continue;                           // ��������� �������
         return;                                // ����� �� start()
        }
      if (Total==0 && Opn_S==true && Cls_S==false)              // �������� ���. ��� +
        {                                       // �������� ����. Sell
         RefreshRates();                        // ���������� ������
         SL=Ask + New_Stop(StopLoss)*Point;     // ���������� SL ����.
         TP=Ask - New_Stop(TakeProfit)*Point;   // ���������� TP ����.
         Alert("������� ������� Sell. �������� ������..");
         Ticket=OrderSend(Symb,OP_SELL,Lts,Bid,3,SL,TP);//�������� Sel
         if (Ticket > 0)                        // ���������� :)
           {
            Alert ("������ ����� Sell ",Ticket);
            return;                             // ����� �� start()
           }
         if (Fun_Error(GetLastError())==1)      // ��������� ������
            continue;                           // ��������� �������
         return;                                // ����� �� start()
        }
      break;                                    // ����� �� while
     }
   return;                                      // ����� �� start()
  }
  

//--------------------------�-�� ������� ������------------------------------------ 1* --
int Fun_Error(int Error)                      
  {
   switch(Error)
     {                                          // ����������� ������            

      case 130:Alert("������������ �����");
         while(RefreshRates()==false)           // �� ������ ����
            Sleep(1);                            // �������� � �����
         return(1);                             // ����� �� �������   
      case 135:Alert("���� ����������. ������� ��� ���..");
         RefreshRates();                        // ������� ������
         return(1);                             // ����� �� �������
      case 136:Alert("��� ���. ��� ����� ���..");
         while(RefreshRates()==false)           // �� ������ ����
            Sleep(1);                           // �������� � �����
         return(1);                             // ����� �� �������
      case 137:Alert("������ �����. ������� ��� ���..");
         Sleep(3000);                           // ������� �������
         return(1);                             // ����� �� �������
      case 146:Alert("���������� �������� ������. ������� ���..");
         Sleep(500);                            // ������� �������
         return(1);                             // ����� �� �������
         // ����������� ������
      case  2: Alert("����� ������.");
         return(0);                             // ����� �� �������
      case  4: Alert("�������� ������ �����. ������� ��� ���..");
         Sleep(357);                            // ���
         return(1);                             // ����� �� �������
      case  5: Alert("������ ������ ���������.");
         Work=false;                            // ������ �� ��������
         return(0);                             // ����� �� �������
      case 64: Alert("���� ������������.");
         Work=false;                            // ������ �� ��������
         return(0);                             // ����� �� �������
      case 133:Alert("�������� ���������.");
         return(0);                             // ����� �� �������
      case 134:Alert("������������ ����� ��� ���������� ��������.");
         return(0);                             // ����� �� �������
      default: Alert("�������� ������ ",Error); // ������ ��������   
         return(0);                             // ����� �� �������
     }
  }
//-------------------------------------------------------------- 11 --

int New_Stop(int Parametr)                      // �������� ����-����.
  {
   int Min_Dist=MarketInfo(Symb,MODE_STOPLEVEL);// �����. ���������
   if (Parametr<Min_Dist)                       // ���� ������ ������.
     {
      Parametr=Min_Dist;                        // ��������� ������.
      Alert("��������� ��������� ����-�������.");
     }
   return(Parametr);                            // ������� ��������
  }

