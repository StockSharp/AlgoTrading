import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import RateOfChange
from StockSharp.Algo.Strategies import Strategy


class laguerre_roc_strategy(Strategy):
    def __init__(self):
        super(laguerre_roc_strategy, self).__init__()
        self._period = self.Param("Period", 5) \
            .SetDisplay("Period", "Rate of change lookback", "Indicators")
        self._gamma = self.Param("Gamma", 0.5) \
            .SetDisplay("Gamma", "Laguerre smoothing factor", "Indicators")
        self._up_level = self.Param("UpLevel", 0.75) \
            .SetDisplay("Up Level", "Overbought threshold", "Indicators")
        self._down_level = self.Param("DownLevel", 0.25) \
            .SetDisplay("Down Level", "Oversold threshold", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Type of candles", "General")
        self._roc = None
        self._l0 = 0.0
        self._l1 = 0.0
        self._l2 = 0.0
        self._l3 = 0.0
        self._is_first = True
        self._prev_color = 2

    @property
    def period(self):
        return self._period.Value

    @property
    def gamma(self):
        return self._gamma.Value

    @property
    def up_level(self):
        return self._up_level.Value

    @property
    def down_level(self):
        return self._down_level.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(laguerre_roc_strategy, self).OnReseted()
        self._is_first = True
        self._prev_color = 2
        self._l0 = self._l1 = self._l2 = self._l3 = 0.0
        self._roc = None

    def OnStarted2(self, time):
        super(laguerre_roc_strategy, self).OnStarted2(time)
        self._roc = RateOfChange()
        self._roc.Length = self.period
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self._roc, self.process_candle).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, self._roc)
            self.DrawOwnTrades(area)

    def process_candle(self, candle, roc_value):
        if candle.State != CandleStates.Finished:
            return
        roc_value = float(roc_value)
        g = float(self.gamma)
        if self._is_first:
            l0 = l1 = l2 = l3 = roc_value
            self._is_first = False
        else:
            l0 = (1.0 - g) * roc_value + g * self._l0
            l1 = -g * l0 + self._l0 + g * self._l1
            l2 = -g * l1 + self._l1 + g * self._l2
            l3 = -g * l2 + self._l2 + g * self._l3
        cu = 0.0
        cd = 0.0
        if l0 >= l1:
            cu += l0 - l1
        else:
            cd += l1 - l0
        if l1 >= l2:
            cu += l1 - l2
        else:
            cd += l2 - l1
        if l2 >= l3:
            cu += l2 - l3
        else:
            cd += l3 - l2
        denom = cu + cd
        lroc = cu / denom if denom != 0 else 0.0
        up = float(self.up_level)
        down = float(self.down_level)
        color = 2
        if lroc > up:
            color = 4
        elif lroc > 0.5:
            color = 3
        if lroc < down:
            color = 0
        elif lroc < 0.5:
            color = 1
        if self._prev_color > 2 and color <= 2 and self.Position < 0:
            self.BuyMarket()
        if self._prev_color < 2 and color >= 2 and self.Position > 0:
            self.SellMarket()
        if self._prev_color == 4 and color < 4 and self.Position <= 0:
            self.BuyMarket()
        if self._prev_color == 0 and color > 0 and self.Position >= 0:
            self.SellMarket()
        self._prev_color = color
        self._l0 = l0
        self._l1 = l1
        self._l2 = l2
        self._l3 = l3

    def CreateClone(self):
        return laguerre_roc_strategy()
