import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import DirectionalIndex
from StockSharp.Algo.Strategies import Strategy


class adx_dmi_strategy(Strategy):
    def __init__(self):
        super(adx_dmi_strategy, self).__init__()
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Time frame for strategy calculation", "General")
        self._dmi_period = self.Param("DmiPeriod", 14) \
            .SetDisplay("DMI Period", "Directional Movement Index period", "Indicators")
        self._prev_plus = None
        self._prev_minus = None

    @property
    def candle_type(self):
        return self._candle_type.Value

    @property
    def dmi_period(self):
        return self._dmi_period.Value

    def OnReseted(self):
        super(adx_dmi_strategy, self).OnReseted()
        self._prev_plus = None
        self._prev_minus = None

    def OnStarted(self, time):
        super(adx_dmi_strategy, self).OnStarted(time)
        self._prev_plus = None
        self._prev_minus = None
        dmi = DirectionalIndex()
        dmi.Length = self.dmi_period
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(dmi, self.process_candle).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, dmi)
            self.DrawOwnTrades(area)

    def process_candle(self, candle, dmi_value):
        if candle.State != CandleStates.Finished:
            return
        current_plus = dmi_value.Plus
        current_minus = dmi_value.Minus
        if current_plus is None or current_minus is None:
            return
        current_plus = float(current_plus)
        current_minus = float(current_minus)
        if self._prev_plus is None or self._prev_minus is None:
            self._prev_plus = current_plus
            self._prev_minus = current_minus
            return
        buy_signal = self._prev_minus > self._prev_plus and current_minus <= current_plus
        sell_signal = self._prev_plus > self._prev_minus and current_plus <= current_minus
        if buy_signal and self.Position <= 0:
            self.BuyMarket()
        if sell_signal and self.Position >= 0:
            self.SellMarket()
        self._prev_plus = current_plus
        self._prev_minus = current_minus

    def CreateClone(self):
        return adx_dmi_strategy()
