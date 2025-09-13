//+------------------------------------------------------------------+
//|                       _HPCS_PosNegDIsCrossOver_Mt4_EA_V01_WE.mq4 |
//|                        Copyright 2021, MetaQuotes Software Corp. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+
#property copyright "Copyright 2021, MetaQuotes Software Corp."
#property link      "https://www.mql5.com"
#property version   "1.00"
#property strict
#property script_show_inputs
//+------------------------------------------------------------------+
//| Expert initialization function                                   |
//+------------------------------------------------------------------+
input string is_start = "HH:MM" ;
input string is_stop = "HH:MM" ;

input double id_takeprofit = 10;  // TakeProfit in Pips
input double id_stoploss = 10;    // Stop loss in Pips
input int ii_lots = 1;       // Lots
input int ii_slipage = 10;  // Slipage
input int ii_magicnumber = 1212; // Magic Number
input int ii_martingleMultiplier = 2;
input int ii_martigleCyclelimit = 5;

input int ii_Period = 14;

datetime gdt_TimeCurrent = Time[1];

bool gb_MartigleCycle = false;
int gi_Ticket = -1;
int gi_CurrentMarCycle = 0;
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
int OnInit()
  {
//---

//---
   return(INIT_SUCCEEDED);
  }
//+------------------------------------------------------------------+
//| Expert deinitialization function                                 |
//+------------------------------------------------------------------+
void OnDeinit(const int reason)
  {
//---

  }
//+------------------------------------------------------------------+
//| Expert tick function                                             |
//+------------------------------------------------------------------+
void OnTick()
  {
//---

   datetime ldt_StartTime = StringToTime(is_start);
   datetime ldt_StopTime = StringToTime(is_stop);

   double ld_BuySignal = iCustom(_Symbol,PERIOD_CURRENT,"_HPCS_PosNegDIsCrossOver_MT4_Indi_V01_WE",ii_Period,0,0);
   double ld_SellSignal = iCustom(_Symbol,PERIOD_CURRENT,"_HPCS_PosNegDIsCrossOver_MT4_Indi_V01_WE",ii_Period,1,0);

   if(TimeCurrent()>ldt_StartTime && TimeCurrent()<ldt_StopTime)
     {
      if(ld_BuySignal != EMPTY_VALUE && !gb_MartigleCycle)
        {
         if(gdt_TimeCurrent != Time[0])
           {
            gi_Ticket = PlaceMarketOrder_Generic(OP_BUY,ii_lots,NULL,id_stoploss,id_takeprofit,ii_slipage,ii_magicnumber);
            if(gi_Ticket < 0)
              {
               Print("Order Not Generated",GetLastError());
              }
            else
              {
               gb_MartigleCycle = true;
               gi_CurrentMarCycle = 1;
              }
            gdt_TimeCurrent = Time[0];

           }
        }

      if(ld_SellSignal != EMPTY_VALUE && !gb_MartigleCycle)
        {
         if(gdt_TimeCurrent != Time[0])
           {
            gi_Ticket = PlaceMarketOrder_Generic(OP_SELL,ii_lots,NULL,id_stoploss,id_takeprofit,ii_slipage,ii_magicnumber);
            if(gi_Ticket < 0)
              {
               Print("Order Not Generated",GetLastError());
              }
            else
              {
               gb_MartigleCycle = true;
               gi_CurrentMarCycle =1;
              }
            gdt_TimeCurrent = Time[0];

           }
        }

      if(gb_MartigleCycle)
        {
         if(OrderSelect(gi_Ticket,SELECT_BY_TICKET))
           {
            if(OrderCloseTime() != 0)
              {
               if(OrderProfit() >0)
                 {
                  gb_MartigleCycle = false;
                 }
               if(OrderProfit() <0)
                 {
                ENUM_ORDER_TYPE le_Ordertype = OP_BUY;
                  if(OrderType() == 0)
                    le_Ordertype = OP_BUY;

                  if(OrderType() == 1)
                     le_Ordertype = OP_SELL;

                  gi_Ticket = PlaceMarketOrder_Generic(le_Ordertype,ii_martingleMultiplier*OrderLots(),Symbol(),id_stoploss,id_takeprofit,ii_slipage,ii_magicnumber);
                  if(gi_Ticket <0)
                     Print("Order Send Fail! ",GetLastError());
                  else
                    {
                     gi_CurrentMarCycle++;
                     if(gi_CurrentMarCycle >= ii_martigleCyclelimit)
                        gb_MartigleCycle = false;
                    }
                 }
              }
           }



        }








     }


  }
//+------------------------------------------------------------------+
int PlaceMarketOrder_Generic(ENUM_ORDER_TYPE _OP,
                             double          _Lots,
                             string          _namePair       = NULL,
                             double          _SLinPips       = 0,
                             double          _TPinPips       = 0,
                             int             _SlippageInPips = 10,
                             int             _MagicNumber    = 0,
                             string          _Comment        = "MO_HPCS",
                             bool            _flagDEBUGMsg   = false)
  {
//
   int rtrn_Ticket = -1,
       __Digits    = (int)MarketInfo(_namePair, MODE_DIGITS);
   double _priceOpen = 0,
          _priceSL   = 0,
          _priceTP   = 0,
          _priceAsk  = MarketInfo(_namePair, MODE_ASK),
          _priceBid  = MarketInfo(_namePair, MODE_BID),
          _Points    = MarketInfo(_namePair, MODE_POINT),
          _StopLevel = MarketInfo(_namePair, MODE_STOPLEVEL);
   color _colorArrow = clrBlack;
   int _factor  = 1;
   if(Digits == 5 || Digits == 3)
     {  _factor = 10; }

//

   if(_OP > 1)
     {
      Alert("Wrong Market Order type");
     }
   else
     {
      //
      if(_OP == OP_BUY)
        {
         _priceOpen  = _priceAsk;
         _colorArrow = clrBlue;
        }
      else
         if(_OP == OP_SELL)
           {
            _priceOpen  = _priceBid;
            _colorArrow = clrRed;
           }//



      if(_SLinPips != 0)
        {
         if(_OP == OP_BUY)
           {
            _priceSL = _priceAsk - (_SLinPips * _Points * _factor);
            if((_priceBid - _priceSL) >= (_StopLevel * _Points))
              {
               // Refer: book.mql4.com/appendix/limits
              }
            else
              {
               _priceSL = _priceBid - (_StopLevel * _Points);
              }
           }
         else
            if(_OP == OP_SELL)
              {
               _priceSL = _priceBid + (_SLinPips * _Points * _factor);
               if((_priceSL - _priceAsk) >= (_StopLevel * _Points))
                 {
                  // Refer: book.mql4.com/appendix/limits
                 }
               else
                 {
                  _priceSL = _priceAsk + (_StopLevel * _Points);
                 }
              }
        }
      if(_TPinPips != 0)
        {
         if(_OP == OP_BUY)
           {
            _priceTP = _priceAsk + (_TPinPips * _Points * _factor);
            if((_priceTP - _priceBid) >= (_StopLevel * _Points))
              {
               // Refer: book.mql4.com/appendix/limits
              }
            else
              {
               _priceTP = _priceBid + (_StopLevel * _Points);
              }
           }
         else
            if(_OP == OP_SELL)
              {
               _priceTP = _priceBid - (_TPinPips * _Points * _factor);
               if((_priceAsk - _priceTP) >= (_StopLevel * _Points))
                 {
                  // Refer: book.mql4.com/appendix/limits
                 }
               else
                 {
                  _priceTP = _priceAsk - (_StopLevel * _Points);
                 }
              }
        }

      // normalize all price values to digits
      _priceOpen = NormalizeDouble(_priceOpen, __Digits);
      _priceSL   = NormalizeDouble(_priceSL, __Digits);
      _priceTP   = NormalizeDouble(_priceTP, __Digits);

      // place market order
      rtrn_Ticket = OrderSend(_namePair, _OP, _Lots, _priceOpen, _SlippageInPips, _priceSL, _priceTP, _Comment, _MagicNumber, 0, _colorArrow);
      /* if( _flagDEBUGMsg == true ) {
         Print( "[DE-BUG] " + TimeToString( TimeCurrent(), TIME_DATE|TIME_SECONDS ) + " Ticket = " + IntegerToString( rtrn_Ticket ) );
      }
      if(rtrn_Ticket == -1) {
         int _Error = StringToInteger( Error_Description() ) ;
         if( _flagDEBUGMsg == true ) {
            Print( "Order Place Failed:", _Error );
         }
         //
         if( ( _Error == 129 ) || ( _Error == 135 ) || ( _Error == 136 ) || ( _Error == 138 ) || ( _Error == 146 ) ) {
         // Overcomable errors: 129(invalid price), 135(price changed), 136(no-quotes), 138(re-quotes), 146(busy trade subsystem)
            //if( _Error == 129 ) Alert("Invalid price. Retrying..");
            RefreshRates();                     // Update data
            rtrn_Ticket = PlaceMarketOrder_Generic( _OP, _Lots, _namePair,  _priceSL, _priceTP, _SlippageInPips, _MagicNumber, _Comment, _flagDEBUGMsg );
         }
       */
     }
//
   return(rtrn_Ticket);
  }
//+------------------------------------------------------------------+
