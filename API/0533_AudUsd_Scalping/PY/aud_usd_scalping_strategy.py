import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage, RelativeStrengthIndex
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *

class aud_usd_scalping_strategy(Strategy):
    """
    Scalping strategy using EMA crossover with RSI filter.
    Buys when fast EMA crosses above slow EMA and RSI exits oversold.
    Sells when fast EMA crosses below slow EMA and RSI exits overbought.
    """

    def __init__(self):
        super(aud_usd_scalping_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", tf(1)) \
            .SetDisplay("Candle Type", "Type of candles", "General")
        self._ema_short = self.Param("EmaShort", 13) \
            .SetGreaterThanZero() \
            .SetDisplay("Short EMA", "Fast EMA period", "Indicators")
        self._ema_long = self.Param("EmaLong", 26) \
            .SetGreaterThanZero() \
            .SetDisplay("Long EMA", "Slow EMA period", "Indicators")
        self._rsi_period = self.Param("RsiPeriod", 14) \
            .SetGreaterThanZero() \
            .SetDisplay("RSI Period", "RSI calculation period", "Indicators")
        self._cooldown_bars = self.Param("CooldownBars", 350) \
            .SetDisplay("Cooldown Bars", "Bars between trades", "Trading")

        self._prev_fast = 0.0
        self._prev_slow = 0.0
        self._bar_index = 0
        self._last_trade_bar = 0

    @property
    def CandleType(self): return self._candle_type.Value
    @CandleType.setter
    def CandleType(self, v): self._candle_type.Value = v
    @property
    def EmaShort(self): return self._ema_short.Value
    @EmaShort.setter
    def EmaShort(self, v): self._ema_short.Value = v
    @property
    def EmaLong(self): return self._ema_long.Value
    @EmaLong.setter
    def EmaLong(self, v): self._ema_long.Value = v
    @property
    def RsiPeriod(self): return self._rsi_period.Value
    @RsiPeriod.setter
    def RsiPeriod(self, v): self._rsi_period.Value = v
    @property
    def CooldownBars(self): return self._cooldown_bars.Value
    @CooldownBars.setter
    def CooldownBars(self, v): self._cooldown_bars.Value = v

    def OnReseted(self):
        super(aud_usd_scalping_strategy, self).OnReseted()
        self._prev_fast = 0.0
        self._prev_slow = 0.0
        self._bar_index = 0
        self._last_trade_bar = 0

    def OnStarted2(self, time):
        super(aud_usd_scalping_strategy, self).OnStarted2(time)

        ema_fast = ExponentialMovingAverage()
        ema_fast.Length = self.EmaShort
        ema_slow = ExponentialMovingAverage()
        ema_slow.Length = self.EmaLong
        rsi = RelativeStrengthIndex()
        rsi.Length = self.RsiPeriod

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(ema_fast, ema_slow, rsi, self.ProcessCandle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, ema_fast)
            self.DrawIndicator(area, ema_slow)
            self.DrawOwnTrades(area)

    def ProcessCandle(self, candle, fast_value, slow_value, rsi_value):
        if candle.State != CandleStates.Finished:
            return

        self._bar_index += 1
        cooldown_ok = self._bar_index - self._last_trade_bar > self.CooldownBars

        cross_up = self._prev_fast > 0 and self._prev_fast <= self._prev_slow and fast_value > slow_value
        cross_down = self._prev_fast > 0 and self._prev_fast >= self._prev_slow and fast_value < slow_value

        if cross_up and rsi_value < 60 and self.Position <= 0 and cooldown_ok:
            self.BuyMarket()
            self._last_trade_bar = self._bar_index
        elif cross_down and rsi_value > 40 and self.Position >= 0 and cooldown_ok:
            self.SellMarket()
            self._last_trade_bar = self._bar_index

        self._prev_fast = fast_value
        self._prev_slow = slow_value

    def CreateClone(self):
        """!! REQUIRED!! Creates a new instance of the strategy."""
        return aud_usd_scalping_strategy()
