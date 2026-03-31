import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import SimpleMovingAverage, RelativeStrengthIndex
from StockSharp.Algo.Strategies import Strategy


class manual_trading_lightweight_utility_panel_strategy(Strategy):
    def __init__(self):
        super(manual_trading_lightweight_utility_panel_strategy, self).__init__()

        self._sma_period = self.Param("SmaPeriod", 20) \
            .SetDisplay("SMA Period", "SMA period", "Indicators")
        self._rsi_period = self.Param("RsiPeriod", 14) \
            .SetDisplay("RSI Period", "RSI period", "Indicators")
        self._signal_cooldown = self.Param("SignalCooldownCandles", 8) \
            .SetDisplay("Signal Cooldown", "Bars to wait between trades", "Trading")

        self._sma = None
        self._rsi = None
        self._candles_since_trade = 0

    @property
    def sma_period(self):
        return self._sma_period.Value

    @property
    def rsi_period(self):
        return self._rsi_period.Value

    @property
    def signal_cooldown(self):
        return self._signal_cooldown.Value

    def OnReseted(self):
        super(manual_trading_lightweight_utility_panel_strategy, self).OnReseted()
        self._sma = None
        self._rsi = None
        self._candles_since_trade = self.signal_cooldown

    def OnStarted2(self, time):
        super(manual_trading_lightweight_utility_panel_strategy, self).OnStarted2(time)

        self._sma = SimpleMovingAverage()
        self._sma.Length = self.sma_period
        self._rsi = RelativeStrengthIndex()
        self._rsi.Length = self.rsi_period
        self._candles_since_trade = self.signal_cooldown

        subscription = self.SubscribeCandles(DataType.TimeFrame(TimeSpan.FromMinutes(30)))
        subscription.Bind(self._sma, self._rsi, self._process_candle)
        subscription.Start()

    def _process_candle(self, candle, sma_value, rsi_value):
        if candle.State != CandleStates.Finished:
            return

        if not self._sma.IsFormed or not self._rsi.IsFormed:
            return

        if self._candles_since_trade < self.signal_cooldown:
            self._candles_since_trade += 1

        close = float(candle.ClosePrice)
        sma_val = float(sma_value)
        rsi_val = float(rsi_value)

        if close < sma_val and rsi_val < 35.0 and self.Position <= 0 and self._candles_since_trade >= self.signal_cooldown:
            self.BuyMarket()
            self._candles_since_trade = 0
        elif close > sma_val and rsi_val > 65.0 and self.Position >= 0 and self._candles_since_trade >= self.signal_cooldown:
            self.SellMarket()
            self._candles_since_trade = 0

    def CreateClone(self):
        return manual_trading_lightweight_utility_panel_strategy()
