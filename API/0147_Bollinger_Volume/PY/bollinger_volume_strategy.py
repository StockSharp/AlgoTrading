import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import BollingerBands
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *


class bollinger_volume_strategy(Strategy):
    """
    Strategy that uses Bollinger Bands for mean reversion.
    Enters when price touches/breaks bands, exits at middle band.
    """

    def __init__(self):
        super(bollinger_volume_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", tf(5)) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")

        self._bollinger_period = self.Param("BollingerPeriod", 20) \
            .SetRange(10, 50) \
            .SetDisplay("Bollinger Period", "Period of the Bollinger Bands", "Indicators")

        self._bollinger_deviation = self.Param("BollingerDeviation", 2.0) \
            .SetDisplay("Bollinger Deviation", "Standard deviation multiplier", "Indicators")

        self._cooldown_bars = self.Param("CooldownBars", 100) \
            .SetDisplay("Cooldown Bars", "Bars between trades", "General") \
            .SetRange(5, 500)

        self._cooldown = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    @candle_type.setter
    def candle_type(self, value):
        self._candle_type.Value = value

    @property
    def bollinger_period(self):
        return self._bollinger_period.Value

    @property
    def bollinger_deviation(self):
        return self._bollinger_deviation.Value

    @property
    def cooldown_bars(self):
        return self._cooldown_bars.Value

    def OnStarted(self, time):
        super(bollinger_volume_strategy, self).OnStarted(time)

        self._cooldown = 0

        bollinger = BollingerBands()
        bollinger.Length = self.bollinger_period
        bollinger.Width = self.bollinger_deviation

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(bollinger, self.ProcessCandle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, bollinger)
            self.DrawOwnTrades(area)

    def ProcessCandle(self, candle, bb_value):
        if candle.State != CandleStates.Finished:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        if bb_value.UpBand is None or bb_value.LowBand is None or bb_value.MovingAverage is None:
            return

        upper_band = float(bb_value.UpBand)
        lower_band = float(bb_value.LowBand)
        middle_band = float(bb_value.MovingAverage)

        close = float(candle.ClosePrice)

        if self._cooldown > 0:
            self._cooldown -= 1
            return

        # Long: price below lower band (mean reversion buy)
        if close < lower_band and self.Position == 0:
            self.BuyMarket()
            self._cooldown = self.cooldown_bars
        # Short: price above upper band (mean reversion sell)
        elif close > upper_band and self.Position == 0:
            self.SellMarket()
            self._cooldown = self.cooldown_bars

        # Exit long: price returns to middle band
        if self.Position > 0 and close > middle_band:
            self.SellMarket()
            self._cooldown = self.cooldown_bars
        # Exit short: price returns to middle band
        elif self.Position < 0 and close < middle_band:
            self.BuyMarket()
            self._cooldown = self.cooldown_bars

    def OnReseted(self):
        super(bollinger_volume_strategy, self).OnReseted()
        self._cooldown = 0

    def CreateClone(self):
        return bollinger_volume_strategy()
