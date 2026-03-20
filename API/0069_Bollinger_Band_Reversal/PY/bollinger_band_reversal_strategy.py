import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import BollingerBands
from StockSharp.Algo.Strategies import Strategy

class bollinger_band_reversal_strategy(Strategy):
    """
    Bollinger Band Reversal strategy.
    Enters long when price is below the lower Bollinger Band and candle is bullish.
    Enters short when price is above the upper Bollinger Band and candle is bearish.
    Exits at middle band.
    """

    def __init__(self):
        super(bollinger_band_reversal_strategy, self).__init__()
        self._bollinger_period = self.Param("BollingerPeriod", 20).SetDisplay("Bollinger Period", "Period for Bollinger Bands", "Indicators")
        self._bollinger_deviation = self.Param("BollingerDeviation", 2.0).SetDisplay("Bollinger Deviation", "Standard deviations for Bollinger Bands", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(1))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._cooldown_bars = self.Param("CooldownBars", 500).SetDisplay("Cooldown Bars", "Bars to wait between trades", "General")

        self._cooldown = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(bollinger_band_reversal_strategy, self).OnReseted()
        self._cooldown = 0

    def OnStarted(self, time):
        super(bollinger_band_reversal_strategy, self).OnStarted(time)

        self._cooldown = 0

        bb = BollingerBands()
        bb.Length = self._bollinger_period.Value
        bb.Width = self._bollinger_deviation.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(bb, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, bb)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, bb_value):
        if candle.State != CandleStates.Finished:
            return

        if not bb_value.IsFormed:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        if self._cooldown > 0:
            self._cooldown -= 1
            return

        upper_band = bb_value.UpBand
        lower_band = bb_value.LowBand
        middle_band = bb_value.MovingAverage

        if upper_band is None or lower_band is None or middle_band is None:
            return

        close = float(candle.ClosePrice)
        ub = float(upper_band)
        lb = float(lower_band)
        mb = float(middle_band)
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
        return bollinger_band_reversal_strategy()
