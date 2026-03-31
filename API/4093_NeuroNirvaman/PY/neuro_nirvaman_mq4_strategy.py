import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import RelativeStrengthIndex, ExponentialMovingAverage, AverageTrueRange
from StockSharp.Algo.Strategies import Strategy

class neuro_nirvaman_mq4_strategy(Strategy):
    def __init__(self):
        super(neuro_nirvaman_mq4_strategy, self).__init__()
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))).SetDisplay("Candle Type", "Timeframe.", "General")
        self._rsi_length = self.Param("RsiLength", 14).SetDisplay("RSI Length", "RSI period.", "Indicators")
        self._fast_ema_length = self.Param("FastEmaLength", 10).SetDisplay("Fast EMA", "Fast EMA period.", "Indicators")
        self._slow_ema_length = self.Param("SlowEmaLength", 30).SetDisplay("Slow EMA", "Slow EMA period.", "Indicators")
        self._atr_length = self.Param("AtrLength", 14).SetDisplay("ATR Length", "ATR period.", "Indicators")

    @property
    def CandleType(self): return self._candle_type.Value
    @CandleType.setter
    def CandleType(self, value): self._candle_type.Value = value

    def OnReseted(self):
        super(neuro_nirvaman_mq4_strategy, self).OnReseted()
        self._prev_rsi = 0
        self._prev_fast = 0
        self._prev_slow = 0
        self._entry_price = 0

    def OnStarted2(self, time):
        super(neuro_nirvaman_mq4_strategy, self).OnStarted2(time)
        self._prev_rsi = 0
        self._prev_fast = 0
        self._prev_slow = 0
        self._entry_price = 0

        rsi = RelativeStrengthIndex()
        rsi.Length = self._rsi_length.Value
        fast = ExponentialMovingAverage()
        fast.Length = self._fast_ema_length.Value
        slow = ExponentialMovingAverage()
        slow.Length = self._slow_ema_length.Value
        atr = AverageTrueRange()
        atr.Length = self._atr_length.Value

        sub = self.SubscribeCandles(self.CandleType)
        sub.Bind(rsi, fast, slow, atr, self.OnProcess).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, sub)
            self.DrawIndicator(area, fast)
            self.DrawIndicator(area, slow)
            self.DrawOwnTrades(area)

    def OnProcess(self, candle, rsi_val, fast_val, slow_val, atr_val):
        if candle.State != CandleStates.Finished:
            return
        if self._prev_fast == 0 or self._prev_slow == 0 or self._prev_rsi == 0 or atr_val <= 0:
            self._prev_rsi = rsi_val
            self._prev_fast = fast_val
            self._prev_slow = slow_val
            return

        close = candle.ClosePrice
        rsi_signal = 1 if rsi_val > 50 else -1
        trend_signal = 1 if fast_val > slow_val else -1
        rsi_accel = 1 if (rsi_val - self._prev_rsi) > 0 else -1
        score = rsi_signal * 0.4 + trend_signal * 0.4 + rsi_accel * 0.2

        if self.Position > 0:
            if close <= self._entry_price - atr_val * 2 or close >= self._entry_price + atr_val * 3 or score < -0.5:
                self.SellMarket()
                self._entry_price = 0
        elif self.Position < 0:
            if close >= self._entry_price + atr_val * 2 or close <= self._entry_price - atr_val * 3 or score > 0.5:
                self.BuyMarket()
                self._entry_price = 0

        if self.Position == 0:
            if score > 0.5 and self._prev_rsi <= 50 and rsi_val > 50:
                self._entry_price = close
                self.BuyMarket()
            elif score < -0.5 and self._prev_rsi >= 50 and rsi_val < 50:
                self._entry_price = close
                self.SellMarket()

        self._prev_rsi = rsi_val
        self._prev_fast = fast_val
        self._prev_slow = slow_val

    def CreateClone(self):
        return neuro_nirvaman_mq4_strategy()
