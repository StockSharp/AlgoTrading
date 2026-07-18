import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import WeightedMovingAverage
from StockSharp.Algo.Strategies import Strategy

class template_m5_envelopes_strategy(Strategy):
    def __init__(self):
        super(template_m5_envelopes_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(30)))
        self._wma_period = self.Param("WmaPeriod", 50)
        self._deviation = self.Param("Deviation", 0.3)

        self._was_above_upper = False
        self._was_below_lower = False
        self._has_prev_signal = False

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    @property
    def WmaPeriod(self):
        return self._wma_period.Value

    @WmaPeriod.setter
    def WmaPeriod(self, value):
        self._wma_period.Value = value

    @property
    def Deviation(self):
        return self._deviation.Value

    @Deviation.setter
    def Deviation(self, value):
        self._deviation.Value = value

    def OnReseted(self):
        super(template_m5_envelopes_strategy, self).OnReseted()
        self._was_above_upper = False
        self._was_below_lower = False
        self._has_prev_signal = False

    def OnStarted2(self, time):
        super(template_m5_envelopes_strategy, self).OnStarted2(time)
        self._has_prev_signal = False

        wma = WeightedMovingAverage()
        wma.Length = self.WmaPeriod

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(wma, self._process_candle).Start()

    def _process_candle(self, candle, wma_value):
        if candle.State != CandleStates.Finished:
            return

        close = float(candle.ClosePrice)
        wma_val = float(wma_value)
        upper = wma_val * (1 + float(self.Deviation) / 100.0)
        lower = wma_val * (1 - float(self.Deviation) / 100.0)
        above_upper = close > upper
        below_lower = close < lower

        if self._has_prev_signal:
            if above_upper and not self._was_above_upper and self.Position <= 0:
                self.BuyMarket()
            elif below_lower and not self._was_below_lower and self.Position >= 0:
                self.SellMarket()

        self._was_above_upper = above_upper
        self._was_below_lower = below_lower
        self._has_prev_signal = True

    def CreateClone(self):
        return template_m5_envelopes_strategy()
