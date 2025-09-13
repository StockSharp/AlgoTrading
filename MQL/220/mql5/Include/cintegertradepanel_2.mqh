//+------------------------------------------------------------------+
//|                                           CIntegerTradePanel.mqh |
//|                                        MetaQuotes Software Corp. |
//|                                              http://www.mql5.com |
//+------------------------------------------------------------------+
#property copyright "Integer"
#property link "https://login.mql5.com/ru/users/Integer"
#property description "web: http://dmffx.com\n\nmail: for-good-letters@yandex.ru"
#property version "1.00"

#import "user32.dll"
int GetAncestor(int hWnd,int gaFlags);
int GetDlgItem(int hDlg,int nIDDlgItem);
int SendMessageA(int hWnd,int Msg,int wParam,int lParam);
#import
//+------------------------------------------------------------------+
//| CIntegerTradePanel                                               |
//+------------------------------------------------------------------+
class CIntegerTradePanel
  {
protected:
   string            NameMain,NameForm,NameSelect[8],NameTradeBut,NameBuyBut,NameSellBut,NameStopPriceLine,NameOpenPriceLine,NameSLPriceLine,NameTPPriceLine,NameExpirTimeLine,NameLotsCaption,NameLotsValue,NameSLCaption,NameSLValue,NameTPCaption,NameTPValue,CaptionMainOn,CaptionMainOff,CaptionSelect[8],CaptionTradeBut[8],GVPrefix,NameStoreX,NameStoreY,NameSnd,NameMail,SoundEvent;
   color             ColorMainButBg,ColorMainButBgOn,ColorMainButTxt,ColorTradeBut[2],ColorText,ColorSelectBuyOn,ColorSelectSellOn,ColorSelectOff,ColorTrigger[2],ColorOpen[2],ColorSL[2],ColorTP[2],ColorExp[2],ColorLabel,ColorInputBg,ColorInputTxt,ColorInputWarning,ColorFormBg,ColorFormBorder;
   int               PreId,DefaultLineStopLoss,DefaultLineTakeProfit,DefaultLinePendingLevel,DefaultLineTriggerLevel,gSelectedIndex,PosX,PosY,MinExpiration,OffsetX,OffsetY,LotsDigits,ATRHandle,TradeStopLoss,TradeTakeProfit,Magic;
   datetime          DefaultLineExpiration,rExpiration;
   double            Lots,miAsk,miBid,miMSL,rTriggerPrice,rOpenPrice,rSLPrice,rTPPrice;
   MqlTradeRequest   request;
   MqlTradeResult    result;
   int               fLotsDigits();
   double            fLotsNormalize(double);
   void              ParamLoad();
   void              MainCreateOnOfButton();
   void              fObjCreateButton(string,bool,int,int,int,int,string,color,color,int,int,int,string,long,bool,long);
   void              ParamSave();
   void              FormDelete();
   void              FormControlsDelete();
   void              FormBuySellButtonDelete();
   void              LinesSLTPDelete();
   int               fSLNormalize(int);
   int               fTPNormalize(int);
   void              fGetMarketInfo();
   void              ChartResizeMonitoring();
   bool              fIsWindowActive();
   void              FormCreate();
   void              FormControlsCreate();
   void              FormBuySellButtonsCreate();
   void              FormTradeButtonCreate();
   void              fObjCreateEdit(string,string,int,int,int,int,int,int,int,color,color,int,string,int,bool,double,long,bool,bool);
   void              FormSolvePos();
   void              FormFrameSetPos();
   void              fObjCreateLabel(string,int,int,string,int,int,int,color,int,string,int,bool,double,bool,bool,long);
   void              FormSetSLTPValues();
   void              EventAlert();
   void              EventSLTP(bool,bool,string);
   void              EventSLToL(bool,bool,string);
   double            fDealProfit(ulong);
   string            DS2(double);
   double            ND(double);
   int               fSlippage();
   string            fNameOrderType(long);
   void              FormCheckSLTPValues();
   void              fObjHLine(string,double,string,int,color,color,color,int,bool,bool,bool,long);
   void              fObjVLine(string,datetime,string,int,color,color,color,int,bool,bool,bool,long);
   void              TradeSelected();
   bool              fMEMode();
   bool              fPosModify(string,double,double,string,bool);
   bool              fOpBuy(string,double,double,double,int,ulong,string,string,bool);
   bool              fOpSell(string,double,double,double,int,ulong,string,string,bool);
   bool              fSetBuyLimit(string,double,double,double,double,datetime,int,string,string,bool);
   bool              fSetSellLimit(string,double,double,double,double,datetime,int,string,string,bool);
   bool              fSetBuyStop(string,double,double,double,double,datetime,ulong,string,string,bool);
   bool              fSetSellStop(string,double,double,double,double,datetime,ulong,string,string,bool);
   bool              fSetBuyStopLimit(string,double,double,double,double,double,datetime,int,string,string,bool);
   bool              fSetSellStopLimit(string,double,double,double,double,double,datetime,int,string,string,bool);
   string            fTradeRetCode(int);
   int               TxtToVal(string);
   void              DateInfo();
   string            fNameMonth(int);
   string            fNameWeekDay(int);
   void              TFTimeInfo(ENUM_TIMEFRAMES);
   void              fSolveBuySLTP(string,int,int,double &,double &);
   void              fSolveSellSLTP(string,int,int,double &,double &);
   void              ChartsOnOfAllButtons(string);
   void              FormContolsSelectAllOff();
   void              LinesSLTPSolveAndCreate();
   void              LinesSLTPCreate(double,double,double,double,datetime);
   void              LinesSLTPGetValues(double &,double &,double &,double &,datetime &);
   void              LinesSLTPValuesCorrection(double &,double &,double &,double &,datetime &);
   void              LinesSLTPShowInForm();
public:
   void CIntegerTradePanel()
     {
      NameMain                      =  "OnOf";
      NameForm                      =  "Form";
      NameSelect[0]                 =  "Buy";
      NameSelect[1]                 =  "Sell";
      NameSelect[2]                 =  "BuyStop";
      NameSelect[3]                 =  "SellStop";
      NameSelect[4]                 =  "BuyLimit";
      NameSelect[5]                 =  "SellLimit";
      NameSelect[6]                 =  "BuyStopLimit";
      NameSelect[7]                 =  "SellStopLimit";
      NameTradeBut                  =  "Trade";
      NameBuyBut                    =  "TradeBuy";
      NameSellBut                   =  "TradeSell";
      NameStopPriceLine             =  "StopPrice";
      NameOpenPriceLine             =  "OpenPrice";
      NameSLPriceLine               =  "StopLoss";
      NameTPPriceLine               =  "TakeProfit";
      NameExpirTimeLine             =  "Expiration";
      NameLotsCaption               =  "LotsCaption";
      NameLotsValue                 =  "LotsValue";
      NameSLCaption                 =  "SLCaption";
      NameSLValue                   =  "SLValue";
      NameTPCaption                 =  "TPCaption";
      NameTPValue                   =  "TPValue";
      CaptionMainOn                 =  "Show TradePanel";
      CaptionMainOff                =  "Hide TradePanel";
      CaptionSelect[0]              =  "b";
      CaptionSelect[1]              =  "s";
      CaptionSelect[2]              =  "bs";
      CaptionSelect[3]              =  "ss";
      CaptionSelect[4]              =  "bl";
      CaptionSelect[5]              =  "sl";
      CaptionSelect[6]              =  "bsl";
      CaptionSelect[7]              =  "ssl";
      CaptionTradeBut[0]            =  "Open Buy";
      CaptionTradeBut[1]            =  "Open Sell";
      CaptionTradeBut[2]            =  "Set BuyStop";
      CaptionTradeBut[3]            =  "Set SellStop";
      CaptionTradeBut[4]            =  "Set BuyLimit";
      CaptionTradeBut[5]            =  "Set SellLimit";
      CaptionTradeBut[6]            =  "Set BuyStopLimit";
      CaptionTradeBut[7]            =  "Set SellStopLimit";
      GVPrefix                      =  "eTradePanel_";
      NameStoreX                    =  "StoreX";
      NameStoreY                    =  "StoreY";
      NameSnd                       =  "EventsSoundOn";
      NameMail                      =  "EventsEMailOn";
      ColorMainButBg                =  Silver;
      ColorMainButBgOn              =  LightGreen;
      ColorMainButTxt               =  Black;
      ColorTradeBut[0]              =  LightSkyBlue;
      ColorTradeBut[1]              =  Pink;
      ColorText                     =  Black;
      ColorSelectBuyOn              =  LightGreen;
      ColorSelectSellOn             =  LightGreen;
      ColorSelectOff                =  Silver;
      ColorTrigger[0]               =  Blue;
      ColorTrigger[1]               =  Red;
      ColorOpen[0]                  =  DodgerBlue;
      ColorOpen[1]                  =  HotPink;
      ColorSL[0]                    =  Orange;
      ColorSL[1]                    =  Orange;
      ColorTP[0]                    =  LimeGreen;
      ColorTP[1]                    =  LimeGreen;
      ColorExp[0]                   =  Tan;
      ColorExp[1]                   =  Tan;
      ColorLabel                    =  LightGray;
      ColorInputBg                  =  Ivory;
      ColorInputTxt                 =  DimGray;
      ColorInputWarning             =  Red;
      ColorFormBg                   =  Gray;
      ColorFormBorder               =  LightGray;
      SoundEvent                    =  "alert";
      DefaultLineStopLoss           =  350;
      DefaultLineTakeProfit         =  550;
      DefaultLinePendingLevel       =  250;
      DefaultLineTriggerLevel       =  450;
      DefaultLineExpiration         =  0;
      TradeStopLoss                 =  0;
      TradeTakeProfit               =  0;
      Magic                         =  0;
      Lots                          =  0.1;
      MinExpiration                 =  660;
      OffsetX                       =  0;
      OffsetY                       =  0;
     }
   void              Init();
   void              Deinit();
   void              Tick();
   void              Timer();
   void              ChartEvent(const int id,const long  &lparam,const double  &dparam,const string  &sparam);
  };
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void CIntegerTradePanel::Init()
  {
   ATRHandle=iATR(_Symbol,PERIOD_CURRENT,55);
   Lots=SymbolInfoDouble(_Symbol,SYMBOL_VOLUME_MIN);
   LotsDigits=fLotsDigits();
   Lots=fLotsNormalize(Lots);
   ParamLoad();
   MainCreateOnOfButton();
   ChartRedraw();
   EventSetTimer(1);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
int CIntegerTradePanel::fLotsDigits()
  {
   double minlot=SymbolInfoDouble(_Symbol,SYMBOL_VOLUME_MIN);
   int digits=0;
   for(int i=8;i>=0;i--)
     {
      if(minlot>=NormalizeDouble(1.0/MathPow(10,i),8))
        {
         digits=i;
        }
     }
   return(digits);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
double CIntegerTradePanel::fLotsNormalize(double aLots)
  {
   aLots-=SymbolInfoDouble(_Symbol,SYMBOL_VOLUME_MIN);
   aLots/=SymbolInfoDouble(_Symbol,SYMBOL_VOLUME_STEP);
   aLots=MathRound(aLots);
   aLots*=SymbolInfoDouble(_Symbol,SYMBOL_VOLUME_STEP);
   aLots+=SymbolInfoDouble(_Symbol,SYMBOL_VOLUME_MIN);
   aLots=NormalizeDouble(aLots,LotsDigits);
   aLots=MathMin(aLots,SymbolInfoDouble(_Symbol,SYMBOL_VOLUME_MAX));
   aLots=MathMax(aLots,SymbolInfoDouble(_Symbol,SYMBOL_VOLUME_MIN));
   return(aLots);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void CIntegerTradePanel::ParamLoad()
  {
   string nm;
   nm=GVPrefix+_Symbol+"_Lots";
   if(GlobalVariableCheck(nm))
     {
      Lots=GlobalVariableGet(nm);
     }
   nm=GVPrefix+_Symbol+"_SL";
   if(GlobalVariableCheck(nm))
     {
      TradeStopLoss=(int)GlobalVariableGet(nm);
     }
   nm=GVPrefix+_Symbol+"_TP";
   if(GlobalVariableCheck(nm))
     {
      TradeTakeProfit=(int)GlobalVariableGet(nm);
     }
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void CIntegerTradePanel::MainCreateOnOfButton()
  {
   fObjCreateButton(NameMain,false,100,3,82,15,CaptionMainOn,ColorMainButBg,ColorMainButTxt,0,CORNER_RIGHT_UPPER,7,"Arial",0,false,OBJ_ALL_PERIODS);

   string ch;
   StringSetCharacter(ch,0,37);
   color col=ColorMainButBg;
   if(GlobalVariableCheck(NameSnd))col=ColorMainButBgOn;
   fObjCreateButton(NameSnd,false,130,3,25,15,ch,col,ColorMainButTxt,0,CORNER_RIGHT_UPPER,8,"Wingdings",0,false,OBJ_ALL_PERIODS);

   StringSetCharacter(ch,0,42);
   col=ColorMainButBg;
   if(GlobalVariableCheck(NameMail))col=ColorMainButBgOn;
   fObjCreateButton(NameMail,false,160,3,25,15,ch,col,ColorMainButTxt,0,CORNER_RIGHT_UPPER,8,"Wingdings",0,false,OBJ_ALL_PERIODS);

  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void CIntegerTradePanel::fObjCreateButton(
                                          string   aObjName,
                                          bool     aState,
                                          int      aX          =  30,
                                          int      aY          =  30,
                                          int      aWidth      =  100,
                                          int      aHeight     =  30,
                                          string   aCaption    =  "Push Me",
                                          color    aBgColor    =  Silver,
                                          color    aTextColor  =  Red,
                                          int      aWindow     =  0,
                                          int      aCorner     =  CORNER_LEFT_UPPER,
                                          int      aFontSize   =  8,
                                          string   aFont       =  "Arial",
                                          long     aChartID    =  0,
                                          bool     aBack       =  false,
                                          long     aTimeFrames =  OBJ_ALL_PERIODS
                                          )
  {
   ObjectDelete(aChartID,aObjName);
   ObjectCreate(aChartID,aObjName,OBJ_BUTTON,aWindow,0,0);
   ObjectSetInteger(aChartID,aObjName,OBJPROP_SELECTABLE,false);
   ObjectSetInteger(aChartID,aObjName,OBJPROP_SELECTED,false);
   ObjectSetInteger(aChartID,aObjName,OBJPROP_STATE,aState);
   ObjectSetInteger(aChartID,aObjName,OBJPROP_CORNER,aCorner);
   ObjectSetInteger(aChartID,aObjName,OBJPROP_XDISTANCE,aX);
   ObjectSetInteger(aChartID,aObjName,OBJPROP_YDISTANCE,aY);
   ObjectSetInteger(aChartID,aObjName,OBJPROP_XSIZE,aWidth);
   ObjectSetInteger(aChartID,aObjName,OBJPROP_YSIZE,aHeight);
   ObjectSetInteger(aChartID,aObjName,OBJPROP_BGCOLOR,aBgColor);
   ObjectSetInteger(aChartID,aObjName,OBJPROP_COLOR,aTextColor);
   ObjectSetString(aChartID,aObjName,OBJPROP_FONT,aFont);
   ObjectSetInteger(aChartID,aObjName,OBJPROP_FONTSIZE,aFontSize);
   ObjectSetString(aChartID,aObjName,OBJPROP_TEXT,aCaption);
   ObjectSetInteger(aChartID,aObjName,OBJPROP_BACK,aBack);
   ObjectSetInteger(aChartID,aObjName,OBJPROP_TIMEFRAMES,aTimeFrames);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void CIntegerTradePanel::FormDelete()
  {
   FormControlsDelete();
   ObjectDelete(0,NameForm);
   ChartRedraw(0);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void CIntegerTradePanel::FormControlsDelete()
  {
   gSelectedIndex=-1;
   ObjectSetString(0,NameForm,OBJPROP_TEXT,"                       MOVE ME OR DOUBLE CLICK ON CHART");
   for(int i=0;i<ArraySize(NameSelect);i++)
     {
      ObjectDelete(0,NameSelect[i]);
     }
   FormBuySellButtonDelete();
   ObjectDelete(0,NameTradeBut);
   ObjectDelete(0,NameLotsCaption);
   ObjectDelete(0,NameLotsValue);
   ObjectDelete(0,NameSLCaption);
   ObjectDelete(0,NameSLValue);
   ObjectDelete(0,NameTPCaption);
   ObjectDelete(0,NameTPValue);
   LinesSLTPDelete();
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void CIntegerTradePanel::FormBuySellButtonDelete()
  {
   ObjectDelete(0,NameBuyBut);
   ObjectDelete(0,NameSellBut);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void CIntegerTradePanel::LinesSLTPDelete()
  {
   ObjectDelete(0,NameStopPriceLine);
   ObjectDelete(0,NameOpenPriceLine);
   ObjectDelete(0,NameSLPriceLine);
   ObjectDelete(0,NameTPPriceLine);
   ObjectDelete(0,NameExpirTimeLine);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void CIntegerTradePanel::Deinit()
  {
   EventKillTimer();
   if(UninitializeReason()==REASON_REMOVE)
     {
      ObjectDelete(0,NameStoreX);
      ObjectDelete(0,NameStoreY);
     }
   ObjectDelete(0,NameMain);
   ObjectDelete(0,NameSnd);
   ObjectDelete(0,NameMail);
   ParamSave();
   FormDelete();
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void CIntegerTradePanel::ParamSave()
  {
   GlobalVariableSet(GVPrefix+_Symbol+"_Lots",Lots);
   GlobalVariableSet(GVPrefix+_Symbol+"_SL",TradeStopLoss);
   GlobalVariableSet(GVPrefix+_Symbol+"_TP",TradeTakeProfit);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void CIntegerTradePanel::FormCheckSLTPValues()
  {
   if(gSelectedIndex==-1)
     {
      if(TradeStopLoss<fSLNormalize(TradeStopLoss))
        {
         ObjectSetInteger(0,NameSLValue,OBJPROP_COLOR,ColorInputWarning);
         ChartRedraw();
        }
      else
        {
         ObjectSetInteger(0,NameSLValue,OBJPROP_COLOR,ColorInputTxt);
         ChartRedraw();
        }
      if(TradeTakeProfit<fTPNormalize(TradeTakeProfit))
        {
         ObjectSetInteger(0,NameTPValue,OBJPROP_COLOR,ColorInputWarning);
         ChartRedraw();
        }
      else
        {
         ObjectSetInteger(0,NameTPValue,OBJPROP_COLOR,ColorInputTxt);
         ChartRedraw();
        }
     }
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
int CIntegerTradePanel::fSLNormalize(int aSL)
  {
   if(aSL==0)return(0);
   return((int)MathMax(aSL,SymbolInfoInteger(_Symbol,SYMBOL_SPREAD)+SymbolInfoInteger(_Symbol,SYMBOL_TRADE_STOPS_LEVEL)));
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
int CIntegerTradePanel::fTPNormalize(int aTP)
  {
   if(aTP==0)return(0);
   return((int)MathMax(aTP,SymbolInfoInteger(_Symbol,SYMBOL_TRADE_STOPS_LEVEL)));
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void CIntegerTradePanel::fGetMarketInfo()
  {
   miAsk=SymbolInfoDouble(_Symbol,SYMBOL_ASK);
   miBid=SymbolInfoDouble(_Symbol,SYMBOL_BID);
   miMSL=(int)SymbolInfoInteger(_Symbol,SYMBOL_TRADE_STOPS_LEVEL);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void CIntegerTradePanel::ChartResizeMonitoring()
  {
   if(fIsWindowActive())
     {
      if(ObjectFind(0,NameForm)==0)
        {
         if(!ObjectGetInteger(0,NameForm,OBJPROP_SELECTED))
           {
            PosX=(int)ChartGetInteger(0,CHART_WIDTH_IN_PIXELS)-330-OffsetX;
            PosY=(int)ChartGetInteger(0,CHART_HEIGHT_IN_PIXELS)-40-OffsetY;
            if(ObjectGetInteger(0,NameForm,OBJPROP_XDISTANCE)!=PosX || ObjectGetInteger(0,NameForm,OBJPROP_YDISTANCE)!=PosY)
              {
               FormDelete();
               FormCreate();
               ChartRedraw();
              }
           }
        }
     }
   if(ObjectFind(0,NameMain)!=0)
     {
      MainCreateOnOfButton();
      ChartRedraw();
     }
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
bool CIntegerTradePanel::fIsWindowActive()
  {
   if(!MQL5InfoInteger(MQL5_DLLS_ALLOWED))return(true);
   int tWH=(int)ChartGetInteger(0,CHART_WINDOW_HANDLE);
   int tTWnd=GetAncestor(tWH,2);
   int hMDICWnd=GetDlgItem(tTWnd,0xE900);
   int hMDIAWnd=SendMessageA(hMDICWnd,0x0229,0,0);
   int tWH2=GetDlgItem(hMDIAWnd,0xE900);
   return(tWH==tWH2);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void CIntegerTradePanel::FormCreate()
  {
   int ChartWidth=(int)ChartGetInteger(0,CHART_WIDTH_IN_PIXELS);
   int ChartHeigh=(int)ChartGetInteger(0,CHART_HEIGHT_IN_PIXELS);
   PosX=ChartWidth-330-OffsetX;
   PosY=ChartHeigh-40-OffsetY;
   fObjCreateEdit(NameForm,"",PosX,PosY,327,37,0,ANCHOR_LEFT_UPPER,CORNER_LEFT_UPPER,ColorFormBg,ColorFormBorder,7,"Arial",0,false,0,OBJ_ALL_PERIODS,true,false);
   FormSolvePos();
   FormFrameSetPos();
   FormControlsCreate();
   ChartRedraw(0);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void CIntegerTradePanel::FormControlsCreate()
  {
   gSelectedIndex=-1;
   ObjectSetString(0,NameForm,OBJPROP_TEXT,"");
   for(int i=0;i<ArraySize(NameSelect);i++)
     {
      fObjCreateButton(NameSelect[i],false,PosX+i*20+2,PosY+2,20,15+3,CaptionSelect[i],ColorSelectOff,ColorText,0,CORNER_LEFT_UPPER,7,"Arial",0,false,OBJ_ALL_PERIODS);
     }
   fObjCreateLabel(NameLotsCaption,PosX+2+2,PosY+21,"lot:",0,ANCHOR_LEFT_UPPER,CORNER_LEFT_UPPER,ColorLabel,8,"Arial",0,false,0,false,false,OBJ_ALL_PERIODS);
   fObjCreateEdit(NameLotsValue,DoubleToString(Lots,LotsDigits),PosX+15+2+2,PosY+21,35,15,0,ANCHOR_LEFT_UPPER,CORNER_LEFT_UPPER,ColorInputBg,ColorInputTxt,7,"Arial",0,false,0,OBJ_ALL_PERIODS);
   fObjCreateLabel(NameSLCaption,PosX+55+2+1+4,PosY+21,"sl:",0,ANCHOR_LEFT_UPPER,CORNER_LEFT_UPPER,ColorLabel,8,"Arial",0,false,0,false,false,OBJ_ALL_PERIODS);
   fObjCreateEdit(NameSLValue,"",PosX+17+50+2+1+4,PosY+21,35,15,0,ANCHOR_LEFT_UPPER,CORNER_LEFT_UPPER,ColorInputBg,ColorInputTxt,7,"Arial",0,false,0,OBJ_ALL_PERIODS);
   fObjCreateLabel(NameTPCaption,PosX+110+3+3,PosY+21,"tp:",0,ANCHOR_LEFT_UPPER,CORNER_LEFT_UPPER,ColorLabel,8,"Arial",0,false,0,false,false,OBJ_ALL_PERIODS);
   fObjCreateEdit(NameTPValue,"",PosX+128,PosY+21,35,15,0,ANCHOR_LEFT_UPPER,CORNER_LEFT_UPPER,ColorInputBg,ColorInputTxt,7,"Arial",0,false,0,OBJ_ALL_PERIODS);
   FormBuySellButtonsCreate();
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void CIntegerTradePanel::FormBuySellButtonsCreate()
  {
   int TmpSellLeft=PosX+165;
   int TmpBuyLeft=PosX+246;
   fObjCreateButton(NameSellBut,false,TmpSellLeft+1,PosY+3,77,31,"Sell",ColorTradeBut[1],ColorText,0,CORNER_LEFT_UPPER,10,"Arial",0,false,OBJ_ALL_PERIODS);
   fObjCreateButton(NameBuyBut,false,TmpBuyLeft+1,PosY+3,77,31,"Buy",ColorTradeBut[0],ColorText,0,CORNER_LEFT_UPPER,10,"Arial",0,false,OBJ_ALL_PERIODS);
   FormSetSLTPValues();
   FormCheckSLTPValues();
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void CIntegerTradePanel::FormTradeButtonCreate()
  {
   fObjCreateButton(NameTradeBut,false,PosX+166,PosY+3,20*8-2,31,CaptionTradeBut[gSelectedIndex],ColorTradeBut[gSelectedIndex%2],ColorText,0,CORNER_LEFT_UPPER,10,"Arial",0,false,OBJ_ALL_PERIODS);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void CIntegerTradePanel::fObjCreateEdit(
                                        string      aObjName,
                                        string      aText,
                                        int         aX          =  30,
                                        int         aY          =  30,
                                        int         aXSize      =  380,
                                        int         aYSize      =  240,
                                        int         aWindow     =  0,
                                        int         aAnchor     =  ANCHOR_LEFT_UPPER,
                                        int         aCorner     =  CORNER_LEFT_UPPER,
                                        color       aBgColor    =  LightYellow,
                                        color       aColor      =  Chocolate,
                                        int         aFontSize   =  8,
                                        string      aFont       =  "Arial",
                                        int         aChartID    =  0,
                                        bool        aBack       =  false,
                                        double      aAngle      =  0,
                                        long        aTimeFrames =  OBJ_ALL_PERIODS,
                                        bool        aSelectable =  false,
                                        bool        aSelected   =  false
                                        )
  {
   ObjectCreate(aChartID,aObjName,OBJ_EDIT,aWindow,0,0);
   ObjectSetInteger(aChartID,aObjName,OBJPROP_ANCHOR,aAnchor);
   ObjectSetInteger(aChartID,aObjName,OBJPROP_BACK,aBack);
   ObjectSetInteger(aChartID,aObjName,OBJPROP_COLOR,aColor);
   ObjectSetInteger(aChartID,aObjName,OBJPROP_BGCOLOR,aBgColor);
   ObjectSetInteger(aChartID,aObjName,OBJPROP_CORNER,aCorner);
   ObjectSetInteger(aChartID,aObjName,OBJPROP_FONTSIZE,aFontSize);
   ObjectSetInteger(aChartID,aObjName,OBJPROP_SELECTABLE,aSelectable);
   ObjectSetInteger(aChartID,aObjName,OBJPROP_SELECTED,aSelected);
   ObjectSetInteger(aChartID,aObjName,OBJPROP_TIMEFRAMES,aTimeFrames);
   ObjectSetInteger(aChartID,aObjName,OBJPROP_XDISTANCE,aX);
   ObjectSetInteger(aChartID,aObjName,OBJPROP_YDISTANCE,aY);
   ObjectSetInteger(aChartID,aObjName,OBJPROP_XSIZE,aXSize);
   ObjectSetInteger(aChartID,aObjName,OBJPROP_YSIZE,aYSize);
   ObjectSetString(aChartID,aObjName,OBJPROP_FONT,aFont);
   ObjectSetString(aChartID,aObjName,OBJPROP_TEXT,aText);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void CIntegerTradePanel::FormSolvePos()
  {
   PosX=(int)ObjectGetInteger(0,NameForm,OBJPROP_XDISTANCE);
   PosY=(int)ObjectGetInteger(0,NameForm,OBJPROP_YDISTANCE);
   int tx=(int)ChartGetInteger(0,CHART_WIDTH_IN_PIXELS)-330;
   int ty=(int)ChartGetInteger(0,CHART_HEIGHT_IN_PIXELS)-40;
   bool Correct=false;
   if(PosX>tx)
     {
      PosX=tx;
     }
   if(PosY>ty)
     {
      PosY=ty;
     }
   if(PosX<3)
     {
      PosX=3;
     }
   if(PosY<3)
     {
      PosY=3;
     }
   OffsetX=tx-PosX;
   OffsetY=ty-PosY;
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void CIntegerTradePanel::FormFrameSetPos()
  {
   ObjectSetInteger(0,NameForm,OBJPROP_XDISTANCE,PosX);
   ObjectSetInteger(0,NameForm,OBJPROP_YDISTANCE,PosY);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void CIntegerTradePanel::fObjCreateLabel(
                                         string   aObjName    =  "ObjLabel",
                                         int      aX          =  30,
                                         int      aY          =  30,
                                         string   aText       =  "ObjLabel",
                                         int      aWindow     =  0,
                                         int      aAnchor     =  ANCHOR_LEFT_UPPER,
                                         int      aCorner     =  CORNER_LEFT_UPPER,
                                         color    aColor      =  Red,
                                         int      aFontSize   =  8,
                                         string   aFont       =  "Arial",
                                         int      aChartID    =  0,
                                         bool     aBack       =  false,
                                         double   aAngle      =  0,
                                         bool     aSelectable =  true,
                                         bool     aSelected   =  false,
                                         long     aTimeFrames =  OBJ_ALL_PERIODS
                                         )
  {
   ObjectDelete(aChartID,aObjName);
   ObjectCreate(aChartID,aObjName,OBJ_LABEL,aWindow,0,0);
   ObjectSetInteger(aChartID,aObjName,OBJPROP_ANCHOR,aAnchor);
   ObjectSetInteger(aChartID,aObjName,OBJPROP_BACK,aBack);
   ObjectSetInteger(aChartID,aObjName,OBJPROP_COLOR,aColor);
   ObjectSetInteger(aChartID,aObjName,OBJPROP_CORNER,aCorner);
   ObjectSetInteger(aChartID,aObjName,OBJPROP_FONTSIZE,aFontSize);
   ObjectSetInteger(aChartID,aObjName,OBJPROP_SELECTABLE,aSelectable);
   ObjectSetInteger(aChartID,aObjName,OBJPROP_SELECTED,aSelected);
   ObjectSetInteger(aChartID,aObjName,OBJPROP_TIMEFRAMES,aTimeFrames);
   ObjectSetInteger(aChartID,aObjName,OBJPROP_XDISTANCE,aX);
   ObjectSetInteger(aChartID,aObjName,OBJPROP_YDISTANCE,aY);
   ObjectSetString(aChartID,aObjName,OBJPROP_TEXT,aText);
   ObjectSetString(aChartID,aObjName,OBJPROP_FONT,aFont);
   ObjectSetDouble(aChartID,aObjName,OBJPROP_ANGLE,aAngle);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void CIntegerTradePanel::FormSetSLTPValues()
  {
   ObjectSetString(0,NameSLValue,OBJPROP_TEXT,DoubleToString(TradeStopLoss,0));
   ObjectSetString(0,NameTPValue,OBJPROP_TEXT,DoubleToString(TradeTakeProfit,0));
   FormCheckSLTPValues();
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void CIntegerTradePanel::EventAlert()
  {
   bool snd=false;
   bool email=false;
   if(GlobalVariableCheck(NameSnd))
     {
      if(ChartFirst()==ChartID())
        {
         snd=true;
        }
     }
   if(GlobalVariableCheck(NameMail))
     {
      if(ChartFirst()==ChartID())
        {
         email=true;
        }
     }
   EventSLTP(snd,email,SoundEvent);
   EventSLToL(snd,email,SoundEvent);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void CIntegerTradePanel::EventSLTP(bool aSoundON,bool aEMailON,string aSoundFile)
  {
   static int sFrom=-1;
   double profit;
   if(!HistorySelect(0,TimeCurrent()))
     {
      return;
     }
   if(!aSoundON && !aEMailON)
     {
      sFrom=HistoryDealsTotal();
      return;
     }
   if(sFrom==-1)
     {
      sFrom=HistoryDealsTotal();
     }
   for(int i=sFrom;i<HistoryDealsTotal();i++)
     {
      ulong ticket=HistoryDealGetTicket(i);
      if(ticket==0)
        {
         return;
        }
      ulong dealorder=HistoryDealGetInteger(ticket,DEAL_ORDER);
      string symbol=HistoryDealGetString(ticket,DEAL_SYMBOL);
      int digits=(int)SymbolInfoInteger(symbol,SYMBOL_DIGITS);
      string comment=HistoryDealGetString(ticket,DEAL_COMMENT);
      int slpos=StringFind(comment,"sl",0);
      int tppos=StringFind(comment,"tp",0);
      if(slpos==0 || slpos==1)
        {
         profit=fDealProfit(ticket);
         string text=symbol+" SL "+(profit>0?"+"+DS2(profit):DS2(profit))+" "+AccountInfoString(ACCOUNT_CURRENCY);
         if(aEMailON)
           {
            string subj="Account "+(string)AccountInfoInteger(ACCOUNT_LOGIN)+" ("+AccountInfoString(ACCOUNT_COMPANY)+")";
            SendMail(subj,text);
           }
         if(aSoundON)
           {
            PlaySound(aSoundFile);
            Print("EVENT "+text);
           }
         sFrom=i+1;
         continue;
        }
      if(tppos==0 || tppos==1)
        {
         profit=fDealProfit(ticket);
         string text=symbol+" TP "+(profit>0?"+"+DS2(profit):DS2(profit))+" "+AccountInfoString(ACCOUNT_CURRENCY);
         if(aEMailON)
           {
            string subj="Account "+(string)AccountInfoInteger(ACCOUNT_LOGIN)+" ("+AccountInfoString(ACCOUNT_COMPANY)+")";
            SendMail(subj,text);
           }
         if(aSoundON)
           {
            PlaySound(aSoundFile);
            Print("EVENT "+text);
           }
         sFrom=i+1;
         continue;
        }

      if(HistoryOrderSelect(dealorder))
        {
         long type=HistoryOrderGetInteger(dealorder,ORDER_TYPE);
         if(type>1)
           {
            string text=symbol+" "+fNameOrderType(type)+" "+DS2(HistoryOrderGetDouble(dealorder,ORDER_VOLUME_INITIAL))+" "+DoubleToString(HistoryOrderGetDouble(dealorder,ORDER_PRICE_OPEN),digits)+" filled";
            if(aEMailON)
              {
               string subj="Account "+(string)AccountInfoInteger(ACCOUNT_LOGIN)+" ("+AccountInfoString(ACCOUNT_COMPANY)+")";
               SendMail(subj,text);
              }
            if(aSoundON)
              {
               PlaySound(aSoundFile);
               Print("EVENT "+text);
              }
           }
        }
      else
        {
         return;
        }

      sFrom=i+1;
     }
   return;
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void CIntegerTradePanel::EventSLToL(bool aSoundON,bool aEMailON,string aSoundFile)
  {
   static ulong Tickets[];
   bool err=false;
   string symbol;
   int digits;
   string text;
   string subj;
   for(int i=0;i<ArraySize(Tickets);i++)
     {
      if(Tickets[i]>0)
        {
         if(OrderSelect(Tickets[i]))
           {
            switch(OrderGetInteger(ORDER_TYPE))
              {
               case ORDER_TYPE_BUY_LIMIT:
                  symbol=OrderGetString(ORDER_SYMBOL);
                  digits=(int)SymbolInfoInteger(symbol,SYMBOL_DIGITS);
                  text=symbol+" BuyStopLimit "+DS2(OrderGetDouble(ORDER_VOLUME_INITIAL))+" "+DoubleToString(OrderGetDouble(ORDER_PRICE_CURRENT),digits)+" -> BuyLimit "+DoubleToString(OrderGetDouble(ORDER_PRICE_OPEN),digits);
                  if(aEMailON)
                    {
                     subj="Account "+(string)AccountInfoInteger(ACCOUNT_LOGIN)+" ("+AccountInfoString(ACCOUNT_COMPANY)+")";
                     SendMail(subj,text);
                    }
                  if(aSoundON)
                    {
                     PlaySound(aSoundFile);
                     Print("EVENT "+text);
                    }
                  break;
               case ORDER_TYPE_SELL_LIMIT:
                  symbol=OrderGetString(ORDER_SYMBOL);
                  digits=(int)SymbolInfoInteger(symbol,SYMBOL_DIGITS);
                  text=symbol+" SellStopLimit "+DS2(OrderGetDouble(ORDER_VOLUME_INITIAL))+" "+DoubleToString(OrderGetDouble(ORDER_PRICE_CURRENT),digits)+" -> SellLimit "+DoubleToString(OrderGetDouble(ORDER_PRICE_OPEN),digits);
                  if(aEMailON)
                    {
                     subj="Account "+(string)AccountInfoInteger(ACCOUNT_LOGIN)+" ("+AccountInfoString(ACCOUNT_COMPANY)+")";
                     SendMail(subj,text);
                    }
                  if(aSoundON)
                    {
                     PlaySound(aSoundFile);
                     Print("EVENT "+text);
                    }
                  break;
              }
            Tickets[i]=0;
           }
         else
           {
            err=true;
           }
        }
     }

   int Total=OrdersTotal();
   ArrayResize(Tickets,Total);
   for(int i=0;i<Total;i++)
     {
      Tickets[i]=OrderGetTicket(i);
      if(OrderSelect(Tickets[i]))
        {
         if(OrderGetInteger(ORDER_TYPE)<6)
           {
            Tickets[i]=0;
           }
        }
      else
        {
         Tickets[i]=0;
        }

     }
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
double CIntegerTradePanel::fDealProfit(ulong aTicket)
  {
   return(NormalizeDouble(HistoryDealGetDouble(aTicket,DEAL_PROFIT)+HistoryDealGetDouble(aTicket,DEAL_COMMISSION)+HistoryDealGetDouble(aTicket,DEAL_SWAP),2));
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
string CIntegerTradePanel::DS2(double aValue)
  {
   return(DoubleToString(aValue,2));
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
double CIntegerTradePanel::ND(double aValue)
  {
   return(NormalizeDouble(aValue,_Digits));
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
int CIntegerTradePanel::fSlippage()
  {
   return((int)SymbolInfoInteger(_Symbol,SYMBOL_SPREAD)*2);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
string CIntegerTradePanel::fNameOrderType(long aType)
  {
   switch(aType)
     {
      case ORDER_TYPE_BUY:                return("Buy");
      case ORDER_TYPE_SELL:               return("Sell");
      case ORDER_TYPE_BUY_LIMIT:          return("BuyLimit");
      case ORDER_TYPE_SELL_LIMIT:         return("SellLimit");
      case ORDER_TYPE_BUY_STOP:           return("BuyStop");
      case ORDER_TYPE_SELL_STOP:          return("SellStop");
      case ORDER_TYPE_BUY_STOP_LIMIT:     return("BuyStopLimit");
      case ORDER_TYPE_SELL_STOP_LIMIT:    return("SellStopLimit");
     }
   return("?");
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void CIntegerTradePanel::Timer()
  {
   ChartResizeMonitoring();
   EventAlert();
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void CIntegerTradePanel::Tick()
  {
   FormCheckSLTPValues();
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void CIntegerTradePanel::ChartEvent(const int id,
                                    const long &lparam,
                                    const double &dparam,
                                    const string &sparam
                                    )
  {
   color TmpColor;
   if(id==CHARTEVENT_CLICK && PreId==id)
     {
      if(ObjectGetInteger(0,NameForm,OBJPROP_SELECTED))
        {
         FormControlsCreate();
         ObjectSetInteger(0,NameForm,OBJPROP_SELECTED,false);
         ChartRedraw();
        }
      ChartResizeMonitoring();
     }
   if(id==CHARTEVENT_OBJECT_CLICK)
     {
      if(sparam==NameMain)
        {
         Sleep(300);
         ObjectSetInteger(0,NameMain,OBJPROP_STATE,false);
         if(ObjectFind(0,NameForm)==0)
           {
            //ObjectSetString(0,NameMain,OBJPROP_TEXT,CaptionMainOn);
            FormDelete();

           }
         else
           {
            //ObjectSetString(0,NameMain,OBJPROP_TEXT,CaptionMainOff);
            if(ObjectFind(0,NameStoreX)==0)
              {
               OffsetX=(int)StringToInteger(ObjectGetString(0,NameStoreX,OBJPROP_TEXT));
              }
            if(ObjectFind(0,NameStoreY)==0)
              {
               OffsetY=(int)StringToInteger(ObjectGetString(0,NameStoreY,OBJPROP_TEXT));
              }
            FormCreate();
           }
        }
      if(sparam==NameSnd)
        {
         ChartsOnOfAllButtons(sparam);
        }
      if(sparam==NameMail)
        {
         ChartsOnOfAllButtons(sparam);
        }
      if(sparam==NameForm)
        {
         if(ObjectGetInteger(0,sparam,OBJPROP_SELECTED))
           {
            FormControlsDelete();
            ChartRedraw();
           }
         else
           {
            FormControlsCreate();
            ChartRedraw();
           }
        }
      // = TRADE SELECTED =
      if(sparam==NameTradeBut)
        {
         TradeSelected();
         ObjectSetInteger(0,sparam,OBJPROP_STATE,false);
         ChartRedraw(0);
        }
      // = TRADE BUY =
      if(sparam==NameBuyBut)
        {
         double sl,tp;
         fSolveBuySLTP(_Symbol,TradeStopLoss,TradeTakeProfit,sl,tp);
         fOpBuy(_Symbol,Lots,sl,tp,fSlippage(),Magic,"","Open Buy ("+_Symbol+","+DS2(Lots)+")...",true);
         ObjectSetInteger(0,sparam,OBJPROP_STATE,false);
         ChartRedraw(0);
        }
      // = TRADE SELL =
      if(sparam==NameSellBut)
        {
         double sl,tp;
         fSolveSellSLTP(_Symbol,TradeStopLoss,TradeTakeProfit,sl,tp);
         fOpSell(_Symbol,Lots,sl,tp,fSlippage(),Magic,"","Open Sell ("+_Symbol+","+DS2(Lots)+")...",true);
         ObjectSetInteger(0,sparam,OBJPROP_STATE,false);
         ChartRedraw(0);
        }
      for(int i=0;i<ArraySize(NameSelect);i++)
        {
         if(sparam==NameSelect[i])
           {
            if(ObjectGetInteger(0,NameSelect[i],OBJPROP_STATE))
              {
               FormContolsSelectAllOff();
               LinesSLTPDelete();
               gSelectedIndex=i;
               if(gSelectedIndex%2==0)
                 {
                  TmpColor=ColorSelectBuyOn;
                 }
               else
                 {
                  TmpColor=ColorSelectSellOn;
                 }
               ObjectSetInteger(0,NameSelect[i],OBJPROP_BGCOLOR,TmpColor);
               ObjectSetInteger(0,NameSelect[i],OBJPROP_STATE,true);
               FormBuySellButtonDelete();
               FormTradeButtonCreate();
               LinesSLTPSolveAndCreate();
              }
            else
              {
               gSelectedIndex=-1;
               FormContolsSelectAllOff();
               LinesSLTPDelete();
               ObjectDelete(0,NameTradeBut);
               FormBuySellButtonsCreate();
              }
            ChartRedraw(0);
            break;
           }
        }
      if(gSelectedIndex==-1)
        {
         if(sparam==NameSLCaption)
           {
            TradeStopLoss=fSLNormalize(1);
            FormSetSLTPValues();
            ChartRedraw(0);
           }
         if(sparam==NameTPCaption)
           {
            TradeTakeProfit=fTPNormalize(1);
            FormSetSLTPValues();
            ChartRedraw(0);
           }
        }
      if(sparam==NameLotsCaption)
        {
         if(PositionSelect(_Symbol))
           {
            Lots=PositionGetDouble(POSITION_VOLUME);
           }
         else
           {
            Lots=SymbolInfoDouble(_Symbol,SYMBOL_VOLUME_MIN);
           }
         ObjectSetString(0,NameLotsValue,OBJPROP_TEXT,DoubleToString(Lots,LotsDigits));
         ChartRedraw(0);
        }
     }
   if(id==CHARTEVENT_OBJECT_DRAG)
     {
      if(sparam==NameStopPriceLine || sparam==NameOpenPriceLine || sparam==NameSLPriceLine || sparam==NameTPPriceLine || sparam==NameExpirTimeLine)
        {
         double TriggerPrice,OpenPrice,SLPrice,TPPrice;
         datetime Expiration;
         LinesSLTPGetValues(TriggerPrice,OpenPrice,SLPrice,TPPrice,Expiration);
         LinesSLTPValuesCorrection(TriggerPrice,OpenPrice,SLPrice,TPPrice,Expiration);
         LinesSLTPCreate(TriggerPrice,OpenPrice,SLPrice,TPPrice,Expiration);
         ChartRedraw();
        }
      if(sparam==NameForm)
        {
         FormSolvePos();
         FormFrameSetPos();
         FormControlsCreate();
         ObjectSetInteger(0,sparam,OBJPROP_SELECTED,false);
         fObjCreateLabel(NameStoreX,0,0,(string)OffsetX+"   ",0,ANCHOR_RIGHT_LOWER,CORNER_LEFT_UPPER,Red,10,"Arial",0,false,0,false,false,OBJ_NO_PERIODS);
         fObjCreateLabel(NameStoreY,0,0,(string)OffsetY+"   ",0,ANCHOR_RIGHT_LOWER,CORNER_LEFT_UPPER,Red,10,"Arial",0,false,0,false,false,OBJ_NO_PERIODS);
         ChartRedraw();
        }
     }
   if(id==CHARTEVENT_OBJECT_DELETE)
     {
      if(gSelectedIndex!=-1)
        {
         if(sparam==NameStopPriceLine || sparam==NameOpenPriceLine || sparam==NameSLPriceLine || sparam==NameTPPriceLine || sparam==NameExpirTimeLine)
           {
            LinesSLTPCreate(rTriggerPrice,rOpenPrice,rSLPrice,rTPPrice,rExpiration);
            ChartRedraw();
           }
        }
      if(sparam==NameForm)
        {
         ObjectSetString(0,NameMain,OBJPROP_TEXT,CaptionMainOn);
         ChartRedraw();
        }
     }
   if(id==CHARTEVENT_OBJECT_ENDEDIT)
     {
      if(sparam==NameLotsValue)
        {
         string tmp=ObjectGetString(0,sparam,OBJPROP_TEXT);
         int pos=StringFind(tmp,",",0);
         if(pos>=0)
           {
            tmp=StringSubstr(tmp,0,pos)+"."+StringSubstr(tmp,pos+1,StringLen(tmp)-pos-1);
           }
         Lots=StringToDouble(tmp);
         Lots=fLotsNormalize(Lots);
         ObjectSetString(0,sparam,OBJPROP_TEXT,DoubleToString(Lots,LotsDigits));
         ChartRedraw();
        }
      if(sparam==NameSLValue)
        {
         TradeStopLoss=TxtToVal(ObjectGetString(0,sparam,OBJPROP_TEXT));
         ObjectSetString(0,sparam,OBJPROP_TEXT,DoubleToString(TradeStopLoss,0));
         FormCheckSLTPValues();
         ChartRedraw();
        }
      if(sparam==NameTPValue)
        {
         TradeTakeProfit=TxtToVal(ObjectGetString(0,sparam,OBJPROP_TEXT));
         ObjectSetString(0,sparam,OBJPROP_TEXT,DoubleToString(TradeTakeProfit,0));
         FormCheckSLTPValues();
         ChartRedraw();
        }
     }
   if(ObjectFind(0,NameForm)==0)
     {
      if(ObjectGetString(0,NameMain,OBJPROP_TEXT)!=CaptionMainOff)
        {
         ObjectSetString(0,NameMain,OBJPROP_TEXT,CaptionMainOff);
         ChartRedraw();
        }
     }
   else
     {
      if(ObjectGetString(0,NameMain,OBJPROP_TEXT)!=CaptionMainOn)
        {
         ObjectSetString(0,NameMain,OBJPROP_TEXT,CaptionMainOn);
         ChartRedraw();
        }
     }
   PreId=id;
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
int CIntegerTradePanel::TxtToVal(string aTxt)
  {
   StringTrimLeft(aTxt);
   StringTrimRight(aTxt);
   if(StringLen(aTxt)>0)
     {
      if(aTxt=="t")TFTimeInfo(PERIOD_H4);
      if(aTxt=="t2")TFTimeInfo(PERIOD_H2);
      if(aTxt=="t3")TFTimeInfo(PERIOD_H3);
      if(aTxt=="t4")TFTimeInfo(PERIOD_H4);
      if(aTxt=="t6")TFTimeInfo(PERIOD_H6);
      if(aTxt=="t8")TFTimeInfo(PERIOD_H8);
      if(aTxt=="t12")TFTimeInfo(PERIOD_H12);
      if(aTxt=="d")DateInfo();
      string rch=StringSubstr(aTxt,StringLen(aTxt)-1,1);
      string val=StringSubstr(aTxt,0,StringLen(aTxt)-1);
      if(val=="")val="1";
      if(rch=="s")return((int)SymbolInfoInteger(_Symbol,SYMBOL_SPREAD)*(int)StringToInteger(val));
      if(rch=="m")return((int)SymbolInfoInteger(_Symbol,SYMBOL_TRADE_STOPS_LEVEL)*(int)StringToInteger(val));
     }
   return((int)StringToInteger(aTxt));
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void CIntegerTradePanel::DateInfo()
  {
   MqlDateTime ts;
   TimeToStruct(TimeTradeServer(),ts);
   Alert(fNameWeekDay(ts.day_of_week)+" "+IntegerToString(ts.day)+"-th "+fNameMonth(ts.mon)+" "+IntegerToString(ts.year));
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
string CIntegerTradePanel::fNameMonth(int aMonthIndex)
  {
   switch(aMonthIndex)
     {
      case 1:
         return("January");
      case 2:
         return("February");
      case 3:
         return("March");
      case 4:
         return("April");
      case 5:
         return("May");
      case 6:
         return("June");
      case 7:
         return("July");
      case 8:
         return("August");
      case 9:
         return("September");
      case 10:
         return("October");
      case 11:
         return("November");
      case 12:
         return("December");
     }
   return("?");
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
string CIntegerTradePanel::fNameWeekDay(int aMonthIndex)
  {
   switch(aMonthIndex)
     {
      case 0:
         return("Sunday");
      case 1:
         return("Monday");
      case 2:
         return("Tuesday");
      case 3:
         return("Wednesday");
      case 4:
         return("Thursday");
      case 5:
         return("Friday");
      case 6:
         return("Saturday");
     }
   return("?");
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void CIntegerTradePanel::TFTimeInfo(ENUM_TIMEFRAMES aTF)
  {
   datetime tm[1];
   if(CopyTime(_Symbol,aTF,0,1,tm)!=-1)
     {
      Alert(TimeToString(TimeCurrent()-tm[0],TIME_MINUTES));
     }
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void CIntegerTradePanel::fSolveBuySLTP(string aSymbol,int aStopLoss,int aTakeProfit,double  &aSL,double  &aTP)
  {
   bool SLTPCorrection=false;
   aSL=0;
   aTP=0;
   if(aStopLoss<=0 && aTakeProfit<=0)
     {
      return;
     }
   double msl;
   double pAsk=SymbolInfoDouble(aSymbol,SYMBOL_ASK);
   double pBid=SymbolInfoDouble(aSymbol,SYMBOL_BID);
   double pPoint=SymbolInfoDouble(aSymbol,SYMBOL_POINT);
   int pStopLevel=(int)SymbolInfoInteger(aSymbol,SYMBOL_TRADE_STOPS_LEVEL);
   int pDigits=(int)SymbolInfoInteger(aSymbol,SYMBOL_DIGITS);
   if(aStopLoss>0)
     {
      aSL=pAsk-pPoint*aStopLoss;
      aSL=NormalizeDouble(aSL,pDigits);
      if(SLTPCorrection)
        {
         msl=pBid-pPoint*(pStopLevel+1);
         msl=NormalizeDouble(msl,pDigits);
         aSL=MathMin(aSL,msl);
        }
     }
   if(aTakeProfit>0)
     {
      aTP=pAsk+pPoint*aTakeProfit;
      aTP=NormalizeDouble(aTP,pDigits);
      if(SLTPCorrection)
        {
         msl=pAsk+pPoint*(pStopLevel+1);
         msl=NormalizeDouble(msl,pDigits);
         aTP=MathMax(aTP,msl);
        }
     }
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void CIntegerTradePanel::fSolveSellSLTP(string aSymbol,int aStopLoss,int aTakeProfit,double  &aSL,double  &aTP)
  {
   bool SLTPCorrection=false;
   aSL=0;
   aTP=0;
   if(aStopLoss<=0 && aTakeProfit<=0)
     {
      return;
     }
   double msl;
   double pAsk=SymbolInfoDouble(aSymbol,SYMBOL_ASK);
   double pBid=SymbolInfoDouble(aSymbol,SYMBOL_BID);
   double pPoint=SymbolInfoDouble(aSymbol,SYMBOL_POINT);
   int pStopLevel=(int)SymbolInfoInteger(aSymbol,SYMBOL_TRADE_STOPS_LEVEL);
   int pDigits=(int)SymbolInfoInteger(aSymbol,SYMBOL_DIGITS);
   if(aStopLoss>0)
     {
      aSL=pBid+pPoint*aStopLoss;
      aSL=NormalizeDouble(aSL,pDigits);
      if(SLTPCorrection)
        {
         msl=pAsk+pPoint*(pStopLevel+1);
         msl=NormalizeDouble(msl,pDigits);
         aSL=MathMax(aSL,msl);
        }
     }
   if(aTakeProfit>0)
     {
      aTP=pBid-pPoint*aTakeProfit;
      aTP=NormalizeDouble(aTP,pDigits);
      if(SLTPCorrection)
        {
         msl=pBid-pPoint*(pStopLevel+1);
         msl=NormalizeDouble(msl,pDigits);
         aTP=MathMin(aTP,msl);
        }
     }
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void CIntegerTradePanel::ChartsOnOfAllButtons(string sparam)
  {
   color comcol;
   if(ObjectGetInteger(0,sparam,OBJPROP_BGCOLOR)==ColorMainButBg)
     {
      ObjectSetInteger(0,sparam,OBJPROP_BGCOLOR,ColorMainButBgOn);
      comcol=ColorMainButBgOn;
      GlobalVariableSet(sparam,1);
     }
   else
     {
      ObjectSetInteger(0,sparam,OBJPROP_BGCOLOR,ColorMainButBg);
      comcol=ColorMainButBg;
      GlobalVariableDel(sparam);
     }
   if(ObjectGetInteger(0,sparam,OBJPROP_STATE))
     {
      Sleep(200);
      ObjectSetInteger(0,sparam,OBJPROP_STATE,false);

     }
   ChartRedraw(0);
   long chn=ChartFirst();
   do
     {
      if(ObjectFind(chn,sparam)==0)
        {
         ObjectSetInteger(chn,sparam,OBJPROP_BGCOLOR,comcol);
         ChartRedraw(chn);
        }
      chn=ChartNext(chn);
     }
   while(chn!=-1);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void CIntegerTradePanel::FormContolsSelectAllOff()
  {
   for(int i=0;i<ArraySize(NameSelect);i++)
     {
      ObjectSetInteger(0,NameSelect[i],OBJPROP_BGCOLOR,ColorSelectOff);
      ObjectSetInteger(0,NameSelect[i],OBJPROP_STATE,false);
     }
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void CIntegerTradePanel::LinesSLTPSolveAndCreate()
  {
   double TriggerPrice=0;
   double OpenPrice=0;
   double SLPrice=0;
   double TPPrice=0;
   datetime Expiration=0;

   int spr=(int)SymbolInfoInteger(_Symbol,SYMBOL_SPREAD);

   DefaultLineTriggerLevel=spr*7;
   DefaultLinePendingLevel=spr*14;
   DefaultLineStopLoss=spr*14;
   DefaultLineTakeProfit=spr*28;

   double atr[1];
   if(CopyBuffer(ATRHandle,0,1,1,atr)!=-1)
     {
      spr=(int)MathRound(0.15*atr[0]/_Point);

      DefaultLineTriggerLevel=spr*7;
      DefaultLinePendingLevel=spr*14;
      DefaultLineStopLoss=spr*14;
      DefaultLineTakeProfit=spr*28;

     }

   fGetMarketInfo();
   switch(gSelectedIndex)
     {
      case 0: // b
         OpenPrice=ND(miAsk);
         break;
      case 1: // s
         OpenPrice=ND(miBid);
         break;
      case 2: //bs
         OpenPrice=ND(miAsk+_Point*DefaultLinePendingLevel);
         break;
      case 3: // ss
         OpenPrice=ND(miBid-_Point*DefaultLinePendingLevel);
         break;
      case 4: // bl
         OpenPrice=ND(miAsk-_Point*DefaultLinePendingLevel);
         break;
      case 5: // sl
         OpenPrice=ND(miBid+_Point*DefaultLinePendingLevel);
         break;
      case 6: // bsl
         TriggerPrice=ND(miAsk+_Point*DefaultLineTriggerLevel);
         TriggerPrice=MathMax(TriggerPrice,ND(miAsk+_Point+_Point*miMSL));
         OpenPrice=ND(TriggerPrice-_Point*DefaultLinePendingLevel);
         break;
      case 7: // ssl
         TriggerPrice=ND(miBid-_Point*DefaultLineTriggerLevel);
         TriggerPrice=MathMin(TriggerPrice,ND(miBid-_Point-_Point*miMSL));
         OpenPrice=ND(TriggerPrice+_Point*DefaultLinePendingLevel);
         break;
     }
   if(gSelectedIndex%2==0)
     {
      if(DefaultLineStopLoss>0)
        {
         SLPrice=ND(OpenPrice-_Point*DefaultLineStopLoss);
         SLPrice=MathMin(SLPrice,ND(OpenPrice-(miAsk-miBid)-_Point-_Point*miMSL));
        }
      if(DefaultLineTakeProfit>0)
        {
         TPPrice=ND(OpenPrice+_Point*DefaultLineTakeProfit);
         TPPrice=MathMax(TPPrice,ND(OpenPrice+_Point+_Point*miMSL));
        }
     }
   else
     {
      if(DefaultLineStopLoss>0)
        {
         SLPrice=ND(OpenPrice+_Point*DefaultLineStopLoss);
         SLPrice=MathMax(SLPrice,ND(OpenPrice+(miAsk-miBid)+_Point+_Point*miMSL));
        }
      if(DefaultLineTakeProfit>0)
        {
         TPPrice=ND(OpenPrice-_Point*DefaultLineTakeProfit);
         TPPrice=MathMin(TPPrice,ND(OpenPrice-_Point-_Point*miMSL));
        }
     }
   if(gSelectedIndex<2)
     {
      OpenPrice=0;
     }
   else
     {
      datetime dt[1];
      datetime dt2[1];
      datetime TC=TimeCurrent();
      if(DefaultLineExpiration>0)
        {
         CopyTime(_Symbol,PERIOD_CURRENT,0,1,dt);
         Expiration=dt[0]+15*PeriodSeconds();
        }
      else
        {
         CopyTime(_Symbol,PERIOD_CURRENT,10,1,dt);
         Expiration=dt[0];
        }
     }
   LinesSLTPValuesCorrection(TriggerPrice,OpenPrice,SLPrice,TPPrice,Expiration);
   LinesSLTPCreate(TriggerPrice,OpenPrice,SLPrice,TPPrice,Expiration);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void CIntegerTradePanel::LinesSLTPCreate(double aTriggerPrice,double aOpenPrice,double aSLPrice,double aTPPrice,datetime aExpiration)
  {
   if(aTriggerPrice>0)
     {
      fObjHLine(NameStopPriceLine,aTriggerPrice,"StopPrice",0,ColorTrigger[gSelectedIndex%2],1,0,0,false,true,true,OBJ_ALL_PERIODS);
     }
   if(aOpenPrice>0)
     {
      fObjHLine(NameOpenPriceLine,aOpenPrice,"OpenPrice",0,ColorOpen[gSelectedIndex%2],1,0,0,false,true,true,OBJ_ALL_PERIODS);
     }
   if(aSLPrice>0)
     {
      fObjHLine(NameSLPriceLine,aSLPrice,"StopLoss",0,ColorSL[gSelectedIndex%2],1,0,0,false,true,true,OBJ_ALL_PERIODS);
     }
   if(aTPPrice>0)
     {
      fObjHLine(NameTPPriceLine,aTPPrice,"TakeProfit",0,ColorTP[gSelectedIndex%2],1,0,0,false,true,true,OBJ_ALL_PERIODS);
     }

   if(aExpiration>0)
     {
      fObjVLine(NameExpirTimeLine,aExpiration,"Expiration",0,ColorExp[gSelectedIndex%2],1,0,0,false,true,true,OBJ_ALL_PERIODS);
     }
   rTriggerPrice=aTriggerPrice;
   rOpenPrice=aOpenPrice;
   rSLPrice=aSLPrice;
   rTPPrice=aTPPrice;
   rExpiration=aExpiration;
   LinesSLTPShowInForm();
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void CIntegerTradePanel::LinesSLTPGetValues(double  &aTriggerPrice,double  &aOpenPrice,double  &aSLPrice,double  &aTPPrice,datetime  &aExpiration)
  {
   aTriggerPrice=ND(ObjectGetDouble(0,NameStopPriceLine,OBJPROP_PRICE));
   aOpenPrice=ND(ObjectGetDouble(0,NameOpenPriceLine,OBJPROP_PRICE));
   aSLPrice=ND(ObjectGetDouble(0,NameSLPriceLine,OBJPROP_PRICE));
   aTPPrice=ND(ObjectGetDouble(0,NameTPPriceLine,OBJPROP_PRICE));
   aExpiration=datetime(ObjectGetInteger(0,NameExpirTimeLine,OBJPROP_TIME));
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void CIntegerTradePanel::LinesSLTPValuesCorrection(double  &aTriggerPrice,double  &aOpenPrice,double  &aSLPrice,double  &aTPPrice,datetime  &aExpiration)
  {
   fGetMarketInfo();
   if(aTriggerPrice!=0)
     {
      switch(gSelectedIndex)
        {
         case 6: // bsl
            aTriggerPrice=MathMax(aTriggerPrice,ND(miAsk+_Point+_Point*miMSL));
            break;
         case 7: // ssl
            aTriggerPrice=MathMin(aTriggerPrice,ND(miBid-_Point-_Point*miMSL));
            break;
        }
     }
   switch(gSelectedIndex)
     {
      case 0: //bs
         aOpenPrice=ND(miAsk);

         break;
      case 1: // ss
         aOpenPrice=ND(miBid);
         break;
     }
   if(aOpenPrice!=0)
     {
      switch(gSelectedIndex)
        {
         case 2: //bs
            aOpenPrice=MathMax(aOpenPrice,ND(miAsk+_Point+_Point*miMSL));
            break;
         case 3: // ss
            aOpenPrice=MathMin(aOpenPrice,ND(miBid-_Point-_Point*miMSL));
            break;
         case 4: // bl
            aOpenPrice=MathMin(aOpenPrice,ND(miAsk-_Point-_Point*miMSL));
            break;
         case 5: // sl
            aOpenPrice=MathMax(aOpenPrice,ND(miBid+_Point+_Point*miMSL));
            break;
         case 6: // bsl
            aOpenPrice=MathMin(aOpenPrice,ND(aTriggerPrice-_Point-_Point*miMSL));
            break;
         case 7: // ssl
            aOpenPrice=MathMax(aOpenPrice,ND(aTriggerPrice+_Point+_Point*miMSL));
            break;
        }
     }
   if(gSelectedIndex%2==0)
     {
      aSLPrice=MathMin(aSLPrice,ND(aOpenPrice-(miAsk-miBid)-_Point-_Point*miMSL));
      aTPPrice=MathMax(aTPPrice,ND(aOpenPrice+_Point+_Point*miMSL));
     }
   else
     {
      aSLPrice=MathMax(aSLPrice,ND(aOpenPrice+(miAsk-miBid)+_Point+_Point*miMSL));
      aTPPrice=MathMin(aTPPrice,ND(aOpenPrice-_Point-_Point*miMSL));
     }
   if(gSelectedIndex<2)
     {
      aOpenPrice=0;
     }
   else
     {
      if(aExpiration>TimeCurrent() && aExpiration<TimeCurrent()+MinExpiration)
        {
         aExpiration=TimeCurrent()+MinExpiration;
        }
     }
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void CIntegerTradePanel::LinesSLTPShowInForm()
  {
   double TriggerPrice,OpenPrice,SLPrice,TPPrice;
   datetime Expiration;
   LinesSLTPGetValues(TriggerPrice,OpenPrice,SLPrice,TPPrice,Expiration);
   string sl="?";
   string tp="?";
   switch(gSelectedIndex)
     {
      case 0: // b
         fGetMarketInfo();
         OpenPrice=ND(miAsk);
         break;
      case 1: // s
         fGetMarketInfo();
         OpenPrice=ND(miBid);
         break;
     }
   if(gSelectedIndex%2==0)
     {
      if(OpenPrice!=0)
        {
         if(SLPrice!=0)
           {
            sl=IntegerToString((int)MathRound((OpenPrice-SLPrice)/_Point));
           }
         else
           {
            sl="0";
           }
         if(TPPrice!=0)
           {
            tp=IntegerToString((int)MathRound((TPPrice-OpenPrice)/_Point));
           }
         else
           {
            tp="0";
           }
        }
     }
   else
     {
      if(OpenPrice!=0)
        {
         if(SLPrice!=0)
           {
            sl=IntegerToString((int)MathRound((SLPrice-OpenPrice)/_Point));
           }
         else
           {
            sl="0";
           }
         if(TPPrice!=0)
           {
            tp=IntegerToString((int)MathRound((OpenPrice-TPPrice)/_Point));
           }
         else
           {
            tp="0";
           }
        }
     }
   ObjectSetString(0,NameSLValue,OBJPROP_TEXT,sl);
   ObjectSetString(0,NameTPValue,OBJPROP_TEXT,tp);
   ChartRedraw();
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void CIntegerTradePanel::fObjHLine(string   aObjName,
                                   double   aPrice,
                                   string   aText       =  "HLine",
                                   int      aWindow     =  0,
                                   color    aColor      =  Red,
                                   color    aWidth      =  1,
                                   color    aStyle      =  0,
                                   int      aChartID    =  0,
                                   bool     aBack       =  true,
                                   bool     aSelectable =  true,
                                   bool     aSelected   =  false,
                                   long     aTimeFrames =  OBJ_ALL_PERIODS
                                   )
  {
   bool exist=true;
   if(ObjectFind(aChartID,aObjName)!=aWindow)
     {
      exist=false;
     }
   ObjectCreate(aChartID,aObjName,OBJ_HLINE,aWindow,0,aPrice);
   ObjectSetInteger(aChartID,aObjName,OBJPROP_BACK,aBack);
   ObjectSetInteger(aChartID,aObjName,OBJPROP_COLOR,aColor);
   ObjectSetInteger(aChartID,aObjName,OBJPROP_SELECTABLE,aSelectable);
   if(!exist)ObjectSetInteger(aChartID,aObjName,OBJPROP_SELECTED,aSelected);
   ObjectSetInteger(aChartID,aObjName,OBJPROP_TIMEFRAMES,aTimeFrames);
   ObjectSetString(aChartID,aObjName,OBJPROP_TEXT,aText);
   ObjectSetInteger(aChartID,aObjName,OBJPROP_WIDTH,aWidth);
   ObjectSetInteger(aChartID,aObjName,OBJPROP_STYLE,aStyle);
   ObjectMove(aChartID,aObjName,0,0,aPrice);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void CIntegerTradePanel::fObjVLine(string   aObjName,
                                   datetime aTime,
                                   string   aText       =  "",
                                   int      aWindow     =  0,
                                   color    aColor      =  Red,
                                   color    aWidth      =  1,
                                   color    aStyle      =  0,
                                   int      aChartID    =  0,
                                   bool     aBack       =  true,
                                   bool     aSelectable =  true,
                                   bool     aSelected   =  false,
                                   long     aTimeFrames =  OBJ_ALL_PERIODS
                                   )
  {
   bool exist=true;
   if(ObjectFind(aChartID,aObjName)!=aWindow)
     {
      exist=false;
     }
   ObjectCreate(aChartID,aObjName,OBJ_VLINE,aWindow,aTime,0);
   ObjectSetInteger(aChartID,aObjName,OBJPROP_BACK,aBack);
   ObjectSetInteger(aChartID,aObjName,OBJPROP_COLOR,aColor);
   ObjectSetInteger(aChartID,aObjName,OBJPROP_SELECTABLE,aSelectable);
   if(!exist)ObjectSetInteger(aChartID,aObjName,OBJPROP_SELECTED,aSelected);
   ObjectSetInteger(aChartID,aObjName,OBJPROP_TIMEFRAMES,aTimeFrames);
   ObjectSetString(aChartID,aObjName,OBJPROP_TEXT,aText);
   ObjectSetInteger(aChartID,aObjName,OBJPROP_WIDTH,aWidth);
   ObjectSetInteger(aChartID,aObjName,OBJPROP_STYLE,aStyle);
   ObjectMove(aChartID,aObjName,0,aTime,0);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void CIntegerTradePanel::TradeSelected()
  {
   double sp,op,sl,tp;
   datetime ex;
   LinesSLTPGetValues(sp,op,sl,tp,ex);
   if(ex<=TimeCurrent())ex=0;
   int slippage=fSlippage();
   switch(gSelectedIndex)
     {
      case 0:
         fOpBuy(_Symbol,Lots,sl,tp,slippage,Magic,"","Open Buy ("+_Symbol+","+DS2(Lots)+")...",true);
         break;
      case 1:
         fOpSell(_Symbol,Lots,sl,tp,slippage,Magic,"","Open Sell ("+_Symbol+","+DS2(Lots)+")...",true);
         break;
      case 2:
         fSetBuyStop(_Symbol,Lots,op,sl,tp,ex,Magic,"","Set BuyStop ("+_Symbol+","+DS2(Lots)+")...",true);
         break;
      case 3:
         fSetSellStop(_Symbol,Lots,op,sl,tp,ex,Magic,"","Set SellStop ("+_Symbol+","+DS2(Lots)+")...",true);
         break;
      case 4:
         fSetBuyLimit(_Symbol,Lots,op,sl,tp,ex,Magic,"","Set BuyLimit ("+_Symbol+","+DS2(Lots)+")...",true);
         break;
      case 5:
         fSetSellLimit(_Symbol,Lots,op,sl,tp,ex,Magic,"","Set SellLimit ("+_Symbol+","+DS2(Lots)+")...",true);
         break;
      case 6:
         fSetBuyStopLimit(_Symbol,Lots,sp,op,sl,tp,ex,Magic,"","Set BuyStopLimit ("+_Symbol+","+DS2(Lots)+")...",true);
         break;
      case 7:
         fSetSellStopLimit(_Symbol,Lots,sp,op,sl,tp,ex,Magic,"","Set SellStopLimit ("+_Symbol+","+DS2(Lots)+")...",true);
         break;
     }
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
bool CIntegerTradePanel::fMEMode()
  {
   if(PositionSelect(_Symbol))
     {
      double slts,tpts;
      if(GlobalVariableCheck("ME_DIR_"+_Symbol))
        {
         fGetMarketInfo();
         switch(PositionGetInteger(POSITION_TYPE))
           {
            case POSITION_TYPE_BUY:
               if(GlobalVariableGet("ME_DIR_"+_Symbol)==1)
                 {
                  slts=PositionGetDouble(POSITION_SL);
                  tpts=PositionGetDouble(POSITION_SL);
                  if(GlobalVariableCheck("ME_SL_"+_Symbol))
                    {
                     slts=GlobalVariableGet("ME_SL_"+_Symbol);
                     if(slts>0)
                       {
                        double msl=miBid-_Point*(miMSL+1);
                        msl=ND(msl);
                        slts=MathMin(slts,msl);
                       }
                    }
                  if(GlobalVariableCheck("ME_TP_"+_Symbol))
                    {
                     tpts=GlobalVariableGet("ME_TP_"+_Symbol);
                     if(tpts>0)
                       {
                        double msl=miAsk+_Point*(miMSL+1);
                        msl=ND(msl);
                        tpts=MathMax(tpts,msl);
                       }
                    }
                  if(ND(PositionGetDouble(POSITION_SL)-slts)!=0 || ND(PositionGetDouble(POSITION_TP)-tpts)!=0)
                    {
                     if(fPosModify(_Symbol,slts,tpts,"Set SLTP...",true))
                       {
                        GlobalVariableDel("ME_DIR_"+_Symbol);
                        GlobalVariableDel("ME_TP_"+_Symbol);
                        GlobalVariableDel("ME_SL_"+_Symbol);
                       }
                    }
                 }
               break;
            case POSITION_TYPE_SELL:
               if(GlobalVariableGet("ME_DIR_"+_Symbol)==-1)
                 {
                  slts=PositionGetDouble(POSITION_SL);
                  tpts=PositionGetDouble(POSITION_SL);
                  if(GlobalVariableCheck("ME_SL_"+_Symbol))
                    {
                     slts=GlobalVariableGet("ME_SL_"+_Symbol);
                     if(slts>0)
                       {
                        double msl=miAsk+_Point*(miMSL+1);
                        msl=ND(msl);
                        slts=MathMax(slts,msl);
                       }
                    }
                  if(GlobalVariableCheck("ME_TP_"+_Symbol))
                    {
                     tpts=GlobalVariableGet("ME_TP_"+_Symbol);
                     if(tpts>0)
                       {
                        double msl=miBid-_Point*(miMSL+1);
                        msl=ND(msl);
                        tpts=MathMin(tpts,msl);
                       }
                    }
                  if(ND(PositionGetDouble(POSITION_SL)-slts)!=0 || ND(PositionGetDouble(POSITION_TP)-tpts)!=0)
                    {
                     if(fPosModify(_Symbol,slts,tpts,"Set SLTP...",true))
                       {
                        GlobalVariableDel("ME_DIR_"+_Symbol);
                        GlobalVariableDel("ME_TP_"+_Symbol);
                        GlobalVariableDel("ME_SL_"+_Symbol);
                       }
                    }
                 }
               break;

           }
        }
     }
   return(true);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
bool CIntegerTradePanel::fPosModify(string aSymbol,double aStopLoss,double aTakeProfit,string aMessage="",bool aSound=false)
  {
   request.symbol=aSymbol;
   request.action=TRADE_ACTION_SLTP;
   request.sl=aStopLoss;
   request.tp=aTakeProfit;
   if(aMessage!="")Print(aMessage);
   if(aSound)PlaySound("stops");
   OrderSend(request,result);
   if(result.retcode==TRADE_RETCODE_DONE)
     {
      Print("...ok");
      if(aSound)PlaySound("ok");
      return(1);
     }
   else
     {
      Print("...error "+IntegerToString(result.retcode)+" - "+fTradeRetCode(result.retcode));
      if(aSound)PlaySound("timeout");
      return(-1);
     }
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
bool CIntegerTradePanel::fOpBuy(string aSymbol,double aVolume=0.1,double aStopLoss=0,double aTakeProfit=0,int aSlippage=0,ulong aMagic=0,string aComment="",string aMessage="",bool aSound=false)
  {
   if(!HistorySelect(0,TimeCurrent()))return(false);
   int cnt=HistoryDealsTotal();
   ZeroMemory(request);

   request.symbol=aSymbol;
   request.action=TRADE_ACTION_DEAL;
   request.type=ORDER_TYPE_BUY;
   request.volume=aVolume;
   request.price=SymbolInfoDouble(aSymbol,SYMBOL_ASK);
   bool ie=(SymbolInfoInteger(aSymbol,SYMBOL_TRADE_EXEMODE)==SYMBOL_TRADE_EXECUTION_INSTANT);
//ie=false;
   if(ie)
     {
      request.sl=aStopLoss;
      request.tp=aTakeProfit;
     }
   else
     {
      request.sl=0;
      request.tp=0;
     }
   request.deviation=aSlippage;
   request.type_filling=ORDER_FILLING_FOK;
   request.comment=aComment;
   request.magic=aMagic;
   if(aMessage!="")Print(aMessage);
   if(aSound)PlaySound("expert");
   OrderSend(request,result);
   if(result.retcode==TRADE_RETCODE_DONE)
     {
      Print("...ok (#"+IntegerToString(result.order)+")");
      if(aSound)PlaySound("ok");
      if(!ie)
        {
         if(aStopLoss>0)GlobalVariableSet("ME_SL_"+aSymbol,aStopLoss);
         if(aTakeProfit>0)GlobalVariableSet("ME_TP_"+aSymbol,aTakeProfit);
         GlobalVariableSet("ME_DIR_"+aSymbol,1);
         GlobalVariableSet("ME_CNT_"+aSymbol,10);
         for(int i=0;i<100;i++)
           {
            if(HistorySelect(0,TimeCurrent()))
              {
               if(cnt!=HistoryDealsTotal())
                 {
                  break;
                 }
              }
            Sleep(100);
           }
         fMEMode();
        }
      return(true);
     }
   else
     {
      Print("...error "+IntegerToString(result.retcode)+" - "+fTradeRetCode(result.retcode));
      if(aSound)PlaySound("timeout");
      return(false);
     }
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
bool CIntegerTradePanel::fOpSell(string aSymbol,double aVolume=0.1,double aStopLoss=0,double aTakeProfit=0,int aSlippage=0,ulong aMagic=0,string aComment="",string aMessage="",bool aSound=false)
  {

   if(!HistorySelect(0,TimeCurrent()))return(false);
   int cnt=HistoryDealsTotal();
   ZeroMemory(request);

   request.symbol=aSymbol;
   request.action=TRADE_ACTION_DEAL;
   request.type=ORDER_TYPE_SELL;
   request.volume=aVolume;
   request.price=SymbolInfoDouble(aSymbol,SYMBOL_BID);
   bool ie=(SymbolInfoInteger(aSymbol,SYMBOL_TRADE_EXEMODE)==SYMBOL_TRADE_EXECUTION_INSTANT);
//ie=false;
   if(ie)
     {
      request.sl=aStopLoss;
      request.tp=aTakeProfit;
     }
   else
     {
      request.sl=0;
      request.tp=0;
     }
   request.deviation=aSlippage;
   request.type_filling=ORDER_FILLING_FOK;
   request.comment=aComment;
   request.magic=aMagic;
   if(aMessage!="")Print(aMessage);
   if(aSound)PlaySound("expert");
   OrderSend(request,result);
   if(result.retcode==TRADE_RETCODE_DONE)
     {
      Print("...ok (#"+IntegerToString(result.order)+")");
      if(aSound)PlaySound("ok");
      if(!ie)
        {
         if(aStopLoss>0)
           {
            GlobalVariableSet("ME_SL_"+aSymbol,aStopLoss);
           }
         if(aTakeProfit>0)
           {
            GlobalVariableSet("ME_TP_"+aSymbol,aTakeProfit);
           }
         GlobalVariableSet("ME_DIR_"+aSymbol,-1);
         GlobalVariableSet("ME_CNT_"+aSymbol,10);
         for(int i=0;i<100;i++)
           {
            if(HistorySelect(0,TimeCurrent()))
              {
               if(cnt!=HistoryDealsTotal())
                 {
                  break;
                 }
              }
            Sleep(100);
           }
         fMEMode();
        }
      return(true);
     }
   else
     {
      Print("...error "+IntegerToString(result.retcode)+" - "+fTradeRetCode(result.retcode));
      if(aSound)PlaySound("timeout");
      return(false);
     }
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
bool CIntegerTradePanel::fSetBuyLimit(string aSymbol,double aVolume,double aPrice,double aStopLoss=0,double aTakeProfit=0,datetime aExpiration=0,int aMagic=0,string aComment="",string aMessage="",bool aSound=false)
  {
   ZeroMemory(request);
   request.symbol=aSymbol;
   request.action=TRADE_ACTION_PENDING;
   request.volume=aVolume;
   request.price=aPrice;
   request.sl=aStopLoss;
   request.tp=aTakeProfit;
   request.type=ORDER_TYPE_BUY_LIMIT;
   request.type_filling=ORDER_FILLING_FOK;
   if(aExpiration==0)
     {
      request.type_time=ORDER_TIME_GTC;
      request.expiration=0;
     }
   else
     {
      request.type_time=ORDER_TIME_SPECIFIED;
      request.expiration=aExpiration;
     }
   request.comment=aComment;
   request.magic=aMagic;
   if(aMessage!="")Print(aMessage);
   if(aSound)PlaySound("expert");
   OrderSend(request,result);
   if(result.retcode==TRADE_RETCODE_DONE)
     {
      Print("...ok (#"+IntegerToString(result.order)+")");
      if(aSound)PlaySound("ok");
      return(true);
     }
   else
     {
      Print("...error "+IntegerToString(result.retcode)+" - "+fTradeRetCode(result.retcode));
      if(aSound)PlaySound("timeout");
      return(false);
     }
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
bool CIntegerTradePanel::fSetSellLimit(string aSymbol,double aVolume,double aPrice,double aStopLoss=0,double aTakeProfit=0,datetime aExpiration=0,int aMagic=0,string aComment="",string aMessage="",bool aSound=false)
  {
   ZeroMemory(request);
   request.symbol=aSymbol;
   request.action=TRADE_ACTION_PENDING;
   request.volume=aVolume;
   request.price=aPrice;
   request.sl=aStopLoss;
   request.tp=aTakeProfit;
   request.type=ORDER_TYPE_SELL_LIMIT;
   request.type_filling=ORDER_FILLING_FOK;
   if(aExpiration==0)
     {
      request.type_time=ORDER_TIME_GTC;
      request.expiration=0;
     }
   else
     {
      request.type_time=ORDER_TIME_SPECIFIED;
      request.expiration=aExpiration;
     }
   request.comment=aComment;
   request.magic=aMagic;
   if(aMessage!="")Print(aMessage);
   if(aSound)PlaySound("expert");
   OrderSend(request,result);
   if(result.retcode==TRADE_RETCODE_DONE)
     {
      Print("...ok (#"+IntegerToString(result.order)+")");
      if(aSound)PlaySound("ok");
      return(true);
     }
   else
     {
      Print("...error "+IntegerToString(result.retcode)+" - "+fTradeRetCode(result.retcode));
      if(aSound)PlaySound("timeout");
      return(false);
     }
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
bool CIntegerTradePanel::fSetBuyStop(string aSymbol,double aVolume,double aPrice,double aStopLoss=0,double aTakeProfit=0,datetime aExpiration=0,ulong aMagic=0,string aComment="",string aMessage="",bool aSound=false)
  {
   ZeroMemory(request);
   request.symbol=aSymbol;
   request.action=TRADE_ACTION_PENDING;
   request.volume=aVolume;
   request.price=aPrice;
   request.sl=aStopLoss;
   request.tp=aTakeProfit;
   request.deviation=0;
   request.type=ORDER_TYPE_BUY_STOP;
   request.type_filling=ORDER_FILLING_FOK;
   if(aExpiration==0)
     {
      request.type_time=ORDER_TIME_GTC;
      request.expiration=0;
     }
   else
     {
      request.type_time=ORDER_TIME_SPECIFIED;
      request.expiration=aExpiration;
     }
   request.comment=aComment;
   request.magic=aMagic;
   if(aMessage!="")Print(aMessage);
   if(aSound)PlaySound("expert");
   OrderSend(request,result);
   if(result.retcode==TRADE_RETCODE_DONE)
     {
      Print("...ok (#"+IntegerToString(result.order)+")");
      if(aSound)PlaySound("ok");
      return(true);
     }
   else
     {
      Print("...error "+IntegerToString(result.retcode)+" - "+fTradeRetCode(result.retcode));
      if(aSound)PlaySound("timeout");
      return(false);
     }
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
bool CIntegerTradePanel::fSetSellStop(string aSymbol,double aVolume,double aPrice,double aStopLoss=0,double aTakeProfit=0,datetime aExpiration=0,ulong aMagic=0,string aComment="",string aMessage="",bool aSound=false)
  {
   ZeroMemory(request);
   request.symbol=aSymbol;
   request.action=TRADE_ACTION_PENDING;
   request.volume=aVolume;
   request.price=aPrice;
   request.sl=aStopLoss;
   request.tp=aTakeProfit;
   request.deviation=0;
   request.type=ORDER_TYPE_SELL_STOP;
   request.type_filling=ORDER_FILLING_FOK;
   if(aExpiration==0)
     {
      request.type_time=ORDER_TIME_GTC;
      request.expiration=0;
     }
   else
     {
      request.type_time=ORDER_TIME_SPECIFIED;
      request.expiration=aExpiration;
     }
   request.comment=aComment;
   request.magic=aMagic;
   if(aMessage!="")Print(aMessage);
   if(aSound)PlaySound("expert");
   OrderSend(request,result);
   if(result.retcode==TRADE_RETCODE_DONE)
     {
      Print("...ok (#"+IntegerToString(result.order)+")");
      if(aSound)PlaySound("ok");
      return(true);
     }
   else
     {
      Print("...error "+IntegerToString(result.retcode)+" - "+fTradeRetCode(result.retcode));
      if(aSound)PlaySound("timeout");
      return(false);
     }
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
bool CIntegerTradePanel::fSetBuyStopLimit(string aSymbol,double aVolume,double aStopPrice,double aLimitPrice,double aStopLoss=0,double aTakeProfit=0,datetime aExpiration=0,int aMagic=0,string aComment="",string aMessage="",bool aSound=false)
  {
   ZeroMemory(request);
   request.symbol=aSymbol;
   request.action=TRADE_ACTION_PENDING;
   request.volume=aVolume;
   request.stoplimit=aLimitPrice;
   request.price=aStopPrice;
   request.sl=aStopLoss;
   request.tp=aTakeProfit;
   request.type=ORDER_TYPE_BUY_STOP_LIMIT;
   request.type_filling=ORDER_FILLING_FOK;
   if(aExpiration==0)
     {
      request.type_time=ORDER_TIME_GTC;
      request.expiration=0;
     }
   else
     {
      request.type_time=ORDER_TIME_SPECIFIED;
      request.expiration=aExpiration;
     }
   request.comment=aComment;
   request.magic=aMagic;
   if(aMessage!="")Print(aMessage);
   if(aSound)PlaySound("expert");
   OrderSend(request,result);
   if(result.retcode==TRADE_RETCODE_DONE)
     {
      Print("...ok (#"+IntegerToString(result.order)+")");
      if(aSound)PlaySound("ok");
      return(true);
     }
   else
     {
      Print("...error "+IntegerToString(result.retcode)+" - "+fTradeRetCode(result.retcode));
      if(aSound)PlaySound("timeout");
      return(false);
     }
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
bool CIntegerTradePanel::fSetSellStopLimit(string aSymbol,double aVolume,double aStopPrice,double aLimitPrice,double aStopLoss=0,double aTakeProfit=0,datetime aExpiration=0,int aMagic=0,string aComment="",string aMessage="",bool aSound=false)
  {
   ZeroMemory(request);
   request.symbol=aSymbol;
   request.action=TRADE_ACTION_PENDING;
   request.volume=aVolume;
   request.stoplimit=aLimitPrice;
   request.price=aStopPrice;
   request.sl=aStopLoss;
   request.tp=aTakeProfit;
   request.type=ORDER_TYPE_SELL_STOP_LIMIT;
   request.type_filling=ORDER_FILLING_FOK;
   if(aExpiration==0)
     {
      request.type_time=ORDER_TIME_GTC;
      request.expiration=0;
     }
   else
     {
      request.type_time=ORDER_TIME_SPECIFIED;
      request.expiration=aExpiration;
     }
   request.comment=aComment;
   request.magic=aMagic;
   if(aMessage!="")Print(aMessage);
   if(aSound)PlaySound("expert");
   OrderSend(request,result);
   if(result.retcode==TRADE_RETCODE_DONE)
     {
      Print("...ok (#"+IntegerToString(result.order)+")");
      if(aSound)PlaySound("ok");
      return(true);
     }
   else
     {
      Print("...error "+IntegerToString(result.retcode)+" - "+fTradeRetCode(result.retcode));
      if(aSound)PlaySound("timeout");
      return(false);
     }
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
string CIntegerTradePanel::fTradeRetCode(int aRetCode)
  {
   string tErrText="Неизвестная ошибка ("+IntegerToString(aRetCode)+")";
   switch(aRetCode)
     {
      case TRADE_RETCODE_REQUOTE:            return("REQUOTE");
      case TRADE_RETCODE_REJECT:             return("REJECT");
      case TRADE_RETCODE_CANCEL:             return("CANCEL");
      case TRADE_RETCODE_PLACED:             return("PLACED");
      case TRADE_RETCODE_DONE:               return("DONE");
      case TRADE_RETCODE_DONE_PARTIAL:       return("DONE PARTIAL");
      case TRADE_RETCODE_ERROR:              return("ERROR");
      case TRADE_RETCODE_TIMEOUT:            return("TIMEOUT");
      case TRADE_RETCODE_INVALID:            return("INVALID");
      case TRADE_RETCODE_INVALID_VOLUME:     return("INVALID VOLUME");
      case TRADE_RETCODE_INVALID_PRICE:      return("INVALID PRICE");
      case TRADE_RETCODE_INVALID_STOPS:      return("INVALID STOPS");
      case TRADE_RETCODE_TRADE_DISABLED:     return("TRADE DISABLED");
      case TRADE_RETCODE_MARKET_CLOSED:      return("MARKET CLOSED");
      case TRADE_RETCODE_NO_MONEY:           return("NO MONEY");
      case TRADE_RETCODE_PRICE_CHANGED:      return("PRICE CHANGED");
      case TRADE_RETCODE_PRICE_OFF:          return("PRICE OFF");
      case TRADE_RETCODE_INVALID_EXPIRATION: return("INVALID EXPIRATION");
      case TRADE_RETCODE_ORDER_CHANGED:      return("ORDER CHANGED");
      case TRADE_RETCODE_TOO_MANY_REQUESTS:  return("TOO MANY REQUESTS");
      case TRADE_RETCODE_NO_CHANGES:         return("NO CHANGES");
      case TRADE_RETCODE_SERVER_DISABLES_AT: return("SERVER DISABLES AT)");
      case TRADE_RETCODE_CLIENT_DISABLES_AT: return("CLIENT DISABLES AT)");
      case TRADE_RETCODE_LOCKED:             return("LOCKED");
      case TRADE_RETCODE_FROZEN:             return("FROZEN");
      case TRADE_RETCODE_INVALID_FILL:       return("INVALID FILL");
     }
   return("?");
  }
//+------------------------------------------------------------------+
