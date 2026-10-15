import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from System.Collections.Generic import Queue
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy


class digital_filter_t01_strategy(Strategy):
    _coeffs = [
        0.24470985659780, 0.23139774006970, 0.20613796947320, 0.17166230340640,
        0.13146907903600, 0.08950387549560, 0.04960091651250, 0.01502270569607,
        -0.01188033734430, -0.02989873856137, -0.03898967104900, -0.04014113626390,
        -0.03511968085800, -0.02611613850342, -0.01539056955666, -0.00495353651394,
        0.00368588764825, 0.00963614049782, 0.01265138888314, 0.01307496106868,
        0.01169702291063, 0.00974841844086, 0.00898900012545, -0.00649745721156
    ]

    def __init__(self):
        super(digital_filter_t01_strategy, self).__init__()
        self._half_channel = self.Param("HalfChannel", 50.0) \
            .SetDisplay("Half Channel", "Half channel distance for trigger", "Parameters")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Candle type used for the strategy", "General")
        self._prices = []
        self._prev_digital = 0.0
        self._prev_trigger = 0.0
        self._has_prev = False

    @property
    def half_channel(self):
        return self._half_channel.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(digital_filter_t01_strategy, self).OnReseted()
        self._prices = []
        self._prev_digital = 0.0
        self._prev_trigger = 0.0
        self._has_prev = False

    def OnStarted2(self, time):
        super(digital_filter_t01_strategy, self).OnStarted2(time)
        self._prices = []
        self._prev_digital = 0.0
        self._prev_trigger = 0.0
        self._has_prev = False
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self.process_candle).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return
        close = float(candle.ClosePrice)
        self._prices.append(close)
        num_coeffs = len(self._coeffs)
        if len(self._prices) > num_coeffs:
            self._prices.pop(0)
        if len(self._prices) < num_coeffs:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        digital = 0.0
        for i in range(num_coeffs):
            digital += self._coeffs[i] * self._prices[num_coeffs - 1 - i]
        prev_close = self._prices[-2]
        half_ch = float(self.half_channel)
        if digital >= prev_close:
            trigger = prev_close + half_ch
        else:
            trigger = prev_close - half_ch
        if not self._has_prev:
            self._prev_digital = digital
            self._prev_trigger = trigger
            self._has_prev = True
            return
        # Cross detection
        if self._prev_digital > self._prev_trigger and digital < trigger:
            if self.Position <= 0:
                self.BuyMarket()
        elif self._prev_digital < self._prev_trigger and digital > trigger:
            if self.Position >= 0:
                self.SellMarket()
        self._prev_digital = digital
        self._prev_trigger = trigger

    def CreateClone(self):
        return digital_filter_t01_strategy()
