import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, Unit, UnitTypes, CandleStates
from StockSharp.Algo.Indicators import BollingerBands, AverageTrueRange
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *
from indicator_extensions import *

class adaptive_bollinger_breakout_strategy(Strategy):
    """Strategy that trades based on breakouts of Bollinger Bands with adaptively adjusted parameters
    based on market volatility."""

    def __init__(self):
        super(adaptive_bollinger_breakout_strategy, self).__init__()

        # Strategy parameter: Minimum Bollinger period.
        self._min_bollinger_period = self.Param("MinBollingerPeriod", 10) \
            .SetGreaterThanZero() \
            .SetDisplay("Min Bollinger Period", "Minimum period for adaptive Bollinger Bands", "Indicator Settings")

        # Strategy parameter: Maximum Bollinger period.
        self._max_bollinger_period = self.Param("MaxBollingerPeriod", 30) \
            .SetGreaterThanZero() \
            .SetDisplay("Max Bollinger Period", "Maximum period for adaptive Bollinger Bands", "Indicator Settings")

        # Strategy parameter: Minimum Bollinger deviation.
        self._min_bollinger_deviation = self.Param("MinBollingerDeviation", 1.5) \
            .SetGreaterThanZero() \
            .SetDisplay("Min Bollinger Deviation", "Minimum standard deviation multiplier", "Indicator Settings")

        # Strategy parameter: Maximum Bollinger deviation.
        self._max_bollinger_deviation = self.Param("MaxBollingerDeviation", 2.5) \
            .SetGreaterThanZero() \
            .SetDisplay("Max Bollinger Deviation", "Maximum standard deviation multiplier", "Indicator Settings")

        # Strategy parameter: ATR period for volatility calculation.
        self._atr_period = self.Param("AtrPeriod", 14) \
            .SetGreaterThanZero() \
            .SetDisplay("ATR Period", "Period for ATR volatility calculation", "Indicator Settings")

        # Strategy parameter: Candle type.
        self._candle_type = self.Param("CandleType", tf(5)) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")

        # Internal fields
        self._current_bollinger_period = 0
        self._current_bollinger_deviation = 0.0
        self._bollinger = None
        self._atr = None
        self._atr_sum = 0.0
        self._atr_count = 0

    @property
    def min_bollinger_period(self):
        return self._min_bollinger_period.Value

    @min_bollinger_period.setter
    def min_bollinger_period(self, value):
        self._min_bollinger_period.Value = value

    @property
    def max_bollinger_period(self):
        return self._max_bollinger_period.Value

    @max_bollinger_period.setter
    def max_bollinger_period(self, value):
        self._max_bollinger_period.Value = value

    @property
    def min_bollinger_deviation(self):
        return self._min_bollinger_deviation.Value

    @min_bollinger_deviation.setter
    def min_bollinger_deviation(self, value):
        self._min_bollinger_deviation.Value = value

    @property
    def max_bollinger_deviation(self):
        return self._max_bollinger_deviation.Value

    @max_bollinger_deviation.setter
    def max_bollinger_deviation(self, value):
        self._max_bollinger_deviation.Value = value

    @property
    def atr_period(self):
        return self._atr_period.Value

    @atr_period.setter
    def atr_period(self, value):
        self._atr_period.Value = value

    @property
    def candle_type(self):
        return self._candle_type.Value

    @candle_type.setter
    def candle_type(self, value):
        self._candle_type.Value = value

    def GetWorkingSecurities(self):
        return [(self.Security, self.candle_type)]

    def OnReseted(self):
        super(adaptive_bollinger_breakout_strategy, self).OnReseted()
        # Initialize adaptive parameters
        self._current_bollinger_period = self.max_bollinger_period  # Start with maximum period
        self._current_bollinger_deviation = self.min_bollinger_deviation  # Start with minimum deviation
        self._atr = None
        self._bollinger = None
        self._atr_sum = 0.0
        self._atr_count = 0

    def OnStarted(self, time):
        super(adaptive_bollinger_breakout_strategy, self).OnStarted(time)

        # Create ATR indicator for volatility measurement
        self._atr = AverageTrueRange()
        self._atr.Length = self.atr_period

        # Create Bollinger Bands indicator with initial parameters
        self._bollinger = BollingerBands()
        self._bollinger.Length = self._current_bollinger_period
        self._bollinger.Width = self._current_bollinger_deviation

        # Create subscription for candles
        subscription = self.SubscribeCandles(self.candle_type)

        # Bind indicators to subscription and start
        subscription.BindEx(self._atr, self._bollinger, self.ProcessIndicators).Start()

        # Add chart visualization
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, self._bollinger)
            self.DrawOwnTrades(area)

        # Start position protection with ATR-based stop-loss
        self.StartProtection(
            takeProfit=Unit(0),
            stopLoss=Unit(2, UnitTypes.Absolute)
        )
    def ProcessIndicators(self, candle, atr_value, bollinger_value):
        # Skip unfinished candles
        if candle.State != CandleStates.Finished:
            return

        # --- ATR logic (was ProcessAtr) ---
        if atr_value.IsFinal:
            atr = float(atr_value)
            # Maintain running average for ATR
            self._atr_sum += atr
            self._atr_count += 1
            avg_atr = self._atr_sum / self._atr_count if self._atr_count > 0 else atr
            volatility_ratio = float(max(min(atr / (candle.ClosePrice * 0.01), 1), 0))

            # Higher volatility = shorter period and wider bands
            new_period = self.max_bollinger_period - int(round(volatility_ratio * (self.max_bollinger_period - self.min_bollinger_period)))
            new_deviation = self.min_bollinger_deviation + volatility_ratio * (self.max_bollinger_deviation - self.min_bollinger_deviation)

            # Ensure parameters stay within bounds
            new_period = max(self.min_bollinger_period, min(self.max_bollinger_period, new_period))
            new_deviation = max(self.min_bollinger_deviation, min(self.max_bollinger_deviation, new_deviation))

            # Update Bollinger parameters if changed
            if new_period != self._current_bollinger_period or new_deviation != self._current_bollinger_deviation:
                self._current_bollinger_period = new_period
                self._current_bollinger_deviation = new_deviation

                self._bollinger.Length = self._current_bollinger_period
                self._bollinger.Width = self._current_bollinger_deviation

                self.LogInfo("Adjusted Bollinger parameters: Period={0}, Deviation={1:.2f} based on ATR={2:.6f}".format(
                    self._current_bollinger_period, self._current_bollinger_deviation, atr))

        # --- Bollinger logic (was ProcessBollinger) ---
        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        if bollinger_value.IsFinal and self._atr.IsFormed:
            atr_val = float(atr_value)  # use current ATR value
            # use running average
            is_high_volatility = atr_val > (self._atr_sum / self._atr_count if self._atr_count > 0 else atr_val)


            if (
                bollinger_value.UpBand is None
                or bollinger_value.LowBand is None
                or bollinger_value.MovingAverage is None
            ):
                return  # Not enough data to calculate bands

            upper_band = float(bollinger_value.UpBand)
            lower_band = float(bollinger_value.LowBand)
            middle_band = float(bollinger_value.MovingAverage)

            if is_high_volatility:
                # Breakout above upper band - Sell signal
                if candle.ClosePrice > upper_band and self.Position >= 0:
                    self.LogInfo("Sell signal: Price ({0}) broke above upper Bollinger Band ({1}) in high volatility".format(
                        candle.ClosePrice, upper_band))
                    self.SellMarket(self.Volume + Math.Abs(self.Position))
                # Breakout below lower band - Buy signal
                elif candle.ClosePrice < lower_band and self.Position <= 0:
                    self.LogInfo("Buy signal: Price ({0}) broke below lower Bollinger Band ({1}) in high volatility".format(
                        candle.ClosePrice, lower_band))
                    self.BuyMarket(self.Volume + Math.Abs(self.Position))

            # Exit logic based on middle band reversion
            if (self.Position > 0 and candle.ClosePrice > middle_band) or \
               (self.Position < 0 and candle.ClosePrice < middle_band):
                self.LogInfo("Exit signal: Price ({0}) reverted to middle band ({1})".format(
                    candle.ClosePrice, middle_band))
                self.ClosePosition()

    def CreateClone(self):
        """!! REQUIRED!! Creates a new instance of the strategy."""
        return adaptive_bollinger_breakout_strategy()