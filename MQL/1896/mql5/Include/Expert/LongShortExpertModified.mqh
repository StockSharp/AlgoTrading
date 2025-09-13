//+------------------------------------------------------------------+
//|                                      LongShortExpertModified.mqh |
//|                                        Copyright 2013, jlwarrior |
//|                        https://login.mql5.com/en/users/jlwarrior |
//+------------------------------------------------------------------+

#include <Expert\Expert.mqh>
//+------------------------------------------------------------------+
//| enumeration to control whether long / short or both positions are|
//| allowed to be opened                                             |
//+------------------------------------------------------------------+
//--- 
enum ENUM_AVAILABLE_POSITIONS
  {
   LONG_POSITION,
   SHORT_POSITION,
   BOTH_POSITION
  };
//+------------------------------------------------------------------+
//| Class CLongShortExpertModified.                                  |
//| Purpose: Allows only long / short / both positions to be opened  |
//| Derives from class CExpert (modifies only Open / Reverse methods)|
//+------------------------------------------------------------------+
class CLongShortExpertModified : public CExpert
  {
protected:
   ENUM_AVAILABLE_POSITIONS m_positions;
public:
                     CLongShortExpertModified(void);
                    ~CLongShortExpertModified(void);
   virtual bool      CheckOpen(void);
   virtual bool      CheckReverse(void);
   void SetAvailablePositions(ENUM_AVAILABLE_POSITIONS newValue){m_positions=newValue;};
  };
//+------------------------------------------------------------------+
//| Constructor                                                      |
//+------------------------------------------------------------------+
CLongShortExpertModified ::CLongShortExpertModified(void) : m_positions(BOTH_POSITION)
  {
  }
//+------------------------------------------------------------------+
//| Destructor                                                       |
//+------------------------------------------------------------------+
CLongShortExpertModified ::~CLongShortExpertModified(void)
  {
  }
//+------------------------------------------------------------------+
//| Check open for allowed positions                                 |
//+------------------------------------------------------------------+
bool CLongShortExpertModified :: CheckOpen()
  {
   switch(m_positions)
     {
      case LONG_POSITION:
         return CheckOpenLong();         //check only new long positions
      case SHORT_POSITION:
         return CheckOpenShort();        //check only new short positions
      default:
         return CExpert::CheckOpen();    //default behaviour
     }
  }
//+------------------------------------------------------------------+
//| Check reverse only if both position types are allowed            |
//+------------------------------------------------------------------+
bool CLongShortExpertModified::CheckReverse()
  {
   switch(m_positions)
     {
      case LONG_POSITION:
      case SHORT_POSITION:
         return false;                    // no reversal is allowed
      default:
         return CExpert::CheckReverse(); //default behaviour
     }
  }
//+------------------------------------------------------------------+
