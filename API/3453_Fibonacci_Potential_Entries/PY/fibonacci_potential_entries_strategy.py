import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import RelativeStrengthIndex
from StockSharp.Algo.Strategies import Strategy

class fibonacci_potential_entries_strategy(Strategy):
    def __init__(self):
        super(fibonacci_potential_entries_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(30)))
        self._rsi_period = self.Param("RsiPeriod", 14)
        self._signal_cooldown_candles = self.Param("SignalCooldownCandles", 6)

        self._highest_high = 0.0
        self._lowest_low = float('inf')
        self._prev_close = 0.0
        self._bar_count = 0
        self._candles_since_trade = 6
        self._has_prev_close = False

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    @property
    def RsiPeriod(self):
        return self._rsi_period.Value

    @RsiPeriod.setter
    def RsiPeriod(self, value):
        self._rsi_period.Value = value

    @property
    def SignalCooldownCandles(self):
        return self._signal_cooldown_candles.Value

    @SignalCooldownCandles.setter
    def SignalCooldownCandles(self, value):
        self._signal_cooldown_candles.Value = value

    def OnReseted(self):
        super(fibonacci_potential_entries_strategy, self).OnReseted()
        self._highest_high = 0.0
        self._lowest_low = float('inf')
        self._prev_close = 0.0
        self._bar_count = 0
        self._candles_since_trade = self.SignalCooldownCandles
        self._has_prev_close = False

    def OnStarted(self, time):
        super(fibonacci_potential_entries_strategy, self).OnStarted(time)
        self._highest_high = 0.0
        self._lowest_low = float('inf')
        self._prev_close = 0.0
        self._bar_count = 0
        self._candles_since_trade = self.SignalCooldownCandles
        self._has_prev_close = False

        rsi = RelativeStrengthIndex()
        rsi.Length = self.RsiPeriod

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(rsi, self._process_candle).Start()

    def _process_candle(self, candle, rsi_value):
        if candle.State != CandleStates.Finished:
            return

        if self._candles_since_trade < self.SignalCooldownCandles:
            self._candles_since_trade += 1

        high = float(candle.HighPrice)
        low = float(candle.LowPrice)
        close = float(candle.ClosePrice)
        rsi_val = float(rsi_value)

        if high > self._highest_high:
            self._highest_high = high
        if low < self._lowest_low:
            self._lowest_low = low
        self._bar_count += 1

        if self._bar_count < 20:
            self._prev_close = close
            self._has_prev_close = True
            return

        range_val = self._highest_high - self._lowest_low
        if range_val <= 0:
            self._prev_close = close
            self._has_prev_close = True
            return

        fib382 = self._highest_high - range_val * 0.382
        fib618 = self._highest_high - range_val * 0.618

        crossed_into_buy_zone = self._has_prev_close and self._prev_close > fib618 and close <= fib618
        crossed_into_sell_zone = self._has_prev_close and self._prev_close < fib382 and close >= fib382

        if crossed_into_buy_zone and rsi_val < 40 and self.Position <= 0 and self._candles_since_trade >= self.SignalCooldownCandles:
            self.BuyMarket()
            self._candles_since_trade = 0
        elif crossed_into_sell_zone and rsi_val > 60 and self.Position >= 0 and self._candles_since_trade >= self.SignalCooldownCandles:
            self.SellMarket()
            self._candles_since_trade = 0

        self._prev_close = close
        self._has_prev_close = True

    def CreateClone(self):
        return fibonacci_potential_entries_strategy()
