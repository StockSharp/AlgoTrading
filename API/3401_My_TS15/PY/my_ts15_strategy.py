import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import WeightedMovingAverage, AverageTrueRange
from StockSharp.Algo.Strategies import Strategy


class my_ts15_strategy(Strategy):
    def __init__(self):
        super(my_ts15_strategy, self).__init__()

        self._ma_period = self.Param("MaPeriod", 100) \
            .SetDisplay("MA Period", "WMA period", "Indicators")
        self._atr_period = self.Param("AtrPeriod", 14) \
            .SetDisplay("ATR Period", "ATR period for trailing", "Indicators")
        self._trail_multiplier = self.Param("TrailMultiplier", 3.0) \
            .SetDisplay("Trail Multiplier", "ATR multiplier for trailing stop", "Risk")
        self._signal_cooldown = self.Param("SignalCooldownCandles", 12) \
            .SetDisplay("Signal Cooldown", "Bars to wait between trades", "Trading")

        self._wma = None
        self._atr = None
        self._entry_price = 0.0
        self._best_price = 0.0
        self._was_bullish = False
        self._has_prev_signal = False
        self._candles_since_trade = 0

    @property
    def ma_period(self):
        return self._ma_period.Value

    @property
    def atr_period(self):
        return self._atr_period.Value

    @property
    def trail_multiplier(self):
        return self._trail_multiplier.Value

    @property
    def signal_cooldown(self):
        return self._signal_cooldown.Value

    def OnReseted(self):
        super(my_ts15_strategy, self).OnReseted()
        self._wma = None
        self._atr = None
        self._entry_price = 0.0
        self._best_price = 0.0
        self._was_bullish = False
        self._has_prev_signal = False
        self._candles_since_trade = self.signal_cooldown

    def OnStarted2(self, time):
        super(my_ts15_strategy, self).OnStarted2(time)

        self._wma = WeightedMovingAverage()
        self._wma.Length = self.ma_period
        self._atr = AverageTrueRange()
        self._atr.Length = self.atr_period
        self._entry_price = 0.0
        self._best_price = 0.0
        self._has_prev_signal = False
        self._candles_since_trade = self.signal_cooldown

        subscription = self.SubscribeCandles(DataType.TimeFrame(TimeSpan.FromMinutes(120)))
        subscription.Bind(self._wma, self._atr, self._process_candle)
        subscription.Start()

    def _process_candle(self, candle, wma_value, atr_value):
        if candle.State != CandleStates.Finished:
            return

        if not self._wma.IsFormed or not self._atr.IsFormed:
            return

        close = float(candle.ClosePrice)
        wma_val = float(wma_value)
        atr_val = float(atr_value)
        trail_dist = atr_val * self.trail_multiplier
        is_bullish = close > wma_val

        if self._candles_since_trade < self.signal_cooldown:
            self._candles_since_trade += 1

        if self.Position > 0:
            if close > self._best_price:
                self._best_price = close
            if self._best_price - close > trail_dist:
                self.SellMarket()
                self._entry_price = 0.0
                self._best_price = 0.0
                self._candles_since_trade = 0
                return
        elif self.Position < 0:
            if close < self._best_price:
                self._best_price = close
            if close - self._best_price > trail_dist:
                self.BuyMarket()
                self._entry_price = 0.0
                self._best_price = 0.0
                self._candles_since_trade = 0
                return

        if self._has_prev_signal and is_bullish != self._was_bullish and self._candles_since_trade >= self.signal_cooldown:
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
        return my_ts15_strategy()
