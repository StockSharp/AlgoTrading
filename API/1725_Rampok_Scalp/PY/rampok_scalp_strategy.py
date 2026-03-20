import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy


class rampok_scalp_strategy(Strategy):
    def __init__(self):
        super(rampok_scalp_strategy, self).__init__()
        self._period = self.Param("Period", 15) \
            .SetDisplay("Period", "Moving average period", "General")
        self._deviation = self.Param("Deviation", 0.07) \
            .SetDisplay("Deviation", "Envelope deviation percent", "General")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle", "Candle type", "General")
        self._prev_upper = 0.0
        self._prev_lower = 0.0
        self._prev_close = 0.0
        self._entry_price = 0.0
        self._highest_price = 0.0
        self._lowest_price = 0.0
        self._has_prev = False

    @property
    def period(self):
        return self._period.Value

    @property
    def deviation(self):
        return self._deviation.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(rampok_scalp_strategy, self).OnReseted()
        self._prev_upper = 0.0
        self._prev_lower = 0.0
        self._prev_close = 0.0
        self._entry_price = 0.0
        self._highest_price = 0.0
        self._lowest_price = 0.0
        self._has_prev = False

    def OnStarted(self, time):
        super(rampok_scalp_strategy, self).OnStarted(time)
        sma = SimpleMovingAverage()
        sma.Length = self.period
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(sma, self.on_process).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def on_process(self, candle, sma_value):
        if candle.State != CandleStates.Finished:
            return
        upper = sma_value * (1 + self.deviation)
        lower = sma_value * (1 - self.deviation)
        close = candle.ClosePrice
        if not self._has_prev:
            self._prev_upper = upper
            self._prev_lower = lower
            self._prev_close = close
            self._has_prev = True
            return
        if self.Position == 0:
            if self._prev_close < self._prev_lower and close > lower:
                self.BuyMarket()
                self._entry_price = close
                self._highest_price = close
            elif self._prev_close > self._prev_upper and close < upper:
                self.SellMarket()
                self._entry_price = close
                self._lowest_price = close
        elif self.Position > 0:
            self._highest_price = max(self._highest_price, candle.HighPrice)
            # Exit at upper band or trailing
            if close >= upper:
                self.SellMarket()
        elif self.Position < 0:
            self._lowest_price = min(self._lowest_price, candle.LowPrice)
            # Exit at lower band or trailing
            if close <= lower:
                self.BuyMarket()
        self._prev_upper = upper
        self._prev_lower = lower
        self._prev_close = close

    def CreateClone(self):
        return rampok_scalp_strategy()
