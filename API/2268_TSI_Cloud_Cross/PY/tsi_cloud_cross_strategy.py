import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import TrueStrengthIndex
from StockSharp.Algo.Strategies import Strategy


class tsi_cloud_cross_strategy(Strategy):
    def __init__(self):
        super(tsi_cloud_cross_strategy, self).__init__()
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Type of candles", "General")
        self._first_length = self.Param("FirstLength", 25) \
            .SetDisplay("First Length", "First smoothing period for TSI", "TSI")
        self._second_length = self.Param("SecondLength", 13) \
            .SetDisplay("Second Length", "Second smoothing period for TSI", "TSI")
        self._trigger_shift = self.Param("TriggerShift", 1) \
            .SetDisplay("Trigger Shift", "Bars to shift TSI for trigger", "TSI")
        self._tsi = None
        self._tsi_values = []
        self._prev_tsi = 0.0
        self._prev_trigger = 0.0
        self._is_initialized = False

    @property
    def candle_type(self):
        return self._candle_type.Value

    @property
    def first_length(self):
        return self._first_length.Value

    @property
    def second_length(self):
        return self._second_length.Value

    @property
    def trigger_shift(self):
        return self._trigger_shift.Value

    def OnReseted(self):
        super(tsi_cloud_cross_strategy, self).OnReseted()
        self._tsi = None
        self._tsi_values = []
        self._prev_tsi = 0.0
        self._prev_trigger = 0.0
        self._is_initialized = False

    def OnStarted2(self, time):
        super(tsi_cloud_cross_strategy, self).OnStarted2(time)
        self._tsi_values = []
        self._is_initialized = False
        self._tsi = TrueStrengthIndex()
        self._tsi.FirstLength = self.first_length
        self._tsi.SecondLength = self.second_length
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(self._tsi, self.process_candle).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, self._tsi)
            self.DrawOwnTrades(area)

    def process_candle(self, candle, value):
        if candle.State != CandleStates.Finished:
            return
        tsi_val = value.Tsi
        if tsi_val is None:
            return
        tsi_value = float(tsi_val)
        if not self._tsi.IsFormed:
            return
        shift = int(self.trigger_shift)
        self._tsi_values.append(tsi_value)
        if len(self._tsi_values) > shift + 1:
            self._tsi_values.pop(0)
        if len(self._tsi_values) < shift + 1:
            self._prev_tsi = tsi_value
            self._prev_trigger = tsi_value
            return
        trigger = self._tsi_values[0]
        if not self._is_initialized:
            self._prev_tsi = tsi_value
            self._prev_trigger = trigger
            self._is_initialized = True
            return
        cross_up = self._prev_tsi <= self._prev_trigger and tsi_value > trigger
        cross_down = self._prev_tsi >= self._prev_trigger and tsi_value < trigger
        self._prev_tsi = tsi_value
        self._prev_trigger = trigger
        if cross_up and self.Position <= 0:
            self.BuyMarket()
        elif cross_down and self.Position >= 0:
            self.SellMarket()

    def CreateClone(self):
        return tsi_cloud_cross_strategy()
