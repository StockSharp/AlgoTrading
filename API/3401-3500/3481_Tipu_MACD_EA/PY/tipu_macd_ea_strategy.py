import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import MovingAverageConvergenceDivergenceSignal
from StockSharp.Algo.Strategies import Strategy

class tipu_macd_ea_strategy(Strategy):
    def __init__(self):
        super(tipu_macd_ea_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(60)))
        self._fast_period = self.Param("FastPeriod", 12)
        self._slow_period = self.Param("SlowPeriod", 26)
        self._signal_period = self.Param("SignalPeriod", 9)
        self._signal_cooldown_candles = self.Param("SignalCooldownCandles", 4)

        self._prev_macd = 0.0
        self._prev_signal = 0.0
        self._candles_since_trade = 4
        self._has_prev = False

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    @property
    def FastPeriod(self):
        return self._fast_period.Value

    @FastPeriod.setter
    def FastPeriod(self, value):
        self._fast_period.Value = value

    @property
    def SlowPeriod(self):
        return self._slow_period.Value

    @SlowPeriod.setter
    def SlowPeriod(self, value):
        self._slow_period.Value = value

    @property
    def SignalPeriod(self):
        return self._signal_period.Value

    @SignalPeriod.setter
    def SignalPeriod(self, value):
        self._signal_period.Value = value

    @property
    def SignalCooldownCandles(self):
        return self._signal_cooldown_candles.Value

    @SignalCooldownCandles.setter
    def SignalCooldownCandles(self, value):
        self._signal_cooldown_candles.Value = value

    def OnReseted(self):
        super(tipu_macd_ea_strategy, self).OnReseted()
        self._prev_macd = 0.0
        self._prev_signal = 0.0
        self._candles_since_trade = self.SignalCooldownCandles
        self._has_prev = False

    def OnStarted2(self, time):
        super(tipu_macd_ea_strategy, self).OnStarted2(time)
        self._prev_macd = 0.0
        self._prev_signal = 0.0
        self._candles_since_trade = self.SignalCooldownCandles
        self._has_prev = False

        macd = MovingAverageConvergenceDivergenceSignal()
        macd.Macd.ShortMa.Length = self.FastPeriod
        macd.Macd.LongMa.Length = self.SlowPeriod
        macd.SignalMa.Length = self.SignalPeriod

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.BindEx(macd, self._process_candle).Start()

    def _process_candle(self, candle, macd_value):
        if candle.State != CandleStates.Finished:
            return
        if not macd_value.IsFinal:
            return

        if self._candles_since_trade < self.SignalCooldownCandles:
            self._candles_since_trade += 1

        if macd_value.Macd is None or macd_value.Signal is None:
            return
        macd_main = float(macd_value.Macd)
        signal = float(macd_value.Signal)

        if self._has_prev:
            if self._prev_macd <= self._prev_signal and macd_main > signal and self.Position <= 0 and self._candles_since_trade >= self.SignalCooldownCandles:
                self.BuyMarket()
                self._candles_since_trade = 0
            elif self._prev_macd >= self._prev_signal and macd_main < signal and self.Position >= 0 and self._candles_since_trade >= self.SignalCooldownCandles:
                self.SellMarket()
                self._candles_since_trade = 0

        self._prev_macd = macd_main
        self._prev_signal = signal
        self._has_prev = True

    def CreateClone(self):
        return tipu_macd_ea_strategy()
