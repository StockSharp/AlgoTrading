import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import Highest
from StockSharp.Algo.Strategies import Strategy
from indicator_extensions import *


class new_intraday_high_with_weak_bar_strategy(Strategy):
    """
    New intraday high with weak bar strategy.
    When flat, a candle whose high is the highest high of the last HighestLength bars but which closes in the lower WeakRatio part of
    its range opens a long. The long closes when a candle closes above the previous candle's high.
    """

    def __init__(self):
        super(new_intraday_high_with_weak_bar_strategy, self).__init__()
        self._highest_length = self.Param("HighestLength", 10).SetGreaterThanZero().SetDisplay("Highest Length", "Bars the highest high is taken over", "Indicators")
        self._weak_ratio = self.Param("WeakRatio", 0.15).SetDisplay("Weak Ratio", "Maximum (close - low) / (high - low) of a weak bar", "Signals")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(15))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._highest = None
        self._prev_high = None

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(new_intraday_high_with_weak_bar_strategy, self).OnReseted()
        self._highest = None
        self._prev_high = None

    def OnStarted2(self, time):
        super(new_intraday_high_with_weak_bar_strategy, self).OnStarted2(time)

        self._prev_high = None
        self._highest = Highest()
        self._highest.Length = self._highest_length.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, self._highest)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        # The highest high is taken over candle highs, the current one included.
        highest_value = process_value(self._highest, candle.HighPrice, candle.ServerTime, True)
        high = float(candle.HighPrice)
        low = float(candle.LowPrice)
        close = float(candle.ClosePrice)
        last_high = self._prev_high
        self._prev_high = high

        if not self._highest.IsFormed or last_high is None:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        if self.Position > 0:
            if close > last_high:
                self.SellMarket(self.Position)
            return

        if self.Position != 0:
            return

        rng = high - low
        if rng <= 0:
            return

        is_new_high = high >= float(to_decimal(highest_value))
        is_weak = (close - low) / rng < float(self._weak_ratio.Value)

        if is_new_high and is_weak:
            self.BuyMarket(self.Volume)

    def CreateClone(self):
        return new_intraday_high_with_weak_bar_strategy()
