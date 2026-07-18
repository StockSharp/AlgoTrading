import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy


class cm_fishing_strategy(Strategy):
    def __init__(self):
        super(cm_fishing_strategy, self).__init__()
        self._step_size = self.Param("StepSize", 500.0) \
            .SetDisplay("Step Size", "Price step for grid entries", "Parameters")
        self._profit_target = self.Param("ProfitTarget", 300.0) \
            .SetDisplay("Profit Target", "Price profit to close position", "Parameters")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Candle timeframe", "General")
        self._reference_price = 0.0
        self._entry_price = 0.0

    @property
    def step_size(self):
        return self._step_size.Value

    @property
    def profit_target(self):
        return self._profit_target.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(cm_fishing_strategy, self).OnReseted()
        self._reference_price = 0.0
        self._entry_price = 0.0

    def OnStarted2(self, time):
        super(cm_fishing_strategy, self).OnStarted2(time)
        self._reference_price = 0.0
        self._entry_price = 0.0
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self.process_candle).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return
        price = float(candle.ClosePrice)
        if self._reference_price == 0.0:
            self._reference_price = price
            return
        step_size = float(self.step_size)
        profit_target = float(self.profit_target)
        if self.Position > 0 and price >= self._entry_price + profit_target:
            self.SellMarket()
            self._reference_price = price
            self._entry_price = 0.0
            return
        elif self.Position < 0 and price <= self._entry_price - profit_target:
            self.BuyMarket()
            self._reference_price = price
            self._entry_price = 0.0
            return
        if price <= self._reference_price - step_size and self.Position <= 0:
            self.BuyMarket()
            self._entry_price = price
            self._reference_price = price
        elif price >= self._reference_price + step_size and self.Position >= 0:
            self.SellMarket()
            self._entry_price = price
            self._reference_price = price

    def CreateClone(self):
        return cm_fishing_strategy()
