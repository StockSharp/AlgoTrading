//+------------------------------------------------------------------+
//|                                                    Graal-003.mq4 |
//|                                        Copyright � 2005, Registr |
//|                                                  Exsys@pochta.ru |
//+------------------------------------------------------------------+
#property copyright "Copyright � 2005, Registr"
#property link      "Exsys@pochta.ru"
//----
#import "user32.dll"
   int MessageBoxA(int hWnd ,string lpText,string lpCaption,int uType);
#import "Registr.dll"
   string jLeft(string InStr,int Number);
   string jRight(string InStr,int Number);
   string jTrimAll(string InStr);
   string jTrimLeft(string InStr);
   string jTrimRight(string InStr);
   int    jLen(string InStr);
   string jCopy(string InStr,int Ind,int Count);
   int    jPos(string SubInStr,string InStr);
   string jGetProgramName();
   string jGetProgramDir(string ProgName);
   string jGetCurrDir();
   bool   jFileExists(string FileName);
   string jFileExt(string FileName);
   void   jDeleteLogs(string FolderName);
#import
//----
#include <stdlib.mqh>
//---- input parameters
extern double    Lots       =0.1;
extern int       TakeProfit=500;
extern int       StopLoss  =500;
extern int       Limit    =5;     // ������� ��������� ����������� ������
extern int       ProfitLevel= 30; // ����� ������ ��� ���� ������� ��� ������� ��� �����������
extern int       ModeOpeningOfWarrants=1;
/* ����� ��������� �������: 0 - ������� ����������� �������, ����� �������������� ���������� ������,
   1 - ������� �������������� ���������� ������, ����� ����������� �������, 2 - ����������� ������ 
   �������, 3 - �������������� ������ ���������� ������ */
extern int       ModeClosingOfWarrants=1;
/* ����� �������� �������: 0 - ������ ���������� � ��������� ������� �� �������� ���� ( ���������
   ��������, ������ �������, 1 - ������� ����������� �������� �������, ����� ���������� ������, 
   2 - ������� ����������� ���������� ������, ����� �������� �������, 3 - �� �� �����, ��� 2 ��,
   ���������� ������ ����������� � �������� �������(�� �����������)*/
extern bool OpenOnlyOnDepthOfFChannel=false; /*��������� ��������� ��������� ������� ������ �� ������� 
������������ ������*/
extern int  DepthOfFChannel=25; /*������� ������������ ������ �� ������� ����������� ������, � ���������, ��� 
�������� ������� ���� ����� ������������, �� ������ �� ��������*/
extern bool ReferenceOnChannel_HL=false; /*��������� ��������� ��������������� �� ����� High-Low, ��� ��������,
��� �������� �� ����� ��������� ������� Buy, ���� ���� ���� ������ ���������� Low, � ��������*/
extern int  LevelOrientationOnChannel_HL=20; /*������� ���������� � ��������� �� ������ High-Low, ��� ��������
������� ���� ����� ������������, �� ������ �� ��������*/
extern bool ToTradeInFlat=true; /*��������� ��� ��������� ��������� ��������� �� �����*/
extern int  LevelChannel_HLForFlat=20; /*������� ������ High-Low, ��� ������� ��������� ��������� ������*/
extern bool UseWPRInClosePositions=false; /*������������ �� ��������� WPR � �������� ������� � �������� �������*/
extern int  ControlLevelWPR=30; /*����������� ������� ���������� WPR*/
extern bool ToExposeContrwarrants=false; /*���������� ����������� ��� ������������ ���������� �������*/
extern bool OnlyOnePosition=false; 
/* �������������� ���������, ������� ����� ����� � ����, ������� � 
   �������������� ����� ��� �� ������������. ��������� ��� ��������� ��������� ��������� ������ ����
   ����� Buy � ���� ����� Sell, � ������ ������. ����� ��������, ������ ��� ���������� ���������� �����������,
   �.�. � ��������� ������ �������� ����� ����. ����������, � ����������, �������� ��� ������ �� ���������� �������...*/
extern bool      DeleteLogsOnClose=false; /*������� ��� log-�����, ����������� ��4, ��� ���������� ������ ���������*/
/*� ���� ������, ������� ��4 �� ����� ������������ �� ����������*/
extern bool      EraseOldFileOfReport=true; /*������� ������ ���� ���������, ���� ���� FileNameOfReport ��� ����������*/
extern string    FileNameOfReport; /*�������� ����� ���������*/
extern bool      UseSoundInOperations=false; /*������������ �������� ������������� �������� ��������*/
extern string    SoundOpenBuy="OpenBuy.wav"; /*���� ��� �������� ������ Buy*/
extern string    SoundOpenSell="OpenSell.wav"; /*���� ��� �������� ������ Sell*/
extern string    SoundOpenBuyStop="OpenBuyStop.wav"; /*���� ��� ����������� ������ BuyStop*/
extern string    SoundOpenSellStop="OpenSellStop.wav"; /*���� ��� ����������� ������ SellStop*/
extern string    SoundClosing="ClosePositions.wav"; /*���� ��� �������� ���� �������*/
//----
static int magicNumber=123456;
static string NameOfExpert="Graal-003";
static datetime prev_min=D'1.1.1'; // ����� ����������� ������������ ��������
static datetime prev_max=D'1.1.1'; // ����� ����������� ������������ ���������
//----
bool AllowTrade,OpenBuy,OpenSell,StopWorkOfExpert,beginning,WorkWPR;
bool TrendUp,TrendDn,SignalOnClosing,CheckClose,LockForClose;
datetime PrevTime,TimeOfClosing;
int CountBars,jk,CountSessions,CountOperations,CountOrders,CountBuy,CountSell;
int PerHL,TotalBuyStop,CountBuyStop,TotalSellStop,CountSellStop,MaximalDrawdown;
double iFractals_up,iFractals_lo,PrevPrice,FractalPrevUp,FractalPrevDn,UseOfDeposit;
double ChannelHLClose,MidLine,MaxH,MinL,SubMaxH,SubMinL,FChannel;
double CollectorBuy,CollectorSell,SubFractalUp,FractalUp,SubFractalDn,FractalDn;
double InfoOpenOrders[][4];
string FlagFractal;
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
int init()
  {
   int buffer;
   //bool
   AllowTrade=true;OpenBuy=false;OpenSell=false;StopWorkOfExpert=false;beginning=true;
   WorkWPR=false;TrendUp=false;TrendDn=false;SignalOnClosing=false; CheckClose=false;
   LockForClose=true;
   //datetime
   PrevTime=Time[0];TimeOfClosing=0;
   //int
   CountBars=0;jk=0;CountSessions=0;CountOperations=0;CountOrders=0;CountBuy=0;CountSell=0;
   MaximalDrawdown=0;PerHL=14;TotalBuyStop=0;CountBuyStop=0;TotalSellStop=0;CountSellStop=0;
   //double
   iFractals_up=0;iFractals_lo=0;PrevPrice=0;FractalPrevUp=0;FractalPrevDn=0;UseOfDeposit=0;
   CollectorBuy=0;CollectorSell=0;SubFractalUp=0;FractalUp=0;SubFractalDn=0;FractalDn=0;
   //string
   FlagFractal="";
//----
   string StrYear=(CharToStr(50)+CharToStr(48)+CharToStr(48)+CharToStr(54));
   string StrMonth=(CharToStr(51));
   int YearWork=StrToInteger(StrYear),MonthWork=StrToInteger(StrMonth);
   if(TimeYear(CurTime())>=YearWork)
     {//001
      if((TimeMonth(CurTime())>MonthWork)||(TimeYear(CurTime())>YearWork))
        {//002
         Alert("���� �������� ��������� ����!");
         StopWorkOfExpert=true;return(0);
        }//002
     }//001
   if(!IsTesting())
     {
      ObjectsDeleteAll(WindowOnDropped());
      Comment("");
      if((!IsDllsAllowed())||(!IsLibrariesAllowed()))
        {
         Alert("��� ������ �������� ���������� ��������� ������ �������",
          "\n"+"�� ������� � dll ���������! ������ �������� �� ��������!");
         StopWorkOfExpert=true;return(0);
        }
      if(!IsTradeAllowed())
        {
         MessageBoxA(WindowHandle(Symbol(),0),
         "��������� �� ��������� ���������! � ��������� ��������� ���������"
         +"\n"+"������� �� ������ ����� - \" ��������� ��������� ��������� \"."
         ,"��������� "+NameOfExpert+" �������� �� ���������!",16);
         StopWorkOfExpert=true;return(0);
        }
      Sleep(2000);
      PlaySound("Money.wav");
      buffer=ToInformTheUser();
      if(buffer==2) { StopWorkOfExpert=true;return(0); }
      if(!IsConnected())
        {
         Alert("����������� ���������� � ��������! ������ ��������� ��������������!");
        }
     }
   else
     {
      Alert("��������! � ������� ������ ������� �� ��������!");
     }

   if(StringLen(FileNameOfReport)==0) { FileNameOfReport=("Report"+Symbol()+Period()); }
   if(EraseOldFileOfReport) { FileDelete(FileNameOfReport); }
   //
   Print("�������� ���� init");
   return(0);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
int deinit()
  {
   if(!IsTesting())
     {
      ObjectsDeleteAll(WindowOnDropped());
      Comment("");
     }
   WriteLineInFile(FileNameOfReport,GetCurRusTime()
   +"��������� ������ ��������� "+NameOfExpert);
   if(DeleteLogsOnClose)
     {
      jDeleteLogs(jGetCurrDir()+"\\experts\logs");
      jDeleteLogs(jGetCurrDir()+"\\tester\logs");
     }
   Print("�������� ���� deinit");
   return(0);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
int start()
  {
   //if ((Year()<2005) || (Month()<10)) return (0);
   if(StopWorkOfExpert) return(0);
   bool NewBar=false;int i,size;
   //����������� �����
   int a=0; int b=0; int c=0;
   if(a==b)
     {//?
      c=a+b;
     }//?
   if(beginning)
     {
      WriteLineInFile(FileNameOfReport,GetCurRusTime()+"������ ������ ��������� "+NameOfExpert);
      if(IsTesting())WriteLineInFile(FileNameOfReport,GetCurRusTime()+"��������! � ������� ������ ������� �� ��������!");
      WriteLineInFile(FileNameOfReport,"");
      CountSessions++;
      WriteLineInFile(FileNameOfReport,GetCurRusTime()+"������ �������� ����� �"+CountSessions);
      WriteLineInFile(FileNameOfReport,"");
      beginning=false;
     }
   if(!IsTesting()&&(jk<3))
     {
      if(CalculationOfWarrants(Symbol(),0,8,0)>0)
        {
         int answer=InfoByOpenOrders();jk=3;
         if(answer==6) { StopWorkOfExpert=true;return(0); }
        }
      jk++;
     }
   if (Time[0]!=PrevTime)
     {
      CountBars++;
      NewBar=True;
      PrevPrice=0;
      PrevTime=Time[0];
     }
   if((CountBuyStop>CalculationOfWarrants(Symbol(),0,4,0))||
      (CountSellStop>CalculationOfWarrants(Symbol(),0,5,0)))
     {
      int resSS=0,resSB=0,intSize=0;
      size=ArrayRange(InfoOpenOrders,0);
      for(i=0;i<size;i++)
        {
         if(OrderSelect(InfoOpenOrders[i][0],SELECT_BY_TICKET))
           {
            if(OrderType()==InfoOpenOrders[i][2]) continue;
            else
              {
               switch(OrderType())
                 {
                  case OP_BUY:
                     CountOperations++;
                     WriteLineInFile(FileNameOfReport,GetCurRusTime()+"�������� �������� �"+CountOperations);
                     WriteLineInFile(FileNameOfReport,GetCurRusTime()
                     +"�������� ����� BuyStop ����� "+DoubleToStr(InfoOpenOrders[i][0],0)
                     +" ���������� �"+DoubleToStr(InfoOpenOrders[i][1],0)+" �� �������: "
                     +DoubleToStr(InfoOpenOrders[i][3],Digits));
                     WriteLineInFile(FileNameOfReport,GetCurRusTime()
                     +"������� ������� Buy ����� "+DoubleToStr(InfoOpenOrders[i][0],0)
                     +" ���������� �"+DoubleToStr(InfoOpenOrders[i][1],0)+" �� ����: "
                     +DoubleToStr(InfoOpenOrders[i][3],Digits));
                     InfoOpenOrders[i][2]=OP_BUY;
                     FlagFractal="buy";
                     CountBuyStop--;
                     UseOfDeposit=UseOfDeposit+((NormalizeDouble(Ask,Digits)*1000)*OrderLots());
                     if(ToExposeContrwarrants&&AllowTrade)
                       {
                        WriteLineInFile(FileNameOfReport,"��������� ����������� ������������!");
                        CountOrders++;
                        resSS=PerformanceOpenSellStop(Symbol(),Lots,(FractalDn-(Limit*Point)),5,StopLoss,TakeProfit,
                        CountOrders+" SellStop "+NameOfExpert,magicNumber,UseSoundInOperations,SoundOpenSellStop);
                        if(resSS>0)
                          {
                           CountSellStop++;TotalSellStop++;
                           intSize=ArrayRange(InfoOpenOrders,0);
                           ArrayResize(InfoOpenOrders,size+1);
                           InfoOpenOrders[intSize][0]=resSS;
                           InfoOpenOrders[intSize][1]=CountOrders;
                           InfoOpenOrders[intSize][2]=OP_SELLSTOP;
                           InfoOpenOrders[intSize][3]=NormalizeDouble((FractalDn-(Limit*Point)),Digits);
                          }
                        else
                          {
                           WriteLineInFile(FileNameOfReport,GetCurRusTime()+"�� ������� ��������� ���������� ����������!");
                           CountOrders--;
                          }
                       }
                     WriteLineInFile(FileNameOfReport,"");
                     break;
                  case OP_SELL:
                     CountOperations++;
                     WriteLineInFile(FileNameOfReport,GetCurRusTime()+"�������� �������� �"+CountOperations);
                     WriteLineInFile(FileNameOfReport,GetCurRusTime()
                     +"�������� ����� SellStop ����� "+DoubleToStr(InfoOpenOrders[i][0],0)
                     +" ���������� �"+DoubleToStr(InfoOpenOrders[i][1],0)+" �� �������: "
                     +DoubleToStr(InfoOpenOrders[i][3],Digits));
                     WriteLineInFile(FileNameOfReport,GetCurRusTime()
                     +"������� ������� Sell ����� "+DoubleToStr(InfoOpenOrders[i][0],0)
                     +" ���������� �"+DoubleToStr(InfoOpenOrders[i][1],0)+" �� ����: "
                     +DoubleToStr(InfoOpenOrders[i][3],Digits));
                     InfoOpenOrders[i][2]=OP_SELL;
                     FlagFractal="sell";
                     CountSellStop--;
                     UseOfDeposit=UseOfDeposit+((NormalizeDouble(Bid,Digits)*1000)*OrderLots());
                     if(ToExposeContrwarrants&&AllowTrade)
                       {
                        WriteLineInFile(FileNameOfReport,"��������� ����������� ������������!");
                        CountOrders++;
                        resSB=PerformanceOpenBuyStop(Symbol(),Lots,(FractalUp+(Limit*Point)),5,StopLoss,TakeProfit,
                        CountOrders+" SellStop "+NameOfExpert,magicNumber,UseSoundInOperations,SoundOpenBuyStop);
                        if(resSB>0)
                          {
                           CountBuyStop++;TotalBuyStop++;
                           intSize=ArrayRange(InfoOpenOrders,0);
                           ArrayResize(InfoOpenOrders,size+1);
                           InfoOpenOrders[intSize][0]=resSB;
                           InfoOpenOrders[intSize][1]=CountOrders;
                           InfoOpenOrders[intSize][2]=OP_BUYSTOP;
                           InfoOpenOrders[intSize][3]=NormalizeDouble(FractalUp+(Limit*Point),Digits);
                          }
                        else
                          {
                           WriteLineInFile(FileNameOfReport,GetCurRusTime()+"�� ������� ��������� ���������� ����������!");
                           CountOrders--;
                          }
                       }
                     WriteLineInFile(FileNameOfReport,"");
                     break;
                  default:
                     WriteLineInFile(FileNameOfReport,GetCurRusTime()+"�������� � ������� �� �������!");
                     Print("�������� � ������� �� �������!");
                     break;
                 }
              }
           }
         else
           {
            WriteLineInFile(FileNameOfReport,GetCurRusTime()+"�� ������� ������� ����� "+ InfoOpenOrders[i][0]
            +" ���������� �"+InfoOpenOrders[i][1]+", �������: "+ErrorDescription(GetLastError()));
            Print("�� ������� ������� ����� "+ InfoOpenOrders[i][0]+" ���������� �"+InfoOpenOrders[i][1]
            +", �������: "+ErrorDescription(GetLastError()));
           }
        }
     }
   if(MaximalDrawdown>(CalculationOfWarrants(Symbol(),magicNumber,0,1)+CalculationOfWarrants(Symbol(),magicNumber,1,1)))
     {
      MaximalDrawdown=CalculationOfWarrants(Symbol(),magicNumber,0,1)+CalculationOfWarrants(Symbol(),magicNumber,1,1);
     }
   if (CalculationOfWarrants(Symbol(),magicNumber,6,1)>ProfitLevel)
     {
      AllowTrade=false;
      if(LockForClose)
        {
         SignalOnClosing=true;
         WriteLineInFile(FileNameOfReport,GetCurRusTime()
         +"����� ������� ������� �������� ProfitLevel, ������������ ������ �� �������� ������� - SignalOnClosing");
         if(!UseWPRInClosePositions) { WriteLineInFile(FileNameOfReport,""); }
        }
     }
   if(SignalOnClosing&&UseWPRInClosePositions&&LockForClose)
     {
      double InitWPRPrev=0,InitWPRCurr=0;
      InitWPRPrev=iWPR(Symbol(),0,14,1);
      InitWPRCurr=iWPR(Symbol(),0,14,0);
      if((InitWPRPrev>(0-ControlLevelWPR))&&(InitWPRCurr>InitWPRPrev))
        {
         WriteLineInFile(FileNameOfReport,GetCurRusTime()
         +"������ SignalOnClosing ������� �������� \"WPR\", �.�. ����� �����");
         WorkWPR=true;TrendUp=true;SignalOnClosing=false;
        }
      if((InitWPRPrev<(-100+ControlLevelWPR))&&(InitWPRCurr<InitWPRPrev))
        {
         WriteLineInFile(FileNameOfReport,GetCurRusTime()
         +"������ SignalOnClosing ������� �������� \"WPR\", �.�. ����� ����");
         WorkWPR=true;TrendDn=true;SignalOnClosing=false;
        }
      WriteLineInFile(FileNameOfReport,"");
      LockForClose=false;
     }
   if(WorkWPR)
     {
      double WPRPrev=0,WPRCurr=0;
      WPRPrev=iWPR(Symbol(),0,14,1);
      WPRCurr=iWPR(Symbol(),0,14,0);
      if(TrendUp&&(WPRCurr<(0-ControlLevelWPR)))
        {
         WriteLineInFile(FileNameOfReport,GetCurRusTime()
         +"������ \"WPR\" ������������ ������ �� �������� ������� - SignalOnClosing");
         WriteLineInFile(FileNameOfReport,"");
         WorkWPR=false;TrendUp=false;SignalOnClosing=true;
        }
      if(TrendDn&&(WPRCurr>(-100+ControlLevelWPR)))
        {
         WriteLineInFile(FileNameOfReport,GetCurRusTime()
         +"������ \"WPR\" ������������ ������ �� �������� ������� - SignalOnClosing");
         WriteLineInFile(FileNameOfReport,"");
         WorkWPR=false;TrendDn=false;SignalOnClosing=true;
        }
      if ((CalculationOfWarrants(Symbol(),magicNumber,0,1))
      +(CalculationOfWarrants(Symbol(),magicNumber,1,1))<ProfitLevel/2)
        {
         WriteLineInFile(FileNameOfReport,GetCurRusTime()
         +"�������� ������� \"WPR\" ��������,������������ ������ �� �������� ������� - SignalOnClosing");
         WriteLineInFile(FileNameOfReport,"");
         WorkWPR=false;TrendUp=false;TrendDn=false;SignalOnClosing=true;
        }
     }
   if(SignalOnClosing)
     {
      if(!IsTesting())
        {
         if((UseSoundInOperations)&&(CheckSoundFileName(SoundClosing)=="")) PlaySound(SoundClosing);
        }
      CloseAllPositions(Symbol(),magicNumber,ModeClosingOfWarrants);
      SetBadge("K",CurTime(),((Ask+Bid)/2)-3*Point,170,Violet);
      SignalOnClosing=false;LockForClose=false;CheckClose=true;
     }
   if(CheckClose)
     {
      if(CalculationOfWarrants(Symbol(),magicNumber,8,0)==0)
        {
         WriteLineInFile(FileNameOfReport,"��� ������ �������!");
         WriteLineInFile(FileNameOfReport,GetCurRusTime()+"��������� �������� ����� �"+CountSessions);
         WriteLineInFile(FileNameOfReport,"");
         WriteLineInFile(FileNameOfReport,"����� �� ������: ");
         WriteLineInFile(FileNameOfReport,"����� ������� �������: "+CountOrders);
         WriteLineInFile(FileNameOfReport,"����� ������� ������� Buy: "+CountBuy+" ,������� SellStop: "
         +TotalSellStop+" ,�� ��� �����������: "+(TotalSellStop-CountSellStop));
         WriteLineInFile(FileNameOfReport,"����� ������� ������� Sell: "+CountSell+" ,������� BuyStop: "
         +TotalBuyStop+" ,�� ��� �����������: "+(TotalBuyStop-CountBuyStop));
         WriteLineInFile(FileNameOfReport,"����� �������� �������: "+TimeToStr(TimeOfClosing,TIME_SECONDS ));
         WriteLineInFile(FileNameOfReport,"�������/������ �� �������� Buy: "+DoubleToStr(CollectorBuy,2));
         WriteLineInFile(FileNameOfReport,"�������/������ �� �������� Sell: "+DoubleToStr(CollectorSell,2));
         WriteLineInFile(FileNameOfReport,"�������/������ �� ���� ��������: "+DoubleToStr((CollectorBuy+CollectorSell),2));
         WriteLineInFile(FileNameOfReport,"���������� ������������� ��������: "+DoubleToStr(UseOfDeposit,2));
         WriteLineInFile(FileNameOfReport,"������������� ������������� ��������: "
         +DoubleToStr((UseOfDeposit/((AccountBalance()-(CollectorBuy+CollectorSell))/100)),2)+"%");
         WriteLineInFile(FileNameOfReport,"������������ �������� �� ������: "+MaximalDrawdown);
         WriteLineInFile(FileNameOfReport,"");
         CountBuy=0;TotalSellStop=0;CountSellStop=0;CountSell=0;TotalBuyStop=0;CountBuyStop=0;
         TimeOfClosing=0;CollectorBuy=0;CollectorSell=0;UseOfDeposit=0;MaximalDrawdown=0;
         CountOperations=0;CountOrders=0;WorkWPR=false;TrendUp=false;TrendDn=false;
         SignalOnClosing=false;LockForClose=true;CheckClose=false;//FlagFractal="";
         ArrayResize(InfoOpenOrders,0);
         Sleep(5000);
         WriteLineInFile(FileNameOfReport,"");
         WriteLineInFile(FileNameOfReport,"");
         CountSessions++;
         WriteLineInFile(FileNameOfReport,GetCurRusTime()+"������ �������� ����� �"+CountSessions);
         WriteLineInFile(FileNameOfReport,"");
         AllowTrade=true;
        }
     }
   for(i=0; i < 300; i++)
     {
      iFractals_up=iFractals(NULL, 0, MODE_UPPER, i);
      iFractals_lo=iFractals(NULL, 0, MODE_LOWER, i);
      if ((iFractals_up!=0) || (iFractals_lo!=0)) break;
     }
   if ((iFractals_up!=0) && (Time[i]!=prev_max) && (i < 3))
     {
      prev_max=Time[i];
      if(AllowTrade)
        {
         OpenSell=true;
         WriteLineInFile(FileNameOfReport,GetCurRusTime()+"��������� iFractals ������������ ������ OpenSell");
        }
     }
   if ((iFractals_lo!=0) && (Time[i]!=prev_min) && (i < 3))
     {
      prev_min=Time[i];
      if(AllowTrade)
        {
         OpenBuy=true;
         WriteLineInFile(FileNameOfReport,GetCurRusTime()+"��������� iFractals ������������ ������ OpenBuy");
        }
     }
   FractalUp=0;FractalDn=0;
   FractalUp=iFractals_up;
   FractalDn=iFractals_lo;
   if(FractalUp==0)
     {
      if(FractalPrevUp==0)
        {
         FractalUp=High[PerHL];
         for(i=0;i<PerHL;i++)
           {
            if (FractalUp<High[i]) FractalUp=High[i];
           }
        }
         else FractalUp=FractalPrevUp;
     }
      else FractalPrevUp=FractalUp;
   if(FractalDn==0)
     {
      if(FractalPrevDn==0)
        {
         FractalDn=Low[PerHL];
         for(i=0;i<PerHL;i++)
           {
            if (FractalDn>Low[i])  FractalDn=Low[i];
           }
        }
         else FractalDn=FractalPrevDn;
     }
      else FractalPrevDn=FractalDn;
   if(OpenOnlyOnDepthOfFChannel)
     {
      FChannel=MathRound((FractalUp-FractalDn)/Point);
      SubFractalUp=FractalUp-(((FChannel/100)*DepthOfFChannel)*Point);
      SubFractalDn=FractalDn+(((FChannel/100)*DepthOfFChannel)*Point);
      if(!IsTesting())
        {
         bool R1=DrawChannel("FChannel",FractalUp,FractalDn,SubFractalUp,SubFractalDn);
         if(!R1) WriteLineInFile(FileNameOfReport,GetCurRusTime()+"������ � ����������� ������ FChannel");
        }
      if(DepthOfFChannel>0)
        {
         if(OpenBuy)
           {
            RefreshRates();
            if(Close[0]<SubFractalDn)
              {
               OpenBuy=false;
               SetBadge("C",CurTime(),Close[0],76,MediumVioletRed);
               WriteLineInFile(FileNameOfReport,GetCurRusTime()+"������ OpenBuy ������� �������� \"FChannel\", �.�. ���� �� �������� �������� ������� ������");
               WriteLineInFile(FileNameOfReport,"");
              }
           }
         if(OpenSell)
           {
            RefreshRates();
            if(Close[0]>SubFractalUp)
              {
               OpenSell=false;
               SetBadge("C",CurTime(),Close[0],76,DodgerBlue);
               WriteLineInFile(FileNameOfReport,GetCurRusTime()+"������ OpenSell ������� �������� \"FChannel\", �.�. ���� �� �������� �������� ������� ������");
               WriteLineInFile(FileNameOfReport,"");
              }
           }
        }
     }
   MaxH=Close[PerHL];MinL=Close[PerHL];
   for(i=1;i<PerHL;i++)
     {
      if (MaxH<Close[i]) MaxH=Close[i];
      if (MinL>Close[i]) MinL=Close[i];
     }
   MidLine=NormalizeDouble((MaxH+MinL)/2,Digits);
   ChannelHLClose=MathRound((MaxH-MinL)/Point);
   if(ReferenceOnChannel_HL)
     {
      SubMaxH=MaxH-(((ChannelHLClose/100)*LevelOrientationOnChannel_HL)*Point);
      SubMinL=MinL+(((ChannelHLClose/100)*LevelOrientationOnChannel_HL)*Point);
      if(!IsTesting())
        {
         bool R2=DrawChannel("ChannelHLClose",MaxH,MinL,SubMaxH,SubMinL,MidLine);
         if(!R2) WriteLineInFile(FileNameOfReport,GetCurRusTime()+"������ � ����������� ������ ChannelHLClose");
        }
      if(LevelOrientationOnChannel_HL>0)
        {
         if(OpenBuy)
           {
            RefreshRates();
            if(Close[0]<SubMinL)
              {
               OpenBuy=false;
               SetBadge("R",CurTime(),Close[0],251,MediumVioletRed);
               WriteLineInFile(FileNameOfReport,GetCurRusTime()+"������ OpenBuy ������� �������� \"ChannelHLClose\", �.�. ���� ���� ������ ���������� ������");
               WriteLineInFile(FileNameOfReport,"");
              }
           }
         if(OpenSell)
           {
            RefreshRates();
            if(Close[0]>SubMaxH)
              {
               OpenSell=false;
               SetBadge("R",CurTime(),Close[0],251,DodgerBlue);
               WriteLineInFile(FileNameOfReport,GetCurRusTime()+"������ OpenBuy ������� �������� \"ChannelHLClose\", �.�. ���� ���� ������ ���������� ������");
               WriteLineInFile(FileNameOfReport,"");
              }
           }
        }
     }
   if(!ToTradeInFlat)
     {
      if((ChannelHLClose<LevelChannel_HLForFlat)
      &&(CalculationOfWarrants(Symbol(),magicNumber,0,0)==CalculationOfWarrants(Symbol(),magicNumber,1,0)))
        {
         if(OpenBuy)
           {
            SetBadge("F",CurTime(),((Ask+Bid)/2)-3*Point,104,MediumVioletRed);
            WriteLineInFile(FileNameOfReport,GetCurRusTime()
            +"������ OpenBuy ������� �������� \"Flat\", �.�. �������� ��������� ����������");
            WriteLineInFile(FileNameOfReport,"");
            OpenBuy=false;
           }
         if(OpenSell)
           {
            SetBadge("F",CurTime(),((Ask+Bid)/2)+3*Point,104,DodgerBlue);
            WriteLineInFile(FileNameOfReport,GetCurRusTime()
            +"������ OpenSell ������� �������� \"Flat\", �.�. �������� ��������� ����������");
            WriteLineInFile(FileNameOfReport,"");
            OpenSell=false;
           }
        }
     }
   if(OnlyOnePosition&&ToExposeContrwarrants)
     {
      if(OpenBuy)
        {
         if(CalculationOfWarrants(Symbol(),magicNumber,0,0)>0)
           {
            WriteLineInFile(FileNameOfReport,GetCurRusTime()
            +"������ OpenBuy ������� �������� \"OnlyOnePosition\", �.�. ������� ��� Buy ����");
            WriteLineInFile(FileNameOfReport,"");
            OpenBuy=false;
           }
        }
      if(OpenSell)
        {
         if(CalculationOfWarrants(Symbol(),magicNumber,1,0)>0)
           {
            WriteLineInFile(FileNameOfReport,GetCurRusTime()
            +"������ OpenSell ������� �������� \"OnlyOnePosition\", �.�. ������� ��� Sell ����");
            WriteLineInFile(FileNameOfReport,"");
            OpenSell=false;
           }
        }
     }
   if(OpenBuy)
     {
      if(FlagFractal=="buy")
        {
         SetBadge("Z",CurTime(),((Ask+Bid)/2)-3*Point,65,MediumVioletRed);
         WriteLineInFile(FileNameOfReport,GetCurRusTime()
         +"������ OpenBuy ������� �������� \"DoublePosition\", �.�. �� ���� ������ ������� Buy ��� ����");
         OpenBuy=false;
         WriteLineInFile(FileNameOfReport,"");
        }
      else WriteLineInFile(FileNameOfReport,"");
     }
   if(OpenSell)
     {
      if(FlagFractal=="sell")
        {
         SetBadge("Z",CurTime(),((Ask+Bid)/2)+3*Point,65,DodgerBlue);
         WriteLineInFile(FileNameOfReport,GetCurRusTime()
         +"������ OpenSell ������� �������� \"DoublePosition\", �.�. �� ���� ������ ������� Sell ��� ����");
         OpenSell=false;
         WriteLineInFile(FileNameOfReport,"");
        }
      else WriteLineInFile(FileNameOfReport,"");
     }
   if((OpenBuy)&&(AllowTrade))
     {
      int resBuy,resSellStop,process1=0,mode1=ModeOpeningOfWarrants;
      CountOperations++;
      WriteLineInFile(FileNameOfReport,GetCurRusTime()+"�������� �������� �"+CountOperations);
      SetBadge("B",CurTime(),((Ask+Bid)/2)-3*Point,241,Red);
      if((mode1==0)||(mode1==1)) process1=2;
      if((mode1==2)||(mode1==3)) process1=1;
      while(process1>0)
        {
         if((mode1==0)||(mode1==2))
           {
            CountOrders++;
            resBuy=PerformanceOpenBuy(Symbol(),Lots,5,StopLoss,TakeProfit,CountOrders
            +" Buy "+NameOfExpert,magicNumber,UseSoundInOperations,SoundOpenBuy);
            if(resBuy>0)
              {
               CountBuy++;process1--;FlagFractal="buy";
               size=ArrayRange(InfoOpenOrders,0);
               ArrayResize(InfoOpenOrders,size+1);
               InfoOpenOrders[size][0]=resBuy;
               InfoOpenOrders[size][1]=CountOrders;
               InfoOpenOrders[size][2]=OP_BUY;
               InfoOpenOrders[size][3]=NormalizeDouble(Ask,Digits);
               UseOfDeposit=UseOfDeposit+((NormalizeDouble(Ask,Digits)*1000)*Lots);
              }
            else
              {
               WriteLineInFile(FileNameOfReport,GetCurRusTime()+"�� ������� ��������� �������� ��������!");
               CountOrders--;
               break;
              }
            if(mode1==0) mode1=3;
           }
         if((mode1==1)||(mode1==3))
           {
            CountOrders++;
            resSellStop=PerformanceOpenSellStop(Symbol(),Lots,(FractalDn-(Limit*Point)),5,StopLoss,TakeProfit,
            CountOrders+" SellStop "+NameOfExpert,magicNumber,UseSoundInOperations,SoundOpenSellStop);
            if(resSellStop>0)
              {
               CountSellStop++;TotalSellStop++;process1--;
               size=ArrayRange(InfoOpenOrders,0);
               ArrayResize(InfoOpenOrders,size+1);
               InfoOpenOrders[size][0]=resSellStop;
               InfoOpenOrders[size][1]=CountOrders;
               InfoOpenOrders[size][2]=OP_SELLSTOP;
               InfoOpenOrders[size][3]=NormalizeDouble((FractalDn-(Limit*Point)),Digits);
              }
            else
              {
               WriteLineInFile(FileNameOfReport,GetCurRusTime()+"�� ������� ��������� �������� ��������!");
               CountOrders--;
               break;
              }
            if(mode1==1) mode1=2;
           }
        }
      WriteLineInFile(FileNameOfReport,"");
      OpenBuy=false;
     }
   if((OpenSell)&&(AllowTrade))
     {
      int resSell,resBuyStop,process2=0,mode2=ModeOpeningOfWarrants;
      CountOperations++;
      WriteLineInFile(FileNameOfReport,GetCurRusTime()+"�������� �������� �"+CountOperations);
      SetBadge("S",CurTime(),((Ask+Bid)/2)+7*Point,242,Blue);
      if((mode2==0)||(mode2==1)) process2=2;
      if((mode2==2)||(mode2==3)) process2=1;
      while(process2>0)
        {
         if((mode2==0)||(mode2==2))
           {
            CountOrders++;
            resSell=PerformanceOpenSell(Symbol(),Lots,5,StopLoss,TakeProfit,CountOrders
            +" Sell "+NameOfExpert,magicNumber,UseSoundInOperations,SoundOpenSell);
            if(resSell>0)
              {
               CountSell++;process2--;FlagFractal="sell";
               size=ArrayRange(InfoOpenOrders,0);
               ArrayResize(InfoOpenOrders,size+1);
               InfoOpenOrders[size][0]=resSell;
               InfoOpenOrders[size][1]=CountOrders;
               InfoOpenOrders[size][2]=OP_SELL;
               InfoOpenOrders[size][3]=NormalizeDouble(Bid,Digits);
               UseOfDeposit=UseOfDeposit+((NormalizeDouble(Bid,Digits)*1000)*Lots);
              }
            else
              {
               WriteLineInFile(FileNameOfReport,GetCurRusTime()+"�� ������� ��������� �������� ��������!");
               CountOrders--;
               break;
              }
            if(mode2==0) mode2=3;
           }
         if((mode2==1)||(mode2==3))
           {
            CountOrders++;
            resBuyStop=PerformanceOpenBuyStop(Symbol(),Lots,(FractalUp+(Limit*Point)),5,StopLoss,TakeProfit,
            CountOrders+" SellStop "+NameOfExpert,magicNumber,UseSoundInOperations,SoundOpenBuyStop);
            if(resBuyStop>0)
              {
               CountBuyStop++;TotalBuyStop++;process2--;
               size=ArrayRange(InfoOpenOrders,0);
               ArrayResize(InfoOpenOrders,size+1);
               InfoOpenOrders[size][0]=resBuyStop;
               InfoOpenOrders[size][1]=CountOrders;
               InfoOpenOrders[size][2]=OP_BUYSTOP;
               InfoOpenOrders[size][3]=NormalizeDouble(FractalUp+(Limit*Point),Digits);
              }
            else
              {
               WriteLineInFile(FileNameOfReport,GetCurRusTime()+"�� ������� ��������� �������� ��������!");
               CountOrders--;
               break;
              }
            if(mode2==1) mode2=2;
           }
        }
      WriteLineInFile(FileNameOfReport,"");
      OpenSell=false;
     }
   if(!IsTesting())
     {
      string com=("����: "+GetCurRusTime());
      if(OpenOnlyOnDepthOfFChannel) { com=com + ("  ����� FChannel: "+DoubleToStr(FChannel,2)); }
      if(ReferenceOnChannel_HL) { com=com + ("  ����� ChannelHLClose: "+DoubleToStr(ChannelHLClose,2)); }
      Comment(com);
     }
   return(0);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
int ToInformTheUser()
  {
   string InitialMsg,Msg0,Msg1,Msg2,Msg3,Msg4,Msg5,Msg6,Msg7,Msg8,Msg9,Msg10,Msg11,Msg12,Msg13,Msg14,Msg15,Msg16,
   Msg18,Msg19,Msg20,Msg21,Msg22,Msg23,Msg24,Msg25,Msg26,Msg27,Msg28,Msg29,Msg30,Msg31,Msg32,Msg33,Msg34,
   Msg35,Msg36,Msg37,Msg38,Msg39,Msg40,Msg41,Msg42,Msg43,Msg44,Msg45,Msg46,Msg47,Msg48,Msg49,Msg50,Msg51,Msg52,
   Msg53,Msg54,Msg55,Msg56,Msg57,Msg58,Msg59;
   if(IsDemo()) { Msg0=" �� ����-����� �"+AccountNumber(); } else { Msg0=" �� �������� ����� �"+AccountNumber(); }
   Msg1=("������� "+NameOfExpert+" �������� ���� ������"+Msg0+" �� ���������� �������� �����������: "+"\n");
   Msg2=(""+"\n");
   Msg3=("���������� � �����:"+"\n");
   Msg4=(""+"\n");
   Msg5=("�������� ���������� ��������: "+" "+AccountCompany()+"\n");
   Msg6=("������������ ��������� �����: "+AccountName()+"\n"+"\n");
   Msg7=("����� ����� � �������: "+AccountNumber()+"  ");
   Msg8=("������ �����: "+AccountCurrency()+"  ");
   Msg9=("��������� �����: 1 � "+AccountLeverage()+"\n");
   Msg10=(""+"\n");
   Msg11=("��� �������: "+""+DoubleToStr(AccountBalance(),2)+"  ");
   Msg12=("������� ������ (��������): "+""+DoubleToStr(AccountEquity(),2)+"  ");
   if(AccountMargin()!=0) Msg13=("�����: "+""+DoubleToStr(AccountMargin(),2)+"  "); else Msg13="";
   Msg14=("��������� ��������: "+""+DoubleToStr(AccountFreeMargin(),2)+"\n");
   if(AccountMargin()!=0) Msg15=("��������������� �������/������: "+""+DoubleToStr(AccountProfit(),2)
   +"\n"); else Msg15="";
   Msg16=(""+"\n");
   Msg18=("������� ���������:"+"\n");
   Msg19=(""+"\n");
   Msg20=("Lots: "+"           "+DoubleToStr(Lots,1)+(" (������ ����)")+"\n");
   Msg21=("TakeProfit: "+" "+TakeProfit+(" (����� ������� �������� �������)")+"\n");
   Msg22=("StopLoss: "+"   "+StopLoss+(" (����� ������� �������� ������)")+"\n");
   Msg23=("Limit: "+"           "+Limit
   +(" (��������, �� �������, ���� ��� ���� ������ ��������, ������������ ���������� ������)")+"\n");
   Msg24=("ProfitLevel: "+" "+ProfitLevel+(" (������� �������, ��� ������� ����������� ��� ������)")+"\n");
   Msg25=(""+"\n");
   Msg26=("ModeOpeningOfWarrants: "+ModeOpeningOfWarrants+" (����� �������� �������)"+"\n");
   Msg27=("ModeClosingOfWarrants: "+"  "+ModeClosingOfWarrants+" (����� �������� �������)"+"\n");
   Msg28=(""+"\n");
   if(OpenOnlyOnDepthOfFChannel)
     {
      Msg29="��";
      Msg31=("DepthOfFChannel: "+"                       "+DepthOfFChannel
      +" (������� �������� �������, � ��������� ������������ ������)"+"\n");
     }
   else
     {
      Msg29="���";Msg31="";
     }
   Msg30=("OpenOnlyOnDepthOfFChannel: "+"  "+Msg29+" (����������� ������ �� ������� ������������ ������)"+"\n");
   if(ReferenceOnChannel_HL)
     {
      Msg32="��";
      Msg34=("LevelOrientationOnChannel_HL: "+" "+LevelOrientationOnChannel_HL+" (������� ���������� � ��������� �� ������ High-Low)"+"\n");
     }
   else
     {
      Msg32="���";Msg34="";
     }
   Msg33=("ReferenceOnChannel_HL: "+"           "+Msg32+" (��������� ��������� ��������������� �� ����� High-Low)"+"\n");
   if(ToTradeInFlat)
     {
      Msg35="��";
      Msg37="";
     }
   else
     {
      Msg35="���";
      Msg37=("LevelChannel_HLForFlat: "+"             "+LevelChannel_HLForFlat+" (������� ������ High-Low, ��� ������� ��������� ��������� ������)"+"\n");
     }
   Msg36=("ToTradeInFlat: "+"                             "+Msg35+" (��������� ��������� ��������� �� �����)"+"\n");
   if(UseWPRInClosePositions)
     {
      Msg38="��";
      Msg40=("ControlLevelWPR: "+"                        "+ControlLevelWPR+" (����������� ������� ���������� WPR)"+"\n");
     }
   else
     {
      Msg38="���";Msg40="";
     }
   Msg39=("UseWPRInClosePositions: "+"            "+Msg38+" (������������ ��������� WPR � �������� ������� � �������� �������)"+"\n");
   if(ToExposeContrwarrants)
     {
      Msg41="��";
     }
   else
     {
      Msg41="���";
     }
   Msg42=("ToExposeContrwarrants: "+"             "+Msg41+" (���������� ����������� ��� ������������ ���������� �������)"+"\n");
   Msg43=(""+"\n");
   if(DeleteLogsOnClose)
     {
      Msg56="��";
     }
   else
     {
      Msg56="���";
     }
   Msg59=("DeleteLogsOnClose: "+"               "+Msg56+" (������� ��� log-����� ����������� ���������� � ��������)"+"\n");
   if(EraseOldFileOfReport)
     {
      Msg44="��";
      Msg47=("-- ��������! ���� ��������� ���� ��������� ��� ����������, �� ����� ����! --"+"\n");
     }
   else
     {
      Msg44="���";
      Msg47=("-- ��������! ���� ��������� ���� ��������� ��� ����������, � ���� ����� ���������� ������! --"+"\n");
     }
   Msg45=("EraseOldFileOfReport: "+"           "+Msg44+" (������� ������ ���� ���������, ���� ���� ��� ����������)"+"\n");
//----
   if(StringLen(FileNameOfReport)==0)
     {//14
      Msg46=("FileNameOfReport: "+""+"�� ������� �������� ����� ���������. ����� ������������ �������� Report"+Symbol()+Period()+"\n");
     }
   else
     {
      Msg46=("FileNameOfReport: "+"                     "+FileNameOfReport+" (�������� ����� ���������)"+"\n");
     }
   Msg48=(""+"\n");
   if(UseSoundInOperations)
     {
      Msg49="��";
      if(StringLen(SoundOpenBuy)==0) SoundOpenBuy="";
      if(StringLen(SoundOpenSell)==0) SoundOpenSell="";
      if(StringLen(SoundOpenBuyStop)==0) SoundOpenBuyStop="";
      if(StringLen(SoundOpenSellStop)==0) SoundOpenSellStop="";
      if(StringLen(SoundClosing)==0) SoundClosing="";
      if(StringLen(CheckSoundFileName(SoundOpenBuy))==0)
        {
         Msg51=("SoundOpenBuy: "+"             "+SoundOpenBuy+" (���� ��� �������� ������ Buy)"+"\n");
        }
      else
        {
         Msg51=("SoundOpenBuy: "+"        "+CheckSoundFileName(SoundOpenBuy)+" ����� ��� Buy �� �����."+"\n");
        }
      if(StringLen(CheckSoundFileName(SoundOpenSell))==0)
        {
         Msg52=("SoundOpenSell: "+"             "+SoundOpenSell+" (���� ��� �������� ������ Sell)"+"\n");
        }
      else
        {
         Msg52=("SoundOpenSell: "+"         "+CheckSoundFileName(SoundOpenSell)+" ����� ��� Sell �� �����."+"\n");
        }
      if(StringLen(CheckSoundFileName(SoundOpenBuyStop))==0)
        {
         Msg53=("SoundOpenBuyStop: "+"     "+SoundOpenBuyStop+" (���� ��� �������� ������ BuyStop)"+"\n");
        }
      else
        {
         Msg53=("SoundOpenBuyStop: "+" "+CheckSoundFileName(SoundOpenBuyStop)+" ����� ��� BuyStop �� �����."+"\n");
        }
      if(StringLen(CheckSoundFileName(SoundOpenSellStop))==0)
        {
         Msg54=("SoundOpenSellStop: "+"      "+SoundOpenSellStop+" (���� ��� �������� ������ SellStop)"+"\n");
        }
      else
        {
         Msg54=("SoundOpenSellStop: "+" "+CheckSoundFileName(SoundOpenSellStop)+" ����� ��� SellStop �� �����."+"\n");
        }
      if(StringLen(CheckSoundFileName(SoundClosing))==0)
        {
         Msg55=("SoundClosing: "+"                "+SoundClosing+" (���� ��� �������� ���� �������)"+"\n");
        }
      else
        {
         Msg55=("SoundClosing: "+"           "+CheckSoundFileName(SoundClosing)+" ����� ��� Close �� �����."+"\n");
        }
     }
   else
     {
      Msg49="���";Msg51="";Msg52="";Msg53="";Msg54="";Msg55="";
     }
   Msg50=("UseSoundInOperations: "+" "+Msg49+" (������������ �������� ������������� �������� ��������)"+"\n");
   Msg57=(""+"\n");
   Msg58=("���� ��� ���������� ������� ��������� ��������� ������� ������  \" �� \"  � ��������� ��������� ����"+"\n"+"������, ����� ������� ������ \" ������ \"."+"\n");
//----
   InitialMsg=Msg1+Msg2+Msg3+Msg4+Msg5+Msg6+Msg7+Msg8+Msg9+Msg10+Msg11+Msg12+Msg13+Msg14
   +Msg15+Msg16+Msg18+Msg19+Msg20+Msg21+Msg22+Msg23+Msg24+Msg25+Msg26+Msg27+Msg28
   +Msg30+Msg31+Msg33+Msg34+Msg36+Msg37+Msg39+Msg40+Msg42+Msg43+Msg59+Msg45+Msg46+Msg47+Msg48
   +Msg50+Msg51+Msg52+Msg53+Msg54+Msg55+Msg57+Msg58;
//----
   return(MessageBoxA(WindowHandle(Symbol(),0),InitialMsg,"��������� ��������� ������� "+NameOfExpert,65));
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
string CheckSoundFileName(string SoundFileName)
  {
   if(StringLen(SoundFileName)==0)
     {
      return("�� ������� ��� ����� ��������� �������������.");
     }
   else
      if((StringFind(SoundFileName,".wav",0))!=(StringLen(SoundFileName)-4))
        {
         return("������� ������� ��� ����� ��������� �������������.");
        }
      else
         if(!jFileExists(jGetCurrDir()+"\sounds"+CharToStr(92)+SoundFileName))
           {
            return("��������� ���� �� ���������� � �������� ..\sounds.");
           }
         else
           {
            return("");
           }
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
int InfoByOpenOrders()
  {
   string Msg1,Msg2,Msg3,Msg4,Msg5,Msg6,Msg7,Msg8,Msg9,Msg10,Msg11,Msg12,OutString;
//----
   Msg1=("��������! �� �������� ����������� "+Symbol()+" � ��� ������� ��������� �������� ������:"+"\n");
   Msg2=(""+"\n");
   if(CalculationOfWarrants(Symbol(),0,0,0)>0)
     {
      Msg3=(" BUY - "+""+CalculationOfWarrants(Symbol(),0,0,0)+"  ");
     }  else Msg3="";
   if(CalculationOfWarrants(Symbol(),0,1,0)>0)
     {
      Msg4=(" SELL - "+""+CalculationOfWarrants(Symbol(),0,1,0)+"  ");
     }  else Msg4="";
   if(CalculationOfWarrants(Symbol(),0,2,0)>0)
     {
      Msg5=(" BUYLIMIT - "+""+CalculationOfWarrants(Symbol(),0,2,0)+"  ");
     }  else Msg5="";
   if(CalculationOfWarrants(Symbol(),0,3,0)>0)
     {
      Msg6=(" SELLLIMIT - "+""+CalculationOfWarrants(Symbol(),0,3,0)+"  ");
     }  else Msg6="";
   if(CalculationOfWarrants(Symbol(),0,4,0)>0)
     {
      Msg7=(" BUYSTOP - "+""+CalculationOfWarrants(Symbol(),0,4,0)+"  ");
     }  else Msg7="";
   if(CalculationOfWarrants(Symbol(),0,5,0)>0)
     {
      Msg8=(" SELLSTOP - "+""+CalculationOfWarrants(Symbol(),0,5,0)+"  ");
     }  else Msg8="";
   Msg9=(""+"\n");
   Msg10=("�������� �� ����� �������� � ����� ������ ���� �������, ���� ������ ��� ������ �� ����"
    +"\n"+"������� ����� ���� �� ����������. ������, �� ��������� ��������� ����������� ������"
    +"\n"+"���������, ������������ ����������� ������� ��� �������� ������!"+"\n");
   Msg11=(""+"\n");
   Msg12=("�� ������ ��������� ������ ��������� � ������� ���� ��������� ������. ��� ��������-"
    +"\n"+"��� ������ ���������, ������� � ��� �������� � �������, � ����� ����� ��������� �������"
    +"\n"+"�� ������ ����� \" ��������� ��������� ��������� \".  ��������� ������ ���������?"+"\n");
   OutString=Msg1+Msg2+Msg3+Msg4+Msg5+Msg6+Msg7+Msg8+Msg9+Msg9+Msg10+Msg11+Msg12;
   return(MessageBoxA(WindowHandle(Symbol(),0),OutString,"������� �������� ������!",52));
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
int CalculationOfWarrants(string NameOfToll,int UniqueMagic,int Index1,int Index2)
  {
   int Inform[9][2],Zero[9][2];int n=0;
   ArrayCopy(Inform,Zero,0,0,WHOLE_ARRAY);
   bool Condition1;
   if(UniqueMagic==0)
     {
      Condition1=(OrderSymbol()==NameOfToll);
     }
   else
     {
      Condition1=((OrderSymbol()==NameOfToll)&&(OrderMagicNumber()==UniqueMagic));
     }
   for(n=OrdersTotal()-1;n>=0;n--)
     {
      OrderSelect(n,SELECT_BY_POS,MODE_TRADES);
      if (Condition1)
        {
         if(OrderType()==0) {Inform[0][0]=Inform[0][0]+1;Inform[0][1]=Inform[0][1]+OrderProfit();}
         if(OrderType()==1) {Inform[1][0]=Inform[1][0]+1;Inform[1][1]=Inform[1][1]+OrderProfit();}
         if(OrderType()==2) {Inform[2][0]=Inform[2][0]+1;}
         if(OrderType()==3) {Inform[3][0]=Inform[3][0]+1;}
         if(OrderType()==4) {Inform[4][0]=Inform[4][0]+1;}
         if(OrderType()==5) {Inform[5][0]=Inform[5][0]+1;}
        }
     }
   Inform[6][0]=Inform[0][0]+Inform[1][0];
   Inform[6][1]=Inform[0][1]+Inform[1][1];
   Inform[7][0]=Inform[2][0]+Inform[3][0]+Inform[4][0]+Inform[5][0];
   Inform[8][0]=Inform[0][0]+Inform[1][0]+Inform[2][0]+Inform[3][0]
   +Inform[4][0]+Inform[5][0];
   return(Inform[Index1][Index2]);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void WriteLineInFile(string FileName,string Line)
  {
   int HFile=FileOpen(FileName,FILE_READ|FILE_WRITE," ");
   if(HFile>0)
     {
      FileSeek(HFile,0,SEEK_END);
      FileWrite(HFile,Line);
      FileFlush(HFile);
      FileClose(HFile);
     }
   return;
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
string GetCurRusTime()
  {
   string StrMonth="",StrDay="",StrHour="",StrMinute="",StrSeconds="";
   RefreshRates();
   if(Month()<10) { StrMonth="0"+Month(); } else { StrMonth=Month(); }
   if(Day()<10) { StrDay="0"+Day(); } else { StrDay=Day(); }
   if(Hour()<10) { StrHour="0"+Hour(); } else { StrHour=Hour(); }
   if(Minute()<10) { StrMinute="0"+Minute(); } else { StrMinute=Minute(); }
   if(Seconds()<10) { StrSeconds="0"+Seconds(); } else { StrSeconds=Seconds(); }
   return(""+StrDay+"."+StrMonth+"."+Year()+" "+StrHour+":"+StrMinute+":"+StrSeconds+" ");
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void SetBadge(string BadgeName, datetime BadgeTime, double BadgePrice,int BadgeType,color BadgeColor)
  {
   string Name=BadgeName+CurTime();
   if(PrevPrice>0)
     {
      if(((BadgePrice-PrevPrice)>=0*Point)&&((BadgePrice-PrevPrice)<3*Point)) BadgePrice=BadgePrice+3*Point;
      if(((PrevPrice-BadgePrice)>=0*Point)&&((PrevPrice-BadgePrice)<3*Point)) BadgePrice=BadgePrice-3*Point;
     }
   ObjectCreate(Name,OBJ_ARROW,0,BadgeTime,BadgePrice);
   ObjectSet(Name,OBJPROP_ARROWCODE,BadgeType);
   ObjectSet(Name,OBJPROP_COLOR,BadgeColor);
   PrevPrice=BadgePrice;
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
bool DrawChannel(string NaneChannel,double Upper=0,double Down=0,double SubUp=0,double SubDn=0,double Average=0)
  {
   bool res[11];
   if(NaneChannel=="FChannel")
     {
      if(ObjectFind(NaneChannel+"H")>0) ObjectDelete(NaneChannel+"H");
      ObjectCreate(NaneChannel+"H", OBJ_HLINE, 0, Time[0],Close[0]);
      ObjectSet(NaneChannel+"H",OBJPROP_COLOR,Pink);
      if(ObjectFind(NaneChannel+"L")>0) ObjectDelete(NaneChannel+"L");
      ObjectCreate(NaneChannel+"L", OBJ_HLINE, 0, Time[0],Close[0]);
      ObjectSet(NaneChannel+"L",OBJPROP_COLOR,SkyBlue);
      if(ObjectFind(NaneChannel+"SH")>0) ObjectDelete(NaneChannel+"SH");
      ObjectCreate(NaneChannel+"SH",OBJ_RECTANGLE,0 ,0,0,0,0);
      ObjectSet(NaneChannel+"SH",OBJPROP_COLOR,Pink);
      if(ObjectFind(NaneChannel+"LH")>0) ObjectDelete(NaneChannel+"LH");
      ObjectCreate(NaneChannel+"LH",OBJ_RECTANGLE,0 ,0,0,0,0);
      ObjectSet(NaneChannel+"LH",OBJPROP_COLOR,SkyBlue);
     }
   if(NaneChannel=="ChannelHLClose")
     {
      if(ObjectFind(NaneChannel+"H")>0) ObjectDelete(NaneChannel+"H");
      ObjectCreate(NaneChannel+"H", OBJ_HLINE, 0, Time[0],Close[0]);
      ObjectSet(NaneChannel+"H",OBJPROP_COLOR,HotPink);
      if(ObjectFind(NaneChannel+"L")>0) ObjectDelete(NaneChannel+"L");
      ObjectCreate(NaneChannel+"L", OBJ_HLINE, 0, Time[0],Close[0]);
      ObjectSet(NaneChannel+"L",OBJPROP_COLOR,RoyalBlue);
      if(ObjectFind(NaneChannel+"M")>0) ObjectDelete(NaneChannel+"M");
      ObjectCreate(NaneChannel+"M", OBJ_HLINE, 0, Time[0],Close[0]);
      ObjectSet(NaneChannel+"M",OBJPROP_COLOR,Gold);
      if(ObjectFind(NaneChannel+"SH")>0) ObjectDelete(NaneChannel+"SH");
      ObjectCreate(NaneChannel+"SH",OBJ_RECTANGLE,0 ,0,0,0,0);
      ObjectSet(NaneChannel+"SH",OBJPROP_COLOR,HotPink);
      if(ObjectFind(NaneChannel+"LH")>0) ObjectDelete(NaneChannel+"LH");
      ObjectCreate(NaneChannel+"LH",OBJ_RECTANGLE,0 ,0,0,0,0);
      ObjectSet(NaneChannel+"LH",OBJPROP_COLOR,RoyalBlue);
     }
   if((NaneChannel=="FChannel")||(NaneChannel=="ChannelHLClose"))
     {
      res[0]=ObjectSet(NaneChannel+"H",OBJPROP_PRICE1,Upper);
      res[1]=ObjectSet(NaneChannel+"L",OBJPROP_PRICE1,Down);
      if(Average>0)
        {
         res[2]=ObjectSet(NaneChannel+"M",OBJPROP_PRICE1,Average);
        }
      else { res[2]=true; }
      res[3]=ObjectSet(NaneChannel+"SH",OBJPROP_TIME1,CurTime());
      res[4]=ObjectSet(NaneChannel+"SH",OBJPROP_PRICE1,SubUp);
      res[5]=ObjectSet(NaneChannel+"SH",OBJPROP_TIME2,CurTime()-(5760*Period()));
      res[6]=ObjectSet(NaneChannel+"SH",OBJPROP_PRICE2,Upper);
      res[7]=ObjectSet(NaneChannel+"LH",OBJPROP_TIME1,CurTime());
      res[8]=ObjectSet(NaneChannel+"LH",OBJPROP_PRICE1,Down);
      res[9]=ObjectSet(NaneChannel+"LH",OBJPROP_TIME2,CurTime()-(5760*Period()));
      res[10]=ObjectSet(NaneChannel+"LH",OBJPROP_PRICE2,SubDn);
      ObjectsRedraw();
     }
   if(res[0]&&res[1]&&res[2]&&res[3]&&res[4]&&res[5]&&res[6]&&res[7]&&res[8]&&res[9]&&res[10])
      return(true);else return(false);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
int PerformanceOpenBuy(string Toll,double SizeOfLot,int Accuracy,int FixingOfLoss,
int FixingOfProfit,string Note,int UniqueNumber,bool UseSound=false,string NameOfSound="")
  {
   double OpenPrice=0,SL=0,TP=0;int ticket=0,index=0,err=0,attempt=0;datetime InitialTime=CurTime();
   while(ticket<=0)
     {
      while(!IsTradeAllowed()) Sleep(1000);
      RefreshRates();
      OpenPrice=NormalizeDouble(Ask,Digits);
      SL=OpenPrice - FixingOfLoss   * Point;
      TP=OpenPrice + FixingOfProfit * Point;
      ticket=OrderSend(Toll,OP_BUY,SizeOfLot,OpenPrice,Accuracy,SL,TP,Note,UniqueNumber,0,Gold);
      if(ticket>0)
        {
         if(!IsTesting())
           {
            if((UseSound)&&(CheckSoundFileName(NameOfSound)=="")) PlaySound(NameOfSound);
           }
         index=StringFind(Note," ", 0);
         WriteLineInFile(FileNameOfReport,GetCurRusTime()+"������� ������� Buy ����� "
         +ticket+" ���������� � "+StringSubstr(Note,0,index)+" �� ����: "+DoubleToStr(OpenPrice,Digits));
         Sleep(5000);
         return(ticket);
        }
      else
        {
         err=GetLastError();attempt++;
         WriteLineInFile(FileNameOfReport,GetCurRusTime()+"�� ������� ������� ����� Buy, �������: "
         +ErrorDescription(err));
         Print(TimeToStr(CurTime(),TIME_SECONDS)+" InitialTime: "+TimeToStr(InitialTime,TIME_SECONDS)
         +" �� ������� ������� ����� Buy, �������: "+ErrorDescription(err));
         if((err==135)&&(Accuracy<11)) Accuracy++;
         if(((CurTime()-InitialTime)>180)||(attempt==10)) break;
         Sleep(1000);
        }
     }
   return(-1);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
int PerformanceOpenSell(string Toll,double SizeOfLot,int Accuracy,int FixingOfLoss,
int FixingOfProfit,string Note,int UniqueNumber,bool UseSound=false,string NameOfSound="")
  {
   double OpenPrice=0,SL=0,TP=0;int ticket=0,index=0,err=0,attempt=0;datetime InitialTime=CurTime();
   while(ticket<=0)
     {
      while(!IsTradeAllowed()) Sleep(1000);
      RefreshRates();
      OpenPrice=NormalizeDouble(Bid,Digits);
      SL=OpenPrice + StopLoss   * Point;
      TP=OpenPrice - TakeProfit * Point;
      ticket=OrderSend(Toll,OP_SELL,SizeOfLot,OpenPrice,Accuracy,SL,TP,Note,UniqueNumber,0,DeepSkyBlue);
      if(ticket>0)
        {
         if(!IsTesting())
           {
            if((UseSound)&&(CheckSoundFileName(NameOfSound)=="")) PlaySound(NameOfSound);
           }
         index=StringFind(Note," ", 0);
         WriteLineInFile(FileNameOfReport,GetCurRusTime()+"������� ������� Sell ����� "
         +ticket+" ���������� � "+StringSubstr(Note,0,index)+" �� ����: "+DoubleToStr(OpenPrice,Digits));
         Sleep(5000);
         return(ticket);
        }
      else
        {
         err=GetLastError();attempt++;
         WriteLineInFile(FileNameOfReport,GetCurRusTime()+"�� ������� ������� ����� Sell, �������: "
         +ErrorDescription(err));
         Print(TimeToStr(CurTime(),TIME_SECONDS)+" InitialTime: "+TimeToStr(InitialTime,TIME_SECONDS)
         +" �� ������� ������� ����� Sell, �������: "+ErrorDescription(err));
         if((err==135)&&(Accuracy<11)) Accuracy++;
         if(((CurTime()-InitialTime)>180)||(attempt==10)) break;
         Sleep(1000);
        }
     }
   return(-1);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
int PerformanceOpenBuyStop(string Toll,double SizeOfLot,double OpenPrice,int Accuracy,int FixingOfLoss,
int FixingOfProfit,string Note,int UniqueNumber,bool UseSound=false,string NameOfSound="")
  {
   double SL=0,TP=0;int ticket=0,index=0,err=0,attempt=0;datetime InitialTime=CurTime();
   while(ticket<=0)
     {
      while(!IsTradeAllowed()) Sleep(1000);
      RefreshRates();
      OpenPrice=NormalizeDouble(OpenPrice,Digits);
      SL=OpenPrice - FixingOfLoss   * Point;
      TP=OpenPrice + FixingOfProfit * Point;
      ticket=OrderSend(Toll,OP_BUYSTOP,SizeOfLot,OpenPrice,Accuracy,SL,TP,Note,UniqueNumber,0,Orange);
      if(ticket>0)
        {
         if(!IsTesting())
           {
            if((UseSound)&&(CheckSoundFileName(NameOfSound)=="")) PlaySound(NameOfSound);
           }
         index=StringFind(Note," ", 0);
         WriteLineInFile(FileNameOfReport,GetCurRusTime()+"��������� ����� BuyStop ����� "
         +ticket+" ���������� � "+StringSubstr(Note,0,index)+" �� �������: "+DoubleToStr(OpenPrice,Digits));
         Sleep(5000);
         return(ticket);
        }
      else
        {
         err=GetLastError();attempt++;
         WriteLineInFile(FileNameOfReport,GetCurRusTime()+"�� ������� ��������� ����� BuyStop, �������: "
         +ErrorDescription(err));
         Print(TimeToStr(CurTime(),TIME_SECONDS)+" InitialTime: "+TimeToStr(InitialTime,TIME_SECONDS)
         +"�� ������� ��������� ����� BuyStop, �������: "+ErrorDescription(err));
         if((err==135)&&(Accuracy<11)) Accuracy++;
         if(((CurTime()-InitialTime)>180)||(attempt==10)) break;
         Sleep(1000);
        }
     }
   return(-1);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
int PerformanceOpenSellStop(string Toll,double SizeOfLot,double OpenPrice,int Accuracy,int FixingOfLoss,
int FixingOfProfit,string Note,int UniqueNumber,bool UseSound=false,string NameOfSound="")
  {
   double SL=0,TP=0;int ticket=0,index=0,err=0,attempt=0;datetime InitialTime=CurTime();
   while(ticket<=0)
     {
      while(!IsTradeAllowed()) Sleep(1000);
      RefreshRates();
      OpenPrice=NormalizeDouble(OpenPrice,Digits);
      SL=OpenPrice + StopLoss   * Point;
      TP=OpenPrice - TakeProfit * Point;
      ticket=OrderSend(Toll,OP_SELLSTOP,SizeOfLot,OpenPrice,Accuracy,SL,TP,Note,UniqueNumber,0,Blue);
      if(ticket>0)
        {
         if(!IsTesting())
           {
            if((UseSound)&&(CheckSoundFileName(NameOfSound)=="")) PlaySound(NameOfSound);
           }
         index=StringFind(Note," ", 0);
         WriteLineInFile(FileNameOfReport,GetCurRusTime()+"��������� ����� SellStop ����� "
         +ticket+" ���������� � "+StringSubstr(Note,0,index)+" �� �������: "+DoubleToStr(OpenPrice,Digits));
         Sleep(5000);
         return(ticket);
        }
      else
        {
         err=GetLastError();attempt++;
         WriteLineInFile(FileNameOfReport,GetCurRusTime()+"�� ������� ��������� ����� SellStop, �������: "
         +ErrorDescription(err));
         Print(TimeToStr(CurTime(),TIME_SECONDS)+" InitialTime: "+TimeToStr(InitialTime,TIME_SECONDS)
         +"�� ������� ��������� ����� SellStop, �������: "+ErrorDescription(err));
         if((err==135)&&(Accuracy<11)) Accuracy++;
         if(((CurTime()-InitialTime)>180)||(attempt==10)) break;
         Sleep(1000);
        }
     }
   return(-1);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void CloseAllPositions(string NameOfToll,int UniqueMagic,int ModeClosing)
  {
   bool WorkWarrant=false,PostWarrant=false,result=false;
   int attempt,cnt,index=0,NumberOfTry=2,PauseAfterError=1;
   double TimeProfit;
   datetime InitTime=CurTime();
//----
   if(ModeClosing==0) {WorkWarrant=true;PostWarrant=true;}
   if(ModeClosing==1) {WorkWarrant=true;PostWarrant=false;}
   if(ModeClosing==2) {WorkWarrant=false;PostWarrant=true;}
   CountOperations++;
   WriteLineInFile(FileNameOfReport,GetCurRusTime()+"�������� �������� �"+CountOperations);
   while(OrdersTotal()>0)
     {
      for(cnt=OrdersTotal()-1;cnt>=0;cnt--)
        {
         result=false;
         TimeProfit=0;
         if (OrderSelect(cnt, SELECT_BY_POS, MODE_TRADES))
           {
            if ((OrderSymbol()==NameOfToll)&&(OrderMagicNumber()==UniqueMagic))
              {
               for(attempt=0;attempt<NumberOfTry;attempt++)
                 {
                  RefreshRates();
                  while(!IsTradeAllowed()) Sleep(2000);
                  if((OrderType()==0)&&WorkWarrant)
                    {
                     TimeProfit=OrderProfit();
                     result=OrderClose(OrderTicket(),OrderLots(),NormalizeDouble(Bid,Digits),5,Yellow);
                     if (result)
                       {
                        index=StringFind(OrderComment()," ", 0);
                        WriteLineInFile(FileNameOfReport,GetCurRusTime()+"������� ������� Buy ����� "+OrderTicket()
                        +" ���������� � "+StringSubstr(OrderComment(),0,index)+" �� ����: "
                        +DoubleToStr(OrderClosePrice(),Digits));
                        CollectorBuy=CollectorBuy+TimeProfit;
                        Sleep(2000);
                        break;
                       }
                     else
                       {
                        index=StringFind(OrderComment()," ", 0);
                        WriteLineInFile(FileNameOfReport,GetCurRusTime()
                        +" ����� Buy ����� "+OrderTicket()+" ���������� � "+StringSubstr(OrderComment(),0,index)
                        +" �� ������� �������. �������: "+ErrorDescription(GetLastError())+" ������� �"+attempt+1);
                        Alert(" ����� Buy ����� "+OrderTicket()+" ���������� � "+StringSubstr(OrderComment(),0,index)
                        +" �� ������� �������. �������: "+ErrorDescription(GetLastError())+" ������� �"+attempt+1);
                        if(ModeClosing==1) {PostWarrant=true;}
                        Sleep(1000*PauseAfterError);
                       }
                    }
                  if((OrderType()==1)&&WorkWarrant)
                    {
                     TimeProfit=OrderProfit();
                     result=OrderClose(OrderTicket(),OrderLots(),NormalizeDouble(Ask,Digits),5,Blue );
                     if (result)
                       {
                        index=StringFind(OrderComment()," ", 0);
                        WriteLineInFile(FileNameOfReport,GetCurRusTime()+"������� ������� Sell ����� "+OrderTicket()
                        +" ���������� � "+StringSubstr(OrderComment(),0,index)+" �� ����: "
                        +DoubleToStr(OrderClosePrice(),Digits));
                        CollectorSell=CollectorSell+TimeProfit;
                        Sleep(2000);
                        break;
                       }
                     else
                       {
                        index=StringFind(OrderComment()," ", 0);
                        WriteLineInFile(FileNameOfReport,GetCurRusTime()
                        +" ����� Sell ����� "+OrderTicket()+" ���������� � "+StringSubstr(OrderComment(),0,index)
                        +" �� ������� �������. �������: "+ErrorDescription(GetLastError())+" ������� �"+attempt+1);
                        Alert(" ����� Sell ����� "+OrderTicket()+" ���������� � "+StringSubstr(OrderComment(),0,index)
                        +" �� ������� �������. �������: "+ErrorDescription(GetLastError())+" ������� �"+attempt+1);
                        if(ModeClosing==1) {PostWarrant=true;}
                        Sleep(1000*PauseAfterError);
                       }
                    }
                  if((OrderType()==2)&&PostWarrant)
                    {
                     result=OrderDelete(OrderTicket());
                     if (result)
                       {
                        index=StringFind(OrderComment()," ", 0);
                        WriteLineInFile(FileNameOfReport,GetCurRusTime()+"����� ����� BuyLimit ����� "+OrderTicket()
                        +" ���������� � "+StringSubstr(OrderComment(),0,index)+" � �������: "
                        +DoubleToStr(OrderOpenPrice(),Digits));
                        Sleep(2000);
                        break;
                       }
                     else
                       {
                        index=StringFind(OrderComment()," ", 0);
                        WriteLineInFile(FileNameOfReport,GetCurRusTime()
                        +" ����� BuyLimit ����� "+OrderTicket()+" ���������� � "+StringSubstr(OrderComment(),0,index)
                        +" �� ������� �������. �������: "+ErrorDescription(GetLastError())+" ������� �"+attempt+1);
                        Alert(" ����� BuyLimit ����� "+OrderTicket()+" ���������� � "+StringSubstr(OrderComment(),0,index)
                        +" �� ������� �������. �������: "+ErrorDescription(GetLastError())+" ������� �"+attempt+1);
                        if(ModeClosing==1) {PostWarrant=true;}
                        Sleep(1000*PauseAfterError);
                       }
                    }
                  if((OrderType()==3)&&PostWarrant)
                    {
                     result=OrderDelete(OrderTicket());
                     if (result)
                       {
                        index=StringFind(OrderComment()," ", 0);
                        WriteLineInFile(FileNameOfReport,GetCurRusTime()+"����� ����� SellLimit ����� "+OrderTicket()
                        +" ���������� � "+StringSubstr(OrderComment(),0,index)+" � �������: "
                        +DoubleToStr(OrderOpenPrice(),Digits));
                        Sleep(2000);
                        break;
                       }
                     else
                       {
                        index=StringFind(OrderComment()," ", 0);
                        WriteLineInFile(FileNameOfReport,GetCurRusTime()
                        +" ����� SellLimit ����� "+OrderTicket()+" ���������� � "+StringSubstr(OrderComment(),0,index)
                        +" �� ������� �������. �������: "+ErrorDescription(GetLastError())+" ������� �"+attempt+1);
                        Alert(" ����� SellLimit ����� "+OrderTicket()+" ���������� � "+StringSubstr(OrderComment(),0,index)
                        +" �� ������� �������. �������: "+ErrorDescription(GetLastError())+" ������� �"+attempt+1);
                        if(ModeClosing==1) {PostWarrant=true;}
                        Sleep(1000*PauseAfterError);
                       }
                    }
                  if((OrderType()==4)&&PostWarrant)
                    {
                     result=OrderDelete(OrderTicket());
                     if (result)
                       {
                        index=StringFind(OrderComment()," ", 0);
                        WriteLineInFile(FileNameOfReport,GetCurRusTime()+"����� ����� BuyStop ����� "+OrderTicket()
                        +" ���������� � "+StringSubstr(OrderComment(),0,index)+" � �������: "
                        +DoubleToStr(OrderOpenPrice(),Digits));
                        Sleep(2000);
                        break;
                       }
                     else
                       {
                        index=StringFind(OrderComment()," ", 0);
                        WriteLineInFile(FileNameOfReport,GetCurRusTime()
                        +" ����� BuyStop ����� "+OrderTicket()+" ���������� � "+StringSubstr(OrderComment(),0,index)
                        +" �� ������� �������. �������: "+ErrorDescription(GetLastError())+" ������� �"+attempt+1);
                        Alert(" ����� BuyStop ����� "+OrderTicket()+" ���������� � "+StringSubstr(OrderComment(),0,index)
                        +" �� ������� �������. �������: "+ErrorDescription(GetLastError())+" ������� �"+attempt+1);
                        if(ModeClosing==1) {PostWarrant=true;}
                        Sleep(1000*PauseAfterError);
                       }
                    }
                  if((OrderType()==5)&&PostWarrant)
                    {
                     result=OrderDelete(OrderTicket());
                     if (result)
                       {
                        index=StringFind(OrderComment()," ", 0);
                        WriteLineInFile(FileNameOfReport,GetCurRusTime()+"����� ����� SellStop ����� "+OrderTicket()
                        +" ���������� � "+StringSubstr(OrderComment(),0,index)+" � �������: "
                        +DoubleToStr(OrderOpenPrice(),Digits));
                        Sleep(2000);
                        break;
                       }
                     else
                       {
                        index=StringFind(OrderComment()," ", 0);
                        WriteLineInFile(FileNameOfReport,GetCurRusTime()
                        +" ����� SellStop ����� "+OrderTicket()+" ���������� � "+StringSubstr(OrderComment(),0,index)
                        +" �� ������� �������. �������: "+ErrorDescription(GetLastError())+" ������� �"+attempt+1);
                        Alert(" ����� SellStop ����� "+OrderTicket()+" ���������� � "+StringSubstr(OrderComment(),0,index)
                        +" �� ������� �������. �������: "+ErrorDescription(GetLastError())+" ������� �"+attempt+1);
                        if(ModeClosing==1) {PostWarrant=true;}
                        Sleep(1000*PauseAfterError);
                       }
                    }
                 }
               if((ModeClosing==1)&&(CalculationOfWarrants(NameOfToll,UniqueMagic,6,0)==0))
               {PostWarrant=true;}
               if((ModeClosing==2)&&(CalculationOfWarrants(NameOfToll,UniqueMagic,7,0)==0))
               {WorkWarrant=true;cnt=OrdersTotal();}
              }
           }
         else
           {
            switch(OrderType())
              {//0
               case 0:
                  WriteLineInFile(FileNameOfReport,GetCurRusTime()
                  +"�� ������� ������� ����� Buy � "+OrderTicket()
                  +" �������: "+ErrorDescription(GetLastError()));
                  Alert("�� ������� ������� ����� Buy � ",OrderTicket(),
                  " �������: ",ErrorDescription(GetLastError()));
                  break;
               case 1:
                  WriteLineInFile(FileNameOfReport,GetCurRusTime()
                  +"�� ������� ������� ����� Sell � "+OrderTicket()
                  +" �������: "+ErrorDescription(GetLastError()));
                  Alert("�� ������� ������� ����� Sell � ",OrderTicket(),
                  " �������: ",ErrorDescription(GetLastError()));
                  break;
               case 2:
                  WriteLineInFile(FileNameOfReport,GetCurRusTime()
                  +"�� ������� ������� ����� BuyLimit � "+OrderTicket()
                  +" �������: "+ErrorDescription(GetLastError()));
                  Alert("�� ������� ������� ����� BuyLimit � ",OrderTicket(),
                  " �������: ",ErrorDescription(GetLastError()));
                  break;
               case 3:
                  WriteLineInFile(FileNameOfReport,GetCurRusTime()
                  +"�� ������� ������� ����� SellLimit � "+OrderTicket()
                  +" �������: "+ErrorDescription(GetLastError()));
                  Alert("�� ������� ������� ����� SellLimit � ",OrderTicket(),
                  " �������: ",ErrorDescription(GetLastError()));
                  break;
               case 4:
                  WriteLineInFile(FileNameOfReport,GetCurRusTime()
                  +"�� ������� ������� ����� BuyStop � "+OrderTicket()
                  +" �������: "+ErrorDescription(GetLastError()));
                  Alert("�� ������� ������� ����� BuyStop � ",OrderTicket(),
                  " �������: ",ErrorDescription(GetLastError()));
                  break;
               case 5:
                  WriteLineInFile(FileNameOfReport,GetCurRusTime()
                  +"�� ������� ������� ����� SellStop � "+OrderTicket()
                  +" �������: "+ErrorDescription(GetLastError()));
                  Alert("�� ������� ������� ����� SellStop � ",OrderTicket(),
                  " �������: ",ErrorDescription(GetLastError()));
                  break;
              }//0
           }
        }
     }
   TimeOfClosing=CurTime()-InitTime;
   WriteLineInFile(FileNameOfReport,"");
   return;
  }
//+------------------------------------------------------------------+