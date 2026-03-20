import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import RelativeStrengthIndex, ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy

class universal_signal_demo_strategy(Strategy):
    def __init__(self):
        super(universal_signal_demo_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(60)))
        self._rsi_period = self.Param("RsiPeriod", 21)
        self._ema_period = self.Param("EmaPeriod", 50)
        self._signal_cooldown_candles = self.Param("SignalCooldownCandles", 4)

        self._prev_score = 0
        self._candles_since_trade = 4
        self._has_prev_score = False

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
        super(universal_signal_demo_strategy, self).OnReseted()
        self._prev_score = 0
        self._candles_since_trade = self.SignalCooldownCandles
        self._has_prev_score = False

    def OnStarted(self, time):
        super(universal_signal_demo_strategy, self).OnStarted(time)
        self._prev_score = 0
        self._candles_since_trade = self.SignalCooldownCandles
        self._has_prev_score = False

        rsi = RelativeStrengthIndex()
        rsi.Length = self.RsiPeriod
        ema = ExponentialMovingAverage()
        ema.Length = self.EmaPeriod

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(rsi, ema, self._process_candle).Start()

    def _process_candle(self, candle, rsi_value, ema_value):
        if candle.State != CandleStates.Finished:
            return

        if self._candles_since_trade < self.SignalCooldownCandles:
            self._candles_since_trade += 1

        close = float(candle.ClosePrice)
        rsi_val = float(rsi_value)
        ema_val = float(ema_value)
        score = 0

        # RSI signal
        if rsi_val < 30:
            score += 2
        elif rsi_val < 45:
            score += 1
        elif rsi_val > 70:
            score -= 2
        elif rsi_val > 55:
            score -= 1

        # EMA signal
        if close > ema_val:
            score += 1
        elif close < ema_val:
            score -= 1

        # Candle direction
        if float(candle.ClosePrice) > float(candle.OpenPrice):
            score += 1
        elif float(candle.ClosePrice) < float(candle.OpenPrice):
            score -= 1

        crossed_up = (not self._has_prev_score or self._prev_score < 2) and score >= 2
        crossed_down = (not self._has_prev_score or self._prev_score > -2) and score <= -2

        if crossed_up and self.Position <= 0 and self._candles_since_trade >= self.SignalCooldownCandles:
            self.BuyMarket()
            self._candles_since_trade = 0
        elif crossed_down and self.Position >= 0 and self._candles_since_trade >= self.SignalCooldownCandles:
            self.SellMarket()
            self._candles_since_trade = 0

        self._prev_score = score
        self._has_prev_score = True

    def CreateClone(self):
        return universal_signal_demo_strategy()
