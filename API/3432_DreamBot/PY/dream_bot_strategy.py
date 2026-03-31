import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy

class dream_bot_strategy(Strategy):
    def __init__(self):
        super(dream_bot_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(60)))
        self._ema_period = self.Param("EmaPeriod", 100)
        self._signal_cooldown_candles = self.Param("SignalCooldownCandles", 6)

        self._prev_close = 0.0
        self._has_prev_close = False
        self._was_bullish = False
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
        super(dream_bot_strategy, self).OnReseted()
        self._prev_close = 0.0
        self._has_prev_close = False
        self._was_bullish = False
        self._candles_since_trade = self.SignalCooldownCandles

    def OnStarted2(self, time):
        super(dream_bot_strategy, self).OnStarted2(time)
        self._has_prev_close = False
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

        close = float(candle.ClosePrice)
        volume = float(candle.TotalVolume)
        ema_val = float(ema_value)

        if self._has_prev_close and volume > 0:
            force_index = (close - self._prev_close) * volume
            is_bullish = force_index > 0 and close > ema_val

            if is_bullish and not self._was_bullish and self.Position <= 0 and self._candles_since_trade >= self.SignalCooldownCandles:
                self.BuyMarket()
                self._candles_since_trade = 0
            elif not is_bullish and force_index < 0 and close < ema_val and self._was_bullish and self.Position >= 0 and self._candles_since_trade >= self.SignalCooldownCandles:
                self.SellMarket()
                self._candles_since_trade = 0

            self._was_bullish = is_bullish

        self._prev_close = close
        self._has_prev_close = True

    def CreateClone(self):
        return dream_bot_strategy()
