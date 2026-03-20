import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import AverageTrueRange
from StockSharp.Algo.Strategies import Strategy


class universal_trailing_stop_hedge_strategy(Strategy):
    def __init__(self):
        super(universal_trailing_stop_hedge_strategy, self).__init__()
        self._atr_period = self.Param("AtrPeriod", 14) \
            .SetDisplay("ATR Period", "ATR calculation period", "Indicators")
        self._atr_multiplier = self.Param("AtrMultiplier", 2.0) \
            .SetDisplay("ATR Multiplier", "ATR multiplier for stop distance", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Timeframe for calculations", "General")
        self._entry_price = 0.0
        self._trailing_stop = 0.0

    @property
    def atr_period(self):
        return self._atr_period.Value

    @property
    def atr_multiplier(self):
        return self._atr_multiplier.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(universal_trailing_stop_hedge_strategy, self).OnReseted()
        self._entry_price = 0.0
        self._trailing_stop = 0.0

    def OnStarted(self, time):
        super(universal_trailing_stop_hedge_strategy, self).OnStarted(time)
        self._entry_price = 0.0
        self._trailing_stop = 0.0
        atr = AverageTrueRange()
        atr.Length = self.atr_period
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(atr, self.process_candle).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, atr)
            self.DrawOwnTrades(area)

    def process_candle(self, candle, atr):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        atr = float(atr)
        distance = atr * float(self.atr_multiplier)
        close_price = float(candle.ClosePrice)
        open_price = float(candle.OpenPrice)
        high_price = float(candle.HighPrice)
        low_price = float(candle.LowPrice)
        if self.Position == 0:
            if close_price > open_price:
                self.BuyMarket()
                self._entry_price = close_price
                self._trailing_stop = close_price - distance
            elif close_price < open_price:
                self.SellMarket()
                self._entry_price = close_price
                self._trailing_stop = close_price + distance
            return
        if self.Position > 0:
            new_stop = close_price - distance
            if new_stop > self._trailing_stop:
                self._trailing_stop = new_stop
            if low_price <= self._trailing_stop:
                self.SellMarket()
                self._trailing_stop = 0.0
        elif self.Position < 0:
            new_stop = close_price + distance
            if new_stop < self._trailing_stop or self._trailing_stop == 0.0:
                self._trailing_stop = new_stop
            if high_price >= self._trailing_stop:
                self.BuyMarket()
                self._trailing_stop = 0.0

    def CreateClone(self):
        return universal_trailing_stop_hedge_strategy()
