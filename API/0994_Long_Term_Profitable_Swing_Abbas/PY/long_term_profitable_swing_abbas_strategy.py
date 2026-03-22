import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage, RelativeStrengthIndex, AverageTrueRange
from StockSharp.Algo.Strategies import Strategy


class long_term_profitable_swing_abbas_strategy(Strategy):
    def __init__(self):
        super(long_term_profitable_swing_abbas_strategy, self).__init__()
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(15))) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")
        self._fast_ema_length = self.Param("FastEmaLength", 16) \
            .SetGreaterThanZero() \
            .SetDisplay("Fast EMA", "Fast EMA length", "Indicators")
        self._slow_ema_length = self.Param("SlowEmaLength", 25) \
            .SetGreaterThanZero() \
            .SetDisplay("Slow EMA", "Slow EMA length", "Indicators")
        self._rsi_length = self.Param("RsiLength", 9) \
            .SetGreaterThanZero() \
            .SetDisplay("RSI Length", "RSI calculation length", "Indicators")
        self._atr_length = self.Param("AtrLength", 21) \
            .SetGreaterThanZero() \
            .SetDisplay("ATR Length", "ATR calculation length", "Indicators")
        self._rsi_threshold = self.Param("RsiThreshold", 50.0) \
            .SetDisplay("RSI Threshold", "RSI bullish threshold", "Indicators")
        self._atr_stop_mult = self.Param("AtrStopMult", 15.0) \
            .SetDisplay("ATR Stop Mult", "ATR stop loss multiplier", "Risk")
        self._atr_tp_mult = self.Param("AtrTpMult", 20.0) \
            .SetDisplay("ATR TP Mult", "ATR take profit multiplier", "Risk")
        self._prev_fast = 0.0
        self._prev_slow = 0.0

    @property
    def candle_type(self):
        return self._candle_type.Value

    @candle_type.setter
    def candle_type(self, value):
        self._candle_type.Value = value

    def OnReseted(self):
        super(long_term_profitable_swing_abbas_strategy, self).OnReseted()
        self._prev_fast = 0.0
        self._prev_slow = 0.0

    def OnStarted(self, time):
        super(long_term_profitable_swing_abbas_strategy, self).OnStarted(time)
        self._prev_fast = 0.0
        self._prev_slow = 0.0
        self._fast_ema = ExponentialMovingAverage()
        self._fast_ema.Length = self._fast_ema_length.Value
        self._slow_ema = ExponentialMovingAverage()
        self._slow_ema.Length = self._slow_ema_length.Value
        self._rsi = RelativeStrengthIndex()
        self._rsi.Length = self._rsi_length.Value
        self._atr = AverageTrueRange()
        self._atr.Length = self._atr_length.Value
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self._fast_ema, self._slow_ema, self._rsi, self._atr, self.OnProcess).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, self._fast_ema)
            self.DrawIndicator(area, self._slow_ema)
            self.DrawOwnTrades(area)

    def OnProcess(self, candle, fast, slow, rsi, atr):
        if candle.State != CandleStates.Finished:
            return
        if not self._fast_ema.IsFormed or not self._slow_ema.IsFormed or not self._rsi.IsFormed or not self._atr.IsFormed:
            return
        fv = float(fast)
        sv = float(slow)
        if self._prev_fast == 0.0 or self._prev_slow == 0.0:
            self._prev_fast = fv
            self._prev_slow = sv
            return
        cross_up = self._prev_fast <= self._prev_slow and fv > sv
        cross_down = self._prev_fast >= self._prev_slow and fv < sv
        self._prev_fast = fv
        self._prev_slow = sv
        if cross_up and self.Position <= 0:
            self.BuyMarket(self.Volume + abs(self.Position))
        elif cross_down and self.Position >= 0:
            self.SellMarket(self.Volume + abs(self.Position))

    def CreateClone(self):
        return long_term_profitable_swing_abbas_strategy()
