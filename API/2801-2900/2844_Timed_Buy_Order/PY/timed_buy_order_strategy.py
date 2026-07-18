import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy


class timed_buy_order_strategy(Strategy):
    def __init__(self):
        super(timed_buy_order_strategy, self).__init__()

        self._orders_to_place = self.Param("OrdersToPlace", 60) \
            .SetDisplay("Orders To Place", "Number of sequential buy orders before stopping", "Trading")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle type", "Candle type for strategy calculation.", "General")

        self._orders_placed = 0

    @property
    def OrdersToPlace(self):
        return self._orders_to_place.Value

    @property
    def CandleType(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(timed_buy_order_strategy, self).OnReseted()
        self._orders_placed = 0

    def OnStarted2(self, time):
        super(timed_buy_order_strategy, self).OnStarted2(time)

        self._orders_placed = 0

        sma = SimpleMovingAverage()
        sma.Length = 5

        subscription = self.SubscribeCandles(self.CandleType)
        subscription \
            .Bind(sma, self._on_process) \
            .Start()

    def _on_process(self, candle, sma_value):
        if candle.State != CandleStates.Finished:
            return
        if self._orders_placed >= self.OrdersToPlace:
            return

        self.BuyMarket()
        self._orders_placed += 1

    def CreateClone(self):
        return timed_buy_order_strategy()
