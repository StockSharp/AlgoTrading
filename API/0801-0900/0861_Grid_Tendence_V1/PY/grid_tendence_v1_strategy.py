import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Decimal
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy

class grid_tendence_v1_strategy(Strategy):
    """
    Grid Tendence V1 strategy.
    Always in the market, starting long. When the open position gains Percent percent from its entry price it is closed and
    reopened in the same direction; when it loses Percent percent it is closed and a position in the opposite direction is opened.
    """

    def __init__(self):
        super(grid_tendence_v1_strategy, self).__init__()
        self._percent = self.Param("Percent", 1.0).SetGreaterThanZero().SetDisplay("Percent", "Profit or loss percent that reopens or reverses the position", "Trading")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(1))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._entry_price = 0.0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(grid_tendence_v1_strategy, self).OnReseted()
        self._entry_price = 0.0

    def OnStarted2(self, time):
        super(grid_tendence_v1_strategy, self).OnStarted2(time)

        self._entry_price = 0.0

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        close = float(candle.ClosePrice)

        if self.Position == 0:
            # The first entry is always long.
            self.BuyMarket(self.Volume)
            self._entry_price = close
            return

        if self._entry_price <= 0.0:
            self._entry_price = close
            return

        change = (close - self._entry_price) / self._entry_price * 100.0
        profit = change if self.Position > 0 else -change
        percent = float(self._percent.Value)

        if profit >= percent:
            if self.Position > 0:
                self.SellMarket(self.Position)
                self.BuyMarket(self.Volume)
            else:
                self.BuyMarket(-self.Position)
                self.SellMarket(self.Volume)
            self._entry_price = close
        elif profit <= -percent:
            if self.Position > 0:
                self.SellMarket(self.Volume + abs(self.Position))
            else:
                self.BuyMarket(self.Volume + abs(self.Position))
            self._entry_price = close

    def CreateClone(self):
        return grid_tendence_v1_strategy()
