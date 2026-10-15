import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Decimal
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import Ichimoku, AverageDirectionalIndex
from StockSharp.Algo.Strategies import Strategy

class ichimoku_adx_strategy(Strategy):
    """
    Ichimoku ADX strategy.
    A close above the cloud with Tenkan-sen above Kijun-sen while ADX is above AdxThreshold goes long, a close below the cloud with Tenkan-sen
    below Kijun-sen under the same ADX condition goes short, reversing an opposite position. The cloud is the trailing stop: a long closes
    when price closes below the cloud and a short when it closes above it.
    """

    def __init__(self):
        super(ichimoku_adx_strategy, self).__init__()
        self._tenkan_period = self.Param("TenkanPeriod", 9).SetGreaterThanZero().SetDisplay("Tenkan Period", "Period of Tenkan-sen", "Ichimoku")
        self._kijun_period = self.Param("KijunPeriod", 26).SetGreaterThanZero().SetDisplay("Kijun Period", "Period of Kijun-sen", "Ichimoku")
        self._senkou_span_b_period = self.Param("SenkouSpanBPeriod", 52).SetGreaterThanZero().SetDisplay("Senkou Span B Period", "Period of Senkou Span B", "Ichimoku")
        self._adx_period = self.Param("AdxPeriod", 14).SetGreaterThanZero().SetDisplay("ADX Period", "Period of ADX", "ADX")
        self._adx_threshold = self.Param("AdxThreshold", 25.0).SetDisplay("ADX Threshold", "ADX level of a strong trend", "ADX")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(15))).SetDisplay("Candle Type", "Type of candles to use", "General")

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnStarted2(self, time):
        super(ichimoku_adx_strategy, self).OnStarted2(time)

        ichimoku = Ichimoku()
        ichimoku.Tenkan.Length = self._tenkan_period.Value
        ichimoku.Kijun.Length = self._kijun_period.Value
        ichimoku.SenkouB.Length = self._senkou_span_b_period.Value
        adx = AverageDirectionalIndex()
        adx.Length = self._adx_period.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(ichimoku, adx, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, ichimoku)
            self.DrawOwnTrades(area)
            oscillators = self.CreateChartArea()
            if oscillators is not None:
                self.DrawIndicator(oscillators, adx)

    def _process_candle(self, candle, ichimoku_value, adx_value):
        if candle.State != CandleStates.Finished:
            return

        tenkan = ichimoku_value.Tenkan
        kijun = ichimoku_value.Kijun
        senkou_a = ichimoku_value.SenkouA
        senkou_b = ichimoku_value.SenkouB
        if tenkan is None or kijun is None or senkou_a is None or senkou_b is None:
            return
        if not adx_value.IsFormed or adx_value.MovingAverage is None:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        close = candle.ClosePrice
        cloud_top = senkou_a if senkou_a > senkou_b else senkou_b
        cloud_bottom = senkou_b if senkou_a > senkou_b else senkou_a
        strong = adx_value.MovingAverage > Decimal(self._adx_threshold.Value)

        if close > cloud_top and tenkan > kijun and strong and self.Position <= 0:
            self.BuyMarket(self.Volume + abs(self.Position))
        elif close < cloud_bottom and tenkan < kijun and strong and self.Position >= 0:
            self.SellMarket(self.Volume + abs(self.Position))
        elif self.Position > 0 and close < cloud_bottom:
            self.SellMarket(self.Position)
        elif self.Position < 0 and close > cloud_top:
            self.BuyMarket(-self.Position)

    def CreateClone(self):
        return ichimoku_adx_strategy()
