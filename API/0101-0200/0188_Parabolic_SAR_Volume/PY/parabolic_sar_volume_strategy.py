import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Decimal
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ParabolicSar
from StockSharp.Algo.Strategies import Strategy

class parabolic_sar_volume_strategy(Strategy):
    """
    Parabolic SAR Volume strategy.
    A close above the Parabolic SAR on volume above the average of the previous VolumePeriod candles goes long and a close below it on such
    volume goes short, reversing an opposite position. The SAR is the trailing stop: a long closes when the SAR flips above price and a short
    when it flips below.
    """

    def __init__(self):
        super(parabolic_sar_volume_strategy, self).__init__()
        self._acceleration = self.Param("Acceleration", 0.02).SetGreaterThanZero().SetDisplay("SAR Acceleration", "Initial acceleration factor of the SAR", "SAR")
        self._max_acceleration = self.Param("MaxAcceleration", 0.2).SetGreaterThanZero().SetDisplay("SAR Max Acceleration", "Maximum acceleration factor of the SAR", "SAR")
        self._volume_period = self.Param("VolumePeriod", 20).SetGreaterThanZero().SetDisplay("Volume Period", "Previous candles the volume is averaged over", "Volume")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._reset_state()

    @property
    def candle_type(self):
        return self._candle_type.Value

    def _reset_state(self):
        self._volumes = []

    def OnReseted(self):
        super(parabolic_sar_volume_strategy, self).OnReseted()
        self._reset_state()

    def OnStarted2(self, time):
        super(parabolic_sar_volume_strategy, self).OnStarted2(time)

        self._reset_state()

        sar = ParabolicSar()
        sar.Acceleration = Decimal(self._acceleration.Value)
        sar.AccelerationMax = Decimal(self._max_acceleration.Value)

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(sar, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, sar)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, sar_value):
        if candle.State != CandleStates.Finished:
            return

        # Volume is compared with the candles before this one.
        period = self._volume_period.Value
        average = None
        if len(self._volumes) == period:
            total = Decimal(0)
            for volume in self._volumes:
                total += volume
            average = total / Decimal(period)

        self._volumes.append(candle.TotalVolume)
        if len(self._volumes) > period:
            self._volumes.pop(0)

        # The first SAR value is formed but empty.
        if not sar_value.IsFormed or sar_value.IsEmpty or average is None:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        sar = sar_value.GetValue[Decimal](None)
        close = candle.ClosePrice
        surge = candle.TotalVolume > average

        if close > sar and surge and self.Position <= 0:
            self.BuyMarket(self.Volume + abs(self.Position))
        elif close < sar and surge and self.Position >= 0:
            self.SellMarket(self.Volume + abs(self.Position))
        elif self.Position > 0 and close < sar:
            self.SellMarket(self.Position)
        elif self.Position < 0 and close > sar:
            self.BuyMarket(-self.Position)

    def CreateClone(self):
        return parabolic_sar_volume_strategy()
