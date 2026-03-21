import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy

class daily_supertrend_ema_crossover_rsi_filter_strategy(Strategy):
    """
    Daily Supertrend EMA Crossover RSI Filter strategy.
    Enters long on golden cross, short on death cross.
    """

    def __init__(self):
        super(daily_supertrend_ema_crossover_rsi_filter_strategy, self).__init__()
        self._fast_ema_period = self.Param("FastEmaPeriod", 120)             .SetDisplay("Fast EMA", "Fast EMA period", "Indicators")
        self._slow_ema_period = self.Param("SlowEmaPeriod", 450)             .SetDisplay("Slow EMA", "Slow EMA period", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(1)))             .SetDisplay("Candle Type", "Candle type for strategy", "General")

        self._prev_fast_ema = 0.0
        self._prev_slow_ema = 0.0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnStarted(self, time):
        super(daily_supertrend_ema_crossover_rsi_filter_strategy, self).OnStarted(time)

        fast_ema = ExponentialMovingAverage()
        fast_ema.Length = self._fast_ema_period.Value
        slow_ema = ExponentialMovingAverage()
        slow_ema.Length = self._slow_ema_period.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(fast_ema, slow_ema, self.on_process).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, fast_ema)
            self.DrawIndicator(area, slow_ema)
            self.DrawOwnTrades(area)

    def on_process(self, candle, fast_val, slow_val):
        if candle.State != CandleStates.Finished:
            return

        fast_v = float(fast_val)
        slow_v = float(slow_val)

        if self._prev_fast_ema == 0.0 or self._prev_slow_ema == 0.0:
            self._prev_fast_ema = fast_v
            self._prev_slow_ema = slow_v
            return

        if self._prev_fast_ema <= self._prev_slow_ema and fast_v > slow_v and self.Position <= 0:
            self.BuyMarket()
        elif self._prev_fast_ema >= self._prev_slow_ema and fast_v < slow_v and self.Position >= 0:
            self.SellMarket()

        self._prev_fast_ema = fast_v
        self._prev_slow_ema = slow_v

    def CreateClone(self):
        return daily_supertrend_ema_crossover_rsi_filter_strategy()
