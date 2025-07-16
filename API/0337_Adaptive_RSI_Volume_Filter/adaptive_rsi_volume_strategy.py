import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, Unit, UnitTypes, CandleStates
from StockSharp.Algo.Indicators import RelativeStrengthIndex, AverageTrueRange, SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy


class adaptive_rsi_volume_strategy(Strategy):
    """
    Strategy that trades based on Adaptive RSI with volume confirmation.
    The RSI period adapts based on market volatility (ATR).
    """

    def __init__(self):
        super(adaptive_rsi_volume_strategy, self).__init__()

        # Strategy parameter: Minimum RSI period.
        self._minRsiPeriod = self.Param("MinRsiPeriod", 10) \
            .SetGreaterThanZero() \
            .SetDisplay("Min RSI Period", "Minimum period for adaptive RSI", "Indicator Settings")

        # Strategy parameter: Maximum RSI period.
        self._maxRsiPeriod = self.Param("MaxRsiPeriod", 20) \
            .SetGreaterThanZero() \
            .SetDisplay("Max RSI Period", "Maximum period for adaptive RSI", "Indicator Settings")

        # Strategy parameter: ATR period for volatility calculation.
        self._atrPeriod = self.Param("AtrPeriod", 14) \
            .SetGreaterThanZero() \
            .SetDisplay("ATR Period", "Period for ATR volatility calculation", "Indicator Settings")

        # Strategy parameter: Volume lookback period.
        self._volumeLookback = self.Param("VolumeLookback", 20) \
            .SetGreaterThanZero() \
            .SetDisplay("Volume Lookback", "Number of periods to calculate volume average", "Volume Settings")

        # Strategy parameter: Candle type.
        self._candleType = self.Param("CandleType", TimeSpan.FromMinutes(5).TimeFrame()) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")

        # Internal state
        self._adaptiveRsiValue = 0.0
        self._avgVolume = 0.0
        self._currentRsiPeriod = 0

        # Indicators
        self._rsi = None
        self._atr = None
        self._volumeSma = None

    @property
    def MinRsiPeriod(self):
        """Strategy parameter: Minimum RSI period."""
        return self._minRsiPeriod.Value

    @MinRsiPeriod.setter
    def MinRsiPeriod(self, value):
        self._minRsiPeriod.Value = value

    @property
    def MaxRsiPeriod(self):
        """Strategy parameter: Maximum RSI period."""
        return self._maxRsiPeriod.Value

    @MaxRsiPeriod.setter
    def MaxRsiPeriod(self, value):
        self._maxRsiPeriod.Value = value

    @property
    def AtrPeriod(self):
        """Strategy parameter: ATR period for volatility calculation."""
        return self._atrPeriod.Value

    @AtrPeriod.setter
    def AtrPeriod(self, value):
        self._atrPeriod.Value = value

    @property
    def VolumeLookback(self):
        """Strategy parameter: Volume lookback period."""
        return self._volumeLookback.Value

    @VolumeLookback.setter
    def VolumeLookback(self, value):
        self._volumeLookback.Value = value

    @property
    def CandleType(self):
        """Strategy parameter: Candle type."""
        return self._candleType.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candleType.Value = value

    def GetWorkingSecurities(self):
        return [(self.Security, self.CandleType)]

    def OnStarted(self, time):
        super(adaptive_rsi_volume_strategy, self).OnStarted(time)

        # Initialize state variables
        self._adaptiveRsiValue = 50  # Neutral starting point
        self._avgVolume = 0
        self._currentRsiPeriod = self.MaxRsiPeriod  # Start with max period

        # Create indicators
        self._atr = AverageTrueRange()
        self._atr.Length = self.AtrPeriod

        self._rsi = RelativeStrengthIndex()
        self._rsi.Length = self._currentRsiPeriod

        self._volumeSma = SimpleMovingAverage()
        self._volumeSma.Length = self.VolumeLookback

        # Create subscription for candles
        subscription = self.SubscribeCandles(self.CandleType)

        # Bind indicators to subscription and start
        subscription.BindEx(self._atr, self._rsi, self.ProcessCandle).Start()

        # Add chart visualization
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, self._rsi)
            self.DrawOwnTrades(area)

        # Start position protection with percentage-based stop-loss
        self.StartProtection(
            takeProfit=Unit(0),  # No fixed take profit
            stopLoss=Unit(2, UnitTypes.Percent)  # 2% stop-loss
        )

    def ProcessCandle(self, candle, atr_value, rsi_value):
        # Skip unfinished candles
        if candle.State != CandleStates.Finished:
            return

        # Process volume to calculate average
        self.ProcessVolume(candle)

        # Calculate adaptive RSI period based on ATR
        if atr_value.IsFinal:
            atr = atr_value.ToDecimal()

            # Normalize ATR to a value between 0 and 1 using historical range
            # This is a simplified approach - in a real implementation you would
            # track ATR range over a longer period
            normalizedAtr = Math.Min(Math.Max(atr / (candle.ClosePrice * 0.1), 0), 1)

            # Adjust RSI period - higher volatility (ATR) = shorter period
            newPeriod = self.MaxRsiPeriod - int(Math.Round(normalizedAtr * (self.MaxRsiPeriod - self.MinRsiPeriod)))

            # Ensure period stays within bounds
            newPeriod = max(self.MinRsiPeriod, min(self.MaxRsiPeriod, newPeriod))

            # Update RSI period if changed
            if newPeriod != self._currentRsiPeriod:
                self._currentRsiPeriod = newPeriod
                self._rsi.Length = self._currentRsiPeriod

                self.LogInfo("Adjusted RSI period to {0} based on ATR ({1})".format(self._currentRsiPeriod, atr))

        # Check if strategy is ready to trade
        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        # Store RSI value
        if rsi_value.IsFinal:
            self._adaptiveRsiValue = rsi_value.ToDecimal()

            # Trading logic based on RSI with volume confirmation
            if self._avgVolume > 0:  # Make sure we have volume data
                isHighVolume = candle.TotalVolume > self._avgVolume

                # Oversold condition with volume confirmation
                if self._adaptiveRsiValue < 30 and isHighVolume and self.Position <= 0:
                    self.LogInfo(
                        "Buy signal: RSI oversold ({0}) with high volume ({1} > {2})".format(
                            self._adaptiveRsiValue, candle.TotalVolume, self._avgVolume))
                    self.BuyMarket(self.Volume + abs(self.Position))
                # Overbought condition with volume confirmation
                elif self._adaptiveRsiValue > 70 and isHighVolume and self.Position >= 0:
                    self.LogInfo(
                        "Sell signal: RSI overbought ({0}) with high volume ({1} > {2})".format(
                            self._adaptiveRsiValue, candle.TotalVolume, self._avgVolume))
                    self.SellMarket(self.Volume + abs(self.Position))

            # Exit logic based on RSI returning to neutral zone
            if (self.Position > 0 and self._adaptiveRsiValue > 50) or \
               (self.Position < 0 and self._adaptiveRsiValue < 50):
                self.LogInfo(
                    "Exit signal: RSI returned to neutral zone ({0})".format(self._adaptiveRsiValue))
                self.ClosePosition()

    def ProcessVolume(self, candle):
        # Process volume with SMA
        volumeValue = self._volumeSma.Process(candle.TotalVolume, candle.ServerTime,
                                             candle.State == CandleStates.Finished)

        if volumeValue.IsFinal:
            self._avgVolume = volumeValue.ToDecimal()

    def CreateClone(self):
        """!! REQUIRED!! Creates a new instance of the strategy."""
        return adaptive_rsi_volume_strategy()
