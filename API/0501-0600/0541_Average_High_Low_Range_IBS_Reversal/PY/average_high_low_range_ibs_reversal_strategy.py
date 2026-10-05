import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, DateTime, DateTimeKind
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import Highest, SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy
from indicator_extensions import *

RANGE_MULTIPLIER = 2.5


class average_high_low_range_ibs_reversal_strategy(Strategy):
    """
    Average high-low range IBS reversal strategy.
    The buy threshold is the highest high of the last Length candles minus 2.5 times the SMA of the candle range over Length
    candles. When the close has stayed below that threshold for BarsBelowThreshold consecutive candles and the internal bar
    strength (close - low) / (high - low) is below IbsBuyThreshold, a long opens, provided the candle lies between StartTime and
    EndTime. The long closes when a close exceeds the previous candle's high. Long only.
    """

    def __init__(self):
        super(average_high_low_range_ibs_reversal_strategy, self).__init__()
        self._length = self.Param("Length", 20) \
            .SetGreaterThanZero() \
            .SetDisplay("Length", "Lookback of the range average and the highest high", "Indicators")
        self._bars_below_threshold = self.Param("BarsBelowThreshold", 2) \
            .SetGreaterThanZero() \
            .SetDisplay("Bars Below Threshold", "Consecutive closes below the threshold required for an entry", "Signals")
        self._ibs_buy_threshold = self.Param("IbsBuyThreshold", 0.2) \
            .SetDisplay("IBS Buy Threshold", "Maximum internal bar strength for an entry", "Signals")
        self._start_time = self.Param("StartTime", DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc)) \
            .SetDisplay("Start Time", "Start of the trading window", "Time")
        self._end_time = self.Param("EndTime", DateTime(2100, 1, 1, 0, 0, 0, DateTimeKind.Utc)) \
            .SetDisplay("End Time", "End of the trading window", "Time")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(1))) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")

        self._range_average = None
        self._bars_below = 0
        self._prev_high = None

    @property
    def CandleType(self):
        return self._candle_type.Value

    def GetWorkingSecurities(self):
        return [(self.Security, self.CandleType)]

    def OnReseted(self):
        super(average_high_low_range_ibs_reversal_strategy, self).OnReseted()
        self._range_average = None
        self._bars_below = 0
        self._prev_high = None

    def OnStarted2(self, time):
        super(average_high_low_range_ibs_reversal_strategy, self).OnStarted2(time)

        self._bars_below = 0
        self._prev_high = None

        highest = Highest()
        highest.Length = self._length.Value
        self._range_average = SimpleMovingAverage()
        self._range_average.Length = self._length.Value

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(highest, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, highest_value):
        if candle.State != CandleStates.Finished:
            return

        high = float(candle.HighPrice)
        low = float(candle.LowPrice)
        close = float(candle.ClosePrice)
        candle_range = high - low
        average_range = float(to_decimal(process_float(self._range_average, candle_range, candle.ServerTime, True)))

        prev_high = self._prev_high
        self._prev_high = high

        if not self._range_average.IsFormed:
            return

        threshold = float(highest_value) - RANGE_MULTIPLIER * average_range
        self._bars_below = self._bars_below + 1 if close < threshold else 0

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        if self.Position > 0:
            if prev_high is not None and close > prev_high:
                self.SellMarket(self.Position)
            return

        ibs = (close - low) / candle_range if candle_range > 0 else 0.5
        open_time = candle.OpenTime
        in_window = open_time >= self._start_time.Value and open_time <= self._end_time.Value

        if self.Position == 0 and in_window and self._bars_below >= self._bars_below_threshold.Value \
                and ibs < float(self._ibs_buy_threshold.Value):
            self.BuyMarket(self.Volume)

    def CreateClone(self):
        return average_high_low_range_ibs_reversal_strategy()
