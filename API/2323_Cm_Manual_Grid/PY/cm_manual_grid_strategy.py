import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy


class cm_manual_grid_strategy(Strategy):
    def __init__(self):
        super(cm_manual_grid_strategy, self).__init__()
        self._sma_period = self.Param("SmaPeriod", 50) \
            .SetDisplay("SMA Period", "Moving average period for center", "Indicators")
        self._grid_step = self.Param("GridStep", 200.0) \
            .SetDisplay("Grid Step", "Price distance between grid levels", "Grid")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Type of candles", "General")
        self._last_buy_price = 0.0
        self._last_sell_price = 0.0

    @property
    def sma_period(self):
        return self._sma_period.Value

    @property
    def grid_step(self):
        return self._grid_step.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(cm_manual_grid_strategy, self).OnReseted()
        self._last_buy_price = 0.0
        self._last_sell_price = 0.0

    def OnStarted(self, time):
        super(cm_manual_grid_strategy, self).OnStarted(time)
        self._last_buy_price = 0.0
        self._last_sell_price = 0.0
        sma = SimpleMovingAverage()
        sma.Length = self.sma_period
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(sma, self.process_candle).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, sma)
            self.DrawOwnTrades(area)

    def process_candle(self, candle, sma_value):
        if candle.State != CandleStates.Finished:
            return
        price = float(candle.ClosePrice)
        sma_value = float(sma_value)
        step = float(self.grid_step)
        if price < sma_value - step:
            if self._last_buy_price == 0.0 or price <= self._last_buy_price - step:
                self.BuyMarket()
                self._last_buy_price = price
        if price > sma_value + step:
            if self._last_sell_price == 0.0 or price >= self._last_sell_price + step:
                self.SellMarket()
                self._last_sell_price = price
        if price > sma_value and self._last_buy_price != 0.0:
            self._last_buy_price = 0.0
        if price < sma_value and self._last_sell_price != 0.0:
            self._last_sell_price = 0.0

    def CreateClone(self):
        return cm_manual_grid_strategy()
