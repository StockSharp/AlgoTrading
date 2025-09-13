//+------------------------------------------------------------------+
//|  Test creating a Singleton                                       |
//+------------------------------------------------------------------+
#property copyright "MQL4 Singleton Example" 
#property link      ""
#property version   "1.00"
#property strict
//+----------------------------------------------------------------------------------+
//| Singleton from Linux tutorial http://www.yolinux.com/TUTORIALS/C++Singleton.html |
//+----------------------------------------------------------------------------------+
class CYoLinux
  {
private:
   static CYoLinux *m_yoLinux;           // Static pointer to the only instance of Exposure Manager
   int               m_tickCounter;      // A variable so we can have the object do something (Count)
   string            m_counterDisplay;   // A place to store a screen display object name; see it be created and destroyed
   CYoLinux()                            // Private Constructor to create Singleton object
     {
      m_tickCounter=0;                   // ensure initial value is zero
      m_counterDisplay="DisplayCounter"; // Store name for the Display Object
     };
   CYoLinux(CYoLinux const&){};          // Private Copy Constructor prevents copy of the object as part of Singleton pattern
   CYoLinux operator=(CYoLinux const&);  // Private Assignment Operator, prevents assignment as part of Singleton pattern
protected:
public:
   ~CYoLinux() // A Public Destructor for the CYoLinux class
     {
      int windowIndex=ObjectFind(m_counterDisplay); // Find which window the object is in (does it exist?)
      if(windowIndex>=0) // Window found with the object in it 
        {
         ObjectDelete(m_counterDisplay); //  Destroy the object, as this is the destructor
        }
     }
   static CYoLinux *GetInstance() // A Static Function that will provide a pointer to 'this'
     {
      if(!m_yoLinux) // Does the private m_yoLinux pointer point or not?
        {
         m_yoLinux=new CYoLinux;     // Create Class Object on Demand, put the pointer in the pivate, static pointer
        }
      return(m_yoLinux);               // Return a pointer to the one and only object.
     }
   int GetTicks() // Get (and increment) the value of what is being counted
     {
      m_tickCounter++;                 // New request, increment the counter
      return(m_tickCounter);
     }
   void DisplayObject(string displayText,int xPos,int yPos,color textColor) // Another piece of something for the CYoLinux to do
     {
      int windowIndex=ObjectFind(m_counterDisplay); // Find which window the object is in (does it exist?)
      if(windowIndex<0) // No window found with the object in it - Create the object
        {
         ObjectCreate(m_counterDisplay, OBJ_LABEL, 0, 0, 0);    // Create on window 0
         ObjectSet(m_counterDisplay, OBJPROP_CORNER, 0);        // upper left corner
         ObjectSet(m_counterDisplay, OBJPROP_XDISTANCE, xPos);  // justify at xPos from the right
         ObjectSet(m_counterDisplay, OBJPROP_YDISTANCE, yPos);  // place at yPos from the top 
        }
      //bool ObjectSetText( string name, string text, int font_size, string font=NULL, color text_color=CLR_NONE) 
      ObjectSetText(m_counterDisplay,displayText,12,"Times New Roman",textColor);  // display the text
     }
  };
CYoLinux *CYoLinux::m_yoLinux=NULL;  // Initialize Global Pointer;
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
int OnInit()
  {
   EventSetTimer(1);        // Start a Timer
   return(0);               // Return an int, zero is no error on initiation
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void OnDeinit(const int reason)
  {
//  delete YoLinux::yoLinux;     // Cannot access private class member
   delete CYoLinux::GetInstance();  // the Display object remains on the window without an explicit object delete
   EventKillTimer();                // Clean up the timer when the EA stops
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void OnTimer()
  {
//--- Count timer events
   int counter=CYoLinux::GetInstance().GetTicks();  // Get the value of the counter in the object
   string executionMsgText=StringConcatenate("Timer Counter = ",counter); // A string to display
   CYoLinux::GetInstance().DisplayObject(executionMsgText,20,75,clrRed);  // Display on the window
   WindowRedraw();
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void OnTick()
  {
/*
//--- Count Quotes
   int counter = CYoLinux::GetInstance().GetTicks();  // Get the value of the counter in the object
   string executionMsgText = StringConcatenate("Quote Counter = ",counter); // "->" not supported by the compiler
   CYoLinux::GetInstance().DisplayObject(executionMsgText,20,75,clrRed);  // Display on the window
*/
  }
//+------------------------------------------------------------------+
