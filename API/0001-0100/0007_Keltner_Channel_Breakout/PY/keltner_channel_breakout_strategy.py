import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Decimal
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage, AverageTrueRange
from StockSharp.Algo.Strategies import Strategy

class keltner_channel_breakout_strategy(Strategy):
    """
    Strategy based on Keltner Channel breakout.
    The channel is an EMA with bands AtrMultiplier ATRs away. A close that breaks above the upper band opens a long position,
    one that breaks below the lower band a short one. The position closes when the close crosses back through the EMA
    or reaches the stop set AtrMultiplier ATRs from the entry close.
    """

    def __init__(self):
        super(keltner_channel_breakout_strategy, self).__init__()
        self._ema_period = self.Param("EmaPeriod", 20) \
            .SetGreaterThanZero() \
            .SetDisplay("EMA Period", "Period for Exponential Moving Average", "Indicators") \
            .SetOptimize(10, 50, 5)
        self._atr_period = self.Param("AtrPeriod", 14) \
            .SetGreaterThanZero() \
            .SetDisplay("ATR Period", "Period for Average True Range", "Indicators") \
            .SetOptimize(10, 30, 2)
        self._atr_multiplier = self.Param("AtrMultiplier", 2.0) \
            .SetGreaterThanZero() \
            .SetDisplay("ATR Multiplier", "Band and stop distance in ATR multiples", "Indicators") \
            .SetOptimize(1.0, 3.0, 0.5)
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")

        # Close and bands of the previous candle.
        self._prev_close = None
        self._prev_upper = Decimal(0)
        self._prev_lower = Decimal(0)
        self._stop_price = Decimal(0)

    def OnReseted(self):
        super(keltner_channel_breakout_strategy, self).OnReseted()
        self._prev_close = None
        self._prev_upper = Decimal(0)
        self._prev_lower = Decimal(0)
        self._stop_price = Decimal(0)

    def OnStarted2(self, time):
        super(keltner_channel_breakout_strategy, self).OnStarted2(time)

        ema = ExponentialMovingAverage()
        ema.Length = self._ema_period.Value
        atr = AverageTrueRange()
        atr.Length = self._atr_period.Value

        subscription = self.SubscribeCandles(self._candle_type.Value)
        subscription.BindEx(ema, atr, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, ema)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, ema_value, atr_value):
        if candle.State != CandleStates.Finished or not ema_value.IsFormed or not atr_value.IsFormed:
            return

        center = ema_value.GetValue[Decimal](None)
        offset = Decimal(self._atr_multiplier.Value) * atr_value.GetValue[Decimal](None)
        close = candle.ClosePrice

        prev_close = self._prev_close
        prev_upper = self._prev_upper
        prev_lower = self._prev_lower

        self._prev_close = close
        self._prev_upper = center + offset
        self._prev_lower = center - offset

        if prev_close is None or not self.IsFormedAndOnlineAndAllowTrading():
            return

        # A breakout is the first close beyond the band of the previous candle.
        upper_breakout = close > prev_upper and prev_close <= prev_upper
        lower_breakout = close < prev_lower and prev_close >= prev_lower
        position = self.Position

        if upper_breakout and position <= 0:
            self.BuyMarket(self.Volume + abs(position))
            self._stop_price = close - offset
        elif lower_breakout and position >= 0:
            self.SellMarket(self.Volume + abs(position))
            self._stop_price = close + offset
        elif position > 0 and (close < center or close <= self._stop_price):
            self.SellMarket(position)
        elif position < 0 and (close > center or close >= self._stop_price):
            self.BuyMarket(-position)

    def CreateClone(self):
        return keltner_channel_breakout_strategy()
