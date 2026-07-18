import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates, UnitTypes, Unit
from StockSharp.Algo.Indicators import MovingAverageConvergenceDivergenceSignal
from StockSharp.Algo.Strategies import Strategy


class macd_volume_cluster_strategy(Strategy):
    """
    MACD with Volume Cluster strategy.
    Enters positions when MACD signal coincides with abnormal volume spike.
    """

    def __init__(self):
        super(macd_volume_cluster_strategy, self).__init__()

        self._fast_macd_period = self.Param("FastMacdPeriod", 12) \
            .SetGreaterThanZero() \
            .SetDisplay("Fast MACD Period", "Period for fast EMA in MACD calculation", "MACD Settings") \
            .SetCanOptimize(True) \
            .SetOptimize(8, 16, 2)

        self._slow_macd_period = self.Param("SlowMacdPeriod", 26) \
            .SetGreaterThanZero() \
            .SetDisplay("Slow MACD Period", "Period for slow EMA in MACD calculation", "MACD Settings") \
            .SetCanOptimize(True) \
            .SetOptimize(20, 30, 2)

        self._macd_signal_period = self.Param("MacdSignalPeriod", 9) \
            .SetGreaterThanZero() \
            .SetDisplay("MACD Signal Period", "Period for signal line in MACD calculation", "MACD Settings") \
            .SetCanOptimize(True) \
            .SetOptimize(7, 12, 1)

        self._volume_period = self.Param("VolumePeriod", 20) \
            .SetGreaterThanZero() \
            .SetDisplay("Volume Period", "Period for volume moving average calculation", "Volume Settings") \
            .SetCanOptimize(True) \
            .SetOptimize(10, 30, 5)

        self._volume_deviation_factor = self.Param("VolumeDeviationFactor", 2.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Volume Deviation Factor", "Factor multiplied by standard deviation to detect volume spikes", "Volume Settings") \
            .SetCanOptimize(True) \
            .SetOptimize(1.5, 3.0, 0.5)

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")

        self._avg_volume = 0.0
        self._volume_std_dev = 0.0
        self._processed_candles = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def GetWorkingSecurities(self):
        return [(self.Security, self.candle_type)]

    def OnReseted(self):
        super(macd_volume_cluster_strategy, self).OnReseted()
        self._avg_volume = 0.0
        self._volume_std_dev = 0.0
        self._processed_candles = 0

    def OnStarted2(self, time):
        super(macd_volume_cluster_strategy, self).OnStarted2(time)

        macd = MovingAverageConvergenceDivergenceSignal()
        macd.Macd.ShortMa.Length = int(self._fast_macd_period.Value)
        macd.Macd.LongMa.Length = int(self._slow_macd_period.Value)
        macd.SignalMa.Length = int(self._macd_signal_period.Value)

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(macd, self.ProcessMacdAndVolume).Start()

        self.StartProtection(
            takeProfit=Unit(2, UnitTypes.Percent),
            stopLoss=Unit(1, UnitTypes.Percent)
        )

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, macd)
            self.DrawOwnTrades(area)

    def ProcessMacdAndVolume(self, candle, macd_value):
        if candle.State != CandleStates.Finished:
            return

        self._processed_candles += 1
        volume = float(candle.TotalVolume)
        vol_period = int(self._volume_period.Value)
        vol_factor = float(self._volume_deviation_factor.Value)

        if self._processed_candles == 1:
            self._avg_volume = volume
            self._volume_std_dev = 0.0
        else:
            alpha = 2.0 / (vol_period + 1)
            old_avg = self._avg_volume
            self._avg_volume = alpha * volume + (1.0 - alpha) * self._avg_volume
            volume_dev = abs(volume - old_avg)
            self._volume_std_dev = alpha * volume_dev + (1.0 - alpha) * self._volume_std_dev

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        macd_line = macd_value.Macd
        signal_line = macd_value.Signal

        if macd_line is None or signal_line is None:
            return

        macd_line = float(macd_line)
        signal_line = float(signal_line)

        is_volume_spike = volume > (self._avg_volume + vol_factor * self._volume_std_dev)

        if is_volume_spike:
            if macd_line > signal_line and self.Position <= 0:
                if self.Position < 0:
                    self.BuyMarket(Math.Abs(self.Position))
                self.BuyMarket(self.Volume)
            elif macd_line < signal_line and self.Position >= 0:
                if self.Position > 0:
                    self.SellMarket(Math.Abs(self.Position))
                self.SellMarket(self.Volume)

        if (self.Position > 0 and macd_line < signal_line) or \
           (self.Position < 0 and macd_line > signal_line):
            if self.Position > 0:
                self.SellMarket(self.Position)
            elif self.Position < 0:
                self.BuyMarket(Math.Abs(self.Position))

    def CreateClone(self):
        return macd_volume_cluster_strategy()
