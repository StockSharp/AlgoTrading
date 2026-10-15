import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy


class supermacbot_by_the_guardian_forex_tv_strategy(Strategy):

    def __init__(self):
        super(supermacbot_by_the_guardian_forex_tv_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Type of candles to process", "General")
        self._fast_ma_period = self.Param("FastMaPeriod", 12) \
            .SetGreaterThanZero() \
            .SetDisplay("Fast SMA", "Fast SMA period", "Indicators")
        self._slow_ma_period = self.Param("SlowMaPeriod", 26) \
            .SetGreaterThanZero() \
            .SetDisplay("Slow SMA", "Slow SMA period", "Indicators")
        self._macd_fast_period = self.Param("MacdFastPeriod", 12) \
            .SetGreaterThanZero() \
            .SetDisplay("MACD Fast", "MACD fast EMA period", "Indicators")
        self._macd_slow_period = self.Param("MacdSlowPeriod", 24) \
            .SetGreaterThanZero() \
            .SetDisplay("MACD Slow", "MACD slow EMA period", "Indicators")
        self._macd_signal_period = self.Param("MacdSignalPeriod", 9) \
            .SetGreaterThanZero() \
            .SetDisplay("MACD Signal", "MACD signal EMA period", "Indicators")
        self._histogram_threshold = self.Param("HistogramThreshold", 0.0) \
            .SetDisplay("Histogram Threshold", "Required MACD histogram magnitude", "Logic")
        self._trailing_period = self.Param("TrailingPeriod", 12) \
            .SetGreaterThanZero() \
            .SetDisplay("Trailing SMA", "Trailing SMA period", "Logic")

        self._is_histogram_initialized = False
        self._prev_histogram = 0.0

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    @property
    def FastMaPeriod(self):
        return self._fast_ma_period.Value

    @FastMaPeriod.setter
    def FastMaPeriod(self, value):
        self._fast_ma_period.Value = value

    @property
    def SlowMaPeriod(self):
        return self._slow_ma_period.Value

    @SlowMaPeriod.setter
    def SlowMaPeriod(self, value):
        self._slow_ma_period.Value = value

    @property
    def TrailingPeriod(self):
        return self._trailing_period.Value

    @TrailingPeriod.setter
    def TrailingPeriod(self, value):
        self._trailing_period.Value = value

    def OnReseted(self):
        super(supermacbot_by_the_guardian_forex_tv_strategy, self).OnReseted()
        self._is_histogram_initialized = False
        self._prev_histogram = 0.0

    def OnStarted2(self, time):
        super(supermacbot_by_the_guardian_forex_tv_strategy, self).OnStarted2(time)

        fast_ma = SimpleMovingAverage()
        fast_ma.Length = self.FastMaPeriod
        slow_ma = SimpleMovingAverage()
        slow_ma.Length = self.SlowMaPeriod
        trailing_ma = SimpleMovingAverage()
        trailing_ma.Length = self.TrailingPeriod

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(fast_ma, slow_ma, trailing_ma, self._process_candle).Start()

        self.StartProtection(
            takeProfit=Unit(2, UnitTypes.Percent),
            stopLoss=Unit(1, UnitTypes.Percent))

    def _process_candle(self, candle, fast_ma, slow_ma, trailing_ma):
        if candle.State != CandleStates.Finished:
            return

        if self.Position != 0:
            return

        fma = float(fast_ma)
        sma_val = float(slow_ma)
        tma = float(trailing_ma)
        price = float(candle.ClosePrice)

        bullish_trend = fma > sma_val
        bearish_trend = fma < sma_val

        if bullish_trend and price > tma:
            self.BuyMarket()
        elif bearish_trend and price < tma:
            self.SellMarket()

    def CreateClone(self):
        return supermacbot_by_the_guardian_forex_tv_strategy()
