import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy


class inw_coin_martingale_strategy(Strategy):
    def __init__(self):
        super(inw_coin_martingale_strategy, self).__init__()
        self._take_profit_percent = self.Param("TakeProfitPercent", 2.0) \
            .SetDisplay("Take Profit %", "Profit percent to exit", "Parameters")
        self._martingale_percent = self.Param("MartingalePercent", 5.0) \
            .SetDisplay("Martingale %", "Price drop percent for averaging", "Parameters")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Type of candles", "General")
        self._avg_price = 0.0
        self._martingale_count = 0
        self._prev_histogram = 0.0
        self._is_first = True

    @property
    def candle_type(self):
        return self._candle_type.Value

    @candle_type.setter
    def candle_type(self, value):
        self._candle_type.Value = value

    def OnReseted(self):
        super(inw_coin_martingale_strategy, self).OnReseted()
        self._avg_price = 0.0
        self._martingale_count = 0
        self._prev_histogram = 0.0
        self._is_first = True

    def OnStarted2(self, time):
        super(inw_coin_martingale_strategy, self).OnStarted2(time)
        ema_short = ExponentialMovingAverage()
        ema_short.Length = 12
        ema_long = ExponentialMovingAverage()
        ema_long.Length = 26
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(ema_short, ema_long, self.OnProcess).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, ema_short)
            self.DrawIndicator(area, ema_long)
            self.DrawOwnTrades(area)

    def OnProcess(self, candle, ema_short_val, ema_long_val):
        if candle.State != CandleStates.Finished:
            return
        price = float(candle.ClosePrice)
        histogram = float(ema_short_val) - float(ema_long_val)
        if self._is_first:
            self._prev_histogram = histogram
            self._is_first = False
            return
        buy_signal = self._prev_histogram <= 0 and histogram > 0
        if buy_signal and self.Position <= 0 and self._martingale_count == 0:
            self.BuyMarket()
            self._avg_price = price
            self._martingale_count = 1
        elif self.Position > 0 and self._avg_price > 0:
            drop = (price - self._avg_price) / self._avg_price * 100.0
            mart_pct = float(self._martingale_percent.Value)
            if drop <= -mart_pct and self._martingale_count < 5:
                self.BuyMarket()
                self._avg_price = (self._avg_price * self.Position + price) / (self.Position + 1)
                self._martingale_count += 1
            profit = (price - self._avg_price) / self._avg_price * 100.0
            tp = float(self._take_profit_percent.Value)
            if profit >= tp:
                self.SellMarket()
                self._martingale_count = 0
                self._avg_price = 0.0
        self._prev_histogram = histogram

    def CreateClone(self):
        return inw_coin_martingale_strategy()
