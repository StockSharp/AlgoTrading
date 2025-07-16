import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates, UnitTypes, Unit
from StockSharp.Algo.Indicators import SimpleMovingAverage, Highest, Lowest
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *

class wyckoff_distribution_strategy(Strategy):
    """
    Strategy based on Wyckoff Distribution pattern, which identifies a period of institutional distribution
    that leads to a downward price movement.
    """

    class WyckoffPhase:
        """Internal enumeration for Wyckoff phases."""
        NONE = 0
        PHASE_A = 1  # Buying climax, automatic reaction, secondary test
        PHASE_B = 2  # Distribution, top building
        PHASE_C = 3  # Upthrust, test of resistance
        PHASE_D = 4  # Sign of weakness, failed test
        PHASE_E = 5  # Markdown, price decline

    def __init__(self):
        super(wyckoff_distribution_strategy, self).__init__()

        # Initialize strategy parameters
        self._candle_type = self.Param("CandleType", tf(15)) \
            .SetDisplay("Candle Type", "Type of candles to use for analysis", "General")

        self._ma_period = self.Param("MaPeriod", 20) \
            .SetDisplay("MA Period", "Period for moving average calculation", "Trend") \
            .SetRange(10, 50)

        self._volume_avg_period = self.Param("VolumeAvgPeriod", 20) \
            .SetDisplay("Volume Avg Period", "Period for volume average calculation", "Volume") \
            .SetRange(10, 50)

        self._highest_period = self.Param("HighestPeriod", 20) \
            .SetDisplay("High/Low Period", "Period for high/low calculation", "Range") \
            .SetRange(10, 50)

        self._stop_loss_percent = self.Param("StopLossPercent", 2.0) \
            .SetDisplay("Stop Loss %", "Stop-loss percentage from entry price", "Protection") \
            .SetRange(1.0, 5.0)

        # Internal state
        self._current_phase = self.WyckoffPhase.NONE
        self._last_range_high = 0.0
        self._last_range_low = 0.0
        self._sideways_count = 0
        self._upthrust_high = 0.0
        self._position_opened = False

    @property
    def candle_type(self):
        """Candle type and timeframe for the strategy."""
        return self._candle_type.Value

    @candle_type.setter
    def candle_type(self, value):
        self._candle_type.Value = value

    @property
    def ma_period(self):
        """Period for moving average calculation."""
        return self._ma_period.Value

    @ma_period.setter
    def ma_period(self, value):
        self._ma_period.Value = value

    @property
    def volume_avg_period(self):
        """Period for volume average calculation."""
        return self._volume_avg_period.Value

    @volume_avg_period.setter
    def volume_avg_period(self, value):
        self._volume_avg_period.Value = value

    @property
    def highest_period(self):
        """Period for highest/lowest calculation."""
        return self._highest_period.Value

    @highest_period.setter
    def highest_period(self, value):
        self._highest_period.Value = value

    @property
    def stop_loss_percent(self):
        """Stop-loss percentage from entry price."""
        return self._stop_loss_percent.Value

    @stop_loss_percent.setter
    def stop_loss_percent(self, value):
        self._stop_loss_percent.Value = value

    def OnStarted(self, time):
        """Called when the strategy starts. Initializes indicators and subscriptions."""
        super(wyckoff_distribution_strategy, self).OnStarted(time)

        # Initialize indicators
        self._ma = SimpleMovingAverage()
        self._ma.Length = self.ma_period
        self._volume_avg = SimpleMovingAverage()
        self._volume_avg.Length = self.volume_avg_period
        self._highest = Highest()
        self._highest.Length = self.highest_period
        self._lowest = Lowest()
        self._lowest.Length = self.highest_period

        self._current_phase = self.WyckoffPhase.NONE
        self._sideways_count = 0
        self._position_opened = False
        self._last_range_high = 0.0
        self._last_range_low = 0.0
        self._upthrust_high = 0.0

        # Create and setup subscription for candles
        subscription = self.SubscribeCandles(self.candle_type)

        # Bind indicators and processor
        subscription.Bind(self._ma, self._volume_avg, self._highest, self._lowest, self.ProcessCandle).Start()

        # Enable stop-loss protection
        self.StartProtection(
            takeProfit=Unit(0),
            stopLoss=Unit(self.stop_loss_percent, UnitTypes.Percent)
        )
        # Setup chart if available
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, self._ma)
            self.DrawOwnTrades(area)

    def ProcessCandle(self, candle, ma_value, volume_avg_value, highest_value, lowest_value):
        """Process each finished candle and run Wyckoff Distribution logic."""
        # Skip unfinished candles
        if candle.State != CandleStates.Finished:
            return

        # Check if strategy is ready to trade
        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        # Update range values
        self._last_range_high = highest_value
        self._last_range_low = lowest_value

        # Determine candle characteristics
        is_bullish = candle.ClosePrice > candle.OpenPrice
        is_bearish = candle.ClosePrice < candle.OpenPrice
        high_volume = candle.TotalVolume > volume_avg_value * 1.5
        price_above_ma = candle.ClosePrice > ma_value
        price_below_ma = candle.ClosePrice < ma_value
        is_narrow_range = (candle.HighPrice - candle.LowPrice) < (highest_value - lowest_value) * 0.3

        # State machine for Wyckoff Distribution phases
        if self._current_phase == self.WyckoffPhase.NONE:
            # Look for Phase A: Buying climax (high volume, wide range up bar)
            if is_bullish and high_volume and candle.ClosePrice > highest_value:
                self._current_phase = self.WyckoffPhase.PHASE_A
                self.LogInfo("Wyckoff Phase A detected: Buying climax at {0}".format(candle.ClosePrice))
        elif self._current_phase == self.WyckoffPhase.PHASE_A:
            # Look for automatic reaction (pullback from buying climax)
            if is_bearish and candle.ClosePrice < ma_value:
                self._current_phase = self.WyckoffPhase.PHASE_B
                self.LogInfo("Entering Wyckoff Phase B: Automatic reaction at {0}".format(candle.ClosePrice))
                self._sideways_count = 0
        elif self._current_phase == self.WyckoffPhase.PHASE_B:
            # Phase B is characterized by sideways movement (distribution)
            if is_narrow_range and candle.ClosePrice > self._last_range_low and candle.ClosePrice < self._last_range_high:
                self._sideways_count += 1
                # After sufficient sideways movement, look for Phase C
                if self._sideways_count >= 5:
                    self._current_phase = self.WyckoffPhase.PHASE_C
                    self.LogInfo("Entering Wyckoff Phase C: Distribution complete after {0} sideways candles".format(self._sideways_count))
            else:
                self._sideways_count = 0  # Reset if we don't see sideways movement
        elif self._current_phase == self.WyckoffPhase.PHASE_C:
            # Phase C includes an upthrust (price briefly goes above resistance)
            if candle.HighPrice > self._last_range_high and candle.ClosePrice < self._last_range_high:
                self._upthrust_high = candle.HighPrice
                self._current_phase = self.WyckoffPhase.PHASE_D
                self.LogInfo("Entering Wyckoff Phase D: Upthrust detected at {0}".format(self._upthrust_high))
        elif self._current_phase == self.WyckoffPhase.PHASE_D:
            # Phase D shows sign of weakness (strong move down with volume)
            if is_bearish and high_volume and price_below_ma:
                self._current_phase = self.WyckoffPhase.PHASE_E
                self.LogInfo("Entering Wyckoff Phase E: Sign of weakness detected at {0}".format(candle.ClosePrice))
        elif self._current_phase == self.WyckoffPhase.PHASE_E:
            # Phase E is the markdown phase where we enter our position
            if is_bearish and price_below_ma and not self._position_opened:
                volume = self.Volume + Math.Abs(self.Position)
                self.SellMarket(volume)

                self._position_opened = True
                self.LogInfo("Wyckoff Distribution complete. Short entry at {0}".format(candle.ClosePrice))

        # Exit conditions
        if self._position_opened and self.Position < 0:
            # Exit when price drops below previous low (target achieved)
            if candle.LowPrice < self._last_range_low:
                self.BuyMarket(Math.Abs(self.Position))
                self._position_opened = False
                self._current_phase = self.WyckoffPhase.NONE  # Reset the pattern detection
                self.LogInfo("Exit signal: Price broke below range low ({0}). Closed short position at {1}".format(self._last_range_low, candle.ClosePrice))
            # Exit also if price rises back above MA (failed pattern)
            elif price_above_ma:
                self.BuyMarket(Math.Abs(self.Position))
                self._position_opened = False
                self._current_phase = self.WyckoffPhase.NONE  # Reset the pattern detection
                self.LogInfo("Exit signal: Price rose above MA. Pattern may have failed. Closed short position at {0}".format(candle.ClosePrice))

    def CreateClone(self):
        """!! REQUIRED!! Creates a new instance of the strategy."""
        return wyckoff_distribution_strategy()
