import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Math, Decimal
from StockSharp.Messages import DataType, CandleStates, Unit
from StockSharp.Algo.Indicators import Ichimoku, SimpleMovingAverage, StandardDeviation
from StockSharp.Algo.Strategies import Strategy
from indicator_extensions import *

class ichimoku_volume_cluster_strategy(Strategy):
    """
    Strategy based on Ichimoku Cloud with volume cluster confirmation.
    """

    def __init__(self):
        super(ichimoku_volume_cluster_strategy, self).__init__()

        self._tenkan_period = self.Param("TenkanPeriod", 9) \
            .SetGreaterThanZero() \
            .SetDisplay("Tenkan-sen Period", "Period for Tenkan-sen (Conversion Line)", "Ichimoku Settings")

        self._kijun_period = self.Param("KijunPeriod", 26) \
            .SetGreaterThanZero() \
            .SetDisplay("Kijun-sen Period", "Period for Kijun-sen (Base Line)", "Ichimoku Settings")

        self._senkou_span_b_period = self.Param("SenkouSpanBPeriod", 52) \
            .SetGreaterThanZero() \
            .SetDisplay("Senkou Span B Period", "Period for Senkou Span B (Leading Span B)", "Ichimoku Settings")

        self._volume_avg_period = self.Param("VolumeAvgPeriod", 20) \
            .SetGreaterThanZero() \
            .SetDisplay("Volume Average Period", "Period for volume average and standard deviation", "Volume Settings")

        self._volume_std_dev_multiplier = self.Param("VolumeStdDevMultiplier", 2.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Volume StdDev Multiplier", "Standard deviation multiplier for volume threshold", "Volume Settings")

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(1))) \
            .SetDisplay("Candle Type", "Type of candles for strategy", "General")

        self._volume_avg = None
        self._volume_std_dev = None

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnStarted2(self, time):
        super(ichimoku_volume_cluster_strategy, self).OnStarted2(time)

        vol_period = int(self._volume_avg_period.Value)
        self._volume_avg = SimpleMovingAverage()
        self._volume_avg.Length = vol_period
        self._volume_std_dev = StandardDeviation()
        self._volume_std_dev.Length = vol_period

        ichimoku = Ichimoku()
        ichimoku.Tenkan.Length = int(self._tenkan_period.Value)
        ichimoku.Kijun.Length = int(self._kijun_period.Value)
        ichimoku.SenkouB.Length = int(self._senkou_span_b_period.Value)

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(ichimoku, self._process_candle).Start()

        self.StartProtection(Unit(0), Unit(0), True)

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, ichimoku)
            self.DrawOwnTrades(area)

            volume_area = self.CreateChartArea()
            if volume_area is not None:
                self.DrawIndicator(volume_area, self._volume_avg)

    def _process_candle(self, candle, ichimoku_value):
        if candle.State != CandleStates.Finished:
            return

        volume = candle.TotalVolume

        volume_avg_value = float(process_float(self._volume_avg, volume, candle.ServerTime, True))

        volume_std_dev_value = float(process_float(self._volume_std_dev, volume, candle.ServerTime, True))

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        tenkan = ichimoku_value.Tenkan
        kijun = ichimoku_value.Kijun
        senkou_a = ichimoku_value.SenkouA
        senkou_b = ichimoku_value.SenkouB

        if tenkan is None or kijun is None or senkou_a is None or senkou_b is None:
            return

        tenkan_f = float(tenkan)
        kijun_f = float(kijun)
        senkou_a_f = float(senkou_a)
        senkou_b_f = float(senkou_b)
        close_price = float(candle.ClosePrice)

        price_above_cloud = close_price > max(senkou_a_f, senkou_b_f)
        price_below_cloud = close_price < min(senkou_a_f, senkou_b_f)

        vsm = float(self._volume_std_dev_multiplier.Value)
        volume_threshold = volume_avg_value + vsm * volume_std_dev_value
        has_volume_spike = float(volume) > volume_threshold

        long_entry = price_above_cloud and tenkan_f > kijun_f and has_volume_spike and self.Position <= 0
        short_entry = price_below_cloud and tenkan_f < kijun_f and has_volume_spike and self.Position >= 0

        long_exit = price_below_cloud and self.Position > 0
        short_exit = price_above_cloud and self.Position < 0

        if long_entry:
            position_size = self.Volume + Math.Abs(self.Position)
            self.BuyMarket(position_size)
        elif short_entry:
            position_size = self.Volume + Math.Abs(self.Position)
            self.SellMarket(position_size)
        elif long_exit:
            self.SellMarket(Math.Abs(self.Position))
        elif short_exit:
            self.BuyMarket(Math.Abs(self.Position))

    def CreateClone(self):
        return ichimoku_volume_cluster_strategy()
