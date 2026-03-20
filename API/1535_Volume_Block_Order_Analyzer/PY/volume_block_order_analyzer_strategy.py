import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Strategies import Strategy


class volume_block_order_analyzer_strategy(Strategy):
    def __init__(self):
        super(volume_block_order_analyzer_strategy, self).__init__()
        self._volume_threshold = self.Param("VolumeThreshold", 1.05) \
            .SetDisplay("Volume Threshold", "Relative volume required for an impact update", "Volume")
        self._lookback_period = self.Param("LookbackPeriod", 20) \
            .SetDisplay("Lookback Period", "Lookback used for average volume", "Volume")
        self._impact_decay = self.Param("ImpactDecay", 0.9) \
            .SetDisplay("Impact Decay", "Decay applied to accumulated impact", "Impact")
        self._impact_normalization = self.Param("ImpactNormalization", 2.0) \
            .SetDisplay("Impact Normalization", "Normalization applied to directional volume", "Impact")
        self._signal_threshold = self.Param("SignalThreshold", 0.3) \
            .SetDisplay("Signal Threshold", "Absolute impact required for a new trade", "Strategy")
        self._signal_cooldown_bars = self.Param("SignalCooldownBars", 10) \
            .SetDisplay("Signal Cooldown", "Bars to wait after entries and exits", "Strategy")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Type of candles", "General")
        self._cumulative_impact = 0.0
        self._cooldown_remaining = 0
        self._volume_buffer = []

    @property
    def volume_threshold(self):
        return self._volume_threshold.Value

    @property
    def lookback_period(self):
        return self._lookback_period.Value

    @property
    def impact_decay(self):
        return self._impact_decay.Value

    @property
    def impact_normalization(self):
        return self._impact_normalization.Value

    @property
    def signal_threshold(self):
        return self._signal_threshold.Value

    @property
    def signal_cooldown_bars(self):
        return self._signal_cooldown_bars.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(volume_block_order_analyzer_strategy, self).OnReseted()
        self._cumulative_impact = 0.0
        self._cooldown_remaining = 0
        self._volume_buffer = []

    def OnStarted(self, time):
        super(volume_block_order_analyzer_strategy, self).OnStarted(time)
        self._cumulative_impact = 0.0
        self._cooldown_remaining = 0
        self._volume_buffer = []
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self.on_process).Start()
        self.StartProtection(
            takeProfit=Unit(2, UnitTypes.Percent),
            stopLoss=Unit(1, UnitTypes.Percent))
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def on_process(self, candle):
        if candle.State != CandleStates.Finished:
            return
        if self._cooldown_remaining > 0:
            self._cooldown_remaining -= 1
        self._volume_buffer.append(float(candle.TotalVolume))
        if len(self._volume_buffer) > self.lookback_period:
            self._volume_buffer.pop(0)
        if len(self._volume_buffer) < self.lookback_period:
            return
        sum_vol = 0.0
        for i in range(len(self._volume_buffer)):
            sum_vol += self._volume_buffer[i]
        average_volume = sum_vol / len(self._volume_buffer)
        relative_volume = 0.0 if average_volume <= 0 else float(candle.TotalVolume) / average_volume
        if candle.ClosePrice > candle.OpenPrice:
            directional_move = 1.0
        elif candle.ClosePrice < candle.OpenPrice:
            directional_move = -1.0
        else:
            directional_move = 0.0
        impact = (directional_move * relative_volume / self.impact_normalization) if relative_volume >= self.volume_threshold else 0.0
        self._cumulative_impact = self._cumulative_impact * self.impact_decay + impact
        if self.Position != 0 or self._cooldown_remaining > 0:
            return
        if self._cumulative_impact >= self.signal_threshold:
            self.BuyMarket()
            self._cooldown_remaining = self.signal_cooldown_bars
        elif self._cumulative_impact <= -self.signal_threshold:
            self.SellMarket()
            self._cooldown_remaining = self.signal_cooldown_bars

    def CreateClone(self):
        return volume_block_order_analyzer_strategy()
