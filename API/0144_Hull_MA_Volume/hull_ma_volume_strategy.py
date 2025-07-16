import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import Unit, UnitTypes, DataType, CandleStates
from StockSharp.Algo.Indicators import HullMovingAverage, SimpleMovingAverage, AverageTrueRange
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *

class hull_ma_volume_strategy(Strategy):
    """
    Strategy that uses Hull Moving Average for trend direction
    and volume confirmation for trade entries.

    See more examples: https://github.com/StockSharp/AlgoTrading
    """

    def __init__(self):
        super(hull_ma_volume_strategy, self).__init__()

        # Strategy parameters
        self._hull_period = self.Param("HullPeriod", 9) \
            .SetGreaterThanZero() \
            .SetDisplay("Hull MA Period", "Period of the Hull Moving Average", "Indicators") \
            .SetCanOptimize(True) \
            .SetOptimize(5, 15, 2)

        self._volume_period = self.Param("VolumePeriod", 20) \
            .SetGreaterThanZero() \
            .SetDisplay("Volume Period", "Period for volume averaging", "Indicators") \
            .SetCanOptimize(True) \
            .SetOptimize(10, 30, 5)

        self._volume_multiplier = self.Param("VolumeMultiplier", 1.5) \
            .SetGreaterThanZero() \
            .SetDisplay("Volume Multiplier", "Multiplier for average volume to confirm entry", "Indicators") \
            .SetCanOptimize(True) \
            .SetOptimize(1.0, 2.0, 0.5)

        self._stop_loss_atr = self.Param("StopLossAtr", 2.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Stop Loss ATR", "Stop loss as ATR multiplier", "Risk Management") \
            .SetCanOptimize(True) \
            .SetOptimize(1.0, 3.0, 0.5)

        self._atr_period = self.Param("AtrPeriod", 14) \
            .SetGreaterThanZero() \
            .SetDisplay("ATR Period", "Period of the ATR for stop loss calculation", "Risk Management") \
            .SetCanOptimize(True) \
            .SetOptimize(7, 21, 7)

        self._candle_type = self.Param("CandleType", tf(5)) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")

        # Internal state
        self._prev_hull_value = None
        self._avg_volume = 0

    @property
    def hull_period(self):
        """Hull Moving Average period."""
        return self._hull_period.Value

    @hull_period.setter
    def hull_period(self, value):
        self._hull_period.Value = value

    @property
    def volume_period(self):
        """Volume averaging period."""
        return self._volume_period.Value

    @volume_period.setter
    def volume_period(self, value):
        self._volume_period.Value = value

    @property
    def volume_multiplier(self):
        """Volume multiplier for confirmation."""
        return self._volume_multiplier.Value

    @volume_multiplier.setter
    def volume_multiplier(self, value):
        self._volume_multiplier.Value = value

    @property
    def stop_loss_atr(self):
        """Stop loss in ATR multiples."""
        return self._stop_loss_atr.Value

    @stop_loss_atr.setter
    def stop_loss_atr(self, value):
        self._stop_loss_atr.Value = value

    @property
    def atr_period(self):
        """ATR period."""
        return self._atr_period.Value

    @atr_period.setter
    def atr_period(self, value):
        self._atr_period.Value = value

    @property
    def candle_type(self):
        """Candle type for strategy calculation."""
        return self._candle_type.Value

    @candle_type.setter
    def candle_type(self, value):
        self._candle_type.Value = value

    def OnReseted(self):
        """Resets internal state when strategy is reset."""
        super(hull_ma_volume_strategy, self).OnReseted()
        self._prev_hull_value = None
        self._avg_volume = 0

    def OnStarted(self, time):
        """Called when the strategy starts. Sets up indicators and subscriptions."""
        super(hull_ma_volume_strategy, self).OnStarted(time)

        # Initialize variables
        self._prev_hull_value = None
        self._avg_volume = 0

        # Create indicators
        hull_ma = HullMovingAverage()
        hull_ma.Length = self.hull_period

        volume_avg = SimpleMovingAverage()
        volume_avg.Length = self.volume_period

        atr = AverageTrueRange()
        atr.Length = self.atr_period

        # Create subscription and bind indicators
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(volume_avg, hull_ma, atr, self.ProcessIndicators).Start()

        # Setup position protection with ATR-based stop loss
        self.StartProtection(Unit(0, UnitTypes.Absolute), Unit(self.stop_loss_atr, UnitTypes.Absolute))

        # Setup chart visualization if available
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, hull_ma)
            self.DrawIndicator(area, atr)
            self.DrawOwnTrades(area)

    def ProcessIndicators(self, candle, volume_avg_value, hull_value, atr_value):
        """Process Hull MA and ATR indicator values."""
        # Skip unfinished candles
        if candle.State != CandleStates.Finished:
            return

        # Check if strategy is ready to trade
        if not self.IsFormedAndOnlineAndAllowTrading() or self._avg_volume <= 0:
            return

        self._avg_volume = volume_avg_value
        current_hull_value = hull_value

        if self._prev_hull_value is not None:
            # Check volume confirmation
            is_volume_high_enough = candle.TotalVolume > self._avg_volume * self.volume_multiplier

            if is_volume_high_enough:
                if current_hull_value > self._prev_hull_value and self.Position <= 0:
                    volume = self.Volume + Math.Abs(self.Position)
                    self.BuyMarket(volume)
                elif current_hull_value < self._prev_hull_value and self.Position >= 0:
                    volume = self.Volume + Math.Abs(self.Position)
                    self.SellMarket(volume)

            # Exit logic - reverse in Hull MA direction
            if self.Position > 0 and current_hull_value < self._prev_hull_value:
                self.SellMarket(Math.Abs(self.Position))
            elif self.Position < 0 and current_hull_value > self._prev_hull_value:
                self.BuyMarket(Math.Abs(self.Position))

        # Update previous Hull MA value
        self._prev_hull_value = current_hull_value

    def CreateClone(self):
        """!! REQUIRED!! Creates a new instance of the strategy."""
        return hull_ma_volume_strategy()
