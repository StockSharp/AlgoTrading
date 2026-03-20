import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy

class pinbar_reversal_strategy(Strategy):
    """
    Pinbar Reversal strategy.
    Enters long on bullish pinbar (long lower wick) below SMA.
    Enters short on bearish pinbar (long upper wick) above SMA.
    Exits via SMA crossover.
    """

    def __init__(self):
        super(pinbar_reversal_strategy, self).__init__()
        self._tail_to_body_ratio = self.Param("TailToBodyRatio", 2.0).SetDisplay("Tail/Body Ratio", "Min tail to body ratio", "Pattern")
        self._ma_period = self.Param("MAPeriod", 20).SetDisplay("MA Period", "Period for SMA", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(1))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._cooldown_bars = self.Param("CooldownBars", 500).SetDisplay("Cooldown Bars", "Bars to wait between trades", "General")

        self._cooldown = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(pinbar_reversal_strategy, self).OnReseted()
        self._cooldown = 0

    def OnStarted(self, time):
        super(pinbar_reversal_strategy, self).OnStarted(time)

        self._cooldown = 0

        sma = SimpleMovingAverage()
        sma.Length = self._ma_period.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(sma, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, sma)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, sma_val):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        if self._cooldown > 0:
            self._cooldown -= 1
            return

        body_size = abs(float(candle.OpenPrice) - float(candle.ClosePrice))
        lower_wick = min(float(candle.OpenPrice), float(candle.ClosePrice)) - float(candle.LowPrice)
        upper_wick = float(candle.HighPrice) - max(float(candle.OpenPrice), float(candle.ClosePrice))

        ratio = float(self._tail_to_body_ratio.Value)

        # Bullish pinbar: long lower wick, small upper wick
        is_bullish_pinbar = body_size > 0 and lower_wick > body_size * ratio and upper_wick < body_size * 0.5
        # Bearish pinbar: long upper wick, small lower wick
        is_bearish_pinbar = body_size > 0 and upper_wick > body_size * ratio and lower_wick < body_size * 0.5

        close = float(candle.ClosePrice)
        sv = float(sma_val)
        cd = self._cooldown_bars.Value

        if self.Position == 0 and is_bullish_pinbar and close < sv:
            self.BuyMarket()
            self._cooldown = cd
        elif self.Position == 0 and is_bearish_pinbar and close > sv:
            self.SellMarket()
            self._cooldown = cd
        elif self.Position > 0 and close < sv:
            self.SellMarket()
            self._cooldown = cd
        elif self.Position < 0 and close > sv:
            self.BuyMarket()
            self._cooldown = cd

    def CreateClone(self):
        return pinbar_reversal_strategy()
