import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Math, Decimal
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import Ichimoku
from StockSharp.Algo.Strategies import Strategy

class ichimoku_tenkan_kijun_strategy(Strategy):
    """
    Ichimoku Tenkan/Kijun Cross strategy.
    Tenkan-sen crossing above Kijun-sen while the close is above the cloud opens a long position, the opposite cross below the
    cloud a short one. The stop is the Kijun-sen at entry; an opposite cross closes the position, reversing it when the close
    is on the other side of the cloud.
    """

    def __init__(self):
        super(ichimoku_tenkan_kijun_strategy, self).__init__()
        self._tenkan_period = self.Param("TenkanPeriod", 9).SetGreaterThanZero().SetDisplay("Tenkan Period", "Period for Tenkan-sen", "Ichimoku")
        self._kijun_period = self.Param("KijunPeriod", 26).SetGreaterThanZero().SetDisplay("Kijun Period", "Period for Kijun-sen", "Ichimoku")
        self._senkou_span_b_period = self.Param("SenkouSpanBPeriod", 52).SetGreaterThanZero().SetDisplay("Senkou Span B Period", "Period for Senkou Span B", "Ichimoku")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(30))).SetDisplay("Candle Type", "Type of candles to use", "General")

        self._prev_tenkan_above = None
        self._stop_price = Decimal(0)

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(ichimoku_tenkan_kijun_strategy, self).OnReseted()
        self._prev_tenkan_above = None
        self._stop_price = Decimal(0)

    def OnStarted2(self, time):
        super(ichimoku_tenkan_kijun_strategy, self).OnStarted2(time)

        self._prev_tenkan_above = None
        self._stop_price = Decimal(0)

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

    def _process_candle(self, candle, ichimoku_iv):
        if candle.State != CandleStates.Finished:
            return

        tenkan = ichimoku_iv.Tenkan
        kijun = ichimoku_iv.Kijun
        senkou_a = ichimoku_iv.SenkouA
        senkou_b = ichimoku_iv.SenkouB
        if tenkan is None or kijun is None or senkou_a is None or senkou_b is None:
            return

        # Equal lines are no cross; the previous side holds.
        was_above = self._prev_tenkan_above
        if tenkan != kijun:
            self._prev_tenkan_above = tenkan > kijun

        if was_above is None or not self.IsFormedAndOnlineAndAllowTrading():
            return

        bullish_cross = not was_above and tenkan > kijun
        bearish_cross = was_above and tenkan < kijun
        close = candle.ClosePrice
        upper_kumo = Math.Max(senkou_a, senkou_b)
        lower_kumo = Math.Min(senkou_a, senkou_b)

        if self.Position > 0:
            if bearish_cross and close < lower_kumo:
                self.SellMarket(self.Volume + self.Position)
                self._stop_price = kijun
            elif bearish_cross or close <= self._stop_price:
                self.SellMarket(self.Position)
        elif self.Position < 0:
            if bullish_cross and close > upper_kumo:
                self.BuyMarket(self.Volume - self.Position)
                self._stop_price = kijun
            elif bullish_cross or close >= self._stop_price:
                self.BuyMarket(-self.Position)
        elif bullish_cross and close > upper_kumo:
            self.BuyMarket(self.Volume)
            self._stop_price = kijun
        elif bearish_cross and close < lower_kumo:
            self.SellMarket(self.Volume)
            self._stop_price = kijun

    def CreateClone(self):
        return ichimoku_tenkan_kijun_strategy()
