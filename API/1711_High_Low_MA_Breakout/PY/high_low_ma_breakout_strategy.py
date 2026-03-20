import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy


class high_low_ma_breakout_strategy(Strategy):
    def __init__(self):
        super(high_low_ma_breakout_strategy, self).__init__()
        self._ma_high_period = self.Param("MaHighPeriod", 14) \
            .SetDisplay("High MA Period", "Period of high price MA", "Parameters")
        self._ma_low_period = self.Param("MaLowPeriod", 10) \
            .SetDisplay("Low MA Period", "Period of low price MA", "Parameters")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Type of candles", "General")
        self._prev_ma_high = 0.0
        self._prev_ma_low = 0.0
        self._prev_close = 0.0
        self._has_prev = False

    @property
    def ma_high_period(self):
        return self._ma_high_period.Value

    @property
    def ma_low_period(self):
        return self._ma_low_period.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(high_low_ma_breakout_strategy, self).OnReseted()
        self._prev_ma_high = 0.0
        self._prev_ma_low = 0.0
        self._prev_close = 0.0
        self._has_prev = False

    def OnStarted(self, time):
        super(high_low_ma_breakout_strategy, self).OnStarted(time)
        ma_high = SimpleMovingAverage()
        ma_high.Length = self.ma_high_period
        ma_low = SimpleMovingAverage()
        ma_low.Length = self.ma_low_period
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(ma_high, ma_low, self.on_process).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def on_process(self, candle, ma_high, ma_low):
        if candle.State != CandleStates.Finished:
            return
        close = candle.ClosePrice
        if not self._has_prev:
            self._prev_close = close
            self._prev_ma_high = ma_high
            self._prev_ma_low = ma_low
            self._has_prev = True
            return
        # Cross above high MA => buy
        if self._prev_close <= self._prev_ma_high and close > ma_high and self.Position <= 0:
            if self.Position < 0) BuyMarket(:
                self.BuyMarket()
        # Cross below low MA => sell
        elif self._prev_close >= self._prev_ma_low and close < ma_low and self.Position >= 0:
            if self.Position > 0) SellMarket(:
                self.SellMarket()
        self._prev_close = close
        self._prev_ma_high = ma_high
        self._prev_ma_low = ma_low

    def CreateClone(self):
        return high_low_ma_breakout_strategy()
