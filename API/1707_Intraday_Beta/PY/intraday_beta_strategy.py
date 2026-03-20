import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import RelativeStrengthIndex, SimpleMovingAverage, StandardDeviation
from StockSharp.Algo.Strategies import Strategy


class intraday_beta_strategy(Strategy):
    def __init__(self):
        super(intraday_beta_strategy, self).__init__()
        self._rsi_period = self.Param("RsiPeriod", 9) \
            .SetDisplay("RSI Period", "RSI period", "Indicators")
        self._atr_period = self.Param("AtrPeriod", 14) \
            .SetDisplay("ATR Period", "ATR period for trailing", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Type of candles", "General")
        self._prev_ma10 = 0.0
        self._prev_slope = 0.0
        self._prev_candle_diff = 0.0
        self._long_stop = 0.0
        self._short_stop = 0.0
        self._entry_price = 0.0

    @property
    def rsi_period(self):
        return self._rsi_period.Value

    @property
    def atr_period(self):
        return self._atr_period.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(intraday_beta_strategy, self).OnReseted()
        self._prev_ma10 = 0.0
        self._prev_slope = 0.0
        self._prev_candle_diff = 0.0
        self._long_stop = 0.0
        self._short_stop = 0.0
        self._entry_price = 0.0

    def OnStarted(self, time):
        super(intraday_beta_strategy, self).OnStarted(time)
        ma10 = SimpleMovingAverage()
        ma10.Length = 10
        rsi = RelativeStrengthIndex()
        rsi.Length = self.rsi_period
        atr = StandardDeviation()
        atr.Length = self.atr_period
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(ma10, rsi, atr, self.on_process).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def on_process(self, candle, ma10_value, rsi_value, atr_value):
        if candle.State != CandleStates.Finished:
            return
        if self._prev_ma10 == 0:
            ma10_slope = ma10_value - self._prev_ma10
        candle_diff = candle.ClosePrice - candle.OpenPrice
        trail_dist = (atr_value * 2 if atr_value > 0 else 100)
        sell_signal = ma10_slope < 0 and self._prev_slope > 0 and rsi_value >= 30 and self._prev_candle_diff < 0
        buy_signal = ma10_slope > 0 and self._prev_slope < 0 and rsi_value <= 70 and self._prev_candle_diff > 0
        if sell_signal and self.Position >= 0:
            if self.Position > 0) SellMarket(:
                self.SellMarket()
            self._entry_price = candle.ClosePrice
            self._short_stop = self._entry_price + trail_dist
        elif buy_signal and self.Position <= 0:
            if self.Position < 0) BuyMarket(:
                self.BuyMarket()
            self._entry_price = candle.ClosePrice
            self._long_stop = self._entry_price - trail_dist
        if self.Position > 0:
            new_stop = candle.ClosePrice - trail_dist
            if new_stop > self._long_stop and candle.ClosePrice > self._entry_price:
                self._long_stop = new_stop
            if candle.LowPrice <= self._long_stop) SellMarket(:
            elif self.Position < 0:
            new_stop = candle.ClosePrice + trail_dist
            if new_stop < self._short_stop and candle.ClosePrice < self._entry_price:
                self._short_stop = new_stop
            if candle.HighPrice >= self._short_stop) BuyMarket(:
            self._prev_ma10 = ma10_value
        self._prev_slope = ma10_slope
        self._prev_candle_diff = candle_diff

    def CreateClone(self):
        return intraday_beta_strategy()
