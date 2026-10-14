import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Decimal
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy

class inside_bar_breakout_strategy(Strategy):
    """
    Inside Bar Breakout strategy.
    An inside bar's range lies within the previous candle's high and low. While flat, a close above the latest inside bar's
    high buys and a close below its low sells. The stop lies StopLossPercent percent beyond the opposite side of the pattern,
    and a close beyond the previous candle's extreme against the position also exits.
    """

    def __init__(self):
        super(inside_bar_breakout_strategy, self).__init__()
        self._stop_loss_percent = self.Param("StopLossPercent", 1.0).SetNotNegative().SetDisplay("Stop Loss %", "Distance of the stop beyond the pattern, in percent", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))).SetDisplay("Candle Type", "Type of candles to use", "General")

        self._prev_candle = None
        self._inside_bar = None
        self._stop_price = Decimal(0)

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(inside_bar_breakout_strategy, self).OnReseted()
        self._prev_candle = None
        self._inside_bar = None
        self._stop_price = Decimal(0)

    def OnStarted2(self, time):
        super(inside_bar_breakout_strategy, self).OnStarted2(time)

        self._prev_candle = None
        self._inside_bar = None
        self._stop_price = Decimal(0)

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        previous = self._prev_candle
        pattern = self._inside_bar
        self._prev_candle = (candle.HighPrice, candle.LowPrice)

        is_inside = previous is not None and candle.HighPrice <= previous[0] and candle.LowPrice >= previous[1]
        close = candle.ClosePrice

        # A close outside the pattern uses it up; a new inside bar replaces it.
        if pattern is not None and (close > pattern[0] or close < pattern[1]):
            self._inside_bar = None
        if is_inside:
            self._inside_bar = (candle.HighPrice, candle.LowPrice)

        if previous is None or not self.IsFormedAndOnlineAndAllowTrading():
            return

        percent = Decimal(self._stop_loss_percent.Value) / Decimal(100)

        if self.Position > 0:
            if close <= self._stop_price or close < previous[1]:
                self.SellMarket(self.Position)
        elif self.Position < 0:
            if close >= self._stop_price or close > previous[0]:
                self.BuyMarket(-self.Position)
        elif pattern is not None:
            if close > pattern[0]:
                self.BuyMarket(self.Volume)
                self._stop_price = pattern[1] * (Decimal(1) - percent)
            elif close < pattern[1]:
                self.SellMarket(self.Volume)
                self._stop_price = pattern[0] * (Decimal(1) + percent)

    def CreateClone(self):
        return inside_bar_breakout_strategy()
