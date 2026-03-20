import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import RelativeStrengthIndex
from StockSharp.Algo.Strategies import Strategy


class cbc_ws_rsi_strategy(Strategy):
    def __init__(self):
        super(cbc_ws_rsi_strategy, self).__init__()

        self._rsi_period = self.Param("RsiPeriod", 14) \
            .SetDisplay("RSI Period", "RSI period for confirmation", "Indicators")
        self._signal_cooldown = self.Param("SignalCooldownCandles", 6) \
            .SetDisplay("Signal Cooldown", "Bars to wait between trades", "Trading")

        self._rsi = None
        self._bull_count = 0
        self._bear_count = 0
        self._candles_since_trade = 0

    @property
    def rsi_period(self):
        return self._rsi_period.Value

    @property
    def signal_cooldown(self):
        return self._signal_cooldown.Value

    def OnReseted(self):
        super(cbc_ws_rsi_strategy, self).OnReseted()
        self._rsi = None
        self._bull_count = 0
        self._bear_count = 0
        self._candles_since_trade = self.signal_cooldown

    def OnStarted(self, time):
        super(cbc_ws_rsi_strategy, self).OnStarted(time)

        self._rsi = RelativeStrengthIndex()
        self._rsi.Length = self.rsi_period
        self._bull_count = 0
        self._bear_count = 0
        self._candles_since_trade = self.signal_cooldown

        subscription = self.SubscribeCandles(DataType.TimeFrame(TimeSpan.FromMinutes(60)))
        subscription.Bind(self._rsi, self._process_candle)
        subscription.Start()

    def _process_candle(self, candle, rsi_value):
        if candle.State != CandleStates.Finished:
            return

        if not self._rsi.IsFormed:
            return

        rsi_val = float(rsi_value)

        if self._candles_since_trade < self.signal_cooldown:
            self._candles_since_trade += 1

        close = float(candle.ClosePrice)
        open_p = float(candle.OpenPrice)

        if close > open_p:
            self._bull_count += 1
            self._bear_count = 0
        elif close < open_p:
            self._bear_count += 1
            self._bull_count = 0
        else:
            self._bull_count = 0
            self._bear_count = 0

        if self.Position > 0 and rsi_val > 75.0 and self._candles_since_trade >= self.signal_cooldown:
            self.SellMarket()
            self._candles_since_trade = 0
        elif self.Position < 0 and rsi_val < 25.0 and self._candles_since_trade >= self.signal_cooldown:
            self.BuyMarket()
            self._candles_since_trade = 0

        if self._bull_count >= 3 and rsi_val < 60.0 and self.Position <= 0 and self._candles_since_trade >= self.signal_cooldown:
            self.BuyMarket()
            self._bull_count = 0
            self._candles_since_trade = 0
        elif self._bear_count >= 3 and rsi_val > 40.0 and self.Position >= 0 and self._candles_since_trade >= self.signal_cooldown:
            self.SellMarket()
            self._bear_count = 0
            self._candles_since_trade = 0

    def CreateClone(self):
        return cbc_ws_rsi_strategy()
