import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import Ichimoku
from StockSharp.Algo.Strategies import Strategy

class ichimoku_cloud_breakout_only_long_strategy(Strategy):
    """
    Ichimoku cloud breakout (long only) strategy.
    A long opens when the close crosses above the top of the Ichimoku cloud, max(SenkouA, SenkouB), and closes when the close crosses
    below the bottom of the cloud, min(SenkouA, SenkouB). There are no stops.
    """

    def __init__(self):
        super(ichimoku_cloud_breakout_only_long_strategy, self).__init__()
        self._tenkan_period = self.Param("TenkanPeriod", 9).SetGreaterThanZero().SetDisplay("Tenkan Period", "Tenkan-sen period", "Ichimoku")
        self._kijun_period = self.Param("KijunPeriod", 26).SetGreaterThanZero().SetDisplay("Kijun Period", "Kijun-sen period", "Ichimoku")
        self._senkou_span_b_period = self.Param("SenkouSpanBPeriod", 52).SetGreaterThanZero().SetDisplay("Senkou Span B Period", "Senkou Span B period", "Ichimoku")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(1))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._reset_state()

    @property
    def candle_type(self):
        return self._candle_type.Value

    def _reset_state(self):
        self._prev_close = None
        self._prev_top = None
        self._prev_bottom = None

    def OnReseted(self):
        super(ichimoku_cloud_breakout_only_long_strategy, self).OnReseted()
        self._reset_state()

    def OnStarted2(self, time):
        super(ichimoku_cloud_breakout_only_long_strategy, self).OnStarted2(time)

        self._reset_state()

        ichimoku = Ichimoku()
        ichimoku.Tenkan.Length = self._tenkan_period.Value
        ichimoku.Kijun.Length = self._kijun_period.Value
        ichimoku.SenkouB.Length = self._senkou_span_b_period.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(ichimoku, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, ichimoku)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, ichimoku_value):
        if candle.State != CandleStates.Finished:
            return

        if not ichimoku_value.IsFormed or ichimoku_value.SenkouA is None or ichimoku_value.SenkouB is None:
            return

        close = candle.ClosePrice
        senkou_a = ichimoku_value.SenkouA
        senkou_b = ichimoku_value.SenkouB
        top = max(senkou_a, senkou_b)
        bottom = min(senkou_a, senkou_b)

        prev_close = self._prev_close
        prev_top = self._prev_top
        prev_bottom = self._prev_bottom
        self._prev_close = close
        self._prev_top = top
        self._prev_bottom = bottom

        if prev_close is None or prev_top is None or prev_bottom is None:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        if self.Position <= 0 and prev_close <= prev_top and close > top:
            self.BuyMarket(self.Volume + abs(self.Position))
        elif self.Position > 0 and prev_close >= prev_bottom and close < bottom:
            self.SellMarket(self.Position)

    def CreateClone(self):
        return ichimoku_cloud_breakout_only_long_strategy()
