import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import BollingerBands, SimpleMovingAverage, RelativeStrengthIndex, IndicatorHelper
from StockSharp.Algo.Strategies import Strategy


class bollinger_breakout2_strategy(Strategy):
    """Bollinger Breakout Strategy."""

    def __init__(self):
        super(bollinger_breakout2_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(30))) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")
        self._bollinger_length = self.Param("BollingerLength", 20) \
            .SetDisplay("Bollinger Length", "Bollinger Bands period", "Bollinger Bands")
        self._bollinger_multiplier = self.Param("BollingerMultiplier", 1.8) \
            .SetDisplay("StdDev Multiplier", "Standard deviation multiplier", "Bollinger Bands")
        self._trend_length = self.Param("TrendLength", 50) \
            .SetDisplay("Trend MA Length", "Length for trend moving average", "Filters")
        self._rsi_length = self.Param("RsiLength", 14) \
            .SetDisplay("RSI Length", "RSI calculation length", "Filters")
        self._cooldown_bars = self.Param("CooldownBars", 10) \
            .SetDisplay("Cooldown Bars", "Bars between trades", "Risk")

        self._prev_close = 0.0
        self._prev_upper = 0.0
        self._prev_lower = 0.0
        self._is_initial = True
        self._cooldown_remaining = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(bollinger_breakout2_strategy, self).OnReseted()
        self._prev_close = 0.0
        self._prev_upper = 0.0
        self._prev_lower = 0.0
        self._is_initial = True
        self._cooldown_remaining = 0

    def OnStarted2(self, time):
        super(bollinger_breakout2_strategy, self).OnStarted2(time)

        trend_sma = SimpleMovingAverage()
        trend_sma.Length = int(self._trend_length.Value)
        rsi = RelativeStrengthIndex()
        rsi.Length = int(self._rsi_length.Value)
        bb = BollingerBands()
        bb.Length = int(self._bollinger_length.Value)
        bb.Width = self._bollinger_multiplier.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(bb, trend_sma, rsi, self._on_process).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, bb)
            self.DrawOwnTrades(area)

    def _on_process(self, candle, bb_value, trend_value, rsi_value):
        if candle.State != CandleStates.Finished:
            return

        if bb_value.IsEmpty or trend_value.IsEmpty or rsi_value.IsEmpty:
            return

        if bb_value.UpBand is None or bb_value.LowBand is None or bb_value.MovingAverage is None:
            return

        upper_band = float(bb_value.UpBand)
        lower_band = float(bb_value.LowBand)
        middle_band = float(bb_value.MovingAverage)
        trend = float(IndicatorHelper.ToDecimal(trend_value))
        close = float(candle.ClosePrice)

        if self._is_initial:
            self._prev_close = close
            self._prev_upper = upper_band
            self._prev_lower = lower_band
            self._is_initial = False
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            self._prev_close = close
            self._prev_upper = upper_band
            self._prev_lower = lower_band
            return

        if self._cooldown_remaining > 0:
            self._cooldown_remaining -= 1
            self._prev_close = close
            self._prev_upper = upper_band
            self._prev_lower = lower_band
            return

        cooldown = int(self._cooldown_bars.Value)
        trend_long = close > trend
        trend_short = close < trend

        if close > upper_band and self._prev_close <= self._prev_upper and self.Position <= 0 and trend_long:
            if self.Position < 0:
                self.BuyMarket(Math.Abs(self.Position))
            self.BuyMarket(self.Volume)
            self._cooldown_remaining = cooldown
        elif close < lower_band and self._prev_close >= self._prev_lower and self.Position >= 0 and trend_short:
            if self.Position > 0:
                self.SellMarket(Math.Abs(self.Position))
            self.SellMarket(self.Volume)
            self._cooldown_remaining = cooldown
        elif self.Position > 0 and close < middle_band:
            self.SellMarket(Math.Abs(self.Position))
            self._cooldown_remaining = cooldown
        elif self.Position < 0 and close > middle_band:
            self.BuyMarket(Math.Abs(self.Position))
            self._cooldown_remaining = cooldown

        self._prev_close = close
        self._prev_upper = upper_band
        self._prev_lower = lower_band

    def CreateClone(self):
        return bollinger_breakout2_strategy()
