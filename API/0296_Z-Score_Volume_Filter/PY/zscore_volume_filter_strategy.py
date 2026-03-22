import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import SimpleMovingAverage, StandardDeviation, DecimalIndicatorValue
from StockSharp.Algo.Strategies import Strategy


class zscore_volume_filter_strategy(Strategy):
    """
    Mean-reversion strategy that combines price Z-score extremes with above-average volume.
    """

    def __init__(self):
        super(zscore_volume_filter_strategy, self).__init__()

        self._lookback_period = self.Param("LookbackPeriod", 30) \
            .SetDisplay("Lookback Period", "Lookback period for price and volume statistics", "Parameters")

        self._z_score_threshold = self.Param("ZScoreThreshold", 0.6) \
            .SetDisplay("Entry Z-Score", "Absolute Z-score required for entry", "Signals")

        self._exit_threshold = self.Param("ExitThreshold", 0.15) \
            .SetDisplay("Exit Z-Score", "Absolute Z-score required for exit", "Signals")

        self._volume_factor = self.Param("VolumeFactor", 0.5) \
            .SetDisplay("Volume Factor", "Minimum multiple of average volume required for entry", "Signals")

        self._stop_loss_percent = self.Param("StopLossPercent", 2.0) \
            .SetDisplay("Stop Loss %", "Stop loss percentage", "Risk")

        self._cooldown_bars = self.Param("CooldownBars", 20) \
            .SetDisplay("Cooldown Bars", "Bars to wait after each order", "Risk")

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Candle series for signals", "General")

        self._price_sma = None
        self._price_std_dev = None
        self._volume_sma = None
        self._cooldown = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(zscore_volume_filter_strategy, self).OnReseted()
        self._price_sma = None
        self._price_std_dev = None
        self._volume_sma = None
        self._cooldown = 0

    def OnStarted(self, time):
        super(zscore_volume_filter_strategy, self).OnStarted(time)

        lb = int(self._lookback_period.Value)
        self._price_sma = SimpleMovingAverage()
        self._price_sma.Length = lb
        self._price_std_dev = StandardDeviation()
        self._price_std_dev.Length = lb
        self._volume_sma = SimpleMovingAverage()
        self._volume_sma.Length = lb
        self._cooldown = 0

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self._price_sma, self._price_std_dev, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, self._price_sma)
            self.DrawOwnTrades(area)

        self.StartProtection(Unit(0, UnitTypes.Absolute), Unit(self._stop_loss_percent.Value, UnitTypes.Percent), False)

    def _process_candle(self, candle, sma_value, std_dev_value):
        if candle.State != CandleStates.Finished:
            return

        vol_result = self._volume_sma.Process(
            DecimalIndicatorValue(self._volume_sma, candle.TotalVolume, candle.OpenTime))
        volume_average = float(vol_result)

        if not self._price_sma.IsFormed or not self._price_std_dev.IsFormed or not self._volume_sma.IsFormed:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        if self._cooldown > 0:
            self._cooldown -= 1
            return

        std_val = float(std_dev_value)
        if std_val <= 0:
            return

        sma_val = float(sma_value)
        z_score = (float(candle.ClosePrice) - sma_val) / std_val
        is_high_volume = float(candle.TotalVolume) >= volume_average * float(self._volume_factor.Value)

        z_threshold = float(self._z_score_threshold.Value)
        exit_threshold = float(self._exit_threshold.Value)
        cd = int(self._cooldown_bars.Value)

        if self.Position == 0:
            if not is_high_volume:
                return

            if z_score <= -z_threshold:
                self.BuyMarket()
                self._cooldown = cd
            elif z_score >= z_threshold:
                self.SellMarket()
                self._cooldown = cd
            return

        if self.Position > 0 and z_score >= -exit_threshold:
            self.SellMarket(Math.Abs(self.Position))
            self._cooldown = cd
        elif self.Position < 0 and z_score <= exit_threshold:
            self.BuyMarket(Math.Abs(self.Position))
            self._cooldown = cd

    def CreateClone(self):
        return zscore_volume_filter_strategy()
