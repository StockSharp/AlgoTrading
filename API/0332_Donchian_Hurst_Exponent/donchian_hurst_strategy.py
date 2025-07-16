import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import DonchianChannels, FractalDimension
from StockSharp.Algo.Strategies import Strategy

class donchian_hurst_strategy(Strategy):
    """
    Strategy that trades based on Donchian Channel breakouts with Hurst Exponent filter.
    Enters position when price breaks Donchian Channel with Hurst Exponent indicating trend persistence.

    See more examples: https://github.com/StockSharp/AlgoTrading
    """

    def __init__(self):
        super(donchian_hurst_strategy, self).__init__()

        # Initialize strategy parameters
        self._donchianPeriod = self.Param("DonchianPeriod", 20) \
            .SetGreaterThanZero() \
            .SetDisplay("Donchian Period", "Period for Donchian Channel indicator", "Indicator Settings")

        self._hurstPeriod = self.Param("HurstPeriod", 100) \
            .SetGreaterThanZero() \
            .SetDisplay("Hurst Period", "Period for Hurst Exponent calculation", "Indicator Settings")

        self._hurstThreshold = self.Param("HurstThreshold", 0.5) \
            .SetRange(0, 1) \
            .SetDisplay("Hurst Threshold", "Minimum Hurst Exponent value for trend persistence (>0.5 is trending)", "Indicator Settings")

        self._stopLossPercent = self.Param("StopLossPercent", 2.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Stop Loss %", "Stop Loss percentage from entry price", "Risk Management")

        self._candleType = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")

        # Internal state
        self._hurstValue = 0
        self._donchianIsFormed = False

    @property
    def DonchianPeriod(self):
        """Strategy parameter: Donchian Channel period."""
        return self._donchianPeriod.Value

    @DonchianPeriod.setter
    def DonchianPeriod(self, value):
        self._donchianPeriod.Value = value

    @property
    def HurstPeriod(self):
        """Strategy parameter: Hurst Exponent calculation period."""
        return self._hurstPeriod.Value

    @HurstPeriod.setter
    def HurstPeriod(self, value):
        self._hurstPeriod.Value = value

    @property
    def HurstThreshold(self):
        """Strategy parameter: Hurst Exponent threshold for trend persistence."""
        return self._hurstThreshold.Value

    @HurstThreshold.setter
    def HurstThreshold(self, value):
        self._hurstThreshold.Value = value

    @property
    def StopLossPercent(self):
        """Strategy parameter: Stop-loss percentage."""
        return self._stopLossPercent.Value

    @StopLossPercent.setter
    def StopLossPercent(self, value):
        self._stopLossPercent.Value = value

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
        super(donchian_hurst_strategy, self).OnStarted(time)

        # Reset state variables
        self._hurstValue = 0
        self._donchianIsFormed = False

        # Create Donchian Channel indicator
        donchian = DonchianChannels()
        donchian.Length = self.DonchianPeriod

        # Create FractalDimension indicator for Hurst calculation
        # We use 1 - FractalDimension to get Hurst Exponent (H = 2 - D)
        fractalDimension = FractalDimension()
        fractalDimension.Length = self.HurstPeriod

        # Create subscription for candles
        subscription = self.SubscribeCandles(self.CandleType)

        # Bind indicators to subscription and start
        subscription.BindEx(donchian, fractalDimension, self.ProcessIndicators).Start()

        # Add chart visualization
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, donchian)
            self.DrawOwnTrades(area)

        # Start position protection with percentage-based stop-loss
        self.StartProtection(
            takeProfit=Unit(0),  # No take profit, using Donchian Channel for exit
            stopLoss=Unit(self.StopLossPercent, UnitTypes.Percent)
        )

    def ProcessIndicators(self, candle, donchianValue, fractalDimensionValue):
        # Skip unfinished candles
        if candle.State != CandleStates.Finished:
            return

        # --- FractalDimension logic (was ProcessFractalDimension) ---
        fractalDimension = fractalDimensionValue.ToDecimal()
        self._hurstValue = 2 - fractalDimension

        # Log Hurst Exponent value periodically
        if candle.OpenTime.Second == 0 and candle.OpenTime.Minute % 15 == 0:
            self.LogInfo(f"Current Hurst Exponent: {self._hurstValue}(>{self.HurstThreshold} indicates trend persistence)")

        # --- Donchian logic (was ProcessDonchianChannel) ---
        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        # Check if Donchian Channel is formed
        if not self._donchianIsFormed:
            self._donchianIsFormed = True
            return

        # Convert indicator values to decimal
        try:
            upper = float(donchianValue.UpperBand)
            lower = float(donchianValue.LowerBand)
            middle = float(donchianValue.Middle)
        except Exception:
            return

        # Check for Hurst Exponent indicating trend persistence
        if self._hurstValue > self.HurstThreshold:
            # Check for breakout signals
            if candle.ClosePrice > upper and self.Position <= 0:
                # Breakout above upper band - Buy signal
                self.LogInfo(f"Buy signal: Breakout above Donchian upper band ({upper}) with Hurst = {self._hurstValue}")
                self.BuyMarket(self.Volume + Math.Abs(self.Position))
            elif candle.ClosePrice < lower and self.Position >= 0:
                # Breakout below lower band - Sell signal
                self.LogInfo(f"Sell signal: Breakout below Donchian lower band ({lower}) with Hurst = {self._hurstValue}")
                self.SellMarket(self.Volume + Math.Abs(self.Position))

        # Exit rules based on middle band reversion
        if (self.Position > 0 and candle.ClosePrice < middle) or (self.Position < 0 and candle.ClosePrice > middle):
            self.LogInfo(f"Exit signal: Price reverted to middle band ({middle})")
            self.ClosePosition()

    def CreateClone(self):
        """!! REQUIRED!! Creates a new instance of the strategy."""
        return donchian_hurst_strategy()
