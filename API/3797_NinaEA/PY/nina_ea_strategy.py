import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import SuperTrend
from StockSharp.Algo.Strategies import Strategy


class nina_ea_strategy(Strategy):
    """SuperTrend-based strategy. Trades on SuperTrend direction flips.
    Go long on up trend, go short on down trend."""

    def __init__(self):
        super(nina_ea_strategy, self).__init__()

        self._atr_period = self.Param("AtrPeriod", 10) \
            .SetDisplay("ATR Period", "ATR length for SuperTrend", "Indicators")
        self._atr_multiplier = self.Param("AtrMultiplier", 1.0) \
            .SetDisplay("ATR Multiplier", "SuperTrend multiplier", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Candle timeframe", "General")

        self._previous_trend_up = None

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
    def AtrMultiplier(self):
        return self._atr_multiplier.Value

    def OnReseted(self):
        super(nina_ea_strategy, self).OnReseted()
        self._previous_trend_up = None

    def OnStarted(self, time):
        super(nina_ea_strategy, self).OnStarted(time)

        self._previous_trend_up = None

        super_trend = SuperTrend()
        super_trend.Length = self.AtrPeriod
        super_trend.Multiplier = self.AtrMultiplier

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.BindEx(super_trend, self._process_candle).Start()

    def _process_candle(self, candle, super_trend_value):
        if candle.State != CandleStates.Finished:
            return

        if not super_trend_value.IsFinal:
            return

        is_up_trend = super_trend_value.IsUpTrend if hasattr(super_trend_value, 'IsUpTrend') else None
        if is_up_trend is None:
            return

        if self._previous_trend_up is not None:
            prev_up = self._previous_trend_up

            # Trend flipped to up - go long
            if is_up_trend and not prev_up:
                if self.Position < 0:
                    self.BuyMarket()
                if self.Position <= 0:
                    self.BuyMarket()
            # Trend flipped to down - go short
            elif not is_up_trend and prev_up:
                if self.Position > 0:
                    self.SellMarket()
                if self.Position >= 0:
                    self.SellMarket()

        self._previous_trend_up = is_up_trend

    def CreateClone(self):
        return nina_ea_strategy()
