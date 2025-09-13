//+------------------------------------------------------------------+
//|                                          TrendlineCrossAlert.mq5 |
//|                                             Copyright 2018, Strx |
//|                               https://www.mql5.com/en/users/strx |
//+------------------------------------------------------------------+
#define   EAName      "TrendlineCrossAlert"
#define   EAVersion   "1.01"

#property copyright   "Copyright 2018, Strx"
#property link        "https://www.mql5.com/en/users/strx"
#property version     EAVersion
#property description "Horizontal and Trendlines cross detection on any pair/timeframe"
#property description "good for scalping on small timeframes (M1 to M15) and trend following on bigger ones."
#property description "It send notifications when price crosses trendlines and horizontal lines"
#property description "of specified color and when it happens, it change its color to CrossedColor input parameter"
#property description "to be sure not to send multiple notifications for same line"

#include <Arrays\ArrayString.mqh>
#include <Trade\SymbolInfo.mqh>
#include <ChartObjects\ChartObject.mqh>
#include <ChartObjects\ChartObjectsLines.mqh>

//--- External inputs
sinput color   MonitoringColor = clrYellow;   //MonitoringColor, EA only monitors this lines color
sinput color   CrossedColor = clrGreen;       //CrossedColor, EA changes crossed lines color to this value
sinput bool    EnableAlerts = true;
sinput bool    EnableNotifications=false;
sinput bool    EnableEmails=false;

//+------------------------------------------------------------------+
//| Trendlines and Horizontal lines crossing notification expert
//+------------------------------------------------------------------+
class CTrendlineCrossAlertExpert
  {
protected:
   CSymbolInfo       m_symbol;                     // symbol info object

   //--- Logic vars
   CArrayString      m_objectNames;

public:
                     CTrendlineCrossAlertExpert(void);
                    ~CTrendlineCrossAlertExpert(void);
   bool              Init(void);

   void              Deinit(void);
   bool              Processing(void);

protected:

   //--- Inline functions 
   string              FormatPrice(double price){ return StringFormat("%."+(string)m_symbol.Digits()+"f",price); };
   bool                IsNewBar(){ return iVolume(NULL,0,0)==1; };

   //--- Logic functions
   int               FindActiveObjects();
   bool              ActiveObject(string objName);
   bool              PriceCrossed(string objName);
   void              NotifyCrossing(string objName);

  };

//--- global expert
CTrendlineCrossAlertExpert ExtExpert;

//+------------------------------------------------------------------+
//| Constructor                                                      |
//+------------------------------------------------------------------+
CTrendlineCrossAlertExpert::CTrendlineCrossAlertExpert(void){}

//+------------------------------------------------------------------+
//| Destructor                                                       |
//+------------------------------------------------------------------+
CTrendlineCrossAlertExpert::~CTrendlineCrossAlertExpert(void){}
//+------------------------------------------------------------------+
//| Initialization and checking for input parameters                 |
//+------------------------------------------------------------------+
bool CTrendlineCrossAlertExpert::Init(void)
  {
//--- initialize common information
   m_symbol.Name(Symbol());                  // symbol   

   return(true);
  }
//+------------------------------------------------------------------+
//| main function returns true if any position processed             |
//+------------------------------------------------------------------+
bool CTrendlineCrossAlertExpert::Processing(void)
  {
   bool rv=false;
   int i;
   string objName;

   if(m_symbol.RefreshRates())
     {
      if(FindActiveObjects())
        {
         for(i=0; i<m_objectNames.Total(); i++)
           {
            objName=m_objectNames[i];
            if(PriceCrossed(objName))
              {
               ObjectSetInteger(0,objName,OBJPROP_COLOR,CrossedColor);
               NotifyCrossing(objName);
              }
           }
        }
     }

   Comment(EAName," v.",EAVersion," parameters: MonitoringColor=",MonitoringColor,", CrossedColor=",CrossedColor,", EnableAlerts=",EnableAlerts,", EnableNotifications=",EnableNotifications,", EnableEmails=",EnableEmails,"\n",
           "Monitoring ",m_objectNames.Total()," lines");

   return(rv);
  }
//+------------------------------------------------------------------+
//| Send Notifications function
//+------------------------------------------------------------------+
void CTrendlineCrossAlertExpert::NotifyCrossing(string objName)
  {
   string msg="Price crossed line ''"+objName+"' on "+m_symbol.Name();

   if(EnableAlerts) Alert(msg);
   if(EnableNotifications) SendNotification(msg);
   if(EnableEmails) SendMail(EAName+" Event",msg);

   Print(msg);
  }
//+------------------------------------------------------------------+
//| Returns true if objName has been crossed up or down by current price
//+------------------------------------------------------------------+
bool CTrendlineCrossAlertExpert::PriceCrossed(string objName)
  {
   ENUM_OBJECT objType;
   double objPrice=0.0,openPrice,curPrice;

   objType=(ENUM_OBJECT)ObjectGetInteger(0,objName,OBJPROP_TYPE);
   if(objType==OBJ_HLINE)
     {
      objPrice= ObjectGetDouble(0, objName, OBJPROP_PRICE);
        }else if(objType==OBJ_TREND){
      objPrice=ObjectGetValueByTime(0,objName,TimeCurrent(),0);
     }

   if(objPrice)
     {
      openPrice= iOpen(NULL,0,0);
      curPrice = iClose(NULL,0,0);

      return (openPrice<=objPrice && curPrice>objPrice) || (openPrice>=objPrice && curPrice<objPrice);
     }

   return false;
  }
//+------------------------------------------------------------------+
//| Returns true if object has to be monitored based on color and
//| previous crosses
//+------------------------------------------------------------------+
bool CTrendlineCrossAlertExpert::ActiveObject(string objName)
  {
   int objColor=(int)ObjectGetInteger(0,objName,OBJPROP_COLOR,0);
   return objColor == MonitoringColor;
  }
//+------------------------------------------------------------------+
//| Returns the list of object to be monitored
//+------------------------------------------------------------------+
int CTrendlineCrossAlertExpert::FindActiveObjects()
  {
   m_objectNames.Clear();

   int nHLines=ObjectsTotal(0,-1,OBJ_HLINE),
   nTrendLines=ObjectsTotal(0,-1,OBJ_TREND),
   i;

   string objName;

   for(i=0; i<nHLines; i++)
     {
      objName=ObjectName(0,i,0,OBJ_HLINE);
      if(ActiveObject(objName))
        {
         m_objectNames.Add(objName);
        }
     }
   for(i=0; i<nTrendLines; i++)
     {
      objName=ObjectName(0,i,0,OBJ_TREND);
      if(ActiveObject(objName))
        {
         m_objectNames.Add(objName);
        }
     }

   return m_objectNames.Total();
  }
//+------------------------------------------------------------------+
//| Expert initialization function                                   |
//+------------------------------------------------------------------+
int OnInit(void)
  {
   if(!ExtExpert.Init())
      return(INIT_FAILED);

   return(INIT_SUCCEEDED);
  }
//+------------------------------------------------------------------+
//| Expert new tick handling function                                |
//+------------------------------------------------------------------+
void OnTick(void)
  {
   ExtExpert.Processing();
  }
//+------------------------------------------------------------------+
