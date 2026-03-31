import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import KeltnerChannels
from StockSharp.Algo.Strategies import Strategy

class keltner_channel_reversal_strategy(Strategy):
    """
    Keltner Channel Reversal strategy.
    Enters long when price is below lower Keltner Channel with a bullish candle.
    Enters short when price is above upper Keltner Channel with a bearish candle.
    Exits at middle band.
    """

    def __init__(self):
        super(keltner_channel_reversal_strategy, self).__init__()
        self._ema_period = self.Param("EmaPeriod", 20).SetDisplay("EMA Period", "Period for EMA in Keltner Channel", "Indicators")
        self._atr_multiplier = self.Param("AtrMultiplier", 2.0).SetDisplay("ATR Multiplier", "Multiplier for ATR in Keltner Channel", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(1))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._cooldown_bars = self.Param("CooldownBars", 500).SetDisplay("Cooldown Bars", "Bars to wait between trades", "General")

        self._cooldown = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(keltner_channel_reversal_strategy, self).OnReseted()
        self._cooldown = 0

    def OnStarted2(self, time):
        super(keltner_channel_reversal_strategy, self).OnStarted2(time)

        self._cooldown = 0

        keltner = KeltnerChannels()
        keltner.Length = self._ema_period.Value
        keltner.Multiplier = self._atr_multiplier.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(keltner, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, keltner)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, keltner_value):
        if candle.State != CandleStates.Finished:
            return

        if not keltner_value.IsFormed:
            return

        if self._cooldown > 0:
            self._cooldown -= 1
            return

        upper = keltner_value.Upper
        lower = keltner_value.Lower
        middle = keltner_value.Middle

        if upper is None or lower is None or middle is None:
            return

        close = float(candle.ClosePrice)
        ub = float(upper)
        lb = float(lower)
        mb = float(middle)
        cd = self._cooldown_bars.Value

        is_bullish = candle.ClosePrice > candle.OpenPrice
        is_bearish = candle.ClosePrice < candle.OpenPrice

        if self.Position == 0 and close < lb and is_bullish:
            self.BuyMarket()
            self._cooldown = cd
        elif self.Position == 0 and close > ub and is_bearish:
            self.SellMarket()
            self._cooldown = cd
        elif self.Position > 0 and close > mb:
            self.SellMarket()
            self._cooldown = cd
        elif self.Position < 0 and close < mb:
            self.BuyMarket()
            self._cooldown = cd

    def CreateClone(self):
        return keltner_channel_reversal_strategy()
