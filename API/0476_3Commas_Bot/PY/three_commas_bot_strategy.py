import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage, AverageTrueRange
from StockSharp.Algo.Strategies import Strategy


class three_commas_bot_strategy(Strategy):
    def __init__(self):
        super(three_commas_bot_strategy, self).__init__()
        self._ma_length1 = self.Param("MaLength1", 50) \
            .SetDisplay("MA Length #1", "Fast moving average length", "MA Settings") \
            .SetGreaterThanZero()
        self._ma_length2 = self.Param("MaLength2", 100) \
            .SetDisplay("MA Length #2", "Slow moving average length", "MA Settings") \
            .SetGreaterThanZero()
        self._atr_length = self.Param("AtrLength", 14) \
            .SetDisplay("ATR length", "ATR calculation period", "Risk Management") \
            .SetGreaterThanZero()
        self._risk_m = self.Param("RiskM", 3.0) \
            .SetDisplay("Risk Adjustment", "ATR multiplier for stop", "Risk Management") \
            .SetGreaterThanZero()
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(30))) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")
        self._cooldown_bars = self.Param("CooldownBars", 15) \
            .SetDisplay("Cooldown Bars", "Bars between trades", "Risk")
        self._stop_price = 0.0
        self._entry_price = 0.0
        self._initialized = False
        self._was_fast_above_slow = False
        self._cooldown_remaining = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    @candle_type.setter
    def candle_type(self, value):
        self._candle_type.Value = value

    @property
    def cooldown_bars(self):
        return self._cooldown_bars.Value

    @cooldown_bars.setter
    def cooldown_bars(self, value):
        self._cooldown_bars.Value = value

    def OnReseted(self):
        super(three_commas_bot_strategy, self).OnReseted()
        self._stop_price = 0.0
        self._entry_price = 0.0
        self._initialized = False
        self._was_fast_above_slow = False
        self._cooldown_remaining = 0

    def OnStarted(self, time):
        super(three_commas_bot_strategy, self).OnStarted(time)
        fast_ma = ExponentialMovingAverage()
        fast_ma.Length = self._ma_length1.Value
        slow_ma = ExponentialMovingAverage()
        slow_ma.Length = self._ma_length2.Value
        atr = AverageTrueRange()
        atr.Length = self._atr_length.Value
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(fast_ma, slow_ma, atr, self.OnProcess).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, fast_ma)
            self.DrawIndicator(area, slow_ma)
            self.DrawOwnTrades(area)

    def OnProcess(self, candle, fast_value, slow_value, atr_value):
        if candle.State != CandleStates.Finished:
            return
        fast_v = float(fast_value)
        slow_v = float(slow_value)
        atr_v = float(atr_value)
        if not self._initialized:
            self._was_fast_above_slow = fast_v > slow_v
            self._initialized = True
            return
        if self.Position > 0 and self._stop_price > 0 and float(candle.LowPrice) <= self._stop_price:
            self.SellMarket()
            self._stop_price = 0.0
            self._cooldown_remaining = self.cooldown_bars
        elif self.Position < 0 and self._stop_price > 0 and float(candle.HighPrice) >= self._stop_price:
            self.BuyMarket()
            self._stop_price = 0.0
            self._cooldown_remaining = self.cooldown_bars
        is_fast_above_slow = fast_v > slow_v
        if self._cooldown_remaining > 0:
            self._cooldown_remaining -= 1
            self._was_fast_above_slow = is_fast_above_slow
            return
        if self._was_fast_above_slow != is_fast_above_slow:
            risk_m = float(self._risk_m.Value)
            if is_fast_above_slow and self.Position <= 0:
                if self.Position < 0:
                    self.BuyMarket()
                self.BuyMarket()
                self._entry_price = float(candle.ClosePrice)
                self._stop_price = float(candle.ClosePrice) - atr_v * risk_m
                self._cooldown_remaining = self.cooldown_bars
            elif not is_fast_above_slow and self.Position >= 0:
                if self.Position > 0:
                    self.SellMarket()
                self.SellMarket()
                self._entry_price = float(candle.ClosePrice)
                self._stop_price = float(candle.ClosePrice) + atr_v * risk_m
                self._cooldown_remaining = self.cooldown_bars
        self._was_fast_above_slow = is_fast_above_slow

    def CreateClone(self):
        return three_commas_bot_strategy()
