import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy


class bullish_reversal_strategy(Strategy):
    def __init__(self):
        super(bullish_reversal_strategy, self).__init__()
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Type of candles", "General")
        self._ma_period = self.Param("MaPeriod", 50) \
            .SetDisplay("MA Period", "EMA length", "Parameters")
        self._prev_open1 = 0.0
        self._prev_close1 = 0.0
        self._prev_low1 = 0.0
        self._prev_open2 = 0.0
        self._prev_close2 = 0.0
        self._prev_low2 = 0.0
        self._prev_open3 = 0.0
        self._prev_close3 = 0.0
        self._prev_low3 = 0.0
        self._candle_count = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    @property
    def ma_period(self):
        return self._ma_period.Value

    def OnReseted(self):
        super(bullish_reversal_strategy, self).OnReseted()
        self._prev_open1 = 0.0
        self._prev_close1 = 0.0
        self._prev_low1 = 0.0
        self._prev_open2 = 0.0
        self._prev_close2 = 0.0
        self._prev_low2 = 0.0
        self._prev_open3 = 0.0
        self._prev_close3 = 0.0
        self._prev_low3 = 0.0
        self._candle_count = 0

    def OnStarted(self, time):
        super(bullish_reversal_strategy, self).OnStarted(time)
        ema = ExponentialMovingAverage()
        ema.Length = self.ma_period
        self.SubscribeCandles(self.candle_type).Bind(ema, self.process_candle).Start()

    def process_candle(self, candle, ma):
        if candle.State != CandleStates.Finished:
            return

        self._candle_count += 1

        if self._candle_count < 4:
            self._shift_candles(candle)
            return

        mv = float(ma)

        three_white_soldiers = (self._prev_open3 < self._prev_close3 and
                                self._prev_open2 < self._prev_close2 and
                                self._prev_open1 < self._prev_close1 and
                                self._prev_close3 < self._prev_close2 and
                                self._prev_close2 < self._prev_close1)

        three_inside_up = (self._prev_open3 > self._prev_close3 and
                           abs(self._prev_close2 - self._prev_open2) <= 0.6 * abs(self._prev_open3 - self._prev_close3) and
                           self._prev_close2 > self._prev_open2 and
                           self._prev_close1 > self._prev_open1 and
                           self._prev_close1 > self._prev_open3)

        three_black_crows = (self._prev_open3 > self._prev_close3 and
                             self._prev_open2 > self._prev_close2 and
                             self._prev_open1 > self._prev_close1 and
                             self._prev_close3 > self._prev_close2 and
                             self._prev_close2 > self._prev_close1)

        three_inside_down = (self._prev_open3 < self._prev_close3 and
                             abs(self._prev_close2 - self._prev_open2) <= 0.6 * abs(self._prev_open3 - self._prev_close3) and
                             self._prev_close2 < self._prev_open2 and
                             self._prev_close1 < self._prev_open1 and
                             self._prev_close1 < self._prev_open3)

        bull_signal = three_white_soldiers or three_inside_up
        bear_signal = three_black_crows or three_inside_down

        close = float(candle.ClosePrice)

        if bull_signal and close > mv and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket()
            self.BuyMarket()
        elif bear_signal and close < mv and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket()
            self.SellMarket()

        self._shift_candles(candle)

    def _shift_candles(self, candle):
        self._prev_open3 = self._prev_open2
        self._prev_close3 = self._prev_close2
        self._prev_low3 = self._prev_low2
        self._prev_open2 = self._prev_open1
        self._prev_close2 = self._prev_close1
        self._prev_low2 = self._prev_low1
        self._prev_open1 = float(candle.OpenPrice)
        self._prev_close1 = float(candle.ClosePrice)
        self._prev_low1 = float(candle.LowPrice)

    def CreateClone(self):
        return bullish_reversal_strategy()
