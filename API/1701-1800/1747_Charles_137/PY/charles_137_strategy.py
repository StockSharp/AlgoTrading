import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy

class charles_137_strategy(Strategy):
    """
    Charles 1.3.7 breakout strategy using symmetric price levels.
    """

    def __init__(self):
        super(charles_137_strategy, self).__init__()
        self._anchor = self.Param("Anchor", 200.0) \
            .SetDisplay("Anchor", "Distance for breakout levels", "General")
        self._trailing_profit = self.Param("TrailingProfit", 500.0) \
            .SetDisplay("Trailing Profit", "Profit target distance", "General")
        self._stop_loss = self.Param("StopLossVal", 300.0) \
            .SetDisplay("Stop Loss", "Stop loss distance", "General")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Working timeframe", "General")

        self._entry_price = 0.0
        self._buy_level = 0.0
        self._sell_level = 0.0
        self._levels_set = False

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(charles_137_strategy, self).OnReseted()
        self._entry_price = 0.0
        self._buy_level = 0.0
        self._sell_level = 0.0
        self._levels_set = False

    def OnStarted2(self, time):
        super(charles_137_strategy, self).OnStarted2(time)

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self.on_process).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def on_process(self, candle):
        if candle.State != CandleStates.Finished:
            return

        price = float(candle.ClosePrice)

        if self.Position == 0:
            if not self._levels_set:
                self._buy_level = price + self._anchor.Value
                self._sell_level = price - self._anchor.Value
                self._levels_set = True
                return

            if price >= self._buy_level:
                self.BuyMarket()
                self._entry_price = price
                self._levels_set = False
            elif price <= self._sell_level:
                self.SellMarket()
                self._entry_price = price
                self._levels_set = False
            else:
                self._buy_level = price + self._anchor.Value
                self._sell_level = price - self._anchor.Value
        elif self.Position > 0:
            profit = price - self._entry_price
            if profit >= self._trailing_profit.Value or profit <= -self._stop_loss.Value:
                self.SellMarket()
                self._levels_set = False
        elif self.Position < 0:
            profit = self._entry_price - price
            if profit >= self._trailing_profit.Value or profit <= -self._stop_loss.Value:
                self.BuyMarket()
                self._levels_set = False

    def CreateClone(self):
        return charles_137_strategy()
