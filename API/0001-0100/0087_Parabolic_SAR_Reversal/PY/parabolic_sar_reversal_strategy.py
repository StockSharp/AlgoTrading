import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Math, Decimal
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ParabolicSar
from StockSharp.Algo.Strategies import Strategy

class parabolic_sar_reversal_strategy(Strategy):
    """
    Parabolic SAR Reversal strategy.
    When the SAR moves from above the close to below it the position turns long, and when it moves from below to above it turns short.
    There is no other exit.
    """

    def __init__(self):
        super(parabolic_sar_reversal_strategy, self).__init__()
        self._initial_acceleration = self.Param("InitialAcceleration", 0.02).SetGreaterThanZero().SetDisplay("Initial Acceleration", "Initial acceleration factor of the SAR", "Indicators")
        self._max_acceleration = self.Param("MaxAcceleration", 0.2).SetGreaterThanZero().SetDisplay("Max Acceleration", "Maximum acceleration factor of the SAR", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(15))).SetDisplay("Candle Type", "Type of candles to use", "General")

        self._prev_sar_above = None

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(parabolic_sar_reversal_strategy, self).OnReseted()
        self._prev_sar_above = None

    def OnStarted2(self, time):
        super(parabolic_sar_reversal_strategy, self).OnStarted2(time)

        self._prev_sar_above = None

        sar = ParabolicSar()
        sar.Acceleration = Decimal(self._initial_acceleration.Value)
        sar.AccelerationMax = Decimal(self._max_acceleration.Value)

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(sar, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, sar)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, sar_value):
        if candle.State != CandleStates.Finished or not sar_value.IsFormed or sar_value.IsEmpty:
            return

        sar = sar_value.GetValue[Decimal](None)
        if sar <= 0:
            return

        is_sar_above = sar > candle.ClosePrice
        was_sar_above = self._prev_sar_above
        self._prev_sar_above = is_sar_above

        if was_sar_above is None or was_sar_above == is_sar_above or not self.IsFormedAndOnlineAndAllowTrading():
            return

        if not is_sar_above and self.Position <= 0:
            self.BuyMarket(self.Volume + Math.Abs(self.Position))
        elif is_sar_above and self.Position >= 0:
            self.SellMarket(self.Volume + Math.Abs(self.Position))

    def CreateClone(self):
        return parabolic_sar_reversal_strategy()
