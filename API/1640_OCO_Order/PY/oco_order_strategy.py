import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import StandardDeviation
from StockSharp.Algo.Strategies import Strategy


class oco_order_strategy(Strategy):
    def __init__(self):
        super(oco_order_strategy, self).__init__()
        self._lookback_period = self.Param("LookbackPeriod", 20) \
            .SetDisplay("Lookback", "Bars for high/low calculation", "General")
        self._std_multiplier = self.Param("StdMultiplier", 1.5) \
            .SetDisplay("StdDev Multiplier", "Multiplier for SL/TP distance", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Type of candles", "General")
        self._recent_high = 0.0
        self._recent_low = 1e18
        self._entry_price = 0.0
        self._bar_count = 0

    @property
    def lookback_period(self):
        return self._lookback_period.Value

    @property
    def std_multiplier(self):
        return self._std_multiplier.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(oco_order_strategy, self).OnReseted()
        self._recent_high = 0.0
        self._recent_low = 1e18
        self._entry_price = 0.0
        self._bar_count = 0

    def OnStarted2(self, time):
        super(oco_order_strategy, self).OnStarted2(time)
        std_dev = StandardDeviation()
        std_dev.Length = self.lookback_period
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(std_dev, self.on_process).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def on_process(self, candle, std_value):
        if candle.State != CandleStates.Finished:
            return
        self._bar_count += 1
        # Track rolling high/low
        if self._bar_count <= self.lookback_period:
            if candle.HighPrice > self._recent_high:
                self._recent_high = candle.HighPrice
            if candle.LowPrice < self._recent_low:
                self._recent_low = candle.LowPrice
            return
        if std_value <= 0:
            return
        close = candle.ClosePrice
        sl_distance = std_value * self.std_multiplier
        # Exit logic
        if self.Position > 0:
            if close <= self._entry_price - sl_distance or close >= self._entry_price + sl_distance * 2:
                self.SellMarket()
                self._entry_price = 0
        elif self.Position < 0:
            if close >= self._entry_price + sl_distance or close <= self._entry_price - sl_distance * 2:
                self.BuyMarket()
                self._entry_price = 0
        # Entry: breakout above recent high or below recent low
        if self.Position == 0:
            if close > self._recent_high:
                self.BuyMarket()
                self._entry_price = close
            elif close < self._recent_low:
                self.SellMarket()
                self._entry_price = close
        # Update high/low
        if candle.HighPrice > self._recent_high:
            self._recent_high = candle.HighPrice
        if candle.LowPrice < self._recent_low:
            self._recent_low = candle.LowPrice

    def CreateClone(self):
        return oco_order_strategy()
