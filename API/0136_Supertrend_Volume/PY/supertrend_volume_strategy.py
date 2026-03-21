import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import AverageTrueRange
from StockSharp.Algo.Strategies import Strategy

class supertrend_volume_strategy(Strategy):
    """
    Supertrend Volume strategy.
    Manual Supertrend calculation with ATR for trend direction.
    Entries and exits based on Supertrend direction.
    """

    def __init__(self):
        super(supertrend_volume_strategy, self).__init__()
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._supertrend_period = self.Param("SupertrendPeriod", 10).SetDisplay("Supertrend Period", "Period for Supertrend ATR calculation", "Supertrend Settings")
        self._supertrend_multiplier = self.Param("SupertrendMultiplier", 3.0).SetDisplay("Supertrend Multiplier", "Multiplier for Supertrend ATR", "Supertrend Settings")
        self._cooldown_bars = self.Param("CooldownBars", 100).SetDisplay("Cooldown Bars", "Bars between trades", "General")

        self._upper_band = None
        self._lower_band = None
        self._supertrend = None
        self._is_bullish = None
        self._cooldown = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(supertrend_volume_strategy, self).OnReseted()
        self._upper_band = None
        self._lower_band = None
        self._supertrend = None
        self._is_bullish = None
        self._cooldown = 0

    def OnStarted(self, time):
        super(supertrend_volume_strategy, self).OnStarted(time)

        self._upper_band = None
        self._lower_band = None
        self._supertrend = None
        self._is_bullish = None
        self._cooldown = 0

        atr = AverageTrueRange()
        atr.Length = self._supertrend_period.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(atr, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)
            atr_area = self.CreateChartArea()
            if atr_area is not None:
                self.DrawIndicator(atr_area, atr)

    def _process_candle(self, candle, atr_iv):
        if candle.State != CandleStates.Finished:
            return

        if not atr_iv.IsFormed:
            return

        atr = float(atr_iv.Value)
        if atr <= 0:
            return

        close = float(candle.ClosePrice)
        high = float(candle.HighPrice)
        low = float(candle.LowPrice)
        mult = float(self._supertrend_multiplier.Value)
        cd = self._cooldown_bars.Value

        # Calculate Supertrend
        basic_price = (high + low) / 2.0
        new_upper = basic_price + mult * atr
        new_lower = basic_price - mult * atr

        if self._upper_band is None or self._lower_band is None or self._supertrend is None or self._is_bullish is None:
            self._upper_band = new_upper
            self._lower_band = new_lower
            self._supertrend = new_upper
            self._is_bullish = False
            return

        # Update upper band
        if new_upper < self._upper_band or close > self._upper_band:
            self._upper_band = new_upper

        # Update lower band
        if new_lower > self._lower_band or close < self._lower_band:
            self._lower_band = new_lower

        # Determine trend direction
        if self._supertrend == self._upper_band:
            if close > self._upper_band:
                self._supertrend = self._lower_band
                self._is_bullish = True
            else:
                self._supertrend = self._upper_band
                self._is_bullish = False
        else:
            if close < self._lower_band:
                self._supertrend = self._upper_band
                self._is_bullish = False
            else:
                self._supertrend = self._lower_band
                self._is_bullish = True

        if self._cooldown > 0:
            self._cooldown -= 1
            return

        # Entry: bullish supertrend
        if self._is_bullish and close > self._supertrend and self.Position == 0:
            self.BuyMarket()
            self._cooldown = cd
        # Entry: bearish supertrend
        elif not self._is_bullish and close < self._supertrend and self.Position == 0:
            self.SellMarket()
            self._cooldown = cd

        # Exit long on bearish flip
        if self.Position > 0 and not self._is_bullish:
            self.SellMarket()
            self._cooldown = cd
        # Exit short on bullish flip
        elif self.Position < 0 and self._is_bullish:
            self.BuyMarket()
            self._cooldown = cd

    def CreateClone(self):
        return supertrend_volume_strategy()
