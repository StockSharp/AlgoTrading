import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import SimpleMovingAverage, RelativeStrengthIndex, AverageTrueRange
from StockSharp.Algo.Strategies import Strategy


class pricer_ea_strategy(Strategy):
    def __init__(self):
        super(pricer_ea_strategy, self).__init__()

        self._bb_period = self.Param("BbPeriod", 20) \
            .SetDisplay("BB Period", "Bollinger Bands period", "Indicators")
        self._rsi_period = self.Param("RsiPeriod", 14) \
            .SetDisplay("RSI Period", "RSI period", "Indicators")
        self._atr_period = self.Param("AtrPeriod", 14) \
            .SetDisplay("ATR Period", "ATR period", "Indicators")
        self._signal_cooldown = self.Param("SignalCooldownCandles", 8) \
            .SetDisplay("Signal Cooldown", "Bars to wait between trades", "Trading")

        self._sma = None
        self._rsi = None
        self._atr = None
        self._candles_since_trade = 0

    @property
    def bb_period(self):
        return self._bb_period.Value

    @property
    def rsi_period(self):
        return self._rsi_period.Value

    @property
    def atr_period(self):
        return self._atr_period.Value

    @property
    def signal_cooldown(self):
        return self._signal_cooldown.Value

    def OnReseted(self):
        super(pricer_ea_strategy, self).OnReseted()
        self._sma = None
        self._rsi = None
        self._atr = None
        self._candles_since_trade = self.signal_cooldown

    def OnStarted(self, time):
        super(pricer_ea_strategy, self).OnStarted(time)

        self._sma = SimpleMovingAverage()
        self._sma.Length = self.bb_period
        self._rsi = RelativeStrengthIndex()
        self._rsi.Length = self.rsi_period
        self._atr = AverageTrueRange()
        self._atr.Length = self.atr_period
        self._candles_since_trade = self.signal_cooldown

        subscription = self.SubscribeCandles(DataType.TimeFrame(TimeSpan.FromMinutes(30)))
        subscription.Bind(self._sma, self._rsi, self._atr, self._process_candle)
        subscription.Start()

    def _process_candle(self, candle, sma_value, rsi_value, atr_value):
        if candle.State != CandleStates.Finished:
            return

        if not self._sma.IsFormed or not self._rsi.IsFormed or not self._atr.IsFormed:
            return

        close = float(candle.ClosePrice)
        sma_val = float(sma_value)
        rsi_val = float(rsi_value)
        atr_val = float(atr_value)
        band_distance = atr_val * 2.5

        if self._candles_since_trade < self.signal_cooldown:
            self._candles_since_trade += 1

        if self.Position > 0 and (close >= sma_val or rsi_val >= 50.0):
            self.SellMarket()
            self._candles_since_trade = 0
        elif self.Position < 0 and (close <= sma_val or rsi_val <= 50.0):
            self.BuyMarket()
            self._candles_since_trade = 0
        elif self.Position == 0 and self._candles_since_trade >= self.signal_cooldown and close <= sma_val - band_distance and rsi_val < 30.0:
            self.BuyMarket()
            self._candles_since_trade = 0
        elif self.Position == 0 and self._candles_since_trade >= self.signal_cooldown and close >= sma_val + band_distance and rsi_val > 70.0:
            self.SellMarket()
            self._candles_since_trade = 0

    def CreateClone(self):
        return pricer_ea_strategy()
