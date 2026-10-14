import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Decimal
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import AverageTrueRange
from StockSharp.Algo.Strategies import Strategy

class grover_llorens_activator_strategy(Strategy):
    """
    Grover Llorens Activator strategy.
    The activator line restarts Multiplier ATRs away from price whenever the close crosses it: below price after an upward cross,
    above price after a downward one. Between crosses it moves towards price by ATR / Length times the number of candles since the
    cross, so it accelerates the longer the trend lasts. The strategy buys when close minus the line crosses above zero and sells
    when it crosses below zero, reversing an opposite position.
    """

    def __init__(self):
        super(grover_llorens_activator_strategy, self).__init__()
        self._length = self.Param("Length", 480).SetGreaterThanZero().SetDisplay("Length", "ATR period and step divisor", "Indicators")
        self._multiplier = self.Param("Multiplier", 14.0).SetGreaterThanZero().SetDisplay("Multiplier", "ATR multiplier for the restart distance", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._reset_state()

    @property
    def candle_type(self):
        return self._candle_type.Value

    def _reset_state(self):
        self._ts = None
        self._prev_diff = 0.0
        self._step = 0.0
        self._bars_since_cross = 0
        self._direction = 0

    def OnReseted(self):
        super(grover_llorens_activator_strategy, self).OnReseted()
        self._reset_state()

    def OnStarted2(self, time):
        super(grover_llorens_activator_strategy, self).OnStarted2(time)

        self._reset_state()

        atr = AverageTrueRange()
        atr.Length = self._length.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(atr, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, atr_value):
        if candle.State != CandleStates.Finished:
            return

        if not atr_value.IsFormed:
            return

        atr = float(atr_value.GetValue[Decimal](None))
        close = float(candle.ClosePrice)
        prev_ts = self._ts if self._ts is not None else close
        diff = close - prev_ts

        cross_up = diff > 0 and self._prev_diff <= 0
        cross_down = diff < 0 and self._prev_diff >= 0
        multiplier = float(self._multiplier.Value)

        if cross_up or cross_down:
            self._direction = 1 if cross_up else -1
            self._step = atr / self._length.Value
            self._bars_since_cross = 0
            self._ts = close - atr * multiplier if cross_up else close + atr * multiplier
        else:
            self._bars_since_cross += 1
            self._ts = prev_ts + self._direction * self._step * self._bars_since_cross

        self._prev_diff = diff

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        if cross_up and self.Position <= 0:
            self.BuyMarket(self.Volume + abs(self.Position))
        elif cross_down and self.Position >= 0:
            self.SellMarket(self.Volume + abs(self.Position))

    def CreateClone(self):
        return grover_llorens_activator_strategy()
