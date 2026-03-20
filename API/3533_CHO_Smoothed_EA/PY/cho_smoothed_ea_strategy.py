import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import CommodityChannelIndex
from StockSharp.Algo.Strategies import Strategy

class cho_smoothed_ea_strategy(Strategy):
    def __init__(self):
        super(cho_smoothed_ea_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(60)))
        self._cci_period = self.Param("CciPeriod", 20)
        self._ma_period = self.Param("MaPeriod", 9)

        self._cci_history = []
        self._prev_cci = 0.0
        self._prev_signal = 0.0
        self._has_prev = False

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    @property
    def CciPeriod(self):
        return self._cci_period.Value

    @CciPeriod.setter
    def CciPeriod(self, value):
        self._cci_period.Value = value

    @property
    def MaPeriod(self):
        return self._ma_period.Value

    @MaPeriod.setter
    def MaPeriod(self, value):
        self._ma_period.Value = value

    def OnReseted(self):
        super(cho_smoothed_ea_strategy, self).OnReseted()
        self._cci_history = []
        self._prev_cci = 0.0
        self._prev_signal = 0.0
        self._has_prev = False

    def OnStarted(self, time):
        super(cho_smoothed_ea_strategy, self).OnStarted(time)
        self._cci_history = []
        self._prev_cci = 0.0
        self._prev_signal = 0.0
        self._has_prev = False

        cci = CommodityChannelIndex()
        cci.Length = self.CciPeriod

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(cci, self._process_candle).Start()

    def _process_candle(self, candle, cci_value):
        if candle.State != CandleStates.Finished:
            return

        cci_val = float(cci_value)
        ma_len = self.MaPeriod

        self._cci_history.append(cci_val)
        while len(self._cci_history) > ma_len:
            self._cci_history.pop(0)

        if len(self._cci_history) < ma_len:
            self._prev_cci = cci_val
            return

        signal_val = sum(self._cci_history) / ma_len

        if self._has_prev:
            cross_up = self._prev_cci <= self._prev_signal and cci_val > signal_val
            cross_down = self._prev_cci >= self._prev_signal and cci_val < signal_val
            min_spread = 25.0

            if cross_up and abs(cci_val - signal_val) >= min_spread and self.Position <= 0:
                self.BuyMarket()
            elif cross_down and abs(cci_val - signal_val) >= min_spread and self.Position >= 0:
                self.SellMarket()

        self._prev_cci = cci_val
        self._prev_signal = signal_val
        self._has_prev = True

    def CreateClone(self):
        return cho_smoothed_ea_strategy()
