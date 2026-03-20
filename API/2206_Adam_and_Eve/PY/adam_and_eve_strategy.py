import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage, AverageTrueRange
from StockSharp.Algo.Strategies import Strategy


class adam_and_eve_strategy(Strategy):
    def __init__(self):
        super(adam_and_eve_strategy, self).__init__()
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")
        self._atr_period = self.Param("AtrPeriod", 14) \
            .SetDisplay("ATR Period", "Period for ATR based profit target", "Indicators")
        self._prev_ha_open = None
        self._prev_ha_close = None
        self._prev_ha_high = None
        self._prev_ha_low = None
        self._sma5_prev1 = None
        self._sma5_prev2 = None
        self._sma7_prev1 = None
        self._sma7_prev2 = None
        self._sma9_prev1 = None
        self._sma9_prev2 = None
        self._sma10_prev1 = None
        self._sma10_prev2 = None
        self._sma12_prev1 = None
        self._sma12_prev2 = None
        self._sma14_prev1 = None
        self._sma14_prev2 = None
        self._sma20_prev1 = None
        self._sma20_prev2 = None
        self._target_price = None

    @property
    def candle_type(self):
        return self._candle_type.Value

    @property
    def atr_period(self):
        return self._atr_period.Value

    def OnReseted(self):
        super(adam_and_eve_strategy, self).OnReseted()
        self._prev_ha_open = None
        self._prev_ha_close = None
        self._prev_ha_high = None
        self._prev_ha_low = None
        self._sma5_prev1 = None
        self._sma5_prev2 = None
        self._sma7_prev1 = None
        self._sma7_prev2 = None
        self._sma9_prev1 = None
        self._sma9_prev2 = None
        self._sma10_prev1 = None
        self._sma10_prev2 = None
        self._sma12_prev1 = None
        self._sma12_prev2 = None
        self._sma14_prev1 = None
        self._sma14_prev2 = None
        self._sma20_prev1 = None
        self._sma20_prev2 = None
        self._target_price = None

    def _compute_heiken_ashi(self, candle):
        o = float(candle.OpenPrice)
        h = float(candle.HighPrice)
        l = float(candle.LowPrice)
        c = float(candle.ClosePrice)
        ha_close = (o + h + l + c) / 4.0
        if self._prev_ha_open is not None:
            ha_open = (self._prev_ha_open + self._prev_ha_close) / 2.0
        else:
            ha_open = (o + c) / 2.0
        ha_high = max(h, max(ha_open, ha_close))
        ha_low = min(l, min(ha_open, ha_close))
        return ha_open, ha_close, ha_high, ha_low

    @staticmethod
    def _is_decreasing(current, prev1, prev2):
        if prev1 is None or prev2 is None:
            return False
        return current < prev1 and prev1 < prev2

    @staticmethod
    def _is_increasing(current, prev1, prev2):
        if prev1 is None or prev2 is None:
            return False
        return current > prev1 and prev1 > prev2

    def OnStarted(self, time):
        super(adam_and_eve_strategy, self).OnStarted(time)
        sma5 = ExponentialMovingAverage()
        sma5.Length = 5
        sma7 = ExponentialMovingAverage()
        sma7.Length = 7
        sma9 = ExponentialMovingAverage()
        sma9.Length = 9
        sma10 = ExponentialMovingAverage()
        sma10.Length = 10
        sma12 = ExponentialMovingAverage()
        sma12.Length = 12
        sma14 = ExponentialMovingAverage()
        sma14.Length = 14
        sma20 = ExponentialMovingAverage()
        sma20.Length = 20
        atr = AverageTrueRange()
        atr.Length = self.atr_period
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(sma5, sma7, sma9, sma10, sma12, sma14, sma20, atr, self.process_candle).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def process_candle(self, candle, v5, v7, v9, v10, v12, v14, v20, atr_val):
        if candle.State != CandleStates.Finished:
            return
        v5 = float(v5)
        v7 = float(v7)
        v9 = float(v9)
        v10 = float(v10)
        v12 = float(v12)
        v14 = float(v14)
        v20 = float(v20)
        atr_val = float(atr_val)

        if self._prev_ha_open is None:
            ha_open, ha_close, ha_high, ha_low = self._compute_heiken_ashi(candle)
            self._prev_ha_open = ha_open
            self._prev_ha_close = ha_close
            self._prev_ha_high = ha_high
            self._prev_ha_low = ha_low
            self._sma5_prev2 = self._sma5_prev1
            self._sma5_prev1 = v5
            self._sma7_prev2 = self._sma7_prev1
            self._sma7_prev1 = v7
            self._sma9_prev2 = self._sma9_prev1
            self._sma9_prev1 = v9
            self._sma10_prev2 = self._sma10_prev1
            self._sma10_prev1 = v10
            self._sma12_prev2 = self._sma12_prev1
            self._sma12_prev1 = v12
            self._sma14_prev2 = self._sma14_prev1
            self._sma14_prev1 = v14
            self._sma20_prev2 = self._sma20_prev1
            self._sma20_prev1 = v20
            return

        bearish_prev = self._prev_ha_close < self._prev_ha_open
        bullish_prev = self._prev_ha_close > self._prev_ha_open
        no_upper_wick_prev = self._prev_ha_open == self._prev_ha_high
        no_lower_wick_prev = self._prev_ha_open == self._prev_ha_low

        smas_down = (
            self._is_decreasing(v5, self._sma5_prev1, self._sma5_prev2) and
            self._is_decreasing(v7, self._sma7_prev1, self._sma7_prev2) and
            self._is_decreasing(v9, self._sma9_prev1, self._sma9_prev2) and
            self._is_decreasing(v10, self._sma10_prev1, self._sma10_prev2) and
            self._is_decreasing(v12, self._sma12_prev1, self._sma12_prev2) and
            self._is_decreasing(v14, self._sma14_prev1, self._sma14_prev2) and
            self._is_decreasing(v20, self._sma20_prev1, self._sma20_prev2)
        )
        smas_up = (
            self._is_increasing(v5, self._sma5_prev1, self._sma5_prev2) and
            self._is_increasing(v7, self._sma7_prev1, self._sma7_prev2) and
            self._is_increasing(v9, self._sma9_prev1, self._sma9_prev2) and
            self._is_increasing(v10, self._sma10_prev1, self._sma10_prev2) and
            self._is_increasing(v12, self._sma12_prev1, self._sma12_prev2) and
            self._is_increasing(v14, self._sma14_prev1, self._sma14_prev2) and
            self._is_increasing(v20, self._sma20_prev1, self._sma20_prev2)
        )

        close_price = float(candle.ClosePrice)
        high_price = float(candle.HighPrice)
        low_price = float(candle.LowPrice)

        if self.Position == 0:
            if bearish_prev and no_upper_wick_prev and smas_down:
                self.SellMarket()
                self._target_price = close_price - atr_val
            elif bullish_prev and no_lower_wick_prev and smas_up:
                self.BuyMarket()
                self._target_price = close_price + atr_val
        elif self.Position > 0 and self._target_price is not None and high_price >= self._target_price:
            self.SellMarket()
            self._target_price = None
        elif self.Position < 0 and self._target_price is not None and low_price <= self._target_price:
            self.BuyMarket()
            self._target_price = None

        ha_open, ha_close, ha_high, ha_low = self._compute_heiken_ashi(candle)
        self._prev_ha_open = ha_open
        self._prev_ha_close = ha_close
        self._prev_ha_high = ha_high
        self._prev_ha_low = ha_low

        self._sma5_prev2 = self._sma5_prev1
        self._sma5_prev1 = v5
        self._sma7_prev2 = self._sma7_prev1
        self._sma7_prev1 = v7
        self._sma9_prev2 = self._sma9_prev1
        self._sma9_prev1 = v9
        self._sma10_prev2 = self._sma10_prev1
        self._sma10_prev1 = v10
        self._sma12_prev2 = self._sma12_prev1
        self._sma12_prev1 = v12
        self._sma14_prev2 = self._sma14_prev1
        self._sma14_prev1 = v14
        self._sma20_prev2 = self._sma20_prev1
        self._sma20_prev1 = v20

    def CreateClone(self):
        return adam_and_eve_strategy()
