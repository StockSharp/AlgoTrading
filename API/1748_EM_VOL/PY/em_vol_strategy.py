import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import StandardDeviation
from StockSharp.Algo.Strategies import Strategy

class em_vol_strategy(Strategy):
    """
    Pivot breakout strategy with StdDev volatility filter.
    Enters when price breaks previous high/low + volatility band.
    """

    def __init__(self):
        super(em_vol_strategy, self).__init__()
        self._take_profit = self.Param("TakeProfit", 1000.0) \
            .SetDisplay("Take Profit", "Take profit distance", "Risk")
        self._stop_loss = self.Param("StopLoss", 500.0) \
            .SetDisplay("Stop Loss", "Stop loss distance", "Risk")
        self._stdev_period = self.Param("StdevPeriod", 14) \
            .SetDisplay("StdDev Period", "Volatility period", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Working candle timeframe", "General")

        self._prev_high = 0.0
        self._prev_low = 0.0
        self._prev_stdev = 0.0
        self._has_prev = False
        self._entry_price = 0.0
        self._stop_price = 0.0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(em_vol_strategy, self).OnReseted()
        self._prev_high = 0.0
        self._prev_low = 0.0
        self._prev_stdev = 0.0
        self._has_prev = False
        self._entry_price = 0.0
        self._stop_price = 0.0

    def OnStarted(self, time):
        super(em_vol_strategy, self).OnStarted(time)

        stdev = StandardDeviation()
        stdev.Length = self._stdev_period.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(stdev, self._process_candle).Start()

    def _process_candle(self, candle, stdev_val):
        if candle.State != CandleStates.Finished:
            return

        stdev_val = float(stdev_val)

        if not self._has_prev:
            self._prev_high = float(candle.HighPrice)
            self._prev_low = float(candle.LowPrice)
            self._prev_stdev = stdev_val
            self._has_prev = True
            return

        price = float(candle.ClosePrice)
        res1 = self._prev_high + self._prev_stdev
        sup1 = self._prev_low - self._prev_stdev

        if self.Position == 0:
            if price > res1:
                self.BuyMarket()
                self._entry_price = price
                self._stop_price = price - self._stop_loss.Value
            elif price < sup1:
                self.SellMarket()
                self._entry_price = price
                self._stop_price = price + self._stop_loss.Value
        elif self.Position > 0:
            if price - self._entry_price >= self._take_profit.Value or price <= self._stop_price:
                self.SellMarket()
        elif self.Position < 0:
            if self._entry_price - price >= self._take_profit.Value or price >= self._stop_price:
                self.BuyMarket()

        self._prev_high = float(candle.HighPrice)
        self._prev_low = float(candle.LowPrice)
        self._prev_stdev = stdev_val

    def CreateClone(self):
        return em_vol_strategy()
