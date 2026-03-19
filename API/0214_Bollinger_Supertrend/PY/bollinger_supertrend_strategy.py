import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import BollingerBands, AverageTrueRange
from StockSharp.Algo.Strategies import Strategy


class bollinger_supertrend_strategy(Strategy):
    def __init__(self):
        super(bollinger_supertrend_strategy, self).__init__()
        self._bollinger_period = self.Param("BollingerPeriod", 20) \
            .SetDisplay("Bollinger Period", "Period for Bollinger Bands calculation", "Indicators")
        self._bollinger_deviation = self.Param("BollingerDeviation", 2.0) \
            .SetDisplay("Bollinger Deviation", "Standard deviation multiplier for Bollinger Bands", "Indicators")
        self._supertrend_period = self.Param("SupertrendPeriod", 10) \
            .SetDisplay("Supertrend Period", "ATR period for Supertrend calculation", "Indicators")
        self._supertrend_multiplier = self.Param("SupertrendMultiplier", 3.0) \
            .SetDisplay("Supertrend Multiplier", "ATR multiplier for Supertrend calculation", "Indicators")
        self._cooldown_bars = self.Param("CooldownBars", 60) \
            .SetDisplay("Cooldown Bars", "Bars between trades", "General")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")
        self._is_long_trend = False
        self._supertrend_value = 0.0
        self._last_close = 0.0
        self._cooldown = 0

    @property
    def bollinger_period(self):
        return self._bollinger_period.Value
    @property
    def bollinger_deviation(self):
        return self._bollinger_deviation.Value
    @property
    def supertrend_period(self):
        return self._supertrend_period.Value
    @property
    def supertrend_multiplier(self):
        return self._supertrend_multiplier.Value
    @property
    def cooldown_bars(self):
        return self._cooldown_bars.Value
    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(bollinger_supertrend_strategy, self).OnReseted()
        self._is_long_trend = False
        self._supertrend_value = 0.0
        self._last_close = 0.0
        self._cooldown = 0

    def OnStarted(self, time):
        super(bollinger_supertrend_strategy, self).OnStarted(time)
        bollinger = BollingerBands()
        bollinger.Length = self.bollinger_period
        bollinger.Width = self.bollinger_deviation
        atr = AverageTrueRange()
        atr.Length = self.supertrend_period
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(bollinger, atr, self.OnProcess).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, bollinger)
            self.DrawOwnTrades(area)

    def OnProcess(self, candle, bollinger_value, atr_value):
        if candle.State != CandleStates.Finished:
            return
        bb = bollinger_value
        if bb.MovingAverage is None or bb.UpBand is None or bb.LowBand is None:
            return
        middle_band = float(bb.MovingAverage)
        upper_band = float(bb.UpBand)
        lower_band = float(bb.LowBand)

        atr_val = float(atr_value) * float(self.supertrend_multiplier)
        hl2 = (float(candle.HighPrice) + float(candle.LowPrice)) / 2.0
        upper_band2 = hl2 + atr_val
        lower_band2 = hl2 - atr_val

        if self._last_close == 0:
            self._supertrend_value = lower_band2 if float(candle.ClosePrice) > hl2 else upper_band2
            self._is_long_trend = float(candle.ClosePrice) > self._supertrend_value
        else:
            if self._is_long_trend:
                if float(candle.ClosePrice) < self._supertrend_value:
                    self._is_long_trend = False
                    self._supertrend_value = upper_band2
                else:
                    self._supertrend_value = max(lower_band2, self._supertrend_value)
            else:
                if float(candle.ClosePrice) > self._supertrend_value:
                    self._is_long_trend = True
                    self._supertrend_value = lower_band2
                else:
                    self._supertrend_value = min(upper_band2, self._supertrend_value)

        self._last_close = float(candle.ClosePrice)

        is_above_supertrend = float(candle.ClosePrice) > self._supertrend_value
        is_above_upper = float(candle.ClosePrice) > upper_band
        is_below_lower = float(candle.ClosePrice) < lower_band

        if self._cooldown > 0:
            self._cooldown -= 1

        if self._cooldown == 0 and is_above_upper and is_above_supertrend:
            if self.Position <= 0:
                self.BuyMarket()
                self._cooldown = self.cooldown_bars
        elif self._cooldown == 0 and is_below_lower and not is_above_supertrend:
            if self.Position >= 0:
                self.SellMarket()
                self._cooldown = self.cooldown_bars
        elif (self.Position > 0 and not is_above_supertrend) or (self.Position < 0 and is_above_supertrend):
            if self.Position > 0:
                self.SellMarket()
                self._cooldown = self.cooldown_bars
            elif self.Position < 0:
                self.BuyMarket()
                self._cooldown = self.cooldown_bars

    def CreateClone(self):
        return bollinger_supertrend_strategy()
