import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import AverageTrueRange
from StockSharp.Algo.Strategies import Strategy


class order_stabilization_strategy(Strategy):
    """Trades breakouts after periods of low volatility (stabilization).
    When previous candle body is small relative to ATR, waits for directional breakout.
    Goes long on bullish breakout candle, short on bearish."""

    def __init__(self):
        super(order_stabilization_strategy, self).__init__()

        self._atr_period = self.Param("AtrPeriod", 14) \
            .SetDisplay("ATR Period", "ATR period for volatility", "Indicators")
        self._stabilization_factor = self.Param("StabilizationFactor", 0.5) \
            .SetDisplay("Stabilization Factor", "Body must be less than ATR * factor for stabilization", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Candle timeframe", "General")

        self._prev_body = 0.0
        self._prev_atr = 0.0
        self._has_prev = False

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
    def StabilizationFactor(self):
        return self._stabilization_factor.Value

    def OnReseted(self):
        super(order_stabilization_strategy, self).OnReseted()
        self._prev_body = 0.0
        self._prev_atr = 0.0
        self._has_prev = False

    def OnStarted(self, time):
        super(order_stabilization_strategy, self).OnStarted(time)

        self._has_prev = False

        atr = AverageTrueRange()
        atr.Length = self.AtrPeriod

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(atr, self._process_candle).Start()

    def _process_candle(self, candle, atr_value):
        if candle.State != CandleStates.Finished:
            return

        atr_val = float(atr_value)
        close = float(candle.ClosePrice)
        open_price = float(candle.OpenPrice)
        body = abs(close - open_price)
        factor = float(self.StabilizationFactor)
        threshold = atr_val * factor

        if not self._has_prev:
            self._prev_body = body
            self._prev_atr = atr_val
            self._has_prev = True
            return

        prev_threshold = self._prev_atr * factor
        was_stabilized = self._prev_body < prev_threshold

        # After stabilization, trade breakout candle
        if was_stabilized and body > threshold:
            bullish = close > open_price

            if bullish and self.Position <= 0:
                if self.Position < 0:
                    self.BuyMarket()
                self.BuyMarket()
            elif not bullish and self.Position >= 0:
                if self.Position > 0:
                    self.SellMarket()
                self.SellMarket()

        self._prev_body = body
        self._prev_atr = atr_val

    def CreateClone(self):
        return order_stabilization_strategy()
