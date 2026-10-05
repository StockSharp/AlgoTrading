import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Decimal
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import Highest, Lowest, SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy


class grid_bot_backtesting_strategy(Strategy):
    """
    Grid bot backtesting strategy.
    GridLines lines are spread evenly from the lower to the upper bound. With AutoBounds the bounds are the BoundLookback highest high
    and lowest low ("Hi & Low") or the BoundLookback SMA of the close ("Average"), widened by BoundDeviation; otherwise UpperBound and
    LowerBound are used. A close crossing below a line that holds no order buys one Volume for that line, and a close crossing above the
    next line up sells it again. Only longs are opened.
    """

    HI_LOW_SOURCE = "Hi & Low"

    def __init__(self):
        super(grid_bot_backtesting_strategy, self).__init__()
        self._auto_bounds = self.Param("AutoBounds", True).SetDisplay("Auto Bounds", "Calculate the bounds from recent data", "Grid")
        self._bound_source = self.Param("BoundSource", self.HI_LOW_SOURCE).SetDisplay("Bound Source", "Source of the automatic bounds: Hi & Low or Average", "Grid")
        self._bound_lookback = self.Param("BoundLookback", 250).SetGreaterThanZero().SetDisplay("Bound Lookback", "Candles used for the automatic bounds", "Grid")
        self._bound_deviation = self.Param("BoundDeviation", 0.10).SetNotNegative().SetDisplay("Bound Deviation", "Fraction the automatic bounds are widened by", "Grid")
        self._upper_bound = self.Param("UpperBound", 0.285).SetGreaterThanZero().SetDisplay("Upper Bound", "Manual upper bound", "Grid")
        self._lower_bound = self.Param("LowerBound", 0.225).SetGreaterThanZero().SetDisplay("Lower Bound", "Manual lower bound", "Grid")
        self._grid_lines = self.Param("GridLines", 30).SetRange(2, 1000).SetDisplay("Grid Lines", "Number of grid lines", "Grid")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(1))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._filled = []
        self._prev_close = None

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(grid_bot_backtesting_strategy, self).OnReseted()
        self._filled = []
        self._prev_close = None

    def OnStarted2(self, time):
        super(grid_bot_backtesting_strategy, self).OnStarted2(time)

        self._filled = [False] * self._grid_lines.Value
        self._prev_close = None

        highest = Highest()
        highest.Length = self._bound_lookback.Value
        lowest = Lowest()
        lowest.Length = self._bound_lookback.Value
        average = SimpleMovingAverage()
        average.Length = self._bound_lookback.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(highest, lowest, average, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, highest, lowest, average):
        if candle.State != CandleStates.Finished:
            return

        prev_close = self._prev_close
        self._prev_close = candle.ClosePrice

        if not self.IsFormedAndOnlineAndAllowTrading() or prev_close is None:
            return

        if self._auto_bounds.Value:
            deviation = Decimal(self._bound_deviation.Value)
            hi_low = self._bound_source.Value == self.HI_LOW_SOURCE
            upper = (highest if hi_low else average) * (Decimal(1) + deviation)
            lower = (lowest if hi_low else average) * (Decimal(1) - deviation)
        else:
            upper = Decimal(self._upper_bound.Value)
            lower = Decimal(self._lower_bound.Value)

        if upper <= lower:
            return

        close = candle.ClosePrice
        count = len(self._filled)
        step = (upper - lower) / Decimal(count - 1)

        for i in range(count):
            line = lower + step * Decimal(i)

            if not self._filled[i] and prev_close > line and close <= line:
                self.BuyMarket(self.Volume)
                self._filled[i] = True
            elif self._filled[i] and i + 1 < count:
                next_line = line + step
                if prev_close < next_line and close >= next_line:
                    self.SellMarket(self.Volume)
                    self._filled[i] = False

    def CreateClone(self):
        return grid_bot_backtesting_strategy()
