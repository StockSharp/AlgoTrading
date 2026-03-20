import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy


class specific_day_time_strategy(Strategy):
    def __init__(self):
        super(specific_day_time_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", TimeSpan.FromMinutes(30) \
            .SetDisplay("Candle Type", "Candle timeframe", "General")
        self._fast_period = self.Param("FastPeriod", 8) \
            .SetDisplay("Candle Type", "Candle timeframe", "General")
        self._slow_period = self.Param("SlowPeriod", 21) \
            .SetDisplay("Candle Type", "Candle timeframe", "General")
        self._trade_hour = self.Param("TradeHour", 12) \
            .SetDisplay("Candle Type", "Candle timeframe", "General")
        self._cooldown_bars = self.Param("CooldownBars", 8) \
            .SetDisplay("Candle Type", "Candle timeframe", "General")

        self._cooldown = 0.0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(specific_day_time_strategy, self).OnReseted()
        self._cooldown = 0.0

    def OnStarted(self, time):
        super(specific_day_time_strategy, self).OnStarted(time)

        self._fast = SimpleMovingAverage()
        self._fast.Length = self.fast_period
        self._slow = SimpleMovingAverage()
        self._slow.Length = self.slow_period

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self._fast, self._slow, self._process_candle).Start()

    def _process_candle(self, candle, *args):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        # Trading logic placeholder
        pass

    def CreateClone(self):
        return specific_day_time_strategy()
