import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *
from indicator_extensions import *

class vr_overturn_strategy(Strategy):
    """Martingale/anti-martingale reversal: alternates direction after losses with StartProtection SL/TP."""
    def __init__(self):
        super(vr_overturn_strategy, self).__init__()
        self._sl = self.Param("StopLossPips", 300).SetGreaterThanZero().SetDisplay("Stop Loss", "SL in pips", "Risk")
        self._tp = self.Param("TakeProfitPips", 900).SetGreaterThanZero().SetDisplay("Take Profit", "TP in pips", "Risk")
        self._candle_type = self.Param("CandleType", TimeSpan.FromHours(4).TimeFrame()).SetDisplay("Candle Type", "Timeframe", "General")

    @property
    def CandleType(self): return self._candle_type.Value
    @CandleType.setter
    def CandleType(self, value): self._candle_type.Value = value

    def OnReseted(self):
        super(vr_overturn_strategy, self).OnReseted()
        self._entered = False
        self._is_long = True

    def OnStarted2(self, time):
        super(vr_overturn_strategy, self).OnStarted2(time)
        self._entered = False
        self._is_long = True

        sub = self.SubscribeCandles(self.CandleType)
        sub.Bind(self.OnProcess).Start()

        sl = self._sl.Value
        tp = self._tp.Value
        if sl > 0 or tp > 0:
            self.StartProtection(self.CreateProtection(sl if sl > 0 else 0, tp if tp > 0 else 0))

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, sub)
            self.DrawOwnTrades(area)

    def OnProcess(self, candle):
        if candle.State != CandleStates.Finished:
            return

        if self.Position == 0 and not self._entered:
            # Initial entry
            self.BuyMarket()
            self._is_long = True
            self._entered = True
        elif self.Position == 0 and self._entered:
            # Re-enter in opposite direction after exit
            if self._is_long:
                self.SellMarket()
                self._is_long = False
            else:
                self.BuyMarket()
                self._is_long = True

    def CreateClone(self):
        return vr_overturn_strategy()
