import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.BusinessEntities")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, Unit, UnitTypes, CandleStates
from StockSharp.Algo.Indicators import AverageTrueRange, StandardDeviation, SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy
from StockSharp.BusinessEntities import Security
from datatype_extensions import *
from indicator_extensions import *


class pairs_trading_volatility_filter_strategy(Strategy):
    """
    Pairs Trading Strategy with Volatility Filter strategy.
    """

    def __init__(self):
        super(pairs_trading_volatility_filter_strategy, self).__init__()

        # Initialize strategy parameters
        self._security2 = self.Param[Security]("Security2", None) \
            .SetDisplay("Second Security", "Second security of the pair", "Parameters")

        self._lookback_period = self.Param("LookbackPeriod", 20) \
            .SetRange(5, 100) \
            .SetDisplay("Lookback Period", "Lookback period for moving averages and standard deviation", "Parameters") \
            .SetCanOptimize(True)

        self._entry_threshold = self.Param("EntryThreshold", 2.0) \
            .SetRange(1.0, 5.0) \
            .SetDisplay("Entry Threshold", "Entry threshold in standard deviations", "Parameters") \
            .SetCanOptimize(True)

        self._exit_threshold = self.Param("ExitThreshold", 0.0) \
            .SetRange(0.0, 1.0) \
            .SetDisplay("Exit Threshold", "Exit threshold in standard deviations", "Parameters") \
            .SetCanOptimize(True)

        self._stop_loss_percent = self.Param("StopLossPercent", 2.0) \
            .SetRange(0.5, 5.0) \
            .SetDisplay("Stop Loss", "Stop loss percentage from entry price", "Parameters") \
            .SetCanOptimize(True)

        # Internal state
        self._current_spread = 0.0
        self._previous_spread = 0.0
        self._average_spread = 0.0
        self._standard_deviation = 0.0
        self._current_atr = 0.0
        self._average_atr = 0.0
        self._volume_ratio = 1.0  # Default 1:1 ratio
        self._entry_price = 0.0
        self._last_price1 = 0.0
        self._last_price2 = 0.0

        # Indicators
        self._atr = None
        self._std_dev = None
        self._spread_sma = None
        self._atr_sma = None

    # region Properties
    @property
    def Security1(self):
        """First security in the pair."""
        return self.Security

    @Security1.setter
    def Security1(self, value):
        self.Security = value

    @property
    def Security2(self):
        """Second security in the pair."""
        return self._security2.Value

    @Security2.setter
    def Security2(self, value):
        self._security2.Value = value

    @property
    def LookbackPeriod(self):
        """Lookback period for moving averages and standard deviation."""
        return self._lookback_period.Value

    @LookbackPeriod.setter
    def LookbackPeriod(self, value):
        self._lookback_period.Value = value

    @property
    def EntryThreshold(self):
        """Entry threshold in standard deviations."""
        return self._entry_threshold.Value

    @EntryThreshold.setter
    def EntryThreshold(self, value):
        self._entry_threshold.Value = value

    @property
    def ExitThreshold(self):
        """Exit threshold in standard deviations."""
        return self._exit_threshold.Value

    @ExitThreshold.setter
    def ExitThreshold(self, value):
        self._exit_threshold.Value = value

    @property
    def StopLossPercent(self):
        """Stop loss percentage from entry price."""
        return self._stop_loss_percent.Value

    @StopLossPercent.setter
    def StopLossPercent(self, value):
        self._stop_loss_percent.Value = value
    # endregion

    def GetWorkingSecurities(self):
        dt = tf(5)
        result = []
        if self.Security1 is not None:
            result.append((self.Security1, dt))
        if self.Security2 is not None:
            result.append((self.Security2, dt))
        return result

    def OnStarted(self, time):
        super(pairs_trading_volatility_filter_strategy, self).OnStarted(time)

        self._current_atr = 0
        self._average_atr = 0
        self._current_spread = 0
        self._previous_spread = 0
        self._average_spread = 0
        self._standard_deviation = 0
        self._entry_price = 0
        self._last_price1 = 0
        self._last_price2 = 0

        if self.Security1 is None:
            raise Exception("First security is not specified.")

        if self.Security2 is None:
            raise Exception("Second security is not specified.")

        # Initialize indicators
        self._atr = AverageTrueRange()
        self._atr.Length = self.LookbackPeriod
        self._spread_sma = SimpleMovingAverage()
        self._spread_sma.Length = self.LookbackPeriod
        self._atr_sma = SimpleMovingAverage()
        self._atr_sma.Length = self.LookbackPeriod
        self._std_dev = StandardDeviation()
        self._std_dev.Length = self.LookbackPeriod

        # Set volume ratio to normalize pair
        self._volume_ratio = self._calculate_volume_ratio()

        # Subscribe to both securities' candles
        subscription1 = self.SubscribeCandles(TimeSpan.FromMinutes(5), False, self.Security1)
        subscription2 = self.SubscribeCandles(TimeSpan.FromMinutes(5), False, self.Security2)

        # Subscribe to ticks for both securities to track last prices
        self.SubscribeTicks(self.Security1) \
            .Bind(lambda tick: self._set_last_price1(tick.TradePrice)) \
            .Start()

        self.SubscribeTicks(self.Security2) \
            .Bind(lambda tick: self._set_last_price2(tick.TradePrice)) \
            .Start()

        # Process data and calculate spread
        subscription1 \
            .Bind(self._atr, self._process_security1_candle) \
            .Start()

        subscription2 \
            .Bind(self._process_security2_candle) \
            .Start()

        # Setup visualization
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription1)
            self.DrawCandles(area, subscription2)

        # Setup position protection
        self.StartProtection(
            takeProfit=Unit(0, UnitTypes.Absolute),
            stopLoss=Unit(self.StopLossPercent, UnitTypes.Percent),
            isStopTrailing=False
        )
    # Helper methods to store last prices
    def _set_last_price1(self, price):
        self._last_price1 = price

    def _set_last_price2(self, price):
        self._last_price2 = price

    def _calculate_volume_ratio(self):
        # Use last known prices if available
        price1 = self._last_price1
        price2 = self._last_price2
        if price1 == 0 or price2 == 0:
            return 1
        return price1 / price2

    def _process_security1_candle(self, candle, atr_value):
        if candle.State != CandleStates.Finished:
            return

        # Store ATR value for volatility filter
        self._current_atr = atr_value
        atr_sma_value = to_float(process_float(self._atr_sma, atr_value, candle.ServerTime, candle.State == CandleStates.Finished))
        self._average_atr = atr_sma_value

        # Check if we have all necessary data to make a trading decision
        self._check_signal()

    def _process_security2_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        # Calculate spread (Security1 - Security2 * volumeRatio) using last prices
        price1 = self._last_price1
        price2 = self._last_price2

        self._previous_spread = self._current_spread
        self._current_spread = price1 - (price2 * self._volume_ratio)

        # Calculate spread statistics
        spread_sma_value = to_float(process_float(self._spread_sma, self._current_spread, candle.ServerTime, candle.State == CandleStates.Finished))
        std_dev_value = to_float(process_float(self._std_dev, self._current_spread, candle.ServerTime, candle.State == CandleStates.Finished))

        self._average_spread = spread_sma_value
        self._standard_deviation = std_dev_value

        # Check if we have all necessary data to make a trading decision
        self._check_signal()

    def _check_signal(self):
        # Ensure strategy is ready for trading
        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        # Check if indicators are formed
        if not self._spread_sma.IsFormed or not self._std_dev.IsFormed or not self._atr_sma.IsFormed:
            return

        # Prevent division by zero
        if self._standard_deviation == 0:
            return

        # Calculate Z-score for spread
        z_score = (self._current_spread - self._average_spread) / self._standard_deviation

        # Check volatility filter - only trade in low volatility environment
        is_low_volatility = self._current_atr < self._average_atr

        # If we have no position, check for entry signals
        if self.Position == 0:
            # Long signal: spread is below threshold (undervalued) with low volatility
            if z_score < -self.EntryThreshold and is_low_volatility:
                # Long Security1, Short Security2
                volume1 = self.Volume
                volume2 = self.Volume * self._volume_ratio

                # Record entry price for later reference
                self._entry_price = self._current_spread

                # Execute trades
                self.BuyMarket(volume1, self.Security1)
                self.SellMarket(volume2, self.Security2)

                self.LogInfo("LONG SPREAD: {0} vs {1}, Z-Score: {2:.2f}, Volatility: Low".format(
                    self.Security1.Code, self.Security2.Code, z_score))
            # Short signal: spread is above threshold (overvalued) with low volatility
            elif z_score > self.EntryThreshold and is_low_volatility:
                # Short Security1, Long Security2
                volume1 = self.Volume
                volume2 = self.Volume * self._volume_ratio

                # Record entry price for later reference
                self._entry_price = self._current_spread

                # Execute trades
                self.SellMarket(volume1, self.Security1)
                self.BuyMarket(volume2, self.Security2)

                self.LogInfo("SHORT SPREAD: {0} vs {1}, Z-Score: {2:.2f}, Volatility: Low".format(
                    self.Security1.Code, self.Security2.Code, z_score))
        # Check for exit signals
        else:
            # Exit when spread returns to mean
            if (self.Position > 0 and z_score >= self.ExitThreshold) or \
                    (self.Position < 0 and z_score <= -self.ExitThreshold):
                self.ClosePosition()
                self.LogInfo("CLOSE SPREAD: {0} vs {1}, Z-Score: {2:.2f}".format(
                    self.Security1.Code, self.Security2.Code, z_score))

    def CreateClone(self):
        """!! REQUIRED!! Creates a new instance of the strategy."""
        return pairs_trading_volatility_filter_strategy()
