import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy


class breakdown_level_day_strategy(Strategy):
    def __init__(self):
        super(breakdown_level_day_strategy, self).__init__()
        self._lookback = self.Param("Lookback", 20) \
            .SetDisplay("Lookback", "Bars to establish range", "General")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Type of candles", "General")
        self._range_high = 0.0
        self._range_low = 1e18
        self._bar_count = 0
        self._range_established = False

    @property
    def lookback(self):
        return self._lookback.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(breakdown_level_day_strategy, self).OnReseted()
        self._range_high = 0.0
        self._range_low = 1e18
        self._bar_count = 0
        self._range_established = False

    def OnStarted2(self, time):
        super(breakdown_level_day_strategy, self).OnStarted2(time)
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self.on_process).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def on_process(self, candle):
        if candle.State != CandleStates.Finished:
            return
        if not self._range_established:
            if candle.HighPrice > self._range_high:
                self._range_high = candle.HighPrice
            if candle.LowPrice < self._range_low:
                self._range_low = candle.LowPrice
            self._bar_count += 1
            if self._bar_count >= self.lookback:
                self._range_established = True
            return
        price = candle.ClosePrice
        # Breakout above range high
        if price > self._range_high and self.Position <= 0:
            self.BuyMarket()
            # Reset range for next setup
            self._range_high = candle.HighPrice
            self._range_low = candle.LowPrice
            self._bar_count = 1
            self._range_established = False
        # Breakdown below range low
        elif price < self._range_low and self.Position >= 0:
            self.SellMarket()
            self._range_high = candle.HighPrice
            self._range_low = candle.LowPrice
            self._bar_count = 1
            self._range_established = False
        else:
            # Update range
            if candle.HighPrice > self._range_high:
                self._range_high = candle.HighPrice
            if candle.LowPrice < self._range_low:
                self._range_low = candle.LowPrice

    def CreateClone(self):
        return breakdown_level_day_strategy()
