import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import SuperTrend
from StockSharp.Algo.Strategies import Strategy

class tradingview_supertrend_flip_strategy(Strategy):
    """
    Strategy based on Supertrend indicator flips.
    Detects when Supertrend direction changes and trades accordingly.
    """

    def __init__(self):
        super(tradingview_supertrend_flip_strategy, self).__init__()
        self._supertrend_period = self.Param("SupertrendPeriod", 10).SetDisplay("Supertrend Period", "Period for Supertrend calculation", "Indicators")
        self._supertrend_multiplier = self.Param("SupertrendMultiplier", 4.0).SetDisplay("Supertrend Multiplier", "Multiplier for Supertrend", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))).SetDisplay("Candle Type", "Type of candles to use", "General")

        self._prev_supertrend_value = 0.0
        self._prev_is_up_trend = False
        self._has_prev_values = False

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(tradingview_supertrend_flip_strategy, self).OnReseted()
        self._prev_supertrend_value = 0.0
        self._prev_is_up_trend = False
        self._has_prev_values = False

    def OnStarted(self, time):
        super(tradingview_supertrend_flip_strategy, self).OnStarted(time)

        supertrend = SuperTrend()
        supertrend.Length = self._supertrend_period.Value
        supertrend.Multiplier = self._supertrend_multiplier.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(supertrend, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, supertrend)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, st_val):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        sv = float(st_val)
        if sv == 0:
            return

        is_up_trend = float(candle.ClosePrice) > sv

        if not self._has_prev_values:
            self._has_prev_values = True
            self._prev_is_up_trend = is_up_trend
            self._prev_supertrend_value = sv
            return

        flipped_bullish = is_up_trend and not self._prev_is_up_trend
        flipped_bearish = not is_up_trend and self._prev_is_up_trend

        self._prev_is_up_trend = is_up_trend
        self._prev_supertrend_value = sv

        if flipped_bullish and self.Position <= 0:
            self.BuyMarket()
        elif flipped_bearish and self.Position >= 0:
            self.SellMarket()

    def CreateClone(self):
        return tradingview_supertrend_flip_strategy()
