//tao.wu. Create(2017-03-14)Modify(2017-04-18)..file:Test_ChartEvent.mq45 (mt45: same file for mql4 & mql5)
//=============================================================================================================
// WU Tao - http://forex.simula.fr/temp/
//=======================================================================================
// Test the various Chart Events.
// In this EA, I test the key event, mouse event, custom event.
// and I created a class to handle the objects.
// Finally, the OnTimer () function randomly generates the custom event.
// Press the "h" key to get help.
// To use the MouseMove event, press the 'm' key to activate the mode.
//=======================================================================================

#property copyright "WU Tao"
#property link      "http://forex.simula.fr/temp/"
#property version   "1.00"
#ifdef __MQL4__
#property strict
#endif

//--- input parameters
input int iLogLevel=0;   // LogLevel: 0: no log message, 1: a little, 2: more message

                         //========== prototype of function ================
bool Obj_Create();
bool Obj_Delete();
bool Obj_Move(int hSens,int vSens);
void Print_Info();
//
void MouseEventUse(bool bUseMouse);
int MouseMove(int aXX,int aYY,string sState);

//========= global variables =====================
int gChartNo=0;
int gSubWinNo=0;
string gsObj_Name_selected="";

//======== Custom event ===========================
#define cMyEvent_1 0
#define cMyEvent_2 1
#define cMyEvent_3_broadcast 2
//CHARTEVENT_CUSTOM= 1000 et CHARTEVENT_CUSTOM_LAST= 66534

//========== variable of MouseMoveEvent =======================
bool ggmMouseOn;   //Enable or Disable
int ggmLogLevel;   //DebugLevel
int ggmXX;         //X_Coordonne
int ggmYY;         //Y_Coordonne
int ggmStateMouse; //mouse button state
int ggmStateAct;   //mouse action state
uint ggmTimeTop;   //Start
//+------------------------------------------------------------------+
//| a sample of class to manager the button object                   |
//+------------------------------------------------------------------+
class CObjectMan //Object Manager
  {
public:
   string            m_name;
   color             m_color;
   int               m_step;
   //=====================================
   void              CObjectMan(string aName,color aColor=clrRed);
   void             ~CObjectMan();
   bool              f_Create();
   bool              f_Delete();
   bool              f_Move(int hSens,int vSens);
   void              f_print();
  }
gObjectMan1("Green button",clrGreen),gObjectMan2("Yellow button",clrChocolate);
//
//
//======================= initialization function ====================================
//
int OnInit()
  {
   if(iLogLevel)
      Print("CHARTEVENT_CUSTOM=",CHARTEVENT_CUSTOM,", CHARTEVENT_CUSTOM_LAST=",CHARTEVENT_CUSTOM_LAST); //(5000, 66534)
   Obj_Create();
   EventSetTimer(60);
   return(INIT_SUCCEEDED);
  }//int OnInit()
//
//
//
//================================== deinitialization function ==================================
//
void OnDeinit(const int reason)
  {
   Obj_Delete();
   EventKillTimer();
  }
//
//
//================================== Expert tick function ==================================
//
void OnTick()
  {
  }
//
//
//================================== Timer function ==================================
//
void OnTimer()
  {
   int ii=MathRand()%3;
   long lparam=0;
   double dparam= 0.0;
   string sparam= "by "+_Symbol+StringSubstr(EnumToString((ENUM_TIMEFRAMES)_Period), 6);

   if(ii==0)
      EventChartCustom(gChartNo,cMyEvent_1,lparam,dparam,sparam);
   if(ii==1)
      EventChartCustom(gChartNo,cMyEvent_2,lparam,dparam,sparam);
   if(ii==2)
     {
      BroadcastEvent(lparam,dparam,sparam);
     }
  }//void OnTimer()
//
//
//================================== ChartEvent function ==================================
//
//
//See exemple in Help for: OnChartEvent()
#define KEY_NUMPAD_5 12
#define KEY_LEFT 37
#define KEY_UP 38
#define KEY_RIGHT 39
#define KEY_DOWN 40
#define KEY_NUMLOCK_DOWN 98 //2 (De-Select Objet)
#define KEY_NUMLOCK_LEFT 100 //4
#define KEY_NUMLOCK_5 101 //5
#define KEY_NUMLOCK_RIGHT 102 //6
#define KEY_NUMLOCK_UP 104 //8 (Select Objet)
//
void OnChartEvent(const int id,const long &lparam,const double &dparam,const string &sparam)
  {
   bool bRet=false;

   if(id==CHARTEVENT_KEYDOWN)
     {
      //
      bRet=false;
      int vSens=0,hSens=0;
      switch((int)lparam)
        {
         case KEY_NUMLOCK_LEFT: //Print("The KEY_NUMLOCK_LEFT has been pressed");
            hSens=-1;   break;
         case KEY_LEFT: //Print("The KEY_LEFT has been pressed");
            hSens=-1;   break;
         case KEY_NUMLOCK_UP: //bRet= false;
            vSens=-1;   break;
         case KEY_UP: //Print("The KEY_UP has been pressed");
            vSens=-1;   break;
         case KEY_NUMLOCK_RIGHT: //Print("The KEY_NUMLOCK_RIGHT has been pressed");
            hSens=1;   break;
         case KEY_RIGHT: //Print("The KEY_RIGHT has been pressed");
            hSens=1;   break;
         case KEY_NUMLOCK_DOWN: //Print("The KEY_NUMLOCK_DOWN has been pressed (Objet de-select= ", bRet);
            vSens=1;   break;
         case KEY_DOWN: //Print("The KEY_DOWN has been pressed");
            vSens=1;   break;
         case KEY_NUMPAD_5: //Print("The KEY_NUMPAD_5 has been pressed");
            break;
         case KEY_NUMLOCK_5: //Print("The KEY_NUMLOCK_5 has been pressed");
            break;
         case 'H':            //Print("'h' has been pressed");
            Print("Help: use mouse click at an object to select it for moving");
            Print("Help: use direction key or numlock direction key to move the Object");
            Print("Help: use mouse to grag the object");
            Print("Help: Press 'm' to activate mouse move event");
            break;
         case 'I':            //Print("'i' has been pressed");
            Print_Info();
            break;
         case 'M':            //Print("'i' has been pressed");
            MouseEventUse(true);
            if(iLogLevel)
               Print("MouseMoveEvent is activated");
            break;
         default: //Print("Some not listed key has been pressed");
            break;
        }
      if(vSens!=0 || hSens!=0)
        {
         Obj_Move(hSens,vSens);
        }
      //	Print("lparam= ", lparam, ", dparam= ", dparam, ", sparam= ", sparam, ", 'd'= ", 'd');
      //	printf("lparam= %ld, dparam= %g, sparam= %s, %X, %X, %c", lparam, dparam, sparam, StringToInteger(sparam), 'd', 'd');
      //bRet= true;
      //Print("Objet= " , sparam, " is Clicked");
     }//	if(id==CHARTEVENT_KEYDOWN){
//
//
   if(id>=CHARTEVENT_CUSTOM && id<CHARTEVENT_CUSTOM_LAST)
     {
      int custom_id=id-CHARTEVENT_CUSTOM;
      if(iLogLevel)
         Print("Here Custom event: custom_id=",custom_id," ::::: lparam= ",lparam,", dparam= ",dparam,", sparam= ",sparam);
      if(custom_id==cMyEvent_1)
        {
         if(iLogLevel)
            Print("Custom Event #1");
        }
      if(custom_id==cMyEvent_2)
        {
         if(iLogLevel)
            Print("Custom Event #2");
        }
      if(custom_id==cMyEvent_3_broadcast)
        {
         if(iLogLevel)
            Print("Custom Event #3");
        }
     }

//
   bRet=false;
//
   if(id==CHARTEVENT_OBJECT_CHANGE)
     {
      bRet=true;
      if(iLogLevel)
         Print("Objet '",sparam,"' is Changed");
     }
   if(id==CHARTEVENT_OBJECT_DRAG)
     {
      bRet=true;
      if(iLogLevel)
         Print("Objet '",sparam,"' is Draged");
     }
   if(id==CHARTEVENT_OBJECT_DELETE)
     {
      bRet=true;
      if(iLogLevel)
         Print("Objet '",sparam,"' is Deleted");
     }
//
   if(id==CHARTEVENT_OBJECT_CLICK)
     {
      bRet=true;
      if(iLogLevel)
         Print("Objet '",sparam,"' is Clicked");
      if(sparam==gObjectMan1.m_name)
         gsObj_Name_selected=gObjectMan1.m_name;
      else if(sparam==gObjectMan2.m_name)
         gsObj_Name_selected=gObjectMan2.m_name;
      else
         gsObj_Name_selected="";
     }
   if(bRet==true)
     {
/*
		if(iOption==0)
			//Rect_Maj_Change();
		if(iOption==1)
			//Trend_Maj_Change();
		*/
      ChartRedraw();
     }
//
   if(id==CHARTEVENT_MOUSE_MOVE)
     { //See: CHART_EVENT_MOUSE_MOVE
      //Print("Mouse is moving ...");
      MouseMove((int)lparam,(int)dparam,sparam);
      //if(ggMouse.ggmStateAct==3){
      //	ret= 65;
      //}
     }
//
   if(id==CHARTEVENT_CLICK)
     {
      if(!IsMoved((int)lparam,(int)dparam))
        {
         datetime aTime;
         double aPrice;
         //MqlRates bRates[1]; //Give error, why ?
         MqlRates bRates[];
         int ret=0;
         aTime=ChartTimeOnDropped();
         ChartXYToTimePrice(gChartNo,(int)lparam,(int)dparam,gSubWinNo,aTime,aPrice);
         if(iLogLevel)
            Print("Mouse clicked at XX= ",lparam,", YY= ",(int)dparam,", Time= ",aTime,", Price= ",aPrice);
         if(iLogLevel>=2)
           {
            ret=CopyRates(_Symbol,_Period,aTime,1,bRates);
            Print("Bar: Open= ",DoubleToString(bRates[0].open,(int)SymbolInfoInteger(_Symbol,SYMBOL_DIGITS)),
                  ", Close= ",DoubleToString(bRates[0].close,(int)SymbolInfoInteger(_Symbol,SYMBOL_DIGITS)),
                  ", Time= ",bRates[0].time);
           }
         bRet=false;
         //MouseEventUse(false);
         //
         //ggMouse.MouseClick((int)lparam, (int)dparam);
         //if(gMouseEvent.ggmStateAct==1){
         //	ret= 87;
         //}
        }
     }
  }//void OnChartEvent(const int id,const long &lparam,const double &dparam,const string &sparam)
//

//+------------------------------------------------------------------+
//| Print the info about the two objects                             |
//+------------------------------------------------------------------+
void Print_Info()
  {
   gObjectMan1.f_print();
   gObjectMan2.f_print();
  }
//
//+------------------------------------------------------------------+
//| Create the two button objects (at random position                |
//+------------------------------------------------------------------+
bool Obj_Create()
  {
   gObjectMan1.f_Create();
   gObjectMan2.f_Create();
   gsObj_Name_selected=gObjectMan1.m_name;
//
   return true;
  }//bool Obj_Create()
//
//+------------------------------------------------------------------+
//| Delete the two objects                                           |
//+------------------------------------------------------------------+
bool Obj_Delete()
  {
   gObjectMan1.f_Delete();
   gObjectMan2.f_Delete();
//ChartRedraw(0);
   return true;
  }//bool Obj_Delete()
//
//+----------------------------------------------------------------------------------+
//| Move the selected object: hSens: horizontal direction, vSens: vertical direction |
//+----------------------------------------------------------------------------------+
bool Obj_Move(int hSens,int vSens)
  {
   if(gsObj_Name_selected==gObjectMan1.m_name)
      gObjectMan1.f_Move(hSens,vSens);
   if(gsObj_Name_selected==gObjectMan2.m_name)
      gObjectMan2.f_Move(hSens,vSens);
//
   return true;
  }//bool Obj_Move()
//
//+------------------------------------------------------------------+
//| Send the message of broardcast to opened graphiques              |
//+------------------------------------------------------------------+
void BroadcastEvent(long lparam,double dparam,string sparam)
  {
   long currChart=ChartFirst();   //the first Window
   int ii=0;

   while(ii<CHARTS_MAX)
     {
      EventChartCustom(currChart,cMyEvent_3_broadcast,lparam,dparam,sparam);
      currChart=ChartNext(currChart); // from precedent window to the next window
      if(currChart==-1) break;        // we are at the end of the list of windows
      ii++;                           // Increase the compter
     }
  }//void BroadcastEvent()
//+------------------------------------------------------------------+

//=========== an exemple of class: class CObjectMan =====================
//Object Manager
//
//+------------------------------------------------------------------+
//| Constructor (with label and color                                |
//+------------------------------------------------------------------+
void CObjectMan::CObjectMan(string aName,color aColor=clrRed)
  {
   m_name=aName;
   m_color=aColor;
   m_step=10;
  }
//
//+------------------------------------------------------------------+
//| destructor                                                       |
//+------------------------------------------------------------------+
void CObjectMan::~CObjectMan()
  {
   f_Delete();
  }
//
//+------------------------------------------------------------------+
//| Create the object                                                |
//+------------------------------------------------------------------+
bool CObjectMan::f_Create()
  {
//
   bool bRet;
   int XX,YY;
   int Large,Haut;
   color aCouleur=clrWhite;
   ENUM_OBJECT oType;

   XX= 180+MathRand()%300;
   YY= 90+MathRand()%200;
   Large=120;
   Haut=50;
//
   oType= OBJ_LABEL;
   oType= OBJ_BUTTON;
//
   bRet= ObjectCreate(0, m_name, oType, 0, 0, 0);
   bRet= ObjectSetInteger(0, m_name, OBJPROP_XDISTANCE, XX);
   bRet= ObjectSetInteger(0, m_name, OBJPROP_YDISTANCE, YY);
//
   bRet= ObjectSetInteger(0, m_name, OBJPROP_XSIZE, Large);
   bRet= ObjectSetInteger(0, m_name, OBJPROP_YSIZE, Haut);
//
   bRet= ObjectSetString(0, m_name, OBJPROP_TEXT, m_name);
   bRet= ObjectSetInteger(0, m_name, OBJPROP_FONTSIZE, 12);
   bRet=ObjectSetString(0,m_name,OBJPROP_FONT,"Times New Roman");      //"Arial"
   bRet= ObjectSetInteger(0, m_name, OBJPROP_COLOR, aCouleur);
//
   bRet=ObjectSetInteger(0,m_name,OBJPROP_BGCOLOR,m_color);
//bRet= ObjectSetInteger(0, m_name, OBJPROP_BORDER_TYPE, BORDER_SUNKEN);
   bRet= ObjectSetInteger(0, m_name, OBJPROP_BORDER_COLOR, clrAzure);
   bRet= ObjectSetInteger(0, m_name, OBJPROP_WIDTH, 4);

   bRet=ObjectSetInteger(0,m_name,OBJPROP_STATE,false);

   bRet= ObjectSetInteger(0, m_name, OBJPROP_ZORDER, 1);
   bRet= ObjectSetInteger(gChartNo, m_name, OBJPROP_SELECTABLE, true);
   bRet= ObjectSetInteger(gChartNo, m_name, OBJPROP_SELECTED, true);
   return true;
  }
//+------------------------------------------------------------------+
//| Delete the object                                                |
//+------------------------------------------------------------------+
bool CObjectMan::f_Delete()
  {
   if(ObjectFind(0,m_name)==0)
      ObjectDelete(0,m_name);
   return true;
  }
//+---------------------------------------------------------------------------+
//| Move the object: hSens (horizontal direction), vSens (vertical direction) |
//+---------------------------------------------------------------------------+
bool CObjectMan::f_Move(int hSens,int vSens)
  {
   int XX,YY,XX2,YY2;
   int Large,Haut;
   int winWidth,winHight;
   bool bRet=false;

   if(iLogLevel==2)
      Print("Obj_Move(",hSens,", ",vSens,")");

   if(ObjectFind(0,m_name)!=0)
      return false;
//
   if(hSens==0 && vSens==0)
      return false;
//
   XX2= XX= (int)ObjectGetInteger(0, m_name, OBJPROP_XDISTANCE);
   YY2= YY= (int)ObjectGetInteger(0, m_name, OBJPROP_YDISTANCE);
//
   Large=(int)ObjectGetInteger(0,m_name,OBJPROP_XSIZE);
   Haut=(int)ObjectGetInteger(0,m_name,OBJPROP_YSIZE);
//
   winWidth= (int)ChartGetInteger(0, CHART_WIDTH_IN_PIXELS);
   winHight= (int)ChartGetInteger(0, CHART_HEIGHT_IN_PIXELS);
//
   if(hSens>0) XX+= m_step;
   if(hSens<0) XX-= m_step;
   if(vSens>0) YY+= m_step;
   if(vSens<0) YY-= m_step;
   if(XX<0) XX= 0;
   if(YY<0) YY= 0;
   if(XX>winWidth-Large) XX=winWidth-Large;
   if(YY>winHight-Haut) YY=winHight-Haut;
//
   if(XX==XX2 && YY==YY2)
      return false;
//
   if(XX!=XX2)
     {
      bRet=ObjectSetInteger(0,m_name,OBJPROP_XDISTANCE,XX);
     }
   if(YY!=YY2)
     {
      bRet=ObjectSetInteger(0,m_name,OBJPROP_YDISTANCE,YY);
     }
   ChartRedraw(0);
   return true;
  }
//
//+---------------------------------------------------------------------------+
//| Print the object's info: color, position, name                            |
//+---------------------------------------------------------------------------+
void CObjectMan::f_print()
  {
   int XX= (int)ObjectGetInteger(0, m_name, OBJPROP_XDISTANCE);
   int YY= (int)ObjectGetInteger(0, m_name, OBJPROP_YDISTANCE);
   if(iLogLevel)
      Print("Object name=",m_name,", color=",m_color,", position: (",XX,", ",YY,")");
  }
//
//+---------------------------------------------------------------------------+
//| Active/Desactive the MOUSE_MOVE event mode                                |
//+---------------------------------------------------------------------------+
void MouseEventUse(bool bUseMouse)
  {
   ChartSetInteger(ChartID(),CHART_EVENT_MOUSE_MOVE,bUseMouse);
   ggmMouseOn=bUseMouse;
   if(bUseMouse)
     {
      ggmXX=ggmYY=0;
      ggmStateMouse=0;
      ggmStateAct=0;
      ggmTimeTop=0;
     }
  }//void MouseEventUse(bool bUseMouse)
//
//+---------------------------------------------------------------------------+
//| Handle of the Mouse move event : show the move info                       |
//+---------------------------------------------------------------------------+
int MouseMove(int aXX,int aYY,string sState)
  {
   int StateMouse=0;
   string lSS="";

   if(sState=="16"){ lSS="Wheel"; StateMouse=3; }
   if(sState=="1"){ lSS= "Left"; StateMouse= 1; }
   if(sState=="2"){ lSS= "Right"; StateMouse= 2; }
   if(sState=="0"){ lSS= "Non"; StateMouse= 0; }

//Print("Mouse is moving ... lSS= ",lSS);

   if(ggmStateMouse==0 && StateMouse!=0)
     {
      //Memorize
      ggmXX=aXX; ggmYY=aYY;
      ggmStateMouse=StateMouse;
      ggmStateAct=2; //Move Started
      ggmTimeTop=GetTickCount();
     }
   else if(ggmStateMouse!=0 && StateMouse==0)
     {
      if(IsMoved(aXX,aYY))
        {
         if(iLogLevel)
           {
            double pr1,pr2;
            datetime tm1,tm2;
            ChartXYToTimePrice(gChartNo,ggmXX,ggmYY,gSubWinNo,tm1,pr1);
            ChartXYToTimePrice(gChartNo,aXX,aYY,gSubWinNo,tm2,pr2);
            PrintFormat("==> Mouse move from (%d, %d) to (%d, %d) state=%s (%s)",ggmXX,ggmYY,aXX,aYY,sState,lSS);
            PrintFormat("==> from (tm=%s, pr=%G) to (tm=%s, pr=%G)",TimeToString(tm1),pr1,TimeToString(tm2),pr2);
           }
         //
         //CheckAction(1,aXX,aYY);
         ggmXX=aXX; ggmYY=aYY;
         ggmStateMouse=0;
         ggmStateAct=3; //Move to be traited
         ggmTimeTop=0;
         //optional
         //Press the key board 'm' for re-enable
         //TraitMouse(3, aXX, bRates[ii].time, aShift);
         MouseEventUse(false);
        }
      else
        {
         //MyInit(); //Cette mouvement n'est pas vraiment Move : donc annuler'
         ggmXX=ggmYY=0;
         ggmStateMouse=0;
         ggmStateAct=0;
         ggmTimeTop=0;
        }
     }
   else
     {
      //Ne fait rien
     }
//

   return 0;
  }//int MouseMove()
//
//+---------------------------------------------------------------------------+
//| Check if the mouse is monving                                             |
//+---------------------------------------------------------------------------+
bool IsMoved(int aXX,int aYY)
  {
   if(!ggmMouseOn) return false;
//
   if(ggmTimeTop==0 || ggmStateMouse==0)
      return false;
//
//Duration at least one seconde and deplace at least 3 points
//If not, it is considered as "MouseClick"
//
//PrintFormat("Mouse move from (%d, %d) to (%d, %d) state=%s (%s)", mXX, mYY, aXX, aYY, sState, lSS);
   if(ggmTimeTop+460<GetTickCount() && (MathAbs(aXX-ggmXX)>2 || MathAbs(aYY-ggmYY)>2))
     {
      return true;
     }
   return false;
  }
//+------------------------------------------------------------------+
//| Show the mouse moving info                                       |
//+------------------------------------------------------------------+
int CheckAction(int aOption,int aXX,int aYY)
  {
   if(iLogLevel)
      Print("Mouse has moved from (",ggmXX,", ",ggmYY,") to (",aXX,", ",aYY,")");
   return 0;
  }//int CMouseEvent::CheckAction(int aOption, int aXX, int aYY)

//+------------------------------------------------------------------+
