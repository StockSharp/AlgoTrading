import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy


class last_price_strategy(Strategy):
    def __init__(self):
        super(last_price_strategy, self).__init__()
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Candle timeframe", "General")
        self._ma_period = self.Param("MaPeriod", 20) \
            .SetDisplay("MA Period", "Moving average period", "Parameters")
        self._distance_pct = self.Param("DistancePct", 0.5) \
            .SetDisplay("Distance %", "Percent distance from MA to trigger entry", "Parameters")
        self._entry_price = 0.0

    @property
    def candle_type(self):
        return self._candle_type.Value

    @property
    def ma_period(self):
        return self._ma_period.Value

    @property
    def distance_pct(self):
        return self._distance_pct.Value

    def OnReseted(self):
        super(last_price_strategy, self).OnReseted()
        self._entry_price = 0.0

    def OnStarted(self, time):
        super(last_price_strategy, self).OnStarted(time)
        self._entry_price = 0.0
        ma = ExponentialMovingAverage()
        ma.Length = self.ma_period
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(ma, self.process_candle).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, ma)
            self.DrawOwnTrades(area)

    def process_candle(self, candle, ma_value):
        if candle.State != CandleStates.Finished:
            return
        ma_value = float(ma_value)
        price = float(candle.ClosePrice)
        threshold = ma_value * float(self.distance_pct) / 100.0
        # Exit: price returned to MA
        if self.Position > 0 and price >= ma_value:
            self.SellMarket()
            self._entry_price = 0.0
        elif self.Position < 0 and price <= ma_value:
            self.BuyMarket()
            self._entry_price = 0.0
        # Entry: price moved away from MA
        if self.Position == 0:
            if price < ma_value - threshold:
                self.BuyMarket()
                self._entry_price = price
            elif price > ma_value + threshold:
                self.SellMarket()
                self._entry_price = price

    def CreateClone(self):
        return last_price_strategy()
