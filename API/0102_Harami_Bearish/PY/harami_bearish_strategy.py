import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy

class harami_bearish_strategy(Strategy):
    """
    Harami Bearish strategy.
    Enters short on bearish harami (bullish candle followed by smaller bearish candle inside it).
    Enters long on bullish harami (bearish candle followed by smaller bullish candle inside it).
    Uses SMA for exit confirmation.
    """

    def __init__(self):
        super(harami_bearish_strategy, self).__init__()
        self._ma_length = self.Param("MaLength", 20).SetDisplay("MA Length", "Period of SMA for exit", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(1))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._cooldown_bars = self.Param("CooldownBars", 500).SetDisplay("Cooldown Bars", "Bars to wait between trades", "General")

        self._prev_candle = None
        self._cooldown = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(harami_bearish_strategy, self).OnReseted()
        self._prev_candle = None
        self._cooldown = 0

    def OnStarted(self, time):
        super(harami_bearish_strategy, self).OnStarted(time)

        self._prev_candle = None
        self._cooldown = 0

        sma = SimpleMovingAverage()
        sma.Length = self._ma_length.Value

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

        if self._prev_candle is None:
            self._prev_candle = candle
            return

        if self._cooldown > 0:
            self._cooldown -= 1
            self._prev_candle = candle
            return

        cd = self._cooldown_bars.Value
        sv = float(sma_val)

        # Bearish Harami: prev bullish, current bearish, current inside prev
        bearish_harami = (
            self._prev_candle.ClosePrice > self._prev_candle.OpenPrice and
            candle.ClosePrice < candle.OpenPrice and
            candle.HighPrice < self._prev_candle.HighPrice and
            candle.LowPrice > self._prev_candle.LowPrice
        )

        # Bullish Harami: prev bearish, current bullish, current inside prev
        bullish_harami = (
            self._prev_candle.ClosePrice < self._prev_candle.OpenPrice and
            candle.ClosePrice > candle.OpenPrice and
            candle.HighPrice < self._prev_candle.HighPrice and
            candle.LowPrice > self._prev_candle.LowPrice
        )

        if self.Position == 0 and bearish_harami:
            self.SellMarket()
            self._cooldown = cd
        elif self.Position == 0 and bullish_harami:
            self.BuyMarket()
            self._cooldown = cd
        elif self.Position < 0 and float(candle.ClosePrice) > sv:
            self.BuyMarket()
            self._cooldown = cd
        elif self.Position > 0 and float(candle.ClosePrice) < sv:
            self.SellMarket()
            self._cooldown = cd

        self._prev_candle = candle

    def CreateClone(self):
        return harami_bearish_strategy()
