import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy


class z_strike_recovery_strategy(Strategy):
    def __init__(self):
        super(z_strike_recovery_strategy, self).__init__()
        self._z_length = self.Param("ZLength", 16) \
            .SetDisplay("Z-Score Length", "Lookback length for z-score", "Indicators")
        self._z_threshold = self.Param("ZThreshold", 2.5) \
            .SetDisplay("Z-Score Threshold", "Entry threshold", "Trading")
        self._exit_periods = self.Param("ExitPeriods", 10) \
            .SetDisplay("Exit Periods", "Bars to hold position", "Trading")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Type of candles", "General")
        self._prev_close = 0.0
        self._bars_in_position = 0

    @property
    def z_length(self):
        return self._z_length.Value

    @property
    def z_threshold(self):
        return self._z_threshold.Value

    @property
    def exit_periods(self):
        return self._exit_periods.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(z_strike_recovery_strategy, self).OnReseted()
        self._prev_close = 0.0
        self._bars_in_position = 0

    def OnStarted(self, time):
        super(z_strike_recovery_strategy, self).OnStarted(time)
        sma = SimpleMovingAverage()
        sma.Length = 10
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(sma, self.on_process).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def on_process(self, candle, _dummy):
        if candle.State != CandleStates.Finished:
            return
        if self._prev_close == 0:
            self._prev_close = candle.ClosePrice
            return
        change = candle.ClosePrice - self._prev_close
        self._prev_close = candle.ClosePrice
        self._changes.append(change)
        if len(self._changes) > self.z_length * 2:
            self._changes.pop(0)
        # Position management: exit after N bars
        if self.Position != 0:
            self._bars_in_position += 1
            if self._bars_in_position >= self.exit_periods:
                if self.Position > 0:
                    self.SellMarket()
                else:
                    self.BuyMarket()
                self._bars_in_position = 0
        if len(self._changes) < self.z_length:
            return
        # Compute Z-score
        recent = self._changes.Skip(len(self._changes) - self.z_length).ToList()
        mean = recent.Average()
        sum_sq = recent.Sum(v => (v - mean) * (v - mean))
        std = float(Math.Sqrt((double)(sum_sq / self.z_length)))
        if std == 0:
            return
        z = (change - mean) / std
        # Entry: Z-score spike above threshold (strong upward move)
        if z > self.z_threshold and self.Position == 0:
            self.BuyMarket()
            self._bars_in_position = 0
        # Also allow short on extreme negative Z-score
        elif z < -self.z_threshold and self.Position == 0:
            self.SellMarket()
            self._bars_in_position = 0

    def CreateClone(self):
        return z_strike_recovery_strategy()
