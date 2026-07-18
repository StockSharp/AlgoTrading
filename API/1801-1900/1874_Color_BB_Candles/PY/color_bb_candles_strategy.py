import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import BollingerBands
from StockSharp.Algo.Strategies import Strategy


class color_bb_candles_strategy(Strategy):
    NEUTRAL = 0
    ABOVE = 1
    BELOW = 2

    def __init__(self):
        super(color_bb_candles_strategy, self).__init__()
        self._bollinger_period = self.Param("BollingerPeriod", 100) \
            .SetDisplay("Bollinger Period", "Length of Bollinger Bands", "General")
        self._bollinger_deviation = self.Param("BollingerDeviation", 1.5) \
            .SetDisplay("Bollinger Deviation", "Width of Bollinger Bands", "General")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(1))) \
            .SetDisplay("Candle Type", "Type of candles", "General")
        self._breakout_percent = self.Param("BreakoutPercent", 0.0005) \
            .SetDisplay("Breakout %", "Minimum breakout beyond the Bollinger band", "Filters")
        self._cooldown_bars = self.Param("CooldownBars", 3) \
            .SetDisplay("Cooldown Bars", "Completed candles to wait after a position change", "Trading")
        self._previous_state = self.NEUTRAL
        self._cooldown_remaining = 0

    @property
    def bollinger_period(self):
        return self._bollinger_period.Value
    @property
    def bollinger_deviation(self):
        return self._bollinger_deviation.Value
    @property
    def candle_type(self):
        return self._candle_type.Value
    @property
    def breakout_percent(self):
        return self._breakout_percent.Value
    @property
    def cooldown_bars(self):
        return self._cooldown_bars.Value

    def OnReseted(self):
        super(color_bb_candles_strategy, self).OnReseted()
        self._previous_state = self.NEUTRAL
        self._cooldown_remaining = 0

    def OnStarted2(self, time):
        super(color_bb_candles_strategy, self).OnStarted2(time)
        bollinger = BollingerBands()
        bollinger.Length = self.bollinger_period
        bollinger.Width = float(self.bollinger_deviation)
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(bollinger, self.process_candle).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, bollinger)
            self.DrawOwnTrades(area)

    def process_candle(self, candle, bb_value):
        if candle.State != CandleStates.Finished or not bb_value.IsFinal:
            return
        if self._cooldown_remaining > 0:
            self._cooldown_remaining -= 1
        upper_band = bb_value.UpBand
        lower_band = bb_value.LowBand
        if upper_band is None or lower_band is None:
            return
        upper_band = float(upper_band)
        lower_band = float(lower_band)
        close = float(candle.ClosePrice)
        bp = float(self.breakout_percent)
        state = self.NEUTRAL
        if close > upper_band * (1.0 + bp):
            state = self.ABOVE
        elif close < lower_band * (1.0 - bp):
            state = self.BELOW
        if self._cooldown_remaining == 0:
            if state == self.ABOVE and self._previous_state != self.ABOVE:
                if self.Position < 0:
                    self.BuyMarket()
                self.BuyMarket()
                self._cooldown_remaining = self.cooldown_bars
            elif state == self.BELOW and self._previous_state != self.BELOW:
                if self.Position > 0:
                    self.SellMarket()
                self.SellMarket()
                self._cooldown_remaining = self.cooldown_bars
            elif state == self.NEUTRAL and self._previous_state != self.NEUTRAL:
                if self.Position > 0:
                    self.SellMarket()
                elif self.Position < 0:
                    self.BuyMarket()
                self._cooldown_remaining = self.cooldown_bars
        self._previous_state = state

    def CreateClone(self):
        return color_bb_candles_strategy()
