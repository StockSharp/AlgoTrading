import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Decimal
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import WilliamsR, Ichimoku
from StockSharp.Algo.Strategies import Strategy

class williams_ichimoku_strategy(Strategy):
    """
    Williams R Ichimoku strategy.
    Williams %R below WilliamsROversold with a close above the cloud and Tenkan-sen above Kijun-sen goes long, %R above WilliamsROverbought
    with a close below the cloud and Tenkan-sen below Kijun-sen goes short, reversing an opposite position. The cloud is the trailing stop: a long closes
    when price closes below the cloud and a short when it closes above it.
    """

    def __init__(self):
        super(williams_ichimoku_strategy, self).__init__()
        self._tenkan_period = self.Param("TenkanPeriod", 9).SetGreaterThanZero().SetDisplay("Tenkan Period", "Period of Tenkan-sen", "Ichimoku")
        self._kijun_period = self.Param("KijunPeriod", 26).SetGreaterThanZero().SetDisplay("Kijun Period", "Period of Kijun-sen", "Ichimoku")
        self._senkou_span_b_period = self.Param("SenkouSpanBPeriod", 52).SetGreaterThanZero().SetDisplay("Senkou Span B Period", "Period of Senkou Span B", "Ichimoku")
        self._williams_r_period = self.Param("WilliamsRPeriod", 14).SetGreaterThanZero().SetDisplay("Williams %R Period", "Period of Williams %R", "Williams %R")
        self._williams_r_oversold = self.Param("WilliamsROversold", -80.0).SetDisplay("Williams %R Oversold", "Williams %R level for longs", "Williams %R")
        self._williams_r_overbought = self.Param("WilliamsROverbought", -20.0).SetDisplay("Williams %R Overbought", "Williams %R level for shorts", "Williams %R")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(15))).SetDisplay("Candle Type", "Type of candles to use", "General")

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnStarted2(self, time):
        super(williams_ichimoku_strategy, self).OnStarted2(time)

        ichimoku = Ichimoku()
        ichimoku.Tenkan.Length = self._tenkan_period.Value
        ichimoku.Kijun.Length = self._kijun_period.Value
        ichimoku.SenkouB.Length = self._senkou_span_b_period.Value
        williams = WilliamsR()
        williams.Length = self._williams_r_period.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(williams, ichimoku, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, ichimoku)
            self.DrawOwnTrades(area)
            oscillators = self.CreateChartArea()
            if oscillators is not None:
                self.DrawIndicator(oscillators, williams)

    def _process_candle(self, candle, williams_value, ichimoku_value):
        if candle.State != CandleStates.Finished:
            return

        tenkan = ichimoku_value.Tenkan
        kijun = ichimoku_value.Kijun
        senkou_a = ichimoku_value.SenkouA
        senkou_b = ichimoku_value.SenkouB
        if tenkan is None or kijun is None or senkou_a is None or senkou_b is None:
            return
        if not williams_value.IsFormed:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        close = candle.ClosePrice
        cloud_top = senkou_a if senkou_a > senkou_b else senkou_b
        cloud_bottom = senkou_b if senkou_a > senkou_b else senkou_a
        williams = williams_value.GetValue[Decimal](None)

        if williams < Decimal(self._williams_r_oversold.Value) and close > cloud_top and tenkan > kijun and self.Position <= 0:
            self.BuyMarket(self.Volume + abs(self.Position))
        elif williams > Decimal(self._williams_r_overbought.Value) and close < cloud_bottom and tenkan < kijun and self.Position >= 0:
            self.SellMarket(self.Volume + abs(self.Position))
        elif self.Position > 0 and close < cloud_bottom:
            self.SellMarket(self.Position)
        elif self.Position < 0 and close > cloud_top:
            self.BuyMarket(-self.Position)

    def CreateClone(self):
        return williams_ichimoku_strategy()
