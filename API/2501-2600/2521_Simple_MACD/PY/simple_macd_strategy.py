import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import MovingAverageConvergenceDivergence
from StockSharp.Algo.Strategies import Strategy

class simple_macd_strategy(Strategy):
    def __init__(self):
        super(simple_macd_strategy, self).__init__()
        self._fast_period = self.Param("FastPeriod", 12)
        self._slow_period = self.Param("SlowPeriod", 26)
        self._signal_period = self.Param("SignalPeriod", 9)
        self._trade_volume = self.Param("TradeVolume", 0.1)
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(1)))

    @property
    def CandleType(self): return self._candle_type.Value
    @CandleType.setter
    def CandleType(self, value): self._candle_type.Value = value

    def OnReseted(self):
        super(simple_macd_strategy, self).OnReseted()
        self._prev_macd = None
        self._prev_prev_macd = None
        self._prev_slope = None

    def OnStarted2(self, time):
        super(simple_macd_strategy, self).OnStarted2(time)
        self._prev_macd = None
        self._prev_prev_macd = None
        self._prev_slope = None

        self._macd = MovingAverageConvergenceDivergence()
        self._macd.ShortMa.Length = self._fast_period.Value
        self._macd.LongMa.Length = self._slow_period.Value

        sub = self.SubscribeCandles(self.CandleType)
        sub.BindEx(self._macd, self.OnProcess).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, sub)
            self.DrawIndicator(area, self._macd)
            self.DrawOwnTrades(area)

    def OnProcess(self, candle, macd_val):
        if candle.State != CandleStates.Finished:
            return

        if not self._macd.IsFormed or not macd_val.IsFinal:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        macd_line = float(macd_val)

        if self._prev_macd is None:
            self._prev_macd = macd_line
            return

        if self._prev_prev_macd is None:
            self._prev_prev_macd = self._prev_macd
            self._prev_macd = macd_line
            return

        prev = self._prev_macd
        prev_prev = self._prev_prev_macd

        if prev > prev_prev:
            current_slope = 1
        elif prev < prev_prev:
            current_slope = -1
        else:
            current_slope = 0

        if current_slope == 0:
            self._prev_prev_macd = self._prev_macd
            self._prev_macd = macd_line
            return

        if self._prev_slope == current_slope:
            self._prev_prev_macd = self._prev_macd
            self._prev_macd = macd_line
            return

        trade_vol = self._trade_volume.Value

        if current_slope > 0:
            volume_to_buy = trade_vol + max(0, -self.Position)
            if volume_to_buy > 0:
                self.BuyMarket(volume_to_buy)
        else:
            volume_to_sell = trade_vol + max(0, self.Position)
            if volume_to_sell > 0:
                self.SellMarket(volume_to_sell)

        self._prev_slope = current_slope
        self._prev_prev_macd = self._prev_macd
        self._prev_macd = macd_line

    def CreateClone(self):
        return simple_macd_strategy()
