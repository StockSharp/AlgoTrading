import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Decimal
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import BollingerBands
from StockSharp.Algo.Strategies import Strategy


class double_bollinger_bands_signals_strategy(Strategy):
    """
    Double Bollinger Bands Signals strategy.
    Two Bollinger Bands of the same Length use Width1 and Width2 standard deviations. A close crossing above the lower Width2 band
    goes long and a close crossing below the upper Width2 band goes short, reversing an opposite position. A long closes when the
    close crosses above the upper Width1 band and a short when it crosses below the lower Width1 band.
    """

    def __init__(self):
        super(double_bollinger_bands_signals_strategy, self).__init__()
        self._length = self.Param("Length", 20).SetGreaterThanZero().SetDisplay("Length", "Bollinger Bands period", "Indicators")
        self._width1 = self.Param("Width1", 2.0).SetGreaterThanZero().SetDisplay("Width 1", "Standard deviations of the exit bands", "Indicators")
        self._width2 = self.Param("Width2", 3.0).SetGreaterThanZero().SetDisplay("Width 2", "Standard deviations of the entry bands", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._reset_state()

    @property
    def candle_type(self):
        return self._candle_type.Value

    def _reset_state(self):
        self._prev_close = None
        self._prev_upper1 = None
        self._prev_lower1 = None
        self._prev_upper2 = None
        self._prev_lower2 = None

    def OnReseted(self):
        super(double_bollinger_bands_signals_strategy, self).OnReseted()
        self._reset_state()

    def OnStarted2(self, time):
        super(double_bollinger_bands_signals_strategy, self).OnStarted2(time)

        self._reset_state()

        inner = BollingerBands()
        inner.Length = self._length.Value
        inner.Width = Decimal(self._width1.Value)
        outer = BollingerBands()
        outer.Length = self._length.Value
        outer.Width = Decimal(self._width2.Value)

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(inner, outer, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, inner)
            self.DrawIndicator(area, outer)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, inner_value, outer_value):
        if candle.State != CandleStates.Finished:
            return

        if not inner_value.IsFormed or not outer_value.IsFormed:
            return

        upper1 = inner_value.UpBand
        lower1 = inner_value.LowBand
        upper2 = outer_value.UpBand
        lower2 = outer_value.LowBand

        if upper1 is None or lower1 is None or upper2 is None or lower2 is None:
            return

        close = candle.ClosePrice

        pc = self._prev_close
        pu1 = self._prev_upper1
        pl1 = self._prev_lower1
        pu2 = self._prev_upper2
        pl2 = self._prev_lower2

        self._prev_close = close
        self._prev_upper1 = upper1
        self._prev_lower1 = lower1
        self._prev_upper2 = upper2
        self._prev_lower2 = lower2

        if pc is None or pu1 is None or pl1 is None or pu2 is None or pl2 is None:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        long_entry = pc <= pl2 and close > lower2
        short_entry = pc >= pu2 and close < upper2
        long_exit = pc <= pu1 and close > upper1
        short_exit = pc >= pl1 and close < lower1

        if long_entry and self.Position <= 0:
            self.BuyMarket(self.Volume + abs(self.Position))
        elif short_entry and self.Position >= 0:
            self.SellMarket(self.Volume + abs(self.Position))
        elif self.Position > 0 and long_exit:
            self.SellMarket(self.Position)
        elif self.Position < 0 and short_exit:
            self.BuyMarket(-self.Position)

    def CreateClone(self):
        return double_bollinger_bands_signals_strategy()
