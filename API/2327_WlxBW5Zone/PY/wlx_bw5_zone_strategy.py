import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import AwesomeOscillator, SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy
from indicator_extensions import *

class wlx_bw5_zone_strategy(Strategy):
    def __init__(self):
        super(wlx_bw5_zone_strategy, self).__init__()
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Type of candles", "General")
        self._direct = self.Param("Direct", True) \
            .SetDisplay("Direct", "Use direct signals", "General")
        self._signal_bar = self.Param("SignalBar", 1) \
            .SetDisplay("Signal Bar", "Bar shift for signals", "General")
        self._ao = None
        self._ao_sma = None
        self._ao0 = None
        self._ao1 = None
        self._ao2 = None
        self._ao3 = None
        self._ao4 = None
        self._ac0 = None
        self._ac1 = None
        self._ac2 = None
        self._ac3 = None
        self._ac4 = None
        self._flag_up = False
        self._flag_down = False

    @property
    def candle_type(self):
        return self._candle_type.Value

    @property
    def direct(self):
        return self._direct.Value

    @property
    def signal_bar(self):
        return self._signal_bar.Value

    def OnReseted(self):
        super(wlx_bw5_zone_strategy, self).OnReseted()
        self._ao = None
        self._ao_sma = None
        self._ao0 = self._ao1 = self._ao2 = self._ao3 = self._ao4 = None
        self._ac0 = self._ac1 = self._ac2 = self._ac3 = self._ac4 = None
        self._flag_up = False
        self._flag_down = False

    def OnStarted2(self, time):
        super(wlx_bw5_zone_strategy, self).OnStarted2(time)
        self._ao0 = self._ao1 = self._ao2 = self._ao3 = self._ao4 = None
        self._ac0 = self._ac1 = self._ac2 = self._ac3 = self._ac4 = None
        self._flag_up = False
        self._flag_down = False
        self._ao = AwesomeOscillator()
        self._ao_sma = SimpleMovingAverage()
        self._ao_sma.Length = 5
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self._ao, self.process_candle).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, self._ao)
            self.DrawOwnTrades(area)

    def process_candle(self, candle, ao):
        if candle.State != CandleStates.Finished:
            return
        ao = float(ao)
        sma_result = process_float(self._ao_sma, ao, candle.CloseTime, True)
        ac = ao - float(sma_result)
        self._ao4 = self._ao3
        self._ao3 = self._ao2
        self._ao2 = self._ao1
        self._ao1 = self._ao0
        self._ao0 = ao
        self._ac4 = self._ac3
        self._ac3 = self._ac2
        self._ac2 = self._ac1
        self._ac1 = self._ac0
        self._ac0 = ac
        if self._ao4 is None or self._ac4 is None:
            return
        is_up_seq = (self._ao0 > self._ao1 and self._ao1 > self._ao2 and
                     self._ao2 > self._ao3 and self._ao3 > self._ao4 and
                     self._ac0 > self._ac1 and self._ac1 > self._ac2 and
                     self._ac2 > self._ac3 and self._ac3 > self._ac4)
        is_down_seq = (self._ao0 < self._ao1 and self._ao1 < self._ao2 and
                       self._ao2 < self._ao3 and self._ao3 < self._ao4 and
                       self._ac0 < self._ac1 and self._ac1 < self._ac2 and
                       self._ac2 < self._ac3 and self._ac3 < self._ac4)
        if not self._flag_up and is_up_seq:
            if self.direct:
                if self.Position <= 0:
                    self.BuyMarket()
            else:
                if self.Position >= 0:
                    self.SellMarket()
            self._flag_up = True
        if not self._flag_down and is_down_seq:
            if self.direct:
                if self.Position >= 0:
                    self.SellMarket()
            else:
                if self.Position <= 0:
                    self.BuyMarket()
            self._flag_down = True
        if self._ao0 <= self._ao1 or self._ac0 <= self._ac1:
            self._flag_up = False
        if self._ao0 >= self._ao1 or self._ac0 >= self._ac1:
            self._flag_down = False

    def CreateClone(self):
        return wlx_bw5_zone_strategy()
