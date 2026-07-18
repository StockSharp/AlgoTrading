import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import SimpleMovingAverage, AverageTrueRange, RelativeStrengthIndex
from StockSharp.Algo.Strategies import Strategy


class mel_bar_euro_swiss_strategy(Strategy):
    def __init__(self):
        super(mel_bar_euro_swiss_strategy, self).__init__()

        self._bb_period = self.Param("BbPeriod", 18) \
            .SetDisplay("BB Period", "Bollinger Bands period", "Indicators")
        self._rsi_period = self.Param("RsiPeriod", 14) \
            .SetDisplay("RSI Period", "RSI period", "Indicators")
        self._signal_cooldown = self.Param("SignalCooldownCandles", 12) \
            .SetDisplay("Signal Cooldown", "Bars to wait between trades", "Trading")

        self._sma = None
        self._atr = None
        self._rsi = None
        self._candles_since_trade = 0

    @property
    def bb_period(self):
        return self._bb_period.Value

    @property
    def rsi_period(self):
        return self._rsi_period.Value

    @property
    def signal_cooldown(self):
        return self._signal_cooldown.Value

    def OnReseted(self):
        super(mel_bar_euro_swiss_strategy, self).OnReseted()
        self._sma = None
        self._atr = None
        self._rsi = None
        self._candles_since_trade = self.signal_cooldown

    def OnStarted2(self, time):
        super(mel_bar_euro_swiss_strategy, self).OnStarted2(time)

        self._sma = SimpleMovingAverage()
        self._sma.Length = self.bb_period
        self._atr = AverageTrueRange()
        self._atr.Length = self.bb_period
        self._rsi = RelativeStrengthIndex()
        self._rsi.Length = self.rsi_period
        self._candles_since_trade = self.signal_cooldown

        subscription = self.SubscribeCandles(DataType.TimeFrame(TimeSpan.FromMinutes(60)))
        subscription.Bind(self._sma, self._atr, self._rsi, self._process_candle)
        subscription.Start()

    def _process_candle(self, candle, sma_value, atr_value, rsi_value):
        if candle.State != CandleStates.Finished:
            return

        if not self._sma.IsFormed or not self._atr.IsFormed or not self._rsi.IsFormed:
            return

        if self._candles_since_trade < self.signal_cooldown:
            self._candles_since_trade += 1

        close = float(candle.ClosePrice)
        sma_val = float(sma_value)
        atr_val = float(atr_value)
        rsi_val = float(rsi_value)
        band_distance = atr_val * 4.0

        if self._candles_since_trade >= self.signal_cooldown and close <= sma_val - band_distance and rsi_val < 25.0 and self.Position <= 0:
            self.BuyMarket()
            self._candles_since_trade = 0
        elif self._candles_since_trade >= self.signal_cooldown and close >= sma_val + band_distance and rsi_val > 75.0 and self.Position >= 0:
            self.SellMarket()
            self._candles_since_trade = 0

    def CreateClone(self):
        return mel_bar_euro_swiss_strategy()
