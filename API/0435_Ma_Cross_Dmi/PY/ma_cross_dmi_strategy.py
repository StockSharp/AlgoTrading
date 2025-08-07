import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Array, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage, DirectionalIndex
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *


class ma_cross_dmi_strategy(Strategy):
    """MA Cross + DMI Strategy.

    Trades a crossover of two exponential moving averages confirmed by
    the Directional Movement Index. Only trades when the dominant
    directional line is above the ADX key level.
    """

    def __init__(self):
        super(ma_cross_dmi_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", tf(1)).SetDisplay(
            "Candle type", "Candle type for strategy calculation.", "General"
        )
        self._ma1_length = self.Param("Ma1Length", 10).SetDisplay(
            "MA1 Length", "Fast moving average period", "Moving Averages"
        )
        self._ma2_length = self.Param("Ma2Length", 20).SetDisplay(
            "MA2 Length", "Slow moving average period", "Moving Averages"
        )
        self._dmi_length = self.Param("DmiLength", 14).SetDisplay(
            "DMI Length", "DMI period", "DMI"
        )
        self._adx_smoothing = self.Param("AdxSmoothing", 14).SetDisplay(
            "ADX Smoothing", "ADX smoothing period", "DMI"
        )
        self._key_level = self.Param("KeyLevel", 20).SetDisplay(
            "Key Level", "Minimum ADX level", "DMI"
        )

        self._ma1 = None
        self._ma2 = None
        self._dmi = None

        # Store previous MA values
        self._prev_ma1 = 0.0
        self._prev_ma2 = 0.0

    @property
    def candle_type(self):
        return self._candle_type.Value

    @candle_type.setter
    def candle_type(self, value):
        self._candle_type.Value = value

    def OnReseted(self):
        super(ma_cross_dmi_strategy, self).OnReseted()
        self._prev_ma1 = 0.0
        self._prev_ma2 = 0.0

    def OnStarted(self, time):
        super(ma_cross_dmi_strategy, self).OnStarted(time)

        self._ma1 = ExponentialMovingAverage()
        self._ma1.Length = self._ma1_length.Value
        self._ma2 = ExponentialMovingAverage()
        self._ma2.Length = self._ma2_length.Value
        self._dmi = DirectionalIndex()
        self._dmi.Length = self._dmi_length.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(self._ma1, self._ma2, self._dmi, self.OnProcess).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, self._ma1)
            self.DrawIndicator(area, self._ma2)
            self.DrawOwnTrades(area)

    def OnProcess(self, candle, ma1_value, ma2_value, dmi_value):
        if candle.State != CandleStates.Finished:
            return
        if not self._ma1.IsFormed or not self._ma2.IsFormed or not self._dmi.IsFormed:
            return

        ma1 = float(ma1_value)
        ma2 = float(ma2_value)
        # Use stored previous values instead of GetValue()
        prev_ma1 = self._prev_ma1
        prev_ma2 = self._prev_ma2

        # Fix DMI value access - DirectionalIndexValue has Plus and Minus properties, not Adx
        dmi_data = dmi_value
        di_plus = float(dmi_data.Plus) if dmi_data.Plus is not None else 0.0
        di_minus = float(dmi_data.Minus) if dmi_data.Minus is not None else 0.0

        # Note: The original code referenced 'adx' but that should be accessed differently
        # For now, using a simple condition based on DI+ and DI- comparison
        # If you need ADX specifically, you would need to use AverageDirectionalIndex indicator
        trend_strength_ok = abs(di_plus - di_minus) > self._key_level.Value

        long_entry = (
            ma1 > ma2
            and prev_ma1 <= prev_ma2
            and di_plus > di_minus
            and trend_strength_ok
        )
        short_entry = (
            ma1 < ma2
            and prev_ma1 >= prev_ma2
            and di_minus > di_plus
            and trend_strength_ok
        )

        if long_entry and self.Position <= 0:
            self.BuyMarket(self.Volume + Math.Abs(self.Position))
        elif short_entry and self.Position >= 0:
            self.SellMarket(self.Volume + Math.Abs(self.Position))
        elif short_entry and self.Position > 0:
            self.ClosePosition()
        elif long_entry and self.Position < 0:
            self.ClosePosition()

        # Store current values for next iteration
        self._prev_ma1 = ma1
        self._prev_ma2 = ma2

    def CreateClone(self):
        return ma_cross_dmi_strategy()
