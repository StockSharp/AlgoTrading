import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import SimpleMovingAverage, StandardDeviation
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *
from indicator_extensions import *


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

        self._candle_type = self.Param("CandleType", tf(5)) \
            .SetDisplay("Candle Type", "Candle series for signals", "General")

        self._price_sma = None
        self._price_std_dev = None
        self._volume_sma = None
        self._cooldown = 0

    @property
    def LookbackPeriod(self):
        return self._lookback_period.Value

    @LookbackPeriod.setter
    def LookbackPeriod(self, value):
        self._lookback_period.Value = value

    @property
    def ZScoreThreshold(self):
        return self._z_score_threshold.Value

    @ZScoreThreshold.setter
    def ZScoreThreshold(self, value):
        self._z_score_threshold.Value = value

    @property
    def ExitThreshold(self):
        return self._exit_threshold.Value

    @ExitThreshold.setter
    def ExitThreshold(self, value):
        self._exit_threshold.Value = value

    @property
    def VolumeFactor(self):
        return self._volume_factor.Value

    @VolumeFactor.setter
    def VolumeFactor(self, value):
        self._volume_factor.Value = value

    @property
    def StopLossPercent(self):
        return self._stop_loss_percent.Value

    @StopLossPercent.setter
    def StopLossPercent(self, value):
        self._stop_loss_percent.Value = value

    @property
    def CooldownBars(self):
        return self._cooldown_bars.Value

    @CooldownBars.setter
    def CooldownBars(self, value):
        self._cooldown_bars.Value = value

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    def GetWorkingSecurities(self):
        return [(self.Security, self.CandleType)]

    def OnReseted(self):
        super(zscore_volume_filter_strategy, self).OnReseted()
        self._price_sma = None
        self._price_std_dev = None
        self._volume_sma = None
        self._cooldown = 0

    def OnStarted(self, time):
        super(zscore_volume_filter_strategy, self).OnStarted(time)

        self._price_sma = SimpleMovingAverage()
        self._price_sma.Length = self.LookbackPeriod
        self._price_std_dev = StandardDeviation()
        self._price_std_dev.Length = self.LookbackPeriod
        self._volume_sma = SimpleMovingAverage()
        self._volume_sma.Length = self.LookbackPeriod
        self._cooldown = 0

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(self._price_sma, self._price_std_dev, self.ProcessCandle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, self._price_sma)
            self.DrawOwnTrades(area)

        self.StartProtection(
            takeProfit=Unit(0, UnitTypes.Absolute),
            stopLoss=Unit(self.StopLossPercent, UnitTypes.Percent)
        )

    def ProcessCandle(self, candle, sma_value, std_dev_value):
        if candle.State != CandleStates.Finished:
            return

        sma_val = float(sma_value)
        std_val = float(std_dev_value)

        # Process volume through its own SMA
        vol_result = process_float(self._volume_sma, float(candle.TotalVolume), candle.OpenTime, True)
        volume_average = float(vol_result)

        if not self._price_sma.IsFormed or not self._price_std_dev.IsFormed or not self._volume_sma.IsFormed:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        if self._cooldown > 0:
            self._cooldown -= 1
            return

        if std_val <= 0:
            return

        z_score = (float(candle.ClosePrice) - sma_val) / std_val
        is_high_volume = float(candle.TotalVolume) >= volume_average * self.VolumeFactor

        if self.Position == 0:
            if not is_high_volume:
                return

            if z_score <= -self.ZScoreThreshold:
                self.BuyMarket()
                self._cooldown = self.CooldownBars
            elif z_score >= self.ZScoreThreshold:
                self.SellMarket()
                self._cooldown = self.CooldownBars
            return

        if self.Position > 0 and z_score >= -self.ExitThreshold:
            self.SellMarket(abs(self.Position))
            self._cooldown = self.CooldownBars
        elif self.Position < 0 and z_score <= self.ExitThreshold:
            self.BuyMarket(abs(self.Position))
            self._cooldown = self.CooldownBars

    def CreateClone(self):
        return zscore_volume_filter_strategy()
