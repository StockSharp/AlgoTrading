import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import AverageTrueRange
from StockSharp.Algo.Strategies import Strategy


class alexav_speed_up_m1_body_break_strategy(Strategy):
    """Alexav SpeedUp M1 Body strategy.
    Trades based on large candle body breakouts.
    Buys after a large bullish candle, sells after a large bearish candle.
    Uses ATR to define large body threshold."""

    def __init__(self):
        super(alexav_speed_up_m1_body_break_strategy, self).__init__()

        self._atr_period = self.Param("AtrPeriod", 14) \
            .SetDisplay("ATR Period", "ATR period for body threshold", "Indicators")
        self._body_multiplier = self.Param("BodyMultiplier", 1.0) \
            .SetDisplay("Body Multiplier", "Body must exceed ATR * multiplier", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Candle timeframe", "General")

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    @property
    def AtrPeriod(self):
        return self._atr_period.Value

    @property
    def BodyMultiplier(self):
        return self._body_multiplier.Value

    def OnReseted(self):
        super(alexav_speed_up_m1_body_break_strategy, self).OnReseted()

    def OnStarted(self, time):
        super(alexav_speed_up_m1_body_break_strategy, self).OnStarted(time)

        atr = AverageTrueRange()
        atr.Length = self.AtrPeriod

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(atr, self._process_candle).Start()

    def _process_candle(self, candle, atr_value):
        if candle.State != CandleStates.Finished:
            return

        close = float(candle.ClosePrice)
        open_price = float(candle.OpenPrice)
        body = close - open_price
        abs_body = abs(body)
        atr_val = float(atr_value)
        threshold = atr_val * float(self.BodyMultiplier)

        if abs_body < threshold:
            return

        # Large bullish candle - buy
        if body > 0 and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket()
            self.BuyMarket()
        # Large bearish candle - sell
        elif body < 0 and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket()
            self.SellMarket()

    def CreateClone(self):
        return alexav_speed_up_m1_body_break_strategy()
