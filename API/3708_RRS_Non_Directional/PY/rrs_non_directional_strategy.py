import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *
from indicator_extensions import *

class rrs_non_directional_strategy(Strategy):
    def __init__(self):
        super(rrs_non_directional_strategy, self).__init__()
        self._sl_points = self.Param("StopLossPoints", 200).SetDisplay("Stop Loss (pts)", "SL distance", "Risk")
        self._tp_points = self.Param("TakeProfitPoints", 100).SetDisplay("Take Profit (pts)", "TP distance", "Risk")
        self._trailing_start = self.Param("TrailingStartPoints", 30).SetDisplay("Trailing Start", "Trailing activation", "Risk")
        self._trailing_gap = self.Param("TrailingGapPoints", 30).SetDisplay("Trailing Gap", "Trailing gap", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))).SetDisplay("Candle Type", "Candle timeframe", "General")

    @property
    def CandleType(self): return self._candle_type.Value
    @CandleType.setter
    def CandleType(self, value): self._candle_type.Value = value

    def OnReseted(self):
        super(rrs_non_directional_strategy, self).OnReseted()
        self._entry_price = 0
        self._stop_price = None
        self._take_price = None
        self._trailing_stop = None
        self._open_long_next = True

    def OnStarted(self, time):
        super(rrs_non_directional_strategy, self).OnStarted(time)
        self._entry_price = 0
        self._stop_price = None
        self._take_price = None
        self._trailing_stop = None
        self._open_long_next = True
        self._step = 0.0001
        if self.Security is not None and self.Security.PriceStep is not None and self.Security.PriceStep > 0:
            self._step = float(self.Security.PriceStep)

        sub = self.SubscribeCandles(self.CandleType)
        sub.Bind(self.OnProcess).Start()

    def OnProcess(self, candle):
        if candle.State != CandleStates.Finished:
            return

        close = float(candle.ClosePrice)

        # Manage position
        if self.Position > 0:
            if self._stop_price is not None and candle.LowPrice <= self._stop_price:
                self.SellMarket()
                self._reset()
                return
            if self._take_price is not None and candle.HighPrice >= self._take_price:
                self.SellMarket()
                self._reset()
                return
            # Trailing
            if self._trailing_start.Value > 0 and self._trailing_gap.Value > 0 and self._entry_price > 0:
                act = self._trailing_start.Value * self._step
                gap = self._trailing_gap.Value * self._step
                if close >= self._entry_price + act:
                    candidate = close - gap
                    if self._trailing_stop is None or candidate > self._trailing_stop:
                        self._trailing_stop = candidate
                        self._stop_price = candidate
                if self._trailing_stop is not None and close <= self._trailing_stop:
                    self.SellMarket()
                    self._reset()
                    return
        elif self.Position < 0:
            if self._stop_price is not None and candle.HighPrice >= self._stop_price:
                self.BuyMarket()
                self._reset()
                return
            if self._take_price is not None and candle.LowPrice <= self._take_price:
                self.BuyMarket()
                self._reset()
                return
            if self._trailing_start.Value > 0 and self._trailing_gap.Value > 0 and self._entry_price > 0:
                act = self._trailing_start.Value * self._step
                gap = self._trailing_gap.Value * self._step
                if close <= self._entry_price - act:
                    candidate = close + gap
                    if self._trailing_stop is None or candidate < self._trailing_stop:
                        self._trailing_stop = candidate
                        self._stop_price = candidate
                if self._trailing_stop is not None and close >= self._trailing_stop:
                    self.BuyMarket()
                    self._reset()
                    return

        if self.Position != 0:
            return

        sl_dist = self._sl_points.Value * self._step
        tp_dist = self._tp_points.Value * self._step

        if self._open_long_next:
            self.BuyMarket()
            self._entry_price = close
            self._stop_price = close - sl_dist if self._sl_points.Value > 0 else None
            self._take_price = close + tp_dist if self._tp_points.Value > 0 else None
        else:
            self.SellMarket()
            self._entry_price = close
            self._stop_price = close + sl_dist if self._sl_points.Value > 0 else None
            self._take_price = close - tp_dist if self._tp_points.Value > 0 else None

        self._open_long_next = not self._open_long_next

    def _reset(self):
        self._entry_price = 0
        self._stop_price = None
        self._take_price = None
        self._trailing_stop = None

    def CreateClone(self):
        return rrs_non_directional_strategy()
