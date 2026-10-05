import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Strategies")

from collections import deque
from System import TimeSpan, Decimal
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy


class bar_range_strategy(Strategy):
    """
    Bar Range strategy.
    Long only: buys a bearish candle (close below open) whose high-low range has a percent rank of at least PercentRankThreshold
    among the previous LookbackPeriod ranges, and closes the long after ExitBars bars.
    """

    def __init__(self):
        super(bar_range_strategy, self).__init__()
        self._lookback_period = self.Param("LookbackPeriod", 50).SetGreaterThanZero().SetDisplay("Lookback Period", "Previous bars the range is ranked against", "Indicators")
        self._percent_rank_threshold = self.Param("PercentRankThreshold", 95.0).SetRange(0.0, 100.0).SetDisplay("Percent Rank Threshold", "Minimum percent rank of the range that allows an entry", "Indicators")
        self._exit_bars = self.Param("ExitBars", 1).SetGreaterThanZero().SetDisplay("Exit Bars", "Bars after which the long is closed", "Trading")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(1))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._reset_state()

    @property
    def candle_type(self):
        return self._candle_type.Value

    def _reset_state(self):
        self._ranges = deque()
        self._bars_in_position = 0

    def OnReseted(self):
        super(bar_range_strategy, self).OnReseted()
        self._reset_state()

    def OnStarted2(self, time):
        super(bar_range_strategy, self).OnStarted2(time)

        self._reset_state()

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        lookback = self._lookback_period.Value
        rng = candle.HighPrice - candle.LowPrice

        # Percent rank: share of the previous LookbackPeriod ranges that do not exceed the current one.
        rank = None
        if len(self._ranges) == lookback:
            rank = 100.0 * sum(1 for r in self._ranges if r <= rng) / lookback

        self._ranges.append(rng)
        while len(self._ranges) > lookback:
            self._ranges.popleft()

        if self.Position > 0:
            self._bars_in_position += 1
        else:
            self._bars_in_position = 0

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        if self.Position > 0:
            if self._bars_in_position >= self._exit_bars.Value:
                self.SellMarket(self.Position)
            return

        if self.Position == 0 and rank is not None and rank >= float(self._percent_rank_threshold.Value) and candle.ClosePrice < candle.OpenPrice:
            self.BuyMarket(self.Volume)

    def CreateClone(self):
        return bar_range_strategy()
