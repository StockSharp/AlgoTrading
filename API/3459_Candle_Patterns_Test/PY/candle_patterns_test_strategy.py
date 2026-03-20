import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy

class candle_patterns_test_strategy(Strategy):
    def __init__(self):
        super(candle_patterns_test_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(60)))
        self._ema_period = self.Param("EmaPeriod", 50)
        self._signal_cooldown_candles = self.Param("SignalCooldownCandles", 6)

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
    def SignalCooldownCandles(self):
        return self._signal_cooldown_candles.Value

    @SignalCooldownCandles.setter
    def SignalCooldownCandles(self, value):
        self._signal_cooldown_candles.Value = value

    def OnReseted(self):
        super(candle_patterns_test_strategy, self).OnReseted()
        self._candles_since_trade = self.SignalCooldownCandles

    def OnStarted(self, time):
        super(candle_patterns_test_strategy, self).OnStarted(time)
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

        body = abs(float(candle.ClosePrice) - float(candle.OpenPrice))
        range_val = float(candle.HighPrice) - float(candle.LowPrice)
        if range_val <= 0:
            return

        is_doji = body < range_val * 0.05
        close = float(candle.ClosePrice)
        ema_val = float(ema_value)

        if is_doji and close < ema_val and self.Position <= 0 and self._candles_since_trade >= self.SignalCooldownCandles:
            self.BuyMarket()
            self._candles_since_trade = 0
        elif is_doji and close > ema_val and self.Position >= 0 and self._candles_since_trade >= self.SignalCooldownCandles:
            self.SellMarket()
            self._candles_since_trade = 0

    def CreateClone(self):
        return candle_patterns_test_strategy()
