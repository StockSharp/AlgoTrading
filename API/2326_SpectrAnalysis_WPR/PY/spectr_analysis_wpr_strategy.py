import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import WilliamsR
from StockSharp.Algo.Strategies import Strategy


class spectr_analysis_wpr_strategy(Strategy):
    def __init__(self):
        super(spectr_analysis_wpr_strategy, self).__init__()
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Timeframe for indicator", "General")
        self._wpr_period = self.Param("WprPeriod", 13) \
            .SetDisplay("WPR Period", "Williams %R period", "Indicator")
        self._buy_pos_open = self.Param("BuyPosOpen", True) \
            .SetDisplay("Allow Long Entry", "Enable long position opening", "Trading")
        self._sell_pos_open = self.Param("SellPosOpen", True) \
            .SetDisplay("Allow Short Entry", "Enable short position opening", "Trading")
        self._buy_pos_close = self.Param("BuyPosClose", True) \
            .SetDisplay("Allow Long Exit", "Enable closing of long positions", "Trading")
        self._sell_pos_close = self.Param("SellPosClose", True) \
            .SetDisplay("Allow Short Exit", "Enable closing of short positions", "Trading")
        self._prev = None
        self._prev2 = None

    @property
    def candle_type(self):
        return self._candle_type.Value

    @property
    def wpr_period(self):
        return self._wpr_period.Value

    @property
    def buy_pos_open(self):
        return self._buy_pos_open.Value

    @property
    def sell_pos_open(self):
        return self._sell_pos_open.Value

    @property
    def buy_pos_close(self):
        return self._buy_pos_close.Value

    @property
    def sell_pos_close(self):
        return self._sell_pos_close.Value

    def OnReseted(self):
        super(spectr_analysis_wpr_strategy, self).OnReseted()
        self._prev = None
        self._prev2 = None

    def OnStarted(self, time):
        super(spectr_analysis_wpr_strategy, self).OnStarted(time)
        self._prev = None
        self._prev2 = None
        wpr = WilliamsR()
        wpr.Length = self.wpr_period
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(wpr, self.process_candle).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, wpr)
            self.DrawOwnTrades(area)

    def process_candle(self, candle, wpr_value):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        wpr_value = float(wpr_value)
        if self._prev is None or self._prev2 is None:
            self._prev2 = self._prev
            self._prev = wpr_value
            return
        if self._prev < self._prev2 and wpr_value >= self._prev:
            if self.buy_pos_open and self.Position <= 0:
                self.BuyMarket()
            elif self.sell_pos_close and self.Position < 0:
                self.BuyMarket()
        elif self._prev > self._prev2 and wpr_value <= self._prev:
            if self.sell_pos_open and self.Position >= 0:
                self.SellMarket()
            elif self.buy_pos_close and self.Position > 0:
                self.SellMarket()
        self._prev2 = self._prev
        self._prev = wpr_value

    def CreateClone(self):
        return spectr_analysis_wpr_strategy()
