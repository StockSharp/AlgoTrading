import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import DonchianChannels
from StockSharp.Algo.Strategies import Strategy

class donchian_reversal_strategy(Strategy):
    """
    Donchian Reversal strategy.
    Enters long when price bounces from the lower Donchian Channel band.
    Enters short when price bounces from the upper Donchian Channel band.
    Exits at middle band.
    Uses cooldown to control trade frequency.
    """

    def __init__(self):
        super(donchian_reversal_strategy, self).__init__()
        self._period = self.Param("Period", 20).SetDisplay("Period", "Period for Donchian Channel", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(1))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._cooldown_bars = self.Param("CooldownBars", 500).SetDisplay("Cooldown Bars", "Bars to wait between trades", "General")

        self._prev_close = 0.0
        self._cooldown = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(donchian_reversal_strategy, self).OnReseted()
        self._prev_close = 0.0
        self._cooldown = 0

    def OnStarted(self, time):
        super(donchian_reversal_strategy, self).OnStarted(time)

        self._prev_close = 0.0
        self._cooldown = 0

        donchian = DonchianChannels()
        donchian.Length = self._period.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(donchian, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, donchian)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, donchian_iv):
        if candle.State != CandleStates.Finished:
            return

        if not donchian_iv.IsFormed:
            return

        upper_val = donchian_iv.UpperBand
        lower_val = donchian_iv.LowerBand
        middle_val = donchian_iv.Middle

        if upper_val is None or lower_val is None or middle_val is None:
            return

        upper = float(upper_val)
        lower = float(lower_val)
        middle = float(middle_val)

        close = float(candle.ClosePrice)

        if self._prev_close == 0:
            self._prev_close = close
            return

        if self._cooldown > 0:
            self._cooldown -= 1
            self._prev_close = close
            return

        cd = self._cooldown_bars.Value

        # Bounce from lower band = bullish
        bounced_from_lower = self._prev_close <= lower and close > lower
        # Bounce from upper band = bearish
        bounced_from_upper = self._prev_close >= upper and close < upper

        if self.Position == 0 and bounced_from_lower:
            self.BuyMarket()
            self._cooldown = cd
        elif self.Position == 0 and bounced_from_upper:
            self.SellMarket()
            self._cooldown = cd
        elif self.Position > 0 and close >= middle and bounced_from_upper:
            self.SellMarket()
            self._cooldown = cd
        elif self.Position < 0 and close <= middle and bounced_from_lower:
            self.BuyMarket()
            self._cooldown = cd

        self._prev_close = close

    def CreateClone(self):
        return donchian_reversal_strategy()
