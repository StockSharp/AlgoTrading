import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Decimal
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import AverageDirectionalIndex, AverageTrueRange, SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy

# ADX level above which the trend counts as established.
ADX_ENTRY_THRESHOLD = Decimal(25)

class adx_trend_strategy(Strategy):
    """
    Strategy based on Average Directional Index (ADX) trend.
    While ADX is above 25 it holds a long position when the close is above the moving average and a short one when it is below.
    The position closes when ADX falls below the exit threshold or the close crosses the ATR stop set at entry,
    and reverses when the opposite setup appears.
    """

    def __init__(self):
        super(adx_trend_strategy, self).__init__()
        self._adx_period = self.Param("AdxPeriod", 14) \
            .SetGreaterThanZero() \
            .SetDisplay("ADX Period", "Period for the ADX and the ATR of the stop", "Indicators") \
            .SetOptimize(10, 30, 2)
        self._ma_period = self.Param("MaPeriod", 50) \
            .SetGreaterThanZero() \
            .SetDisplay("MA Period", "Period for calculating Moving Average", "Indicators") \
            .SetOptimize(20, 100, 10)
        self._atr_multiplier = self.Param("AtrMultiplier", 2.0) \
            .SetGreaterThanZero() \
            .SetDisplay("ATR Multiplier", "Stop distance in entry ATR multiples", "Risk parameters") \
            .SetOptimize(1.0, 3.0, 0.5)
        self._adx_exit_threshold = self.Param("AdxExitThreshold", 20) \
            .SetRange(0, 100) \
            .SetDisplay("ADX Exit Threshold", "ADX level below which to exit position", "Exit parameters") \
            .SetOptimize(15, 25, 1)
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")

        self._stop_price = Decimal(0)

    def OnReseted(self):
        super(adx_trend_strategy, self).OnReseted()
        self._stop_price = Decimal(0)

    def OnStarted2(self, time):
        super(adx_trend_strategy, self).OnStarted2(time)

        adx = AverageDirectionalIndex()
        adx.Length = self._adx_period.Value
        atr = AverageTrueRange()
        atr.Length = self._adx_period.Value
        ma = SimpleMovingAverage()
        ma.Length = self._ma_period.Value

        subscription = self.SubscribeCandles(self._candle_type.Value)
        subscription.BindEx(adx, atr, ma, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, adx)
            self.DrawIndicator(area, ma)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, adx_value, atr_value, ma_value):
        if candle.State != CandleStates.Finished or not adx_value.IsFormed or not atr_value.IsFormed or not ma_value.IsFormed:
            return
        if adx_value.MovingAverage is None:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        adx = adx_value.MovingAverage
        close = candle.ClosePrice
        ma = ma_value.GetValue[Decimal](None)
        atr = atr_value.GetValue[Decimal](None)
        offset = Decimal(self._atr_multiplier.Value) * atr
        exit_level = Decimal(self._adx_exit_threshold.Value)

        trending = adx > ADX_ENTRY_THRESHOLD
        long_setup = trending and close > ma
        short_setup = trending and close < ma
        position = self.Position

        if position > 0:
            if short_setup:
                self.SellMarket(self.Volume + position)
                self._stop_price = close + offset
            elif adx < exit_level or close <= self._stop_price:
                self.SellMarket(position)
                self._stop_price = Decimal(0)
        elif position < 0:
            if long_setup:
                self.BuyMarket(self.Volume - position)
                self._stop_price = close - offset
            elif adx < exit_level or close >= self._stop_price:
                self.BuyMarket(-position)
                self._stop_price = Decimal(0)
        elif long_setup:
            self.BuyMarket(self.Volume)
            self._stop_price = close - offset
        elif short_setup:
            self.SellMarket(self.Volume)
            self._stop_price = close + offset

    def CreateClone(self):
        return adx_trend_strategy()
