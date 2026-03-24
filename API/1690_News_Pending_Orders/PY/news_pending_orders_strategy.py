import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage, StandardDeviation
from StockSharp.Algo.Strategies import Strategy


class news_pending_orders_strategy(Strategy):
    def __init__(self):
        super(news_pending_orders_strategy, self).__init__()
        self._ema_period = self.Param("EmaPeriod", 10) \
            .SetDisplay("EMA Period", "EMA trend period", "Indicators")
        self._atr_period = self.Param("AtrPeriod", 14) \
            .SetDisplay("ATR Period", "ATR period", "Indicators")
        self._atr_mult = self.Param("AtrMult", 1.5) \
            .SetDisplay("ATR Mult", "ATR expansion multiplier", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Type of candles", "General")
        self._prev_atr = 0.0
        self._entry_price = 0.0

    @property
    def ema_period(self):
        return self._ema_period.Value

    @property
    def atr_period(self):
        return self._atr_period.Value

    @property
    def atr_mult(self):
        return self._atr_mult.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(news_pending_orders_strategy, self).OnReseted()
        self._prev_atr = 0.0
        self._entry_price = 0.0

    def OnStarted(self, time):
        super(news_pending_orders_strategy, self).OnStarted(time)
        ema = ExponentialMovingAverage()
        ema.Length = self.ema_period
        atr = StandardDeviation()
        atr.Length = self.atr_period
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(ema, atr, self.on_process).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def on_process(self, candle, ema, atr):
        if candle.State != CandleStates.Finished:
            return
        if self._prev_atr <= 0:
            self._prev_atr = atr
            return
        close = candle.ClosePrice
        body_size = abs(float(candle.ClosePrice) - float(candle.OpenPrice))
        # Volatility expansion: big body candle relative to stddev
        expansion = body_size > atr * 0.5
        if expansion and close > ema and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket()
            self.BuyMarket()
            self._entry_price = close
        elif expansion and close < ema and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket()
            self.SellMarket()
            self._entry_price = close
        # Exit long
        elif self.Position > 0:
            if close < ema or (self._entry_price > 0 and close <= self._entry_price - atr * 2):
                self.SellMarket()
                self._entry_price = 0
        # Exit short
        elif self.Position < 0:
            if close > ema or (self._entry_price > 0 and close >= self._entry_price + atr * 2):
                self.BuyMarket()
                self._entry_price = 0
        self._prev_atr = atr

    def CreateClone(self):
        return news_pending_orders_strategy()
