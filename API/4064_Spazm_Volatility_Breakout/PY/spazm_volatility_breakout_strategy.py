import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import AverageTrueRange
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *
from indicator_extensions import *

class spazm_volatility_breakout_strategy(Strategy):
    def __init__(self):
        super(spazm_volatility_breakout_strategy, self).__init__()
        self._atr_length = self.Param("AtrLength", 14).SetDisplay("ATR Length", "Period for ATR volatility", "Indicators")
        self._multiplier = self.Param("Multiplier", 2.0).SetDisplay("Multiplier", "ATR multiplier for breakout threshold", "Indicators")
        self._candle_type = self.Param("CandleType", TimeSpan.FromMinutes(30).TimeFrame()).SetDisplay("Candle Type", "Timeframe", "General")

    @property
    def CandleType(self): return self._candle_type.Value
    @CandleType.setter
    def CandleType(self, value): self._candle_type.Value = value

    def OnReseted(self):
        super(spazm_volatility_breakout_strategy, self).OnReseted()
        self._swing_high = 0
        self._swing_low = 999999999
        self._trend_up = True
        self._initialized = False

    def OnStarted(self, time):
        super(spazm_volatility_breakout_strategy, self).OnStarted(time)
        self._swing_high = 0
        self._swing_low = 999999999
        self._trend_up = True
        self._initialized = False

        atr = AverageTrueRange()
        atr.Length = self._atr_length.Value

        sub = self.SubscribeCandles(self.CandleType)
        sub.Bind(atr, self.OnProcess).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, sub)
            self.DrawIndicator(area, atr)
            self.DrawOwnTrades(area)

    def OnProcess(self, candle, atr_val):
        if candle.State != CandleStates.Finished:
            return

        if atr_val <= 0:
            return

        close = float(candle.ClosePrice)
        high = float(candle.HighPrice)
        low = float(candle.LowPrice)
        threshold = atr_val * self._multiplier.Value

        if not self._initialized:
            self._swing_high = high
            self._swing_low = low
            self._initialized = True
            return

        if self._trend_up:
            if high > self._swing_high:
                self._swing_high = high
            if close < self._swing_high - threshold:
                self._trend_up = False
                self._swing_low = low
                if self.Position > 0:
                    self.SellMarket()
                if self.Position == 0:
                    self.SellMarket()
        else:
            if low < self._swing_low:
                self._swing_low = low
            if close > self._swing_low + threshold:
                self._trend_up = True
                self._swing_high = high
                if self.Position < 0:
                    self.BuyMarket()
                if self.Position == 0:
                    self.BuyMarket()

    def CreateClone(self):
        return spazm_volatility_breakout_strategy()
