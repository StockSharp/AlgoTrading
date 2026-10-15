import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import AverageTrueRange
from StockSharp.Algo.Strategies import Strategy


class rapid_doji_strategy(Strategy):
    def __init__(self):
        super(rapid_doji_strategy, self).__init__()

        self._atr_period = self.Param("AtrPeriod", 14) \
            .SetDisplay("ATR Period", "ATR period for volatility filter", "Indicators")
        self._doji_threshold = self.Param("DojiThreshold", 0.15) \
            .SetDisplay("Doji Threshold", "Max body/range ratio for doji detection", "Pattern")
        self._signal_cooldown = self.Param("SignalCooldownCandles", 6) \
            .SetDisplay("Signal Cooldown", "Bars to wait between breakouts", "Trading")

        self._atr = None
        self._prev_high = 0.0
        self._prev_low = 0.0
        self._prev_was_doji = False
        self._candles_since_trade = 0

    @property
    def atr_period(self):
        return self._atr_period.Value

    @property
    def doji_threshold(self):
        return self._doji_threshold.Value

    @property
    def signal_cooldown(self):
        return self._signal_cooldown.Value

    def OnReseted(self):
        super(rapid_doji_strategy, self).OnReseted()
        self._atr = None
        self._prev_high = 0.0
        self._prev_low = 0.0
        self._prev_was_doji = False
        self._candles_since_trade = self.signal_cooldown

    def OnStarted2(self, time):
        super(rapid_doji_strategy, self).OnStarted2(time)

        self._atr = AverageTrueRange()
        self._atr.Length = self.atr_period
        self._prev_was_doji = False
        self._candles_since_trade = self.signal_cooldown

        subscription = self.SubscribeCandles(DataType.TimeFrame(TimeSpan.FromMinutes(15)))
        subscription.Bind(self._atr, self._process_candle)
        subscription.Start()

    def _process_candle(self, candle, atr_value):
        if candle.State != CandleStates.Finished:
            return

        if not self._atr.IsFormed:
            return

        atr_val = float(atr_value)

        if self._candles_since_trade < self.signal_cooldown:
            self._candles_since_trade += 1

        if self._prev_was_doji and atr_val > 0 and self._candles_since_trade >= self.signal_cooldown:
            close = float(candle.ClosePrice)
            if close > self._prev_high + atr_val * 0.2 and self.Position <= 0:
                self.BuyMarket()
                self._candles_since_trade = 0
            elif close < self._prev_low - atr_val * 0.2 and self.Position >= 0:
                self.SellMarket()
                self._candles_since_trade = 0

        high = float(candle.HighPrice)
        low = float(candle.LowPrice)
        range_size = high - low
        body = abs(float(candle.ClosePrice) - float(candle.OpenPrice))
        self._prev_was_doji = range_size > 0 and body <= self.doji_threshold * range_size
        self._prev_high = high
        self._prev_low = low

    def CreateClone(self):
        return rapid_doji_strategy()
