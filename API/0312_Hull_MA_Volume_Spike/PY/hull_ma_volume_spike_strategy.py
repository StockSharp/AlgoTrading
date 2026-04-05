import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Math, Decimal
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import HullMovingAverage, SimpleMovingAverage, StandardDeviation
from StockSharp.Algo.Strategies import Strategy
from indicator_extensions import *

class hull_ma_volume_spike_strategy(Strategy):
    """
    Trend-following strategy that requires a Hull moving average slope change to be confirmed by a volume spike.
    """

    def __init__(self):
        super(hull_ma_volume_spike_strategy, self).__init__()

        self._hma_period = self.Param("HmaPeriod", 9) \
            .SetRange(2, 100) \
            .SetDisplay("HMA Period", "Period for the Hull moving average", "Indicators")

        self._volume_avg_period = self.Param("VolumeAvgPeriod", 20) \
            .SetRange(2, 100) \
            .SetDisplay("Volume Avg Period", "Period for volume statistics", "Indicators")

        self._volume_threshold_factor = self.Param("VolumeThresholdFactor", 1.8) \
            .SetRange(0.1, 10.0) \
            .SetDisplay("Volume Threshold Factor", "Multiplier for volume spike detection", "Signals")

        self._cooldown_bars = self.Param("CooldownBars", 72) \
            .SetRange(1, 500) \
            .SetDisplay("Cooldown Bars", "Bars to wait after each order", "Risk")

        self._stop_loss_percent = self.Param("StopLossPercent", 2.0) \
            .SetRange(0.5, 10.0) \
            .SetDisplay("Stop Loss %", "Stop loss percentage", "Risk")

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")

        self._prev_hma = 0.0
        self._is_initialized = False
        self._cooldown = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(hull_ma_volume_spike_strategy, self).OnReseted()
        self._prev_hma = 0.0
        self._is_initialized = False
        self._cooldown = 0

    def OnStarted2(self, time):
        super(hull_ma_volume_spike_strategy, self).OnStarted2(time)

        hma = HullMovingAverage()
        hma.Length = int(self._hma_period.Value)

        vol_period = int(self._volume_avg_period.Value)
        self._volume_sma = SimpleMovingAverage()
        self._volume_sma.Length = vol_period
        self._volume_std_dev = StandardDeviation()
        self._volume_std_dev.Length = vol_period
        self._is_initialized = False
        self._cooldown = 0

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(hma, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, hma)
            self.DrawOwnTrades(area)

        self.StartProtection(
            Unit(0, UnitTypes.Absolute),
            Unit(self._stop_loss_percent.Value, UnitTypes.Percent),
            False
        )

    def _process_candle(self, candle, hma_val):
        if candle.State != CandleStates.Finished:
            return

        volume = candle.TotalVolume

        volume_avg_value = float(process_float(self._volume_sma, volume, candle.OpenTime, True))

        volume_std_dev_value = float(process_float(self._volume_std_dev, volume, candle.OpenTime, True))

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        if not self._volume_sma.IsFormed or not self._volume_std_dev.IsFormed:
            return

        hma = float(hma_val)

        if not self._is_initialized:
            self._prev_hma = hma
            self._is_initialized = True
            return

        if self._cooldown > 0:
            self._cooldown -= 1
            self._prev_hma = hma
            return

        vtf = float(self._volume_threshold_factor.Value)
        volume_threshold = volume_avg_value + vtf * volume_std_dev_value
        is_volume_spiking = float(volume) >= volume_threshold
        is_hma_rising = hma > self._prev_hma
        is_hma_falling = hma < self._prev_hma

        cd = int(self._cooldown_bars.Value)

        if self.Position == 0:
            if is_hma_rising and is_volume_spiking:
                self.BuyMarket()
                self._cooldown = cd
            elif is_hma_falling and is_volume_spiking:
                self.SellMarket()
                self._cooldown = cd
        elif self.Position > 0:
            if is_hma_falling:
                self.SellMarket(Math.Abs(self.Position))
                self._cooldown = cd
        elif self.Position < 0:
            if is_hma_rising:
                self.BuyMarket(Math.Abs(self.Position))
                self._cooldown = cd

        self._prev_hma = hma

    def CreateClone(self):
        return hull_ma_volume_spike_strategy()
