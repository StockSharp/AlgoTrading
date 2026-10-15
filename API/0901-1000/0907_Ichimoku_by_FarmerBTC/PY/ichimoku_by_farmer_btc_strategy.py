import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Decimal
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import Ichimoku, SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy
from indicator_extensions import *

class ichimoku_by_farmer_btc_strategy(Strategy):
    """
    Ichimoku by FarmerBTC strategy.
    Long only. A long opens when the close is above the Ichimoku cloud, the cloud is bullish (Senkou A above Senkou B), the close is above
    the SmaLength SMA of the higher timeframe and the candle volume exceeds its VolumeLength average multiplied by VolumeMultiplier.
    The position closes when the close falls below the cloud. There are no stops.
    """

    def __init__(self):
        super(ichimoku_by_farmer_btc_strategy, self).__init__()
        self._tenkan_period = self.Param("TenkanPeriod", 10).SetGreaterThanZero().SetDisplay("Tenkan Period", "Tenkan-sen period", "Ichimoku")
        self._kijun_period = self.Param("KijunPeriod", 30).SetGreaterThanZero().SetDisplay("Kijun Period", "Kijun-sen period", "Ichimoku")
        self._senkou_span_b_period = self.Param("SenkouSpanBPeriod", 53).SetGreaterThanZero().SetDisplay("Senkou Span B Period", "Senkou Span B period", "Ichimoku")
        self._sma_length = self.Param("SmaLength", 13).SetGreaterThanZero().SetDisplay("SMA Length", "Higher timeframe SMA period", "Trend")
        self._volume_length = self.Param("VolumeLength", 20).SetGreaterThanZero().SetDisplay("Volume Length", "Volume moving average period", "Volume")
        self._volume_multiplier = self.Param("VolumeMultiplier", 1.5).SetGreaterThanZero().SetDisplay("Volume Multiplier", "Factor applied to the volume average", "Volume")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(1))).SetDisplay("Candle Type", "Working candle type", "General")
        self._htf_candle_type = self.Param("HtfCandleType", DataType.TimeFrame(TimeSpan.FromDays(1))).SetDisplay("HTF Candle Type", "Higher timeframe candle type for the trend SMA", "General")
        self._volume_sma = None
        self._htf_sma = None

    @property
    def candle_type(self):
        return self._candle_type.Value

    @property
    def htf_candle_type(self):
        return self._htf_candle_type.Value

    def GetWorkingSecurities(self):
        return [(self.Security, self.candle_type), (self.Security, self.htf_candle_type)]

    def OnReseted(self):
        super(ichimoku_by_farmer_btc_strategy, self).OnReseted()
        self._volume_sma = None
        self._htf_sma = None

    def OnStarted2(self, time):
        super(ichimoku_by_farmer_btc_strategy, self).OnStarted2(time)

        self._htf_sma = None
        self._volume_sma = SimpleMovingAverage()
        self._volume_sma.Length = self._volume_length.Value

        ichimoku = Ichimoku()
        ichimoku.Tenkan.Length = self._tenkan_period.Value
        ichimoku.Kijun.Length = self._kijun_period.Value
        ichimoku.SenkouB.Length = self._senkou_span_b_period.Value
        htf_sma = SimpleMovingAverage()
        htf_sma.Length = self._sma_length.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(ichimoku, self._process_candle).Start()

        self.SubscribeCandles(self.htf_candle_type).BindEx(htf_sma, self._process_htf_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, ichimoku)
            self.DrawOwnTrades(area)

    def _process_htf_candle(self, candle, sma_value):
        if candle.State != CandleStates.Finished or not sma_value.IsFormed:
            return
        self._htf_sma = sma_value.GetValue[Decimal](None)

    def _process_candle(self, candle, ichimoku_value):
        if candle.State != CandleStates.Finished:
            return

        volume = candle.TotalVolume
        volume_value = process_value(self._volume_sma, volume, candle.OpenTime, True)

        if not self._volume_sma.IsFormed or volume_value.IsEmpty:
            return

        average_volume = to_decimal(volume_value)

        if not ichimoku_value.IsFormed or ichimoku_value.SenkouA is None or ichimoku_value.SenkouB is None:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        close = candle.ClosePrice
        senkou_a = ichimoku_value.SenkouA
        senkou_b = ichimoku_value.SenkouB
        cloud_top = max(senkou_a, senkou_b)
        cloud_bottom = min(senkou_a, senkou_b)

        if self.Position > 0:
            if close < cloud_bottom:
                self.SellMarket(self.Position)
            return

        if self._htf_sma is None:
            return

        if close > cloud_top and senkou_a > senkou_b and close > self._htf_sma and volume > average_volume * Decimal(self._volume_multiplier.Value):
            self.BuyMarket(self.Volume + abs(self.Position))

    def CreateClone(self):
        return ichimoku_by_farmer_btc_strategy()
