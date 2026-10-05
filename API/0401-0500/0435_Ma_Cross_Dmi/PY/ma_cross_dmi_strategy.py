import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Decimal
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage, DirectionalIndex, SmoothedMovingAverage
from StockSharp.Algo.Strategies import Strategy
from indicator_extensions import *


class ma_cross_dmi_strategy(Strategy):
    """
    MA Cross + DMI strategy.
    Goes long when the fast EMA crosses above the slow EMA while +DI is above -DI and ADX is above KeyLevel, and short on
    the mirrored setup. Any opposite EMA crossover closes the position, or reverses it when the DMI confirms.
    """

    def __init__(self):
        super(ma_cross_dmi_strategy, self).__init__()
        self._ma1_length = self.Param("Ma1Length", 10) \
            .SetGreaterThanZero() \
            .SetDisplay("MA1 Length", "Fast EMA period", "Moving Average")
        self._ma2_length = self.Param("Ma2Length", 20) \
            .SetGreaterThanZero() \
            .SetDisplay("MA2 Length", "Slow EMA period", "Moving Average")
        self._dmi_length = self.Param("DmiLength", 14) \
            .SetGreaterThanZero() \
            .SetDisplay("DMI Length", "Directional movement period", "DMI")
        self._adx_smoothing = self.Param("AdxSmoothing", 14) \
            .SetGreaterThanZero() \
            .SetDisplay("ADX Smoothing", "ADX smoothing period", "DMI")
        self._key_level = self.Param("KeyLevel", 20.0) \
            .SetDisplay("Key Level", "ADX level required for entries", "DMI")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(30))) \
            .SetDisplay("Candle type", "Candle type for strategy calculation", "General")

        self._adx = None
        self._prev_ma1 = None
        self._prev_ma2 = None

    @property
    def CandleType(self):
        return self._candle_type.Value

    def GetWorkingSecurities(self):
        return [(self.Security, self.CandleType)]

    def OnReseted(self):
        super(ma_cross_dmi_strategy, self).OnReseted()
        self._adx = None
        self._prev_ma1 = None
        self._prev_ma2 = None

    def OnStarted2(self, time):
        super(ma_cross_dmi_strategy, self).OnStarted2(time)

        self._prev_ma1 = None
        self._prev_ma2 = None

        ma1 = ExponentialMovingAverage()
        ma1.Length = self._ma1_length.Value
        ma2 = ExponentialMovingAverage()
        ma2.Length = self._ma2_length.Value
        dmi = DirectionalIndex()
        dmi.Length = self._dmi_length.Value
        # ADX is the DX of the DmiLength directional lines smoothed over AdxSmoothing bars.
        self._adx = SmoothedMovingAverage()
        self._adx.Length = self._adx_smoothing.Value

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.BindEx(ma1, ma2, dmi, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, ma1)
            self.DrawIndicator(area, ma2)
            self.DrawOwnTrades(area)
            dmi_area = self.CreateChartArea()
            if dmi_area is not None:
                self.DrawIndicator(dmi_area, dmi)

    def _process_candle(self, candle, ma1_value, ma2_value, dmi_value):
        if candle.State != CandleStates.Finished:
            return

        if not dmi_value.IsFormed:
            return

        if dmi_value.Plus is None or dmi_value.Minus is None:
            return

        di_plus = float(dmi_value.Plus)
        di_minus = float(dmi_value.Minus)

        di_sum = di_plus + di_minus
        dx = 0.0 if di_sum == 0 else 100.0 * abs(di_plus - di_minus) / di_sum
        adx = float(to_decimal(process_float(self._adx, dx, candle.ServerTime, True)))

        if not ma1_value.IsFormed or not ma2_value.IsFormed:
            return

        ma1 = float(ma1_value.GetValue[Decimal](None))
        ma2 = float(ma2_value.GetValue[Decimal](None))

        prev_ma1 = self._prev_ma1
        prev_ma2 = self._prev_ma2
        self._prev_ma1 = ma1
        self._prev_ma2 = ma2

        if not self._adx.IsFormed or prev_ma1 is None or prev_ma2 is None:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        key_level = float(self._key_level.Value)
        cross_up = prev_ma1 <= prev_ma2 and ma1 > ma2
        cross_down = prev_ma1 >= prev_ma2 and ma1 < ma2

        if cross_up and di_plus > di_minus and adx > key_level and self.Position <= 0:
            self.BuyMarket(self.Volume + abs(self.Position))
        elif cross_down and di_minus > di_plus and adx > key_level and self.Position >= 0:
            self.SellMarket(self.Volume + abs(self.Position))
        elif self.Position > 0 and cross_down:
            self.SellMarket(self.Position)
        elif self.Position < 0 and cross_up:
            self.BuyMarket(-self.Position)

    def CreateClone(self):
        return ma_cross_dmi_strategy()
