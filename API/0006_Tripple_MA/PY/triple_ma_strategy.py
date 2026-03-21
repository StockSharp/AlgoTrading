import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy

class triple_ma_strategy(Strategy):
    """
    Strategy based on Triple Moving Average crossover.
    Enters long when short MA > middle MA > long MA.
    Enters short when short MA < middle MA < long MA.
    """

    def __init__(self):
        super(triple_ma_strategy, self).__init__()
        self._short_ma_period = self.Param("ShortMaPeriod", 100).SetDisplay("Short MA Period", "Period for short moving average", "Indicators")
        self._middle_ma_period = self.Param("MiddleMaPeriod", 250).SetDisplay("Middle MA Period", "Period for middle moving average", "Indicators")
        self._long_ma_period = self.Param("LongMaPeriod", 500).SetDisplay("Long MA Period", "Period for long moving average", "Indicators")
        self._stop_loss_percent = self.Param("StopLossPercent", 2.0).SetDisplay("Stop Loss (%)", "Stop loss as a percentage of entry price", "Risk parameters")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(1))).SetDisplay("Candle Type", "Type of candles to use", "General")

        self._prev_is_short_above_middle = False
        self._prev_is_bullish = False
        self._prev_is_bearish = False

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(triple_ma_strategy, self).OnReseted()
        self._prev_is_short_above_middle = False
        self._prev_is_bullish = False
        self._prev_is_bearish = False

    def OnStarted(self, time):
        super(triple_ma_strategy, self).OnStarted(time)

        short_ma = ExponentialMovingAverage()
        short_ma.Length = self._short_ma_period.Value
        middle_ma = ExponentialMovingAverage()
        middle_ma.Length = self._middle_ma_period.Value
        long_ma = ExponentialMovingAverage()
        long_ma.Length = self._long_ma_period.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(short_ma, middle_ma, long_ma, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, short_ma)
            self.DrawIndicator(area, middle_ma)
            self.DrawIndicator(area, long_ma)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, short_val, middle_val, long_val):
        if candle.State != CandleStates.Finished:
            return

        s = float(short_val)
        m = float(middle_val)
        l = float(long_val)

        is_short_above_middle = s > m
        is_middle_above_long = m > l

        is_bullish = is_short_above_middle and is_middle_above_long
        is_bearish = not is_short_above_middle and not is_middle_above_long

        if is_bullish and not self._prev_is_bullish and self.Position <= 0:
            self.BuyMarket(self.Volume + abs(self.Position))
        elif is_bearish and not self._prev_is_bearish and self.Position >= 0:
            self.SellMarket(self.Volume + abs(self.Position))

        self._prev_is_short_above_middle = is_short_above_middle
        self._prev_is_bullish = is_bullish
        self._prev_is_bearish = is_bearish

    def CreateClone(self):
        return triple_ma_strategy()
