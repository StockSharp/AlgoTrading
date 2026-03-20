import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage, RelativeStrengthIndex, DecimalIndicatorValue
from StockSharp.Algo.Strategies import Strategy


class color_schaff_trend_cycle_strategy(Strategy):
    def __init__(self):
        super(color_schaff_trend_cycle_strategy, self).__init__()
        self._fast_period = self.Param("FastPeriod", 12) \
            .SetDisplay("Fast EMA", "Fast EMA period", "Indicator")
        self._slow_period = self.Param("SlowPeriod", 26) \
            .SetDisplay("Slow EMA", "Slow EMA period", "Indicator")
        self._high_level = self.Param("HighLevel", 60.0) \
            .SetDisplay("High Level", "Overbought level", "Indicator")
        self._low_level = self.Param("LowLevel", 40.0) \
            .SetDisplay("Low Level", "Oversold level", "Indicator")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Timeframe for analysis", "General")
        self._fast_ema = None
        self._slow_ema = None
        self._prev_stc = 50.0
        self._has_prev = False

    @property
    def fast_period(self):
        return self._fast_period.Value

    @property
    def slow_period(self):
        return self._slow_period.Value

    @property
    def high_level(self):
        return self._high_level.Value

    @property
    def low_level(self):
        return self._low_level.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(color_schaff_trend_cycle_strategy, self).OnReseted()
        self._fast_ema = None
        self._slow_ema = None
        self._prev_stc = 50.0
        self._has_prev = False

    def OnStarted(self, time):
        super(color_schaff_trend_cycle_strategy, self).OnStarted(time)
        self._fast_ema = ExponentialMovingAverage()
        self._fast_ema.Length = self.fast_period
        self._slow_ema = ExponentialMovingAverage()
        self._slow_ema.Length = self.slow_period
        self._prev_stc = 50.0
        self._has_prev = False
        self.Indicators.Add(self._fast_ema)
        self.Indicators.Add(self._slow_ema)
        rsi = RelativeStrengthIndex()
        rsi.Length = 10
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(rsi, self.process_candle).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, rsi)
            self.DrawOwnTrades(area)

    def process_candle(self, candle, rsi_value):
        if candle.State != CandleStates.Finished:
            return
        rsi_value = float(rsi_value)
        fast_input = DecimalIndicatorValue(self._fast_ema, candle.ClosePrice, candle.OpenTime)
        fast_input.IsFinal = True
        fast_result = self._fast_ema.Process(fast_input)
        slow_input = DecimalIndicatorValue(self._slow_ema, candle.ClosePrice, candle.OpenTime)
        slow_input.IsFinal = True
        slow_result = self._slow_ema.Process(slow_input)
        if not fast_result.IsFormed or not slow_result.IsFormed:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        macd = float(fast_result) - float(slow_result)
        stc = rsi_value
        high = float(self.high_level)
        low = float(self.low_level)
        if not self._has_prev:
            self._prev_stc = stc
            self._has_prev = True
            return
        # Rising above high level - bullish
        if self._prev_stc <= high and stc > high and macd > 0:
            if self.Position < 0:
                self.BuyMarket()
            if self.Position <= 0:
                self.BuyMarket()
        # Falling below low level - bearish
        elif self._prev_stc >= low and stc < low and macd < 0:
            if self.Position > 0:
                self.SellMarket()
            if self.Position >= 0:
                self.SellMarket()
        self._prev_stc = stc

    def CreateClone(self):
        return color_schaff_trend_cycle_strategy()
