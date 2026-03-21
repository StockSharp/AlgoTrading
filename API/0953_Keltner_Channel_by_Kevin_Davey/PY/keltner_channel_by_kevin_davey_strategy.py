import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage, AverageTrueRange
from StockSharp.Algo.Strategies import Strategy

class keltner_channel_by_kevin_davey_strategy(Strategy):
    """
    Keltner Channel strategy. Enters long when price closes below lower band,
    short when above upper band. Exits at EMA midline.
    """

    def __init__(self):
        super(keltner_channel_by_kevin_davey_strategy, self).__init__()
        self._ema_period = self.Param("EmaPeriod", 10) \
            .SetDisplay("EMA Period", "Period for EMA", "Indicators")
        self._atr_period = self.Param("AtrPeriod", 14) \
            .SetDisplay("ATR Period", "Period for ATR", "Indicators")
        self._atr_multiplier = self.Param("AtrMultiplier", 1.6) \
            .SetDisplay("ATR Multiplier", "Multiplier for ATR", "Indicators")
        self._max_entries = self.Param("MaxEntries", 45) \
            .SetDisplay("Max Entries", "Maximum entries per run", "Risk")
        self._cooldown_bars = self.Param("CooldownBars", 20) \
            .SetDisplay("Cooldown Bars", "Minimum bars between entries", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Type of candles", "General")

        self._entries_executed = 0
        self._bars_since_signal = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(keltner_channel_by_kevin_davey_strategy, self).OnReseted()
        self._entries_executed = 0
        self._bars_since_signal = 0

    def OnStarted(self, time):
        super(keltner_channel_by_kevin_davey_strategy, self).OnStarted(time)
        self._entries_executed = 0
        self._bars_since_signal = self._cooldown_bars.Value

        ema = ExponentialMovingAverage()
        ema.Length = self._ema_period.Value
        atr = AverageTrueRange()
        atr.Length = self._atr_period.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(ema, atr, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, ema)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, ema_val, atr_val):
        if candle.State != CandleStates.Finished:
            return

        ema = float(ema_val)
        atr = float(atr_val)
        close = float(candle.ClosePrice)
        mult = self._atr_multiplier.Value

        self._bars_since_signal += 1

        upper = ema + mult * atr
        lower = ema - mult * atr

        if self._bars_since_signal < self._cooldown_bars.Value:
            return

        if self.Position > 0 and close >= ema:
            self.SellMarket()
            self._bars_since_signal = 0
        elif self.Position < 0 and close <= ema:
            self.BuyMarket()
            self._bars_since_signal = 0
        elif self.Position == 0 and self._entries_executed < self._max_entries.Value:
            if close < lower:
                self.BuyMarket()
                self._entries_executed += 1
                self._bars_since_signal = 0
            elif close > upper:
                self.SellMarket()
                self._entries_executed += 1
                self._bars_since_signal = 0

    def CreateClone(self):
        return keltner_channel_by_kevin_davey_strategy()
