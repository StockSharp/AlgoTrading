import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import AverageTrueRange, RelativeStrengthIndex, SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy


class volume_value_when_velocity_strategy(Strategy):
    def __init__(self):
        super(volume_value_when_velocity_strategy, self).__init__()
        self._rsi_length = self.Param("RsiLength", 40) \
            .SetDisplay("RSI Length", "RSI period", "Indicators")
        self._rsi_oversold = self.Param("RsiOversold", 60) \
            .SetDisplay("RSI Oversold", "Oversold level", "Indicators")
        self._atr_small = self.Param("AtrSmall", 5) \
            .SetDisplay("ATR Small", "Short ATR period", "Indicators")
        self._atr_big = self.Param("AtrBig", 14) \
            .SetDisplay("ATR Big", "Long ATR period", "Indicators")
        self._distance = self.Param("Distance", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Distance", "Minimum distance between breakouts", "Strategy")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Type of candles", "General")

    @property
    def rsi_length(self):
        return self._rsi_length.Value

    @property
    def rsi_oversold(self):
        return self._rsi_oversold.Value

    @property
    def atr_small(self):
        return self._atr_small.Value

    @property
    def atr_big(self):
        return self._atr_big.Value

    @property
    def distance(self):
        return self._distance.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(volume_value_when_velocity_strategy, self).OnReseted()

    def OnStarted(self, time):
        super(volume_value_when_velocity_strategy, self).OnStarted(time)
        rsi = RelativeStrengthIndex()
        rsi.Length = self.rsi_length
        atr_short = AverageTrueRange()
        atr_short.Length = self.atr_small
        atr_long = AverageTrueRange()
        atr_long.Length = self.atr_big
        sma = SimpleMovingAverage()
        sma.Length = 13
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(rsi, atr_short, atr_long, sma, self.on_process).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, sma)
            self.DrawOwnTrades(area)

    def on_process(self, candle, rsi_value, atr_short_value, atr_long_value, sma_value):
        if candle.State != CandleStates.Finished:
            return
        # track volumes for simple comparison
        if self._prev_volume1 == 0:
            self._prev_volume1 = candle.TotalVolume
            return
        # update bars since last SMA breakout
        if candle.ClosePrice > sma_value:
            self._bars_since_cross = 0
            self._prev_cross = self._last_cross
            self._last_cross = candle.ClosePrice
        else:
            self._bars_since_cross += 1
        prev_close_change = self._prev_cross - self._last_cross
        was_oversold = rsi_value <= self.rsi_oversold
        atr_condition = atr_short_value < atr_long_value
        volume_condition = candle.TotalVolume > self._prev_volume1 and self._prev_volume1 > self._prev_volume2
        self._prev_volume2 = self._prev_volume1
        self._prev_volume1 = candle.TotalVolume
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        if volume_condition and atr_condition and was_oversold and prev_close_change > self.distance and self._bars_since_cross < 5 and self.Position <= 0:
            self.BuyMarket()

    def CreateClone(self):
        return volume_value_when_velocity_strategy()
