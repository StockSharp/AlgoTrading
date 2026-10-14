import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import Ichimoku
from StockSharp.Algo.Strategies import Strategy

class ichimoku_kumo_breakout_strategy(Strategy):
    """
    Strategy based on Ichimoku Kumo (cloud) breakout.
    Buys when the close is above the cloud with Tenkan-sen above Kijun-sen, sells when it is below the cloud with Tenkan-sen
    below Kijun-sen, acting when the last of the two conditions appears. A position is held until the close goes through the cloud.
    """

    def __init__(self):
        super(ichimoku_kumo_breakout_strategy, self).__init__()
        self._tenkan_period = self.Param("TenkanPeriod", 9).SetGreaterThanZero().SetDisplay("Tenkan Period", "Tenkan-sen period", "Indicators")
        self._kijun_period = self.Param("KijunPeriod", 26).SetGreaterThanZero().SetDisplay("Kijun Period", "Kijun-sen period", "Indicators")
        self._senkou_span_period = self.Param("SenkouSpanPeriod", 52).SetGreaterThanZero().SetDisplay("Senkou Span B Period", "Period for Senkou Span B", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(15))).SetDisplay("Candle Type", "Timeframe", "General")

        self._prev_long_setup = None
        self._prev_short_setup = None

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(ichimoku_kumo_breakout_strategy, self).OnReseted()
        self._prev_long_setup = None
        self._prev_short_setup = None

    def OnStarted2(self, time):
        super(ichimoku_kumo_breakout_strategy, self).OnStarted2(time)

        ichimoku = Ichimoku()
        ichimoku.Tenkan.Length = self._tenkan_period.Value
        ichimoku.Kijun.Length = self._kijun_period.Value
        ichimoku.SenkouB.Length = self._senkou_span_period.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(ichimoku, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, ichimoku)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, value):
        if candle.State != CandleStates.Finished:
            return

        # The cloud is plotted ahead, so the spans of this candle were set Kijun periods ago.
        tenkan = value.Tenkan
        kijun = value.Kijun
        span_a = value.SenkouA
        span_b = value.SenkouB
        if tenkan is None or kijun is None or span_a is None or span_b is None:
            return

        close = candle.ClosePrice
        cloud_top = Math.Max(span_a, span_b)
        cloud_bottom = Math.Min(span_a, span_b)

        long_setup = close > cloud_top and tenkan > kijun
        short_setup = close < cloud_bottom and tenkan < kijun

        was_long = self._prev_long_setup
        was_short = self._prev_short_setup
        self._prev_long_setup = long_setup
        self._prev_short_setup = short_setup

        if was_long is None or was_short is None:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        position = self.Position
        if long_setup and not was_long and position <= 0:
            self.BuyMarket(self.Volume + Math.Abs(position))
        elif short_setup and not was_short and position >= 0:
            self.SellMarket(self.Volume + Math.Abs(position))
        elif position > 0 and close < cloud_bottom:
            self.SellMarket(position)
        elif position < 0 and close > cloud_top:
            self.BuyMarket(-position)

    def CreateClone(self):
        return ichimoku_kumo_breakout_strategy()
