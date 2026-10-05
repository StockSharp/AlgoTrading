import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")
clr.AddReference("StockSharp.BusinessEntities")

from System import TimeSpan, Decimal
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage, RelativeStrengthIndex, SimpleMovingAverage, DecimalIndicatorValue
from StockSharp.Algo.Strategies import Strategy


class ema_crossover_rsi_distance_strategy(Strategy):
    """
    EMA crossover with RSI and distance strategy.
    The long signal needs EMA short above EMA medium, EMA long1 above EMA long2, RSI above 50 and above its SMA, the distance
    between EMA short and EMA medium above its average over DistanceLength, a growing distance between EMA long1 and EMA medium
    and the close above EMA short. The short signal mirrors it. A position is opened on its signal, reversing an opposite one,
    and closed as soon as the signal turns neutral.
    """

    def __init__(self):
        super(ema_crossover_rsi_distance_strategy, self).__init__()
        self._ema_short_length = self.Param("EmaShortLength", 5).SetGreaterThanZero().SetDisplay("EMA Short", "Short EMA length", "Indicators")
        self._ema_medium_length = self.Param("EmaMediumLength", 13).SetGreaterThanZero().SetDisplay("EMA Medium", "Medium EMA length", "Indicators")
        self._ema_long1_length = self.Param("EmaLong1Length", 40).SetGreaterThanZero().SetDisplay("EMA Long 1", "First long EMA length", "Indicators")
        self._ema_long2_length = self.Param("EmaLong2Length", 55).SetGreaterThanZero().SetDisplay("EMA Long 2", "Second long EMA length", "Indicators")
        self._rsi_length = self.Param("RsiLength", 14).SetGreaterThanZero().SetDisplay("RSI Length", "RSI length", "Indicators")
        self._rsi_average_length = self.Param("RsiAverageLength", 14).SetGreaterThanZero().SetDisplay("RSI Average", "Length of the RSI SMA", "Indicators")
        self._distance_length = self.Param("DistanceLength", 5).SetGreaterThanZero().SetDisplay("Distance Length", "Length of the EMA distance average", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(1))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._rsi_average = None
        self._distance_average = None
        self._prev_long_distance = None

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(ema_crossover_rsi_distance_strategy, self).OnReseted()
        self._prev_long_distance = None

    def OnStarted2(self, time):
        super(ema_crossover_rsi_distance_strategy, self).OnStarted2(time)

        self._prev_long_distance = None

        ema_short = ExponentialMovingAverage()
        ema_short.Length = self._ema_short_length.Value
        ema_medium = ExponentialMovingAverage()
        ema_medium.Length = self._ema_medium_length.Value
        ema_long1 = ExponentialMovingAverage()
        ema_long1.Length = self._ema_long1_length.Value
        ema_long2 = ExponentialMovingAverage()
        ema_long2.Length = self._ema_long2_length.Value
        rsi = RelativeStrengthIndex()
        rsi.Length = self._rsi_length.Value
        self._rsi_average = SimpleMovingAverage()
        self._rsi_average.Length = self._rsi_average_length.Value
        self._distance_average = SimpleMovingAverage()
        self._distance_average.Length = self._distance_length.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(ema_short, ema_medium, ema_long1, ema_long2, rsi, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, ema_short)
            self.DrawIndicator(area, ema_medium)
            self.DrawIndicator(area, ema_long1)
            self.DrawIndicator(area, ema_long2)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, ema_short_value, ema_medium_value, ema_long1_value, ema_long2_value, rsi_value):
        if candle.State != CandleStates.Finished:
            return

        ema_short = float(ema_short_value)
        ema_medium = float(ema_medium_value)
        ema_long1 = float(ema_long1_value)
        ema_long2 = float(ema_long2_value)
        rsi = float(rsi_value)

        rsi_input = DecimalIndicatorValue(self._rsi_average, Decimal(rsi), candle.OpenTime)
        rsi_input.IsFinal = True
        rsi_average_value = self._rsi_average.Process(rsi_input)

        distance = abs(ema_short - ema_medium)
        distance_input = DecimalIndicatorValue(self._distance_average, Decimal(distance), candle.OpenTime)
        distance_input.IsFinal = True
        distance_average_value = self._distance_average.Process(distance_input)

        long_distance = abs(ema_long1 - ema_medium)
        prev_long_distance = self._prev_long_distance
        self._prev_long_distance = long_distance

        if not rsi_average_value.IsFormed or not distance_average_value.IsFormed or prev_long_distance is None:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        rsi_average = float(rsi_average_value.GetValue[Decimal](None))
        distance_average = float(distance_average_value.GetValue[Decimal](None))
        close = float(candle.ClosePrice)

        strong_trend = distance > distance_average and long_distance > prev_long_distance

        long_signal = ema_short > ema_medium and ema_long1 > ema_long2 and rsi > 50 and rsi > rsi_average and strong_trend and close > ema_short
        short_signal = ema_short < ema_medium and ema_long1 < ema_long2 and rsi < 50 and rsi < rsi_average and strong_trend and close < ema_short

        if long_signal:
            if self.Position <= 0:
                self.BuyMarket(self.Volume + abs(self.Position))
        elif short_signal:
            if self.Position >= 0:
                self.SellMarket(self.Volume + abs(self.Position))
        elif self.Position > 0:
            self.SellMarket(self.Position)
        elif self.Position < 0:
            self.BuyMarket(-self.Position)

    def CreateClone(self):
        return ema_crossover_rsi_distance_strategy()
