import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, ArgumentOutOfRangeException
from StockSharp.Messages import DataType, CandleStates, Sides, OrderStates
from StockSharp.Algo.Strategies import Strategy


class engulfing_candlestick_strategy(Strategy):
    """Trades a selected engulfing pattern and exits after HoldPeriods bars."""

    def __init__(self):
        super(engulfing_candlestick_strategy, self).__init__()
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(15))) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")
        self._hold_periods = self.Param("HoldPeriods", 17) \
            .SetDisplay("Hold Periods", "Bars to hold the position", "Trading")
        self._pattern = self.Param("Pattern", "Bullish") \
            .SetDisplay("Pattern", "Bullish or Bearish engulfing", "Trading")
        self._side = self.Param("Side", Sides.Buy) \
            .SetDisplay("Side", "Buy for long, Sell for short", "Trading")
        self._previous_open = None
        self._previous_close = None
        self._bars_held = 0
        self._pending_order = None

    @property
    def candle_type(self):
        return self._candle_type.Value

    def GetWorkingSecurities(self):
        return [(self.Security, self.candle_type)]

    def OnReseted(self):
        super(engulfing_candlestick_strategy, self).OnReseted()
        self._previous_open = None
        self._previous_close = None
        self._bars_held = 0
        self._pending_order = None

    def OnStarted2(self, time):
        super(engulfing_candlestick_strategy, self).OnStarted2(time)
        if self._pattern.Value not in ("Bullish", "Bearish"):
            raise ArgumentOutOfRangeException("Pattern", self._pattern.Value, "Choose Bullish or Bearish.")
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self.process_candle).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        bullish = (self._previous_open is not None and self._previous_close < self._previous_open
                   and candle.ClosePrice > candle.OpenPrice and candle.OpenPrice <= self._previous_close
                   and candle.ClosePrice >= self._previous_open)
        bearish = (self._previous_open is not None and self._previous_close > self._previous_open
                   and candle.ClosePrice < candle.OpenPrice and candle.OpenPrice >= self._previous_close
                   and candle.ClosePrice <= self._previous_open)

        if self.Position != 0:
            self._bars_held += 1
        if self._pending_order is not None and self._pending_order.State in (OrderStates.Done, OrderStates.Failed):
            self._pending_order = None

        if self._pending_order is None and self.IsFormedAndOnlineAndAllowTrading():
            if self.Position != 0 and self._bars_held >= int(self._hold_periods.Value):
                self._pending_order = self.SellMarket(self.Position) if self.Position > 0 else self.BuyMarket(-self.Position)
            elif self.Position == 0 and (bullish if self._pattern.Value == "Bullish" else bearish):
                self._pending_order = self.BuyMarket() if self._side.Value == Sides.Buy else self.SellMarket()
                self._bars_held = 0

        self._previous_open = candle.OpenPrice
        self._previous_close = candle.ClosePrice

    def CreateClone(self):
        return engulfing_candlestick_strategy()
