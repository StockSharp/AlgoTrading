import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import AverageDirectionalIndex
from StockSharp.Algo.Strategies import Strategy

class dmi_power_move_strategy(Strategy):
    """
    Strategy based on DMI (Directional Movement Index) power moves.
    Enters long when +DI exceeds -DI by threshold and ADX is strong.
    Enters short when -DI exceeds +DI by threshold and ADX is strong.
    """

    def __init__(self):
        super(dmi_power_move_strategy, self).__init__()
        self._dmi_period = self.Param("DmiPeriod", 14) \
            .SetDisplay("DMI Period", "Period for DMI calculation", "Indicators")
        self._di_difference_threshold = self.Param("DiDifferenceThreshold", 3.0) \
            .SetDisplay("DI Difference Threshold", "Min difference between +DI and -DI", "Trading parameters")
        self._adx_threshold = self.Param("AdxThreshold", 15.0) \
            .SetDisplay("ADX Threshold", "Minimum ADX value for entry", "Trading parameters")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")

        self._prev_signal = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(dmi_power_move_strategy, self).OnReseted()
        self._prev_signal = 0

    def OnStarted(self, time):
        super(dmi_power_move_strategy, self).OnStarted(time)

        dmi = AverageDirectionalIndex()
        dmi.Length = self._dmi_period.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(dmi, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, dmi)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, dmi_value):
        if candle.State != CandleStates.Finished:
            return

        if dmi_value.MovingAverage is None:
            return
        if dmi_value.Dx is None or dmi_value.Dx.Plus is None or dmi_value.Dx.Minus is None:
            return

        adx = float(dmi_value.MovingAverage)
        plus_di = float(dmi_value.Dx.Plus)
        minus_di = float(dmi_value.Dx.Minus)

        di_diff = plus_di - minus_di

        if di_diff > self._di_difference_threshold.Value and adx > self._adx_threshold.Value:
            signal = 1
        elif di_diff < -self._di_difference_threshold.Value and adx > self._adx_threshold.Value:
            signal = -1
        else:
            signal = self._prev_signal

        if signal != self._prev_signal and signal != 0:
            if signal == 1 and self.Position <= 0:
                self.BuyMarket(self.Volume + abs(self.Position))
            elif signal == -1 and self.Position >= 0:
                self.SellMarket(self.Volume + abs(self.Position))
            self._prev_signal = signal

    def CreateClone(self):
        return dmi_power_move_strategy()
