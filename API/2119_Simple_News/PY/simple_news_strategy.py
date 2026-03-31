import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import AverageTrueRange
from StockSharp.Algo.Strategies import Strategy


class simple_news_strategy(Strategy):
    def __init__(self):
        super(simple_news_strategy, self).__init__()
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Candle timeframe", "General")
        self._atr_period = self.Param("AtrPeriod", 14) \
            .SetDisplay("ATR Period", "ATR period for volatility", "Parameters")
        self._atr_multiplier = self.Param("AtrMultiplier", 1.0) \
            .SetDisplay("ATR Multiplier", "Multiplier for breakout distance", "Parameters")
        self._prev_atr = 0.0
        self._prev_high = 0.0
        self._prev_low = 0.0
        self._entry_price = 0.0
        self._has_prev = False

    @property
    def candle_type(self):
        return self._candle_type.Value

    @property
    def atr_period(self):
        return self._atr_period.Value

    @property
    def atr_multiplier(self):
        return self._atr_multiplier.Value

    def OnReseted(self):
        super(simple_news_strategy, self).OnReseted()
        self._prev_atr = 0.0
        self._prev_high = 0.0
        self._prev_low = 0.0
        self._entry_price = 0.0
        self._has_prev = False

    def OnStarted2(self, time):
        super(simple_news_strategy, self).OnStarted2(time)
        self._prev_atr = 0.0
        self._prev_high = 0.0
        self._prev_low = 0.0
        self._entry_price = 0.0
        self._has_prev = False
        atr = AverageTrueRange()
        atr.Length = self.atr_period
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(atr, self.process_candle).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, atr)
            self.DrawOwnTrades(area)

    def process_candle(self, candle, atr_ind):
        if candle.State != CandleStates.Finished:
            return
        if not atr_ind.IsFormed:
            return
        atr_value = float(atr_ind)
        price = float(candle.ClosePrice)
        # Exit logic
        if self.Position > 0 and self._entry_price > 0:
            if price <= self._entry_price - atr_value * 2.0 or price >= self._entry_price + atr_value * 3.0:
                self.SellMarket()
                self._entry_price = 0.0
        elif self.Position < 0 and self._entry_price > 0:
            if price >= self._entry_price + atr_value * 2.0 or price <= self._entry_price - atr_value * 3.0:
                self.BuyMarket()
                self._entry_price = 0.0
        if not self._has_prev:
            self._prev_high = float(candle.HighPrice)
            self._prev_low = float(candle.LowPrice)
            self._prev_atr = atr_value
            self._has_prev = True
            return
        # Entry: breakout above previous high or below previous low
        if self.Position == 0:
            breakout_dist = atr_value * float(self.atr_multiplier)
            if price > self._prev_high + breakout_dist:
                self.BuyMarket()
                self._entry_price = price
            elif price < self._prev_low - breakout_dist:
                self.SellMarket()
                self._entry_price = price
        self._prev_high = float(candle.HighPrice)
        self._prev_low = float(candle.LowPrice)
        self._prev_atr = atr_value

    def CreateClone(self):
        return simple_news_strategy()
