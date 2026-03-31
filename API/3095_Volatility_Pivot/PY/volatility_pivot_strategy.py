import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import AverageTrueRange, ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *
from indicator_extensions import *

class volatility_pivot_strategy(Strategy):
    """ATR-based trailing pivot stop: flip long/short when price crosses pivot line."""
    def __init__(self):
        super(volatility_pivot_strategy, self).__init__()
        self._atr_period = self.Param("AtrPeriod", 14).SetGreaterThanZero().SetDisplay("ATR Period", "ATR period", "Indicator")
        self._atr_mult = self.Param("AtrMultiplier", 5).SetGreaterThanZero().SetDisplay("ATR Multiplier", "Multiplier for pivot", "Indicator")
        self._ema_period = self.Param("EmaPeriod", 100).SetGreaterThanZero().SetDisplay("EMA Period", "EMA trend filter", "Indicator")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))).SetDisplay("Candle Type", "Timeframe", "General")

    @property
    def CandleType(self): return self._candle_type.Value
    @CandleType.setter
    def CandleType(self, value): self._candle_type.Value = value

    def OnReseted(self):
        super(volatility_pivot_strategy, self).OnReseted()
        self._pivot_stop = 0
        self._is_long = True
        self._initialized = False
        self._cooldown = 0

    def OnStarted2(self, time):
        super(volatility_pivot_strategy, self).OnStarted2(time)
        self._pivot_stop = 0
        self._is_long = True
        self._initialized = False
        self._cooldown = 0

        atr = AverageTrueRange()
        atr.Length = self._atr_period.Value
        ema = ExponentialMovingAverage()
        ema.Length = self._ema_period.Value

        sub = self.SubscribeCandles(self.CandleType)
        sub.Bind(atr, ema, self.OnProcess).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, sub)
            self.DrawIndicator(area, ema)
            self.DrawOwnTrades(area)

    def OnProcess(self, candle, atr_val, ema_val):
        if candle.State != CandleStates.Finished:
            return

        av = float(atr_val)
        ev = float(ema_val)
        close = float(candle.ClosePrice)
        mult = self._atr_mult.Value

        if av <= 0:
            return

        if self._cooldown > 0:
            self._cooldown -= 1
            return

        delta = av * mult

        if not self._initialized:
            self._pivot_stop = close - delta if close > ev else close + delta
            self._is_long = close > ev
            self._initialized = True
            return

        if self._is_long:
            new_stop = close - delta
            if new_stop > self._pivot_stop:
                self._pivot_stop = new_stop
            if close < self._pivot_stop:
                self._is_long = False
                self._pivot_stop = close + delta
                if self.Position > 0:
                    self.SellMarket()
                self.SellMarket()
                self._cooldown = 60
        else:
            new_stop = close + delta
            if new_stop < self._pivot_stop:
                self._pivot_stop = new_stop
            if close > self._pivot_stop:
                self._is_long = True
                self._pivot_stop = close - delta
                if self.Position < 0:
                    self.BuyMarket()
                self.BuyMarket()
                self._cooldown = 60

    def CreateClone(self):
        return volatility_pivot_strategy()
