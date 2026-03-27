import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *
from indicator_extensions import *

class rrs_chaotic_strategy(Strategy):
    def __init__(self):
        super(rrs_chaotic_strategy, self).__init__()
        self._tp_points = self.Param("TakeProfitPoints", 50000).SetDisplay("Take Profit", "TP distance in price steps", "Risk")
        self._sl_points = self.Param("StopLossPoints", 50000).SetDisplay("Stop Loss", "SL distance in price steps", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))).SetDisplay("Candle Type", "Candle timeframe", "General")

    @property
    def CandleType(self): return self._candle_type.Value
    @CandleType.setter
    def CandleType(self, value): self._candle_type.Value = value

    def OnReseted(self):
        super(rrs_chaotic_strategy, self).OnReseted()
        self._trade_counter = 0
        self._entry_price = 0
        self._stop_price = None
        self._take_price = None

    def OnStarted(self, time):
        super(rrs_chaotic_strategy, self).OnStarted(time)
        self._trade_counter = 0
        self._entry_price = 0
        self._stop_price = None
        self._take_price = None
        self._step = 1.0
        if self.Security is not None and self.Security.PriceStep is not None and self.Security.PriceStep > 0:
            self._step = float(self.Security.PriceStep)

        sub = self.SubscribeCandles(self.CandleType)
        sub.Bind(self.OnProcess).Start()

    def OnProcess(self, candle):
        if candle.State != CandleStates.Finished:
            return

        close = candle.ClosePrice

        # Check SL/TP
        if self.Position > 0:
            if self._take_price is not None and candle.HighPrice >= self._take_price:
                self.SellMarket()
                self._stop_price = None
                self._take_price = None
                return
            if self._stop_price is not None and candle.LowPrice <= self._stop_price:
                self.SellMarket()
                self._stop_price = None
                self._take_price = None
                return
        elif self.Position < 0:
            if self._take_price is not None and candle.LowPrice <= self._take_price:
                self.BuyMarket()
                self._stop_price = None
                self._take_price = None
                return
            if self._stop_price is not None and candle.HighPrice >= self._stop_price:
                self.BuyMarket()
                self._stop_price = None
                self._take_price = None
                return

        if self.Position != 0:
            return

        sl_dist = self._sl_points.Value * self._step
        tp_dist = self._tp_points.Value * self._step

        if self._trade_counter % 2 == 0:
            self.BuyMarket()
            self._entry_price = close
            self._stop_price = close - sl_dist if self._sl_points.Value > 0 else None
            self._take_price = close + tp_dist if self._tp_points.Value > 0 else None
        else:
            self.SellMarket()
            self._entry_price = close
            self._stop_price = close + sl_dist if self._sl_points.Value > 0 else None
            self._take_price = close - tp_dist if self._tp_points.Value > 0 else None

        self._trade_counter += 1

    def CreateClone(self):
        return rrs_chaotic_strategy()
