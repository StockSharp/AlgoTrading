import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *
from indicator_extensions import *

class trailing_stop_activation_strategy(Strategy):
    """EMA direction change entries with trailing stop management."""
    def __init__(self):
        super(trailing_stop_activation_strategy, self).__init__()
        self._ema_period = self.Param("EmaPeriod", 14).SetGreaterThanZero().SetDisplay("EMA Period", "EMA period for entries", "Indicator")
        self._trailing_stop = self.Param("TrailingStop", 500.0).SetGreaterThanZero().SetDisplay("Trailing Stop", "Trailing stop distance", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))).SetDisplay("Candle Type", "Type of candles", "General")

    @property
    def CandleType(self): return self._candle_type.Value
    @CandleType.setter
    def CandleType(self, value): self._candle_type.Value = value

    def OnReseted(self):
        super(trailing_stop_activation_strategy, self).OnReseted()
        self._prev_ema = 0
        self._prev_prev_ema = 0
        self._count = 0
        self._entry_price = 0
        self._stop_price = 0

    def OnStarted(self, time):
        super(trailing_stop_activation_strategy, self).OnStarted(time)
        self._prev_ema = 0
        self._prev_prev_ema = 0
        self._count = 0
        self._entry_price = 0
        self._stop_price = 0

        ema = ExponentialMovingAverage()
        ema.Length = self._ema_period.Value

        sub = self.SubscribeCandles(self.CandleType)
        sub.Bind(ema, self.OnProcess).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, sub)
            self.DrawIndicator(area, ema)
            self.DrawOwnTrades(area)

    def OnProcess(self, candle, ema_val):
        if candle.State != CandleStates.Finished:
            return

        close = float(candle.ClosePrice)
        ema_val = float(ema_val)
        trail = self._trailing_stop.Value

        # Trailing stop check
        if self.Position > 0:
            new_trail = close - trail
            if new_trail > self._stop_price:
                self._stop_price = new_trail
            if float(candle.LowPrice) <= self._stop_price:
                self.SellMarket()
                self._entry_price = 0
                self._stop_price = 0
        elif self.Position < 0:
            new_trail = close + trail
            if self._stop_price == 0 or new_trail < self._stop_price:
                self._stop_price = new_trail
            if float(candle.HighPrice) >= self._stop_price:
                self.BuyMarket()
                self._entry_price = 0
                self._stop_price = 0

        self._count += 1
        if self._count < 3:
            self._prev_prev_ema = self._prev_ema
            self._prev_ema = ema_val
            return

        # Entry on EMA direction change
        turn_up = self._prev_ema < self._prev_prev_ema and ema_val > self._prev_ema
        turn_down = self._prev_ema > self._prev_prev_ema and ema_val < self._prev_ema

        if turn_up and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket()
            self.BuyMarket()
            self._entry_price = close
            self._stop_price = close - trail
        elif turn_down and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket()
            self.SellMarket()
            self._entry_price = close
            self._stop_price = close + trail

        self._prev_prev_ema = self._prev_ema
        self._prev_ema = ema_val

    def CreateClone(self):
        return trailing_stop_activation_strategy()
