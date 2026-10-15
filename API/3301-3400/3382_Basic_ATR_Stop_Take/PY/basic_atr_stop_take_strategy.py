import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage, AverageTrueRange
from StockSharp.Algo.Strategies import Strategy


class basic_atr_stop_take_strategy(Strategy):
    def __init__(self):
        super(basic_atr_stop_take_strategy, self).__init__()

        self._ema_period = self.Param("EmaPeriod", 50) \
            .SetDisplay("EMA Period", "EMA trend period", "Indicators")
        self._atr_period = self.Param("AtrPeriod", 14) \
            .SetDisplay("ATR Period", "ATR period", "Indicators")
        self._stop_factor = self.Param("StopFactor", 1.5) \
            .SetDisplay("Stop Factor", "ATR multiplier for stop loss", "Risk")
        self._take_factor = self.Param("TakeFactor", 2.0) \
            .SetDisplay("Take Factor", "ATR multiplier for take profit", "Risk")
        self._signal_cooldown = self.Param("SignalCooldownCandles", 6) \
            .SetDisplay("Signal Cooldown", "Bars to wait between entries", "Trading")

        self._ema = None
        self._atr = None
        self._entry_price = 0.0
        self._stop_price = 0.0
        self._take_price = 0.0
        self._prev_above_ema = False
        self._has_prev_signal = False
        self._candles_since_trade = 0

    @property
    def ema_period(self):
        return self._ema_period.Value

    @property
    def atr_period(self):
        return self._atr_period.Value

    @property
    def stop_factor(self):
        return self._stop_factor.Value

    @property
    def take_factor(self):
        return self._take_factor.Value

    @property
    def signal_cooldown(self):
        return self._signal_cooldown.Value

    def OnReseted(self):
        super(basic_atr_stop_take_strategy, self).OnReseted()
        self._ema = None
        self._atr = None
        self._entry_price = 0.0
        self._stop_price = 0.0
        self._take_price = 0.0
        self._prev_above_ema = False
        self._has_prev_signal = False
        self._candles_since_trade = self.signal_cooldown

    def OnStarted2(self, time):
        super(basic_atr_stop_take_strategy, self).OnStarted2(time)

        self._ema = ExponentialMovingAverage()
        self._ema.Length = self.ema_period
        self._atr = AverageTrueRange()
        self._atr.Length = self.atr_period
        self._entry_price = 0.0
        self._has_prev_signal = False
        self._candles_since_trade = self.signal_cooldown

        subscription = self.SubscribeCandles(DataType.TimeFrame(TimeSpan.FromMinutes(15)))
        subscription.Bind(self._ema, self._atr, self._process_candle)
        subscription.Start()

    def _process_candle(self, candle, ema_value, atr_value):
        if candle.State != CandleStates.Finished:
            return

        if not self._ema.IsFormed or not self._atr.IsFormed:
            return

        close = float(candle.ClosePrice)
        ema_val = float(ema_value)
        atr_val = float(atr_value)
        above_ema = close > ema_val

        if self._candles_since_trade < self.signal_cooldown:
            self._candles_since_trade += 1

        if self.Position > 0 and self._entry_price > 0:
            if close <= self._stop_price or close >= self._take_price:
                self.SellMarket()
                self._entry_price = 0.0
                self._stop_price = 0.0
                self._take_price = 0.0
                self._candles_since_trade = 0
        elif self.Position < 0 and self._entry_price > 0:
            if close >= self._stop_price or close <= self._take_price:
                self.BuyMarket()
                self._entry_price = 0.0
                self._stop_price = 0.0
                self._take_price = 0.0
                self._candles_since_trade = 0

        if self.Position == 0 and atr_val > 0 and self._has_prev_signal and above_ema != self._prev_above_ema and self._candles_since_trade >= self.signal_cooldown:
            if above_ema:
                self.BuyMarket()
                self._entry_price = close
                self._stop_price = close - atr_val * self.stop_factor
                self._take_price = close + atr_val * self.take_factor
                self._candles_since_trade = 0
            else:
                self.SellMarket()
                self._entry_price = close
                self._stop_price = close + atr_val * self.stop_factor
                self._take_price = close - atr_val * self.take_factor
                self._candles_since_trade = 0

        self._prev_above_ema = above_ema
        self._has_prev_signal = True

    def CreateClone(self):
        return basic_atr_stop_take_strategy()
