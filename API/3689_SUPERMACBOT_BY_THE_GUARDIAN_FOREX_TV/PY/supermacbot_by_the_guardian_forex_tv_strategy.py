import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy
from StockSharp.Messages import Unit, UnitTypes


class supermacbot_by_the_guardian_forex_tv_strategy(Strategy):
    def __init__(self):
        super(supermacbot_by_the_guardian_forex_tv_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", TimeSpan.FromMinutes(5) \
            .SetDisplay("Candle Type", "Type of candles to process", "General")
        self._fast_ma_period = self.Param("FastMaPeriod", 12) \
            .SetDisplay("Candle Type", "Type of candles to process", "General")
        self._slow_ma_period = self.Param("SlowMaPeriod", 26) \
            .SetDisplay("Candle Type", "Type of candles to process", "General")
        self._macd_fast_period = self.Param("MacdFastPeriod", 12) \
            .SetDisplay("Candle Type", "Type of candles to process", "General")
        self._macd_slow_period = self.Param("MacdSlowPeriod", 24) \
            .SetDisplay("Candle Type", "Type of candles to process", "General")
        self._macd_signal_period = self.Param("MacdSignalPeriod", 9) \
            .SetDisplay("Candle Type", "Type of candles to process", "General")
        self._histogram_threshold = self.Param("HistogramThreshold", 0) \
            .SetDisplay("Candle Type", "Type of candles to process", "General")
        self._trailing_period = self.Param("TrailingPeriod", 12) \
            .SetDisplay("Candle Type", "Type of candles to process", "General")

        self._is_histogram_initialized = False
        self._prev_histogram = 0.0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(supermacbot_by_the_guardian_forex_tv_strategy, self).OnReseted()
        self._is_histogram_initialized = False
        self._prev_histogram = 0.0

    def OnStarted(self, time):
        super(supermacbot_by_the_guardian_forex_tv_strategy, self).OnStarted(time)

        self._fast_ma = SimpleMovingAverage()
        self._fast_ma.Length = self.fast_ma_period
        self._slow_ma = SimpleMovingAverage()
        self._slow_ma.Length = self.slow_ma_period
        self._trailing_ma = SimpleMovingAverage()
        self._trailing_ma.Length = self.trailing_period

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self._fast_ma, self._slow_ma, self._trailing_ma, self._process_candle).Start()

    def _process_candle(self, candle, *args):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        # Trading logic placeholder
        pass

    def CreateClone(self):
        return supermacbot_by_the_guardian_forex_tv_strategy()
