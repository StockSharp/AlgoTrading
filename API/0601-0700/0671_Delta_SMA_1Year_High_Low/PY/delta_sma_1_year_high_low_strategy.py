import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from collections import deque

from System import TimeSpan, Decimal
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy
from indicator_extensions import *

WINDOW_TICKS = TimeSpan.FromDays(365).Ticks


class delta_sma_1_year_high_low_strategy(Strategy):
    """
    Delta SMA 1-year high/low strategy.
    The candle volume delta is buy minus sell volume (the candle volume signed by its direction when the split is unknown),
    smoothed by an SMA. Its high and low are tracked over the last year of available history.
    After the delta SMA has been below 70% of that low, a cross above zero opens a long.
    After the delta SMA has risen above 70% of the high, a drop below 60% of the high closes the long.
    """

    def __init__(self):
        super(delta_sma_1_year_high_low_strategy, self).__init__()
        self._delta_sma_length = self.Param("DeltaSmaLength", 14) \
            .SetGreaterThanZero() \
            .SetDisplay("Delta SMA Length", "SMA length of the volume delta", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")
        self._delta_sma = None
        self._reset_state()

    @property
    def candle_type(self):
        return self._candle_type.Value

    def GetWorkingSecurities(self):
        return [(self.Security, self.candle_type)]

    def _reset_state(self):
        self._max_queue = deque()
        self._min_queue = deque()
        self._prev_delta_sma = None
        self._was_low = False
        self._was_high = False

    def OnReseted(self):
        super(delta_sma_1_year_high_low_strategy, self).OnReseted()
        self._reset_state()

    def OnStarted2(self, time):
        super(delta_sma_1_year_high_low_strategy, self).OnStarted2(time)

        self._reset_state()
        self._delta_sma = SimpleMovingAverage()
        self._delta_sma.Length = self._delta_sma_length.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        volume = float(candle.TotalVolume)
        if candle.BuyVolume is not None and candle.SellVolume is not None:
            delta = float(candle.BuyVolume) - float(candle.SellVolume)
        elif candle.ClosePrice > candle.OpenPrice:
            delta = volume
        elif candle.ClosePrice < candle.OpenPrice:
            delta = -volume
        else:
            delta = 0.0

        sma_value = process_float(self._delta_sma, delta, candle.ServerTime, True)
        if not sma_value.IsFormed:
            return

        delta_sma = float(sma_value.GetValue[Decimal](None))
        ticks = candle.OpenTime.Ticks

        self._push(self._max_queue, ticks, delta_sma, True)
        self._push(self._min_queue, ticks, delta_sma, False)

        year_high = self._max_queue[0][1]
        year_low = self._min_queue[0][1]

        prev = self._prev_delta_sma
        self._prev_delta_sma = delta_sma

        if delta_sma < year_low * 0.7:
            self._was_low = True

        if self.Position > 0 and delta_sma > year_high * 0.7:
            self._was_high = True

        if prev is None:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        if self.Position == 0 and self._was_low and prev <= 0 and delta_sma > 0:
            self.BuyMarket()
            self._was_low = False
            self._was_high = False
        elif self.Position > 0 and self._was_high and delta_sma < year_high * 0.6:
            self.SellMarket(self.Position)
            self._was_high = False

    @staticmethod
    def _push(queue, ticks, value, is_max):
        # Monotonic queue: the front always holds the extreme of the window.
        while queue and (queue[-1][1] <= value if is_max else queue[-1][1] >= value):
            queue.pop()
        queue.append((ticks, value))
        while queue and queue[0][0] <= ticks - WINDOW_TICKS:
            queue.popleft()

    def CreateClone(self):
        return delta_sma_1_year_high_low_strategy()
