import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import (
    DonchianChannels, ExponentialMovingAverage,
    MovingAverageConvergenceDivergence, MovingAverageConvergenceDivergenceSignal
)
from StockSharp.Algo.Strategies import Strategy


class parallel_strategies_strategy(Strategy):

    def __init__(self):
        super(parallel_strategies_strategy, self).__init__()

        self._donchian_period = self.Param("DonchianPeriod", 5) \
            .SetDisplay("Donchian Period", "Lookback for breakout calculation", "Indicators")
        self._macd_fast = self.Param("MacdFast", 12) \
            .SetDisplay("MACD Fast", "Fast EMA period", "Indicators")
        self._macd_slow = self.Param("MacdSlow", 26) \
            .SetDisplay("MACD Slow", "Slow EMA period", "Indicators")
        self._macd_signal = self.Param("MacdSignal", 9) \
            .SetDisplay("MACD Signal", "Signal line period", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Time frame for candles", "General")

        self._prev_upper = None
        self._prev_lower = None
        self._prev_trend = None
        self._ha_open = 0.0
        self._ha_close = 0.0
        self._ha_initialized = False

    @property
    def DonchianPeriod(self):
        return self._donchian_period.Value

    @DonchianPeriod.setter
    def DonchianPeriod(self, value):
        self._donchian_period.Value = value

    @property
    def MacdFast(self):
        return self._macd_fast.Value

    @MacdFast.setter
    def MacdFast(self, value):
        self._macd_fast.Value = value

    @property
    def MacdSlow(self):
        return self._macd_slow.Value

    @MacdSlow.setter
    def MacdSlow(self, value):
        self._macd_slow.Value = value

    @property
    def MacdSignal(self):
        return self._macd_signal.Value

    @MacdSignal.setter
    def MacdSignal(self, value):
        self._macd_signal.Value = value

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    def OnStarted(self, time):
        super(parallel_strategies_strategy, self).OnStarted(time)

        donchian = DonchianChannels()
        donchian.Length = self.DonchianPeriod

        macd_core = MovingAverageConvergenceDivergence(
            ExponentialMovingAverage(self.MacdSlow),
            ExponentialMovingAverage(self.MacdFast))
        macd = MovingAverageConvergenceDivergenceSignal(
            macd_core,
            ExponentialMovingAverage(self.MacdSignal))

        self.SubscribeCandles(self.CandleType) \
            .BindEx(donchian, macd, self.ProcessCandle) \
            .Start()

    def ProcessCandle(self, candle, donchian_value, macd_value):
        if candle.State != CandleStates.Finished:
            return

        upper_raw = donchian_value.UpperBand
        lower_raw = donchian_value.LowerBand
        if upper_raw is None or lower_raw is None:
            return

        macd_raw = macd_value.Macd
        signal_raw = macd_value.Signal
        if macd_raw is None or signal_raw is None:
            return

        upper = float(upper_raw)
        lower = float(lower_raw)
        macd_line = float(macd_raw)
        signal_line = float(signal_raw)

        ha_close_new = (float(candle.OpenPrice) + float(candle.HighPrice)
                        + float(candle.LowPrice) + float(candle.ClosePrice)) / 4.0
        if not self._ha_initialized:
            ha_open_new = (float(candle.OpenPrice) + float(candle.ClosePrice)) / 2.0
            self._ha_initialized = True
        else:
            ha_open_new = (self._ha_open + self._ha_close) / 2.0

        self._ha_open = ha_open_new
        self._ha_close = ha_close_new

        trend = 1 if ha_open_new < ha_close_new else -1

        if self._prev_trend is not None:
            if trend > 0 and self._prev_trend < 0 and macd_line > signal_line and self.Position <= 0:
                if self.Position < 0:
                    self.BuyMarket()
                self.BuyMarket()
            elif trend < 0 and self._prev_trend > 0 and macd_line < signal_line and self.Position >= 0:
                if self.Position > 0:
                    self.SellMarket()
                self.SellMarket()

        self._prev_upper = upper
        self._prev_lower = lower
        self._prev_trend = trend

    def OnReseted(self):
        super(parallel_strategies_strategy, self).OnReseted()
        self._prev_upper = None
        self._prev_lower = None
        self._prev_trend = None
        self._ha_open = 0.0
        self._ha_close = 0.0
        self._ha_initialized = False

    def CreateClone(self):
        return parallel_strategies_strategy()
