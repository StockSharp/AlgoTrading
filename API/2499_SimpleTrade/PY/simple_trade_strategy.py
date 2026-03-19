import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *
from indicator_extensions import *

class simple_trade_strategy(Strategy):
    """Compares current open with open 3 bars ago. Holds 1 bar with fixed SL."""
    def __init__(self):
        super(simple_trade_strategy, self).__init__()
        self._sl_pips = self.Param("StopLossPips", 120).SetGreaterThanZero().SetDisplay("Stop Loss (pips)", "Fixed SL distance", "Risk")
        self._candle_type = self.Param("CandleType", TimeSpan.FromHours(4).TimeFrame()).SetDisplay("Candle Type", "Primary candle source", "General")

    @property
    def CandleType(self): return self._candle_type.Value
    @CandleType.setter
    def CandleType(self, value): self._candle_type.Value = value

    def OnReseted(self):
        super(simple_trade_strategy, self).OnReseted()
        self._opens = []
        self._stop_price = None

    def OnStarted(self, time):
        super(simple_trade_strategy, self).OnStarted(time)
        self._opens = []
        self._stop_price = None
        self._pip_size = 1.0
        if self.Security is not None and self.Security.PriceStep is not None and self.Security.PriceStep > 0:
            self._pip_size = float(self.Security.PriceStep)

        sub = self.SubscribeCandles(self.CandleType)
        sub.Bind(self.OnProcess).Start()

    def OnProcess(self, candle):
        if candle.State != CandleStates.Finished:
            return

        # Close existing position first (hold only 1 bar)
        if self.Position > 0:
            if self._stop_price is not None and float(candle.LowPrice) <= self._stop_price:
                self.SellMarket()
            else:
                self.SellMarket()
            self._stop_price = None
            self._update_opens(float(candle.OpenPrice))
            return
        elif self.Position < 0:
            if self._stop_price is not None and float(candle.HighPrice) >= self._stop_price:
                self.BuyMarket()
            else:
                self.BuyMarket()
            self._stop_price = None
            self._update_opens(float(candle.OpenPrice))
            return

        self._update_opens(float(candle.OpenPrice))

        if len(self._opens) < 4:
            return

        open_current = self._opens[-1]
        open_minus3 = self._opens[-4]
        stop_offset = self._pip_size * self._sl_pips.Value

        if open_current > open_minus3:
            self.BuyMarket()
            self._stop_price = float(candle.OpenPrice) - stop_offset
        else:
            self.SellMarket()
            self._stop_price = float(candle.OpenPrice) + stop_offset

    def _update_opens(self, val):
        self._opens.append(val)
        if len(self._opens) > 5:
            self._opens.pop(0)

    def CreateClone(self):
        return simple_trade_strategy()
