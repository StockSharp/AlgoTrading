import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import AwesomeOscillator
from StockSharp.Algo.Strategies import Strategy


class bw_wise_man2_strategy(Strategy):
    def __init__(self):
        super(bw_wise_man2_strategy, self).__init__()
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Candle type", "General")
        self._ao0 = 0.0
        self._ao1 = 0.0
        self._ao2 = 0.0
        self._ao_count = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(bw_wise_man2_strategy, self).OnReseted()
        self._ao0 = 0.0
        self._ao1 = 0.0
        self._ao2 = 0.0
        self._ao_count = 0

    def OnStarted2(self, time):
        super(bw_wise_man2_strategy, self).OnStarted2(time)
        ao = AwesomeOscillator()
        self.SubscribeCandles(self.candle_type) \
            .Bind(ao, self.process_candle) \
            .Start()

    def process_candle(self, candle, ao_value):
        if candle.State != CandleStates.Finished:
            return
        ao_value = float(ao_value)
        self._ao2 = self._ao1
        self._ao1 = self._ao0
        self._ao0 = ao_value
        if self._ao_count < 3:
            self._ao_count += 1
            return
        buy_signal = (self._ao2 < 0 and self._ao1 < 0 and self._ao0 > 0) or \
                     (self._ao2 < self._ao1 and self._ao1 < self._ao0 and self._ao0 > 0)
        sell_signal = (self._ao2 > 0 and self._ao1 > 0 and self._ao0 < 0) or \
                      (self._ao2 > self._ao1 and self._ao1 > self._ao0 and self._ao0 < 0)
        if buy_signal and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket()
            self.BuyMarket()
        elif sell_signal and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket()
            self.SellMarket()

    def CreateClone(self):
        return bw_wise_man2_strategy()
