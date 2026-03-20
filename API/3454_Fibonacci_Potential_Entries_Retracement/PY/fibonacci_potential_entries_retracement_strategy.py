import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy

class fibonacci_potential_entries_retracement_strategy(Strategy):
    def __init__(self):
        super(fibonacci_potential_entries_retracement_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(30)))
        self._ema_period = self.Param("EmaPeriod", 50)
        self._lookback = self.Param("Lookback", 50)
        self._signal_cooldown_candles = self.Param("SignalCooldownCandles", 6)

        self._high = 0.0
        self._low = float('inf')
        self._bar_count = 0
        self._candles_since_trade = 6

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    @property
    def EmaPeriod(self):
        return self._ema_period.Value

    @EmaPeriod.setter
    def EmaPeriod(self, value):
        self._ema_period.Value = value

    @property
    def Lookback(self):
        return self._lookback.Value

    @Lookback.setter
    def Lookback(self, value):
        self._lookback.Value = value

    @property
    def SignalCooldownCandles(self):
        return self._signal_cooldown_candles.Value

    @SignalCooldownCandles.setter
    def SignalCooldownCandles(self, value):
        self._signal_cooldown_candles.Value = value

    def OnReseted(self):
        super(fibonacci_potential_entries_retracement_strategy, self).OnReseted()
        self._high = 0.0
        self._low = float('inf')
        self._bar_count = 0
        self._candles_since_trade = self.SignalCooldownCandles

    def OnStarted(self, time):
        super(fibonacci_potential_entries_retracement_strategy, self).OnStarted(time)
        self._high = 0.0
        self._low = float('inf')
        self._bar_count = 0
        self._candles_since_trade = self.SignalCooldownCandles

        ema = ExponentialMovingAverage()
        ema.Length = self.EmaPeriod

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(ema, self._process_candle).Start()

    def _process_candle(self, candle, ema_value):
        if candle.State != CandleStates.Finished:
            return

        if self._candles_since_trade < self.SignalCooldownCandles:
            self._candles_since_trade += 1

        high = float(candle.HighPrice)
        low = float(candle.LowPrice)
        close = float(candle.ClosePrice)
        ema_val = float(ema_value)

        if high > self._high:
            self._high = high
        if low < self._low:
            self._low = low
        self._bar_count += 1

        if self._bar_count < 20:
            return

        range_val = self._high - self._low
        if range_val <= 0:
            return

        fib618 = self._high - range_val * 0.618
        fib382 = self._high - range_val * 0.382

        if close > ema_val and close <= fib618 and self.Position <= 0 and self._candles_since_trade >= self.SignalCooldownCandles:
            self.BuyMarket()
            self._candles_since_trade = 0
        elif close < ema_val and close >= fib382 and self.Position >= 0 and self._candles_since_trade >= self.SignalCooldownCandles:
            self.SellMarket()
            self._candles_since_trade = 0

    def CreateClone(self):
        return fibonacci_potential_entries_retracement_strategy()
