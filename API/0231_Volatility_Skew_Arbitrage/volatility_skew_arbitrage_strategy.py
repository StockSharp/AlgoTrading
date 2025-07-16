import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import Math
from StockSharp.Messages import Level1Fields, Unit, UnitTypes
from StockSharp.Algo.Indicators import StandardDeviation
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *
from indicator_extensions import *


class volatility_skew_arbitrage_strategy(Strategy):
    """Volatility Skew Arbitrage strategy that trades options based on volatility skew anomalies."""

    def __init__(self):
        super(volatility_skew_arbitrage_strategy, self).__init__()

        # Option with lower implied volatility.
        self._option_with_low_vol_param = self.Param("OptionWithLowVol") \
            .SetDisplay("Option with Low Vol", "The option instrument with lower implied volatility", "Instruments")

        # Option with higher implied volatility.
        self._option_with_high_vol_param = self.Param("OptionWithHighVol") \
            .SetDisplay("Option with High Vol", "The option instrument with higher implied volatility", "Instruments")

        # Period for calculating average volatility skew.
        self._lookback_period_param = self.Param("LookbackPeriod", 20) \
            .SetDisplay("Lookback Period", "Period for calculating average volatility skew", "Strategy") \
            .SetCanOptimize(True) \
            .SetOptimize(10, 50, 5) \
            .SetGreaterThanZero()

        # Threshold multiplier for standard deviation.
        self._threshold_param = self.Param("Threshold", 2.0) \
            .SetDisplay("Threshold", "Threshold multiplier for standard deviation", "Strategy") \
            .SetCanOptimize(True) \
            .SetOptimize(1.0, 3.0, 0.5) \
            .SetNotNegative()

        # Stop loss percentage.
        self._stop_loss_percent_param = self.Param("StopLossPercent", 2.0) \
            .SetDisplay("Stop Loss %", "Stop loss percentage", "Risk Management") \
            .SetNotNegative()

        # Strategy constructor.
        self._vol_skew_std_dev = StandardDeviation()
        self._vol_skew_std_dev.Length = self.lookback_period
        self._avg_vol_skew = 0
        self._bar_count = 0
        self._current_vol_skew = 0

    @property
    def option_with_low_vol(self):
        """Option with lower implied volatility."""
        return self._option_with_low_vol_param.Value

    @option_with_low_vol.setter
    def option_with_low_vol(self, value):
        self._option_with_low_vol_param.Value = value

    @property
    def option_with_high_vol(self):
        """Option with higher implied volatility."""
        return self._option_with_high_vol_param.Value

    @option_with_high_vol.setter
    def option_with_high_vol(self, value):
        self._option_with_high_vol_param.Value = value

    @property
    def lookback_period(self):
        """Period for calculating average volatility skew."""
        return self._lookback_period_param.Value

    @lookback_period.setter
    def lookback_period(self, value):
        self._lookback_period_param.Value = value
        self._vol_skew_std_dev.Length = value

    @property
    def threshold(self):
        """Threshold multiplier for standard deviation."""
        return self._threshold_param.Value

    @threshold.setter
    def threshold(self, value):
        self._threshold_param.Value = value

    @property
    def stop_loss_percent(self):
        """Stop loss percentage."""
        return self._stop_loss_percent_param.Value

    @stop_loss_percent.setter
    def stop_loss_percent(self, value):
        self._stop_loss_percent_param.Value = value

    def OnStarted(self, time):
        super(volatility_skew_arbitrage_strategy, self).OnStarted(time)

        self._bar_count = 0
        self._avg_vol_skew = 0
        self._current_vol_skew = 0

        # Subscribe to implied volatility for both options
        if self.option_with_low_vol is not None and self.option_with_high_vol is not None:
            self.SubscribeLevel1(self.option_with_low_vol) \
                .Bind(self.ProcessLowOptionImpliedVolatility) \
                .Start()

            self.SubscribeLevel1(self.option_with_high_vol) \
                .Bind(self.ProcessHighOptionImpliedVolatility) \
                .Start()
        else:
            self.LogWarning("Option instruments not specified. Strategy won't work properly.")

        # Start position protection with stop-loss
        self.StartProtection(
            takeProfit=None,
            stopLoss=Unit(self.stop_loss_percent, UnitTypes.Percent)
        )
    def ProcessLowOptionImpliedVolatility(self, data):
        low_iv = data.TryGetDecimal(Level1Fields.ImpliedVolatility) or 0
        high_iv = self._current_vol_skew + low_iv

        self.UpdateVolatilitySkew(high_iv - low_iv, data.ServerTime, True)

    def ProcessHighOptionImpliedVolatility(self, data):
        high_iv = data.TryGetDecimal(Level1Fields.ImpliedVolatility) or 0
        self._current_vol_skew = high_iv

    def UpdateVolatilitySkew(self, vol_skew, time, is_final):
        if vol_skew == 0:
            return

        # Process volatility skew through the indicator
        std_dev_value = process_float(self._vol_skew_std_dev, vol_skew, time, is_final)

        # Update running average for the first LookbackPeriod bars
        if self._bar_count < self.lookback_period:
            self._avg_vol_skew = (self._avg_vol_skew * self._bar_count + vol_skew) / (self._bar_count + 1)
            self._bar_count += 1
            return

        # Moving average calculation after initial period
        self._avg_vol_skew = (self._avg_vol_skew * (self.lookback_period - 1) + vol_skew) / self.lookback_period

        # Check if we have enough data
        if not self._vol_skew_std_dev.IsFormed:
            return

        # Check trading conditions
        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        std_dev = to_float(std_dev_value)

        # Trading logic for volatility skew arbitrage
        if vol_skew > self._avg_vol_skew + self.threshold * std_dev and self.Position <= 0:
            # Long low vol option, short high vol option
            self.LogInfo(
                "Volatility skew above threshold: {0} > {1}".format(vol_skew, self._avg_vol_skew + self.threshold * std_dev))
            self.BuyMarket(self.Volume, self.option_with_low_vol)
            self.SellMarket(self.Volume, self.option_with_high_vol)
        elif vol_skew < self._avg_vol_skew - self.threshold * std_dev and self.Position >= 0:
            # Short low vol option, long high vol option
            self.LogInfo(
                "Volatility skew below threshold: {0} < {1}".format(vol_skew, self._avg_vol_skew - self.threshold * std_dev))
            self.SellMarket(self.Volume, self.option_with_low_vol)
            self.BuyMarket(self.Volume, self.option_with_high_vol)
        elif Math.Abs(vol_skew - self._avg_vol_skew) < 0.2 * std_dev:
            # Close position when vol skew returns to average
            self.LogInfo(
                "Volatility skew returned to average: {0} ≈ {1}".format(vol_skew, self._avg_vol_skew))

            if self.GetPositionValue(self.option_with_low_vol) > 0:
                self.SellMarket(Math.Abs(self.GetPositionValue(self.option_with_low_vol)), self.option_with_low_vol)

            if self.GetPositionValue(self.option_with_low_vol) < 0:
                self.BuyMarket(Math.Abs(self.GetPositionValue(self.option_with_low_vol)), self.option_with_low_vol)

            if self.GetPositionValue(self.option_with_high_vol) > 0:
                self.SellMarket(Math.Abs(self.GetPositionValue(self.option_with_high_vol)), self.option_with_high_vol)

            if self.GetPositionValue(self.option_with_high_vol) < 0:
                self.BuyMarket(Math.Abs(self.GetPositionValue(self.option_with_high_vol)), self.option_with_high_vol)

    def get_position_value(self, security):
        return self.GetPositionValue(security, self.Portfolio) or 0

    def CreateClone(self):
        """!! REQUIRED!! Creates a new instance of the strategy."""
        return volatility_skew_arbitrage_strategy()
