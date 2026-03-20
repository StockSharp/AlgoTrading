import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import SimpleMovingAverage, AverageTrueRange
from StockSharp.Algo.Strategies import Strategy

class atr_exhaustion_strategy(Strategy):
    """
    ATR Exhaustion strategy.
    Enters when ATR spikes (current ATR significantly higher than previous ATR).
    ATR spike + bullish candle = buy.
    ATR spike + bearish candle = sell.
    Exits on SMA cross.
    """

    def __init__(self):
        super(atr_exhaustion_strategy, self).__init__()
        self._atr_period = self.Param("AtrPeriod", 14).SetDisplay("ATR Period", "Period for ATR", "Indicators")
        self._ma_period = self.Param("MAPeriod", 20).SetDisplay("MA Period", "Period for SMA", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(1))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._cooldown_bars = self.Param("CooldownBars", 500).SetDisplay("Cooldown Bars", "Bars to wait between trades", "General")

        self._prev_atr = 0.0
        self._cooldown = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(atr_exhaustion_strategy, self).OnReseted()
        self._prev_atr = 0.0
        self._cooldown = 0

    def OnStarted(self, time):
        super(atr_exhaustion_strategy, self).OnStarted(time)

        self._prev_atr = 0.0
        self._cooldown = 0

        sma = SimpleMovingAverage()
        sma.Length = self._ma_period.Value

        atr = AverageTrueRange()
        atr.Length = self._atr_period.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(sma, atr, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, sma)
            self.DrawIndicator(area, atr)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, sma_val, atr_val):
        if candle.State != CandleStates.Finished:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            self._prev_atr = float(atr_val)
            return

        av = float(atr_val)

        if self._prev_atr == 0:
            self._prev_atr = av
            return

        if self._cooldown > 0:
            self._cooldown -= 1
            self._prev_atr = av
            return

        # ATR spike: current ATR significantly higher than previous
        atr_spike = self._prev_atr > 0 and av > self._prev_atr * 1.3

        is_bullish = candle.ClosePrice > candle.OpenPrice
        is_bearish = candle.ClosePrice < candle.OpenPrice

        sv = float(sma_val)
        close = float(candle.ClosePrice)
        cd = self._cooldown_bars.Value

        if self.Position == 0 and atr_spike and is_bullish:
            self.BuyMarket()
            self._cooldown = cd
        elif self.Position == 0 and atr_spike and is_bearish:
            self.SellMarket()
            self._cooldown = cd
        elif self.Position > 0 and close < sv:
            self.SellMarket()
            self._cooldown = cd
        elif self.Position < 0 and close > sv:
            self.BuyMarket()
            self._cooldown = cd

        self._prev_atr = av

    def CreateClone(self):
        return atr_exhaustion_strategy()
