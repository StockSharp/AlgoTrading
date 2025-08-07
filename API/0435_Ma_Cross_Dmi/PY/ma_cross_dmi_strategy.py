import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
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

    @property
    def candle_type(self):
        return self._candle_type.Value

    @candle_type.setter
    def candle_type(self, value):
        self._candle_type.Value = value

    def OnStarted(self, time):
        super(ma_cross_dmi_strategy, self).OnStarted(time)

        self._ma1 = ExponentialMovingAverage()
        self._ma1.Length = self._ma1_length.Value
        self._ma2 = ExponentialMovingAverage()
        self._ma2.Length = self._ma2_length.Value
        self._dmi = DirectionalIndex()
        self._dmi.Length = self._dmi_length.Value
        self._dmi.AdxSmoothing = self._adx_smoothing.Value

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

        ma1 = ma1_value.ToDecimal()
        ma2 = ma2_value.ToDecimal()
        prev_ma1 = self._ma1.GetValue(1)
        prev_ma2 = self._ma2.GetValue(1)

        dmi_data = dmi_value
        di_plus = dmi_data.Plus
        di_minus = dmi_data.Minus
        adx = dmi_data.Adx

        long_entry = (
            ma1 > ma2
            and prev_ma1 <= prev_ma2
            and di_plus > di_minus
            and adx > self._key_level.Value
        )
        short_entry = (
            ma1 < ma2
            and prev_ma1 >= prev_ma2
            and di_minus > di_plus
            and adx > self._key_level.Value
        )

        if long_entry and self.Position <= 0:
            self.BuyMarket(self.Volume + Math.Abs(self.Position))
        elif short_entry and self.Position >= 0:
            self.SellMarket(self.Volume + Math.Abs(self.Position))
        elif short_entry and self.Position > 0:
            self.ClosePosition()
        elif long_entry and self.Position < 0:
            self.ClosePosition()

    def CreateClone(self):
        return ma_cross_dmi_strategy()
