import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy


class lanz_6_0_backtest_strategy(Strategy):
    def __init__(self):
        super(lanz_6_0_backtest_strategy, self).__init__()
        self._enable_buy = self.Param("EnableBuy", True) \
            .SetDisplay("Enable Buy", "Allow long entries", "General")
        self._enable_sell = self.Param("EnableSell", False) \
            .SetDisplay("Enable Sell", "Allow short entries", "General")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(1))) \
            .SetDisplay("Candle Type", "Candle timeframe", "General")
        self._bar_count = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    @candle_type.setter
    def candle_type(self, value):
        self._candle_type.Value = value

    def OnReseted(self):
        super(lanz_6_0_backtest_strategy, self).OnReseted()
        self._bar_count = 0

    def OnStarted(self, time):
        super(lanz_6_0_backtest_strategy, self).OnStarted(time)
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self.OnProcess).Start()

    def OnProcess(self, candle):
        if candle.State != CandleStates.Finished:
            return
        self._bar_count += 1
        close = float(candle.ClosePrice)
        open_p = float(candle.OpenPrice)
        hour = candle.OpenTime.Hour
        if hour == 15 and self.Position != 0:
            if self.Position > 0:
                self.SellMarket()
            elif self.Position < 0:
                self.BuyMarket()
            return
        is_bull = hour == 9 and close > open_p
        is_bear = hour == 9 and close < open_p
        if is_bull and self._enable_buy.Value and self.Position <= 0:
            self.BuyMarket()
        elif is_bear and self._enable_sell.Value and self.Position >= 0:
            self.SellMarket()

    def CreateClone(self):
        return lanz_6_0_backtest_strategy()
