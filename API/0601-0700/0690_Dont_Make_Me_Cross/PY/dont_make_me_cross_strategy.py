import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Decimal
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy


class dont_make_me_cross_strategy(Strategy):
    """
    Dont Make Me Cross strategy.
    Both EMAs are shifted vertically by ShiftAmount. A cross of the shifted short EMA above the shifted long EMA goes long,
    a cross below goes short, and the opposite cross reverses the position.
    """

    def __init__(self):
        super(dont_make_me_cross_strategy, self).__init__()
        self._short_ema_length = self.Param("ShortEmaLength", 9).SetGreaterThanZero().SetDisplay("Short EMA", "Short EMA period", "Indicators")
        self._long_ema_length = self.Param("LongEmaLength", 21).SetGreaterThanZero().SetDisplay("Long EMA", "Long EMA period", "Indicators")
        self._shift_amount = self.Param("ShiftAmount", -50.0).SetDisplay("Shift Amount", "Vertical shift added to both EMAs", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(1))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._reset_state()

    @property
    def candle_type(self):
        return self._candle_type.Value

    def _reset_state(self):
        self._prev_short = None
        self._prev_long = None

    def OnReseted(self):
        super(dont_make_me_cross_strategy, self).OnReseted()
        self._reset_state()

    def OnStarted2(self, time):
        super(dont_make_me_cross_strategy, self).OnStarted2(time)

        self._reset_state()

        short_ema = ExponentialMovingAverage()
        short_ema.Length = self._short_ema_length.Value
        long_ema = ExponentialMovingAverage()
        long_ema.Length = self._long_ema_length.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(short_ema, long_ema, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, short_ema)
            self.DrawIndicator(area, long_ema)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, short_value, long_value):
        if candle.State != CandleStates.Finished:
            return

        if not short_value.IsFormed or not long_value.IsFormed:
            return

        shift = Decimal(self._shift_amount.Value)
        short_ema = short_value.GetValue[Decimal](None) + shift
        long_ema = long_value.GetValue[Decimal](None) + shift

        prev_short = self._prev_short
        prev_long = self._prev_long

        self._prev_short = short_ema
        self._prev_long = long_ema

        if prev_short is None or prev_long is None:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        cross_up = prev_short <= prev_long and short_ema > long_ema
        cross_down = prev_short >= prev_long and short_ema < long_ema

        if cross_up and self.Position <= 0:
            self.BuyMarket(self.Volume + abs(self.Position))
        elif cross_down and self.Position >= 0:
            self.SellMarket(self.Volume + abs(self.Position))

    def CreateClone(self):
        return dont_make_me_cross_strategy()
