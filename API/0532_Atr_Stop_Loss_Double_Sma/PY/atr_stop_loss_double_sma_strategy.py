import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import CandleStates
from StockSharp.Algo.Indicators import SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *

class atr_stop_loss_double_sma_strategy(Strategy):
    """
    Double SMA crossover strategy.
    Buys on fast SMA crossing above slow SMA, sells on crossing below.
    """

    def __init__(self):
        super(atr_stop_loss_double_sma_strategy, self).__init__()

        self._fast_length = self.Param("FastLength", 15) \
            .SetGreaterThanZero() \
            .SetDisplay("Fast SMA", "Period of the fast SMA", "Moving Average")
        self._slow_length = self.Param("SlowLength", 45) \
            .SetGreaterThanZero() \
            .SetDisplay("Slow SMA", "Period of the slow SMA", "Moving Average")
        self._cooldown_bars = self.Param("CooldownBars", 350) \
            .SetDisplay("Cooldown Bars", "Bars between trades", "Trading")
        self._candle_type = self.Param("CandleType", tf(1)) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")

        self._prev_fast = 0.0
        self._prev_slow = 0.0
        self._bar_index = 0
        self._last_trade_bar = 0

    @property
    def FastLength(self): return self._fast_length.Value
    @FastLength.setter
    def FastLength(self, v): self._fast_length.Value = v
    @property
    def SlowLength(self): return self._slow_length.Value
    @SlowLength.setter
    def SlowLength(self, v): self._slow_length.Value = v
    @property
    def CooldownBars(self): return self._cooldown_bars.Value
    @CooldownBars.setter
    def CooldownBars(self, v): self._cooldown_bars.Value = v
    @property
    def CandleType(self): return self._candle_type.Value
    @CandleType.setter
    def CandleType(self, v): self._candle_type.Value = v

    def OnReseted(self):
        super(atr_stop_loss_double_sma_strategy, self).OnReseted()
        self._prev_fast = 0.0
        self._prev_slow = 0.0
        self._bar_index = 0
        self._last_trade_bar = 0

    def OnStarted2(self, time):
        super(atr_stop_loss_double_sma_strategy, self).OnStarted2(time)

        fast_sma = SimpleMovingAverage()
        fast_sma.Length = self.FastLength
        slow_sma = SimpleMovingAverage()
        slow_sma.Length = self.SlowLength

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(fast_sma, slow_sma, self.ProcessCandle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, fast_sma)
            self.DrawIndicator(area, slow_sma)
            self.DrawOwnTrades(area)

    def ProcessCandle(self, candle, fast_value, slow_value):
        if candle.State != CandleStates.Finished:
            return

        self._bar_index += 1
        cooldown_ok = self._bar_index - self._last_trade_bar > self.CooldownBars

        cross_up = self._prev_fast > 0 and self._prev_fast <= self._prev_slow and fast_value > slow_value
        cross_down = self._prev_fast > 0 and self._prev_fast >= self._prev_slow and fast_value < slow_value

        if cross_up and self.Position <= 0 and cooldown_ok:
            self.BuyMarket()
            self._last_trade_bar = self._bar_index
        elif cross_down and self.Position >= 0 and cooldown_ok:
            self.SellMarket()
            self._last_trade_bar = self._bar_index

        self._prev_fast = fast_value
        self._prev_slow = slow_value

    def CreateClone(self):
        """!! REQUIRED!! Creates a new instance of the strategy."""
        return atr_stop_loss_double_sma_strategy()
