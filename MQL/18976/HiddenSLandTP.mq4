//+------------------------------------------------------------------+
//|                                                HiddenSLandTP.mq4 |
//|                                        Copyright 2017, M Wilson. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+
#property copyright "Copyright 2017, M Wilson."
#property link      "https://www.mql5.com"
#property version   "1.00"
#property strict
//+------------------------------------------------------------------+
//| Class for storing trade data                                     |
//+------------------------------------------------------------------+
class C_Trade
  {
private:
   bool              DeleteLine(const string strName="HL_HSLTP_",bool boolReportError=True);
   bool              CreateHorizontalLine(const string name="HLine",double price=0,const color clr=clrRed,const ENUM_LINE_STYLE style=STYLE_SOLID,const int width=1,const bool back=false,const bool selection=true,const bool hidden=false,const long z_order=0);
public:
   //Trade Variables
   int               m_intTicket;
   int               m_intMagicNumber;
   string            m_strSymbol;
   int               m_intType;
   datetime          m_dtOpenTime;
   double            m_dblOpenPrice;
   double            m_dblStopLoss;
   double            m_dblTakeProfit;
   double            m_dblHiddenStopLoss;
   double            m_dblHiddenTakeProfit;
   bool              m_boolHasBeenClosed;
   int               m_intSelectAttempts;
   //Constructor/Desctructor
                     C_Trade(){m_boolHasBeenClosed=False;m_intSelectAttempts=0;};
                    ~C_Trade(){};
   //Public Functions
   bool              InitiateFromSelectedTrade();         //Populate class from selected trade data.
   bool              BreachHiddenStopLossOrTakeProfit();   //Look for breaches of the hidden stoploss or takeprofit.
   void              PlotSLandTP();                       //Draw stoploss and takeprofit as horizontal lines.
  };
//+------------------------------------------------------------------+
//+------------------------------------------------------------------+
//|  Initiate Trade from Selected Trade                              |
//+------------------------------------------------------------------+
bool C_Trade::InitiateFromSelectedTrade()
  {
//Order Select on an active trade must have been called before this function is run
//It populates the trade using the details of the selected trade.

//Only works on live buy/sell orders
   if(!(OrderType()==OP_BUY || OrderType()==OP_SELL)) return False;

//Replicate the selected trade.
   this.m_intTicket=OrderTicket();
   this.m_intMagicNumber=OrderMagicNumber();
   this.m_strSymbol=OrderSymbol();
   this.m_intType=OrderType();
   this.m_dtOpenTime=OrderOpenTime();
   this.m_dblOpenPrice=OrderOpenPrice();
   this.m_dblStopLoss=OrderStopLoss();
   this.m_dblTakeProfit=OrderTakeProfit();
//By default, set the hidden stoploss and takeprofit to the current stoploss and takeprofit.
//They need to be changed later on.
   this.m_dblHiddenStopLoss=this.m_dblStopLoss;
   this.m_dblHiddenTakeProfit=this.m_dblHiddenTakeProfit;

   return True;
  }
//+------------------------------------------------------------------+
//|  Check for breach of hidden stoploss or takeprofit               |
//+------------------------------------------------------------------+
bool C_Trade::BreachHiddenStopLossOrTakeProfit()
  {
   bool boolRet=False;

   RefreshRates();

   if(this.m_intType==OP_BUY)
     {  //Test buy trade to see if the bid has passed the hidden stoploss or takeprofit
      double dblBid=MarketInfo(this.m_strSymbol,MODE_BID);
      if(dblBid>this.m_dblHiddenTakeProfit || dblBid<this.m_dblHiddenStopLoss) boolRet=True;
     }
   else if(this.m_intType==OP_SELL)
     {  //Test sell trade to see if the ask has passed the hidden stoploss or takeprofit
      double dblAsk=MarketInfo(this.m_strSymbol,MODE_ASK);
      if(dblAsk<this.m_dblHiddenTakeProfit || dblAsk>this.m_dblHiddenStopLoss) boolRet=True;
     }

   return boolRet;
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void C_Trade::PlotSLandTP()
  {
//First define name of hidden stoploss and takeprofit
   string strSL = "HL_HSL_"+IntegerToString(this.m_intTicket);
   string strTP = "HL_HTP_"+IntegerToString(this.m_intTicket);

//Delete the lines
   this.DeleteLine(strSL);
   this.DeleteLine(strTP);

//Now add the lines.
   this.CreateHorizontalLine(strSL,this.m_dblHiddenStopLoss,clrRed,STYLE_SOLID,2,True);
   this.CreateHorizontalLine(strTP,this.m_dblHiddenTakeProfit,clrLightGreen,STYLE_SOLID,2,True);

//Refresh the chart
   ChartRedraw();
  }
//+------------------------------------------------------------------+
//|  Private Function to delete line                                 |
//+------------------------------------------------------------------+
bool C_Trade::DeleteLine(const string strName="HL_HSLTP_",bool boolReportError=True)
  {
//Delete's any kind of line, you just have to give it the right name
   ResetLastError();

   if(ObjectFind(0,strName)>=0)
     {
      if(!ObjectDelete(0,strName))
        {
         if(boolReportError) Print(__FUNCTION__,": failed to delete the line: ",strName," Error code = ",GetLastError());
         return(false);
        }
     }

   return(true);
  }
//+------------------------------------------------------------------+
//|  Private Function to create line                                 |
//+------------------------------------------------------------------+
bool C_Trade::CreateHorizontalLine(const string          name="HLine",// line name
                                   double                price=0,// line price
                                   const color           clr=clrRed,        // line color
                                   const ENUM_LINE_STYLE style=STYLE_SOLID, // line style
                                   const int             width=1,           // line width
                                   const bool            back=false,        // in the background
                                   const bool            selection=true,    // highlight to move
                                   const bool            hidden=false,// hidden in the object list
                                   const long            z_order=0) // priority for mouse click
  {

   ResetLastError();

   if(!ObjectCreate(0,name,OBJ_HLINE,0,0,price))
     {
      Print(__FUNCTION__,": failed to create a horizontal line! Error code = ",GetLastError());
      return(false);
     }

   ObjectSetInteger(0,name,OBJPROP_COLOR,clr);

   ObjectSetInteger(0,name,OBJPROP_STYLE,style);

   ObjectSetInteger(0,name,OBJPROP_WIDTH,width);

   ObjectSetInteger(0,name,OBJPROP_BACK,back);
//--- enable (true) or disable (false) the mode of moving the line by mouse
//--- when creating a graphical object using ObjectCreate function, the object cannot be
//--- highlighted and moved by default. Inside this method, selection parameter
//--- is true by default making it possible to highlight and move the object
   ObjectSetInteger(0,name,OBJPROP_SELECTABLE,selection);
   ObjectSetInteger(0,name,OBJPROP_SELECTED,selection);
//--- hide (true) or display (false) graphical object name in the object list
   ObjectSetInteger(0,name,OBJPROP_HIDDEN,hidden);
//--- set the priority for receiving the event of a mouse click in the chart
   ObjectSetInteger(0,name,OBJPROP_ZORDER,z_order);
//--- successful execution
   return(true);
  }
//+------------------------------------------------------------------+
//| Enum to specify if relative to open price or mid.                |
//+------------------------------------------------------------------+
enum RelativePrice
  {
   OpenPrice,
   Mid
  };

//+------------------------------------------------------------------+
//| Inputs to EA.                                                    |
//+------------------------------------------------------------------+
input int I_StopLoss_Points=50;           //Desired StopLoss in points.
input int I_TakeProfit_Points=50;         //Desired TakeProfit in points.
input RelativePrice I_Relative=OpenPrice; //Use OpenPrice or Mid when calculating StopLoss or TakeProfit.
input bool I_DrawLines=True;             //Draw Lines on Chart.
input int I_Slippage=5;                   //Slippage.

//+------------------------------------------------------------------+
//| Global Vaiables.                                                 |
//+------------------------------------------------------------------+
C_Trade *g_objTradeArray[];               //Global Array to store the hidden instruments.
bool g_boolPrintConculsion;               //Global Variable indicating if we print a comment when all trades have been closed.
//+------------------------------------------------------------------+
//| Expert initialization function                                   |
//+------------------------------------------------------------------+
int OnInit()
  {

//Blank the comment
   Comment("");

//Initiate variable telling us when to print out that all trades have been closed.
   g_boolPrintConculsion=True;

//Scan through the live trades and build an array containing the trades
   int intCount=OrdersTotal();
   for(int i=0;i<intCount;i++)
     {
      if(OrderSelect(i,SELECT_BY_POS,MODE_TRADES))
        {
         //Only process trades that are associated with the active charts symbol.
         if(OrderSymbol()==Symbol())
           {
            C_Trade *objTrade=new C_Trade();
            if(objTrade.InitiateFromSelectedTrade())
              {
               //Get the point for the symbol
               double dblPoint=MarketInfo(objTrade.m_strSymbol,MODE_POINT);
               int intDigits=(int)MarketInfo(objTrade.m_strSymbol,MODE_DIGITS);

               //Depending upon the input, calculate the reference price relative to either the open price of the trade,
               //or the current mid of the trades symbol.
               double dblRefPrice=objTrade.m_dblOpenPrice;
               if(I_Relative==Mid)
                 {
                  dblRefPrice=(MarketInfo(objTrade.m_strSymbol,MODE_BID)+MarketInfo(objTrade.m_strSymbol,MODE_ASK))/2;
                 }

               //We have a live trade that has been populated, now add the hidden stoploss and hidden take profit.
               if(objTrade.m_intType==OP_BUY)
                 {
                  objTrade.m_dblHiddenStopLoss=NormalizeDouble(dblRefPrice-(I_StopLoss_Points*dblPoint),intDigits);
                  objTrade.m_dblHiddenTakeProfit=NormalizeDouble(dblRefPrice+(I_TakeProfit_Points*dblPoint),intDigits);

                 }
               else if(objTrade.m_intType==OP_SELL)
                 {
                  objTrade.m_dblHiddenStopLoss=NormalizeDouble(dblRefPrice+(I_StopLoss_Points*dblPoint),intDigits);
                  objTrade.m_dblHiddenTakeProfit=NormalizeDouble(dblRefPrice-(I_TakeProfit_Points*dblPoint),intDigits);
                 }

               //Print out a record of the hidden stoploss and take profit.
               string strPrint=" Ticket: "+IntegerToString(OrderTicket())+" HiddenSL: "+DoubleToString(objTrade.m_dblHiddenStopLoss)+" HiddenTP: "+DoubleToString(objTrade.m_dblHiddenTakeProfit);
               Print(__FUNCTION__,strPrint);

               //Potentially draw the lines
               if(I_DrawLines) objTrade.PlotSLandTP();

               //Add the trade to the array.
               int intArray=ArraySize(g_objTradeArray);
               ArrayResize(g_objTradeArray,intArray+1);
               g_objTradeArray[intArray]=objTrade;
               objTrade=NULL;
              }
            else
              {
               delete objTrade;
              }
           }
        }
      else
        {
         Print(__FUNCTION__," Could not select trade by position");
        }
     }

   return(INIT_SUCCEEDED);
  }
//+------------------------------------------------------------------+
//| Expert deinitialization function                                 |
//+------------------------------------------------------------------+
void OnDeinit(const int reason)
  {
//Blank the comment
   Comment("");

//Delete the array of hidden trades.
   int intArray=ArraySize(g_objTradeArray);
   for(int i=0;i<intArray;i++)
     {
      if(g_objTradeArray[i]!=NULL)  delete g_objTradeArray[i];
     }
   ArrayFree(g_objTradeArray);

  }
//+------------------------------------------------------------------+
//| Expert tick function                                             |
//+------------------------------------------------------------------+
void OnTick()
  {
//Check the array for any breaches of the hidden stoploss.
   int intCount=ArraySize(g_objTradeArray);
   int intOpenTrades=0;
   for(int i=0;i<intCount;i++)
     {
      if(!g_objTradeArray[i].m_boolHasBeenClosed)
        {
         intOpenTrades++;
         if(g_objTradeArray[i].BreachHiddenStopLossOrTakeProfit())
           {
            //Because the array is not cleaned when a trade is closed, trades may exist in the array that are no longer live.
            //Close the trade only if it can be found successfully.
            if(OrderSelect(g_objTradeArray[i].m_intTicket,SELECT_BY_TICKET,MODE_TRADES))
              {

               //Get the appropraite close price based upon the order type.   
               double dblClosePrice=MarketInfo(OrderSymbol(),MODE_BID);
               if(OrderType()==OP_SELL) dblClosePrice=MarketInfo(OrderSymbol(),MODE_ASK);

               //Attempt to close the order.   This could be enhanced with some error checking, I have left this out
               //for now.
               bool boolRes=OrderClose(OrderTicket(),OrderLots(),dblClosePrice,I_Slippage,clrWhite);

               //If successful, mark the trade in the array as being complete.
               if(boolRes)
                 {
                  g_objTradeArray[i].m_boolHasBeenClosed=True;
                 }
               else
                 {
                  Print(__FUNCTION__," Error Closing Ticket: "+IntegerToString(g_objTradeArray[i].m_intTicket)+" Error: ",GetLastError());
                 }
              }
            else
              {
               //If we cannot select the trade, then see if it exists in the history
               if(OrderSelect(g_objTradeArray[i].m_intTicket,SELECT_BY_TICKET,MODE_HISTORY))
                 {  //We have found the ticket in the history, assume the ticket has been closed by another method.
                  g_objTradeArray[i].m_boolHasBeenClosed=True;
                 }
               else
                 {
                  //Could not select the trade, try up to 3 times, and then assume it has been closed
                  g_objTradeArray[i].m_intSelectAttempts++;
                  if(g_objTradeArray[i].m_intSelectAttempts>3)
                    {
                     Print(__FUNCTION__," Could not select trade, assume it has been closed: "+IntegerToString(g_objTradeArray[i].m_intTicket));
                     g_objTradeArray[i].m_boolHasBeenClosed=True;
                    }
                 }
              }
           }
        }
     }

//Add a comment indicating when the EA has closed all of the trades.
   if(intOpenTrades<1 && intCount>0 && g_boolPrintConculsion)
     {
      Comment("TASK COMPLETE - ALL INITIAL TRADES HAVE BEEN CLOSED");
      Print(__FUNCTION__," TASK COMPLETE - ALL INITIAL TRADES HAVE BEEN CLOSED");
      g_boolPrintConculsion=False;
     }
  }
//+------------------------------------------------------------------+
