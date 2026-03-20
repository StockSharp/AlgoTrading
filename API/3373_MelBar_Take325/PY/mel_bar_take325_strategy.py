import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import SimpleMovingAverage, RelativeStrengthIndex
from StockSharp.Algo.Strategies import Strategy


class mel_bar_take325_strategy(Strategy):
    def __init__(self):
        super(mel_bar_take325_strategy, self).__init__()

        self._sma_period = self.Param("SmaPeriod", 12) \
            .SetDisplay("SMA Period", "SMA period", "Indicators")
        self._rsi_period = self.Param("RsiPeriod", 14) \
            .SetDisplay("RSI Period", "RSI period", "Indicators")
        self._rsi_exit_level = self.Param("RsiExitLevel", 75.0) \
            .SetDisplay("RSI Exit Level", "RSI level to close long", "Signals")
        self._signal_cooldown = self.Param("SignalCooldownCandles", 8) \
            .SetDisplay("Signal Cooldown", "Bars to wait between trades", "Trading")

        self._sma = None
        self._rsi = None
        self._prev_sma = 0.0
        self._prev_prev_sma = 0.0
        self._has_prev2 = False
        self._candles_since_trade = 0

    @property
    def sma_period(self):
        return self._sma_period.Value

    @property
    def rsi_period(self):
        return self._rsi_period.Value

    @property
    def rsi_exit_level(self):
        return self._rsi_exit_level.Value

    @property
    def signal_cooldown(self):
        return self._signal_cooldown.Value

    def OnReseted(self):
        super(mel_bar_take325_strategy, self).OnReseted()
        self._sma = None
        self._rsi = None
        self._prev_sma = 0.0
        self._prev_prev_sma = 0.0
        self._has_prev2 = False
        self._candles_since_trade = self.signal_cooldown

    def OnStarted(self, time):
        super(mel_bar_take325_strategy, self).OnStarted(time)

        self._sma = SimpleMovingAverage()
        self._sma.Length = self.sma_period
        self._rsi = RelativeStrengthIndex()
        self._rsi.Length = self.rsi_period
        self._has_prev2 = False
        self._candles_since_trade = self.signal_cooldown

        subscription = self.SubscribeCandles(DataType.TimeFrame(TimeSpan.FromMinutes(15)))
        subscription.Bind(self._sma, self._rsi, self._process_candle)
        subscription.Start()

    def _process_candle(self, candle, sma_value, rsi_value):
        if candle.State != CandleStates.Finished:
            return

        if not self._sma.IsFormed or not self._rsi.IsFormed:
            return

        sma_val = float(sma_value)
        rsi_val = float(rsi_value)

        if self._candles_since_trade < self.signal_cooldown:
            self._candles_since_trade += 1

        if self._has_prev2:
            if self.Position > 0 and rsi_val > self.rsi_exit_level:
                self.SellMarket()
                self._candles_since_trade = 0
            elif self.Position < 0 and rsi_val < (100.0 - self.rsi_exit_level):
                self.BuyMarket()
                self._candles_since_trade = 0

            if self.Position == 0 and self._candles_since_trade >= self.signal_cooldown:
                if self._prev_prev_sma > self._prev_sma and self._prev_sma < sma_val:
                    self.BuyMarket()
                    self._candles_since_trade = 0
                elif self._prev_prev_sma < self._prev_sma and self._prev_sma > sma_val:
                    self.SellMarket()
                    self._candles_since_trade = 0

        self._prev_prev_sma = self._prev_sma
        self._prev_sma = sma_val
        self._has_prev2 = True

    def CreateClone(self):
        return mel_bar_take325_strategy()
