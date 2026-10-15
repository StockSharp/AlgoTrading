import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage, AverageTrueRange
from StockSharp.Algo.Strategies import Strategy

class nnfx_auto_trade_strategy(Strategy):
    def __init__(self):
        super(nnfx_auto_trade_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(120)))
        self._ema_period = self.Param("EmaPeriod", 100)
        self._atr_period = self.Param("AtrPeriod", 14)
        self._atr_multiplier = self.Param("AtrMultiplier", 2.5)
        self._signal_cooldown_candles = self.Param("SignalCooldownCandles", 12)

        self._entry_price = 0.0
        self._best_price = 0.0
        self._was_bullish = False
        self._has_prev_signal = False
        self._candles_since_trade = 12

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
    def AtrPeriod(self):
        return self._atr_period.Value

    @AtrPeriod.setter
    def AtrPeriod(self, value):
        self._atr_period.Value = value

    @property
    def AtrMultiplier(self):
        return self._atr_multiplier.Value

    @AtrMultiplier.setter
    def AtrMultiplier(self, value):
        self._atr_multiplier.Value = value

    @property
    def SignalCooldownCandles(self):
        return self._signal_cooldown_candles.Value

    @SignalCooldownCandles.setter
    def SignalCooldownCandles(self, value):
        self._signal_cooldown_candles.Value = value

    def OnReseted(self):
        super(nnfx_auto_trade_strategy, self).OnReseted()
        self._entry_price = 0.0
        self._best_price = 0.0
        self._was_bullish = False
        self._has_prev_signal = False
        self._candles_since_trade = self.SignalCooldownCandles

    def OnStarted2(self, time):
        super(nnfx_auto_trade_strategy, self).OnStarted2(time)
        self._entry_price = 0.0
        self._best_price = 0.0
        self._has_prev_signal = False
        self._candles_since_trade = self.SignalCooldownCandles

        ema = ExponentialMovingAverage()
        ema.Length = self.EmaPeriod

        atr = AverageTrueRange()
        atr.Length = self.AtrPeriod

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(ema, atr, self._process_candle).Start()

    def _process_candle(self, candle, ema_value, atr_value):
        if candle.State != CandleStates.Finished:
            return

        close = float(candle.ClosePrice)
        stop_dist = float(atr_value) * float(self.AtrMultiplier)
        is_bullish = close > float(ema_value)

        if self._candles_since_trade < self.SignalCooldownCandles:
            self._candles_since_trade += 1

        # Trailing stop check
        if self.Position > 0:
            if close > self._best_price:
                self._best_price = close
            if self._best_price - close > stop_dist:
                self.SellMarket()
                self._entry_price = 0.0
                self._best_price = 0.0
                self._candles_since_trade = 0
                return
        elif self.Position < 0:
            if close < self._best_price:
                self._best_price = close
            if close - self._best_price > stop_dist:
                self.BuyMarket()
                self._entry_price = 0.0
                self._best_price = 0.0
                self._candles_since_trade = 0
                return

        # Entry signals
        if self._has_prev_signal and is_bullish != self._was_bullish and self._candles_since_trade >= self.SignalCooldownCandles:
            if is_bullish and self.Position <= 0:
                self.BuyMarket()
                self._entry_price = close
                self._best_price = close
                self._candles_since_trade = 0
            elif not is_bullish and self.Position >= 0:
                self.SellMarket()
                self._entry_price = close
                self._best_price = close
                self._candles_since_trade = 0

        self._was_bullish = is_bullish
        self._has_prev_signal = True

    def CreateClone(self):
        return nnfx_auto_trade_strategy()
