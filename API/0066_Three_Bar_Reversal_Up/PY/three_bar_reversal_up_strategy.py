import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy

class three_bar_reversal_up_strategy(Strategy):
    """
    Three-Bar Reversal Up strategy.
    Pattern: 1st bar bearish, 2nd bar bearish with lower low, 3rd bar bullish closing above 2nd high.
    Uses SMA for exit.
    """

    def __init__(self):
        super(three_bar_reversal_up_strategy, self).__init__()
        self._ma_period = self.Param("MAPeriod", 20).SetDisplay("MA Period", "Period for SMA", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(1))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._cooldown_bars = self.Param("CooldownBars", 500).SetDisplay("Cooldown Bars", "Bars to wait between trades", "General")

        self._bar1 = None
        self._bar2 = None
        self._cooldown = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(three_bar_reversal_up_strategy, self).OnReseted()
        self._bar1 = None
        self._bar2 = None
        self._cooldown = 0

    def OnStarted(self, time):
        super(three_bar_reversal_up_strategy, self).OnStarted(time)

        self._bar1 = None
        self._bar2 = None
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

        if self._cooldown > 0:
            self._cooldown -= 1
            self._bar1 = self._bar2
            self._bar2 = candle
            return

        if self._bar1 is not None and self._bar2 is not None:
            # Three-bar reversal up: bar1 bearish, bar2 bearish with lower low, bar3 bullish closing above bar2 high
            bar1_bearish = self._bar1.ClosePrice < self._bar1.OpenPrice
            bar2_bearish = self._bar2.ClosePrice < self._bar2.OpenPrice
            bar2_lower_low = self._bar2.LowPrice < self._bar1.LowPrice
            bar3_bullish = candle.ClosePrice > candle.OpenPrice
            bar3_above_bar2_high = candle.ClosePrice > self._bar2.HighPrice

            three_bar_up = bar1_bearish and bar2_bearish and bar2_lower_low and bar3_bullish and bar3_above_bar2_high

            # Three-bar reversal down: bar1 bullish, bar2 bullish with higher high, bar3 bearish closing below bar2 low
            bar1_bullish = self._bar1.ClosePrice > self._bar1.OpenPrice
            bar2_bullish = self._bar2.ClosePrice > self._bar2.OpenPrice
            bar2_higher_high = self._bar2.HighPrice > self._bar1.HighPrice
            bar3_bearish = candle.ClosePrice < candle.OpenPrice
            bar3_below_bar2_low = candle.ClosePrice < self._bar2.LowPrice

            three_bar_down = bar1_bullish and bar2_bullish and bar2_higher_high and bar3_bearish and bar3_below_bar2_low

            sv = float(sma_val)
            close = float(candle.ClosePrice)
            cd = self._cooldown_bars.Value

            if self.Position == 0 and three_bar_up:
                self.BuyMarket()
                self._cooldown = cd
            elif self.Position == 0 and three_bar_down:
                self.SellMarket()
                self._cooldown = cd
            elif self.Position > 0 and close < sv:
                self.SellMarket()
                self._cooldown = cd
            elif self.Position < 0 and close > sv:
                self.BuyMarket()
                self._cooldown = cd

        self._bar1 = self._bar2
        self._bar2 = candle

    def CreateClone(self):
        return three_bar_reversal_up_strategy()
