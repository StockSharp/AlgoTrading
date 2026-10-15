import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Decimal
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy


class ema_crossover_trailing_stop_strategy(Strategy):
    """
    EMA crossover trailing stop strategy.
    The short EMA crossing above the long EMA goes long and crossing below goes short, reversing an opposite position.
    A percent trailing stop follows the best price since entry and closes the position when price reverses by TrailStopPercent.
    """

    def __init__(self):
        super(ema_crossover_trailing_stop_strategy, self).__init__()
        self._short_length = self.Param("ShortLength", 9).SetGreaterThanZero().SetDisplay("Short EMA", "Short EMA length", "Indicators")
        self._long_length = self.Param("LongLength", 21).SetGreaterThanZero().SetDisplay("Long EMA", "Long EMA length", "Indicators")
        self._trail_stop_percent = self.Param("TrailStopPercent", 1.0).SetNotNegative().SetDisplay("Trailing Stop %", "Trailing stop percentage from the best price", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(1))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._reset_state()

    @property
    def candle_type(self):
        return self._candle_type.Value

    def _reset_state(self):
        self._prev_short = None
        self._prev_long = None

    def OnReseted(self):
        super(ema_crossover_trailing_stop_strategy, self).OnReseted()
        self._reset_state()

    def OnStarted2(self, time):
        super(ema_crossover_trailing_stop_strategy, self).OnStarted2(time)

        self._reset_state()

        short_ema = ExponentialMovingAverage()
        short_ema.Length = self._short_length.Value
        long_ema = ExponentialMovingAverage()
        long_ema.Length = self._long_length.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(short_ema, long_ema, self._process_candle).Start()

        trail = float(self._trail_stop_percent.Value)
        if trail > 0:
            self.StartProtection(Unit(), Unit(Decimal(trail), UnitTypes.Percent), isStopTrailing=True, useMarketOrders=True)

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, short_ema)
            self.DrawIndicator(area, long_ema)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, short_value, long_value):
        if candle.State != CandleStates.Finished:
            return

        short_v = float(short_value)
        long_v = float(long_value)

        prev_short = self._prev_short
        prev_long = self._prev_long
        self._prev_short = short_v
        self._prev_long = long_v

        if prev_short is None or prev_long is None:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        if prev_short <= prev_long and short_v > long_v and self.Position <= 0:
            self.BuyMarket(self.Volume + abs(self.Position))
        elif prev_short >= prev_long and short_v < long_v and self.Position >= 0:
            self.SellMarket(self.Volume + abs(self.Position))

    def CreateClone(self):
        return ema_crossover_trailing_stop_strategy()
