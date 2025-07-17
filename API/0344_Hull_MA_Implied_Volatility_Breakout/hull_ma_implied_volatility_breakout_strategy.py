import clr

clr.AddReference("Ecng.Common")
clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from Ecng.Common import RandomGen
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import HullMovingAverage, AverageTrueRange
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *


class hull_ma_implied_volatility_breakout_strategy(Strategy):
    """
    Hull MA with Implied Volatility Breakout strategy.
    Entry condition:
    Long: HMA(t) > HMA(t-1) && IV > Avg(IV, N) + k*StdDev(IV, N)
    Short: HMA(t) < HMA(t-1) && IV > Avg(IV, N) + k*StdDev(IV, N)
    Exit condition:
    Long: HMA(t) < HMA(t-1)
    Short: HMA(t) > HMA(t-1)
    """

    def __init__(self):
        """Constructor with default parameters."""
        super(hull_ma_implied_volatility_breakout_strategy, self).__init__()

        self._hmaPeriod = self.Param("HmaPeriod", 9) \
            .SetGreaterThanZero() \
            .SetDisplay("HMA Period", "Hull Moving Average period", "HMA Settings") \
            .SetCanOptimize(True) \
            .SetOptimize(5, 15, 2)

        self._ivPeriod = self.Param("IVPeriod", 20) \
            .SetGreaterThanZero() \
            .SetDisplay("IV Period", "Implied Volatility averaging period", "Volatility Settings") \
            .SetCanOptimize(True) \
            .SetOptimize(10, 30, 5)

        self._ivMultiplier = self.Param("IVMultiplier", 2.0) \
            .SetGreaterThanZero() \
            .SetDisplay("IV StdDev Multiplier", "Multiplier for IV standard deviation", "Volatility Settings") \
            .SetCanOptimize(True) \
            .SetOptimize(1.5, 3.0, 0.5)

        self._stopLossAtr = self.Param("StopLossAtr", 2.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Stop Loss (ATR)", "Stop Loss in multiples of ATR", "Risk Management") \
            .SetCanOptimize(True) \
            .SetOptimize(1.0, 3.0, 0.5)

        self._candleType = self.Param("CandleType", tf(15)) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")

        # Track trade direction
        self._isLong = False
        self._isShort = False

        self._impliedVolatilityHistory = []
        self._ivAverage = 0.0
        self._ivStdDev = 0.0
        self._currentIv = 0.0

        self._prevHmaValue = 0.0
        self._currentAtr = 0.0

    # Hull Moving Average period.
    @property
    def HmaPeriod(self):
        return self._hmaPeriod.Value

    @HmaPeriod.setter
    def HmaPeriod(self, value):
        self._hmaPeriod.Value = value

    # Implied Volatility averaging period.
    @property
    def IVPeriod(self):
        return self._ivPeriod.Value

    @IVPeriod.setter
    def IVPeriod(self, value):
        self._ivPeriod.Value = value

    # IV standard deviation multiplier for breakout threshold.
    @property
    def IVMultiplier(self):
        return self._ivMultiplier.Value

    @IVMultiplier.setter
    def IVMultiplier(self, value):
        self._ivMultiplier.Value = value

    # Stop loss in ATR multiples.
    @property
    def StopLossAtr(self):
        return self._stopLossAtr.Value

    @StopLossAtr.setter
    def StopLossAtr(self, value):
        self._stopLossAtr.Value = value

    # Type of candles to use.
    @property
    def CandleType(self):
        return self._candleType.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candleType.Value = value

    def GetWorkingSecurities(self):
        return [(self.Security, self.CandleType)]

    def OnStarted(self, time):
        super(hull_ma_implied_volatility_breakout_strategy, self).OnStarted(time)

        # Initialize flags
        self._isLong = False
        self._isShort = False
        self._prevHmaValue = 0.0
        self._currentAtr = 0.0
        self._currentIv = 0.0
        self._ivAverage = 0.0
        self._ivStdDev = 0.0
        self._impliedVolatilityHistory[:] = []

        # Create indicators
        hma = HullMovingAverage()
        hma.Length = self.HmaPeriod
        atr = AverageTrueRange()
        atr.Length = 14  # Fixed ATR period for stop-loss

        # Subscribe to candles and bind indicators
        subscription = self.SubscribeCandles(self.CandleType)

        # We need to bind both HMA and ATR
        subscription.Bind(hma, atr, self.ProcessCandle).Start()

        # Create chart visualization if available
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, hma)
            self.DrawIndicator(area, atr)
            self.DrawOwnTrades(area)

    # Process each candle with HMA and ATR values.
    def ProcessCandle(self, candle, hmaValue, atrValue):
        # Skip unfinished candles
        if candle.State != CandleStates.Finished:
            return

        # Check if strategy is ready to trade
        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        # Store ATR value for stop-loss calculation
        self._currentAtr = atrValue

        # Update implied volatility (in a real system, this would come from market data)
        self.UpdateImpliedVolatility(candle)

        # First run, just store the HMA value
        if self._prevHmaValue == 0:
            self._prevHmaValue = hmaValue
            return

        price = float(candle.ClosePrice)

        # Determine HMA direction
        hmaRising = hmaValue > self._prevHmaValue
        hmaFalling = hmaValue < self._prevHmaValue

        # Calculate IV breakout threshold
        ivBreakoutThreshold = self._ivAverage + self.IVMultiplier * self._ivStdDev
        ivBreakout = self._currentIv > ivBreakoutThreshold

        # Trading logic

        # Entry conditions

        # Long entry: HMA rising and IV breakout
        if hmaRising and ivBreakout and not self._isLong and self.Position <= 0:
            self.LogInfo(f"Long signal: HMA rising ({hmaValue} > {self._prevHmaValue}), IV breakout ({self._currentIv} > {ivBreakoutThreshold})")
            self.BuyMarket(self.Volume)
            self._isLong = True
            self._isShort = False
        # Short entry: HMA falling and IV breakout
        elif hmaFalling and ivBreakout and not self._isShort and self.Position >= 0:
            self.LogInfo(f"Short signal: HMA falling ({hmaValue} < {self._prevHmaValue}), IV breakout ({self._currentIv} > {ivBreakoutThreshold})")
            self.SellMarket(self.Volume)
            self._isShort = True
            self._isLong = False

        # Exit conditions

        # Exit long: HMA starts falling
        if self._isLong and hmaFalling and self.Position > 0:
            self.LogInfo(f"Exit long: HMA falling ({hmaValue} < {self._prevHmaValue})")
            self.SellMarket(Math.Abs(self.Position))
            self._isLong = False
        # Exit short: HMA starts rising
        elif self._isShort and hmaRising and self.Position < 0:
            self.LogInfo(f"Exit short: HMA rising ({hmaValue} > {self._prevHmaValue})")
            self.BuyMarket(Math.Abs(self.Position))
            self._isShort = False

        # Apply ATR-based stop loss
        self.ApplyAtrStopLoss(price)

        # Store HMA value for next iteration
        self._prevHmaValue = hmaValue

    # Update implied volatility value.
    # In a real implementation, this would fetch data from market.
    def UpdateImpliedVolatility(self, candle):
        # Simple IV simulation based on candle's high-low range and volume
        # In reality, this would come from option pricing data
        rangeValue = float((candle.HighPrice - candle.LowPrice) / candle.LowPrice)
        volume = candle.TotalVolume if candle.TotalVolume > 0 else 1

        # Simulate IV based on range and volume with some randomness
        iv = rangeValue * (1 + 0.5 * RandomGen.GetDouble()) * 100

        # Add volume factor - higher volume often correlates with higher IV
        iv *= min(1.5, 1 + Math.Log10(float(volume)) * 0.1)

        self._currentIv = iv

        # Add to history
        self._impliedVolatilityHistory.append(self._currentIv)
        if len(self._impliedVolatilityHistory) > self.IVPeriod:
            self._impliedVolatilityHistory.pop(0)

        # Calculate average
        if len(self._impliedVolatilityHistory) > 0:
            self._ivAverage = sum(self._impliedVolatilityHistory) / len(self._impliedVolatilityHistory)
        else:
            self._ivAverage = 0

        # Calculate standard deviation
        if len(self._impliedVolatilityHistory) > 1:
            mean = self._ivAverage
            sumSquaredDiffs = sum((v - mean) ** 2 for v in self._impliedVolatilityHistory)
            self._ivStdDev = Math.Sqrt(float(sumSquaredDiffs / (len(self._impliedVolatilityHistory) - 1)))
        else:
            self._ivStdDev = 0.5  # Default value until we have enough data

        self.LogInfo(f"IV: {self._currentIv}, Avg: {self._ivAverage}, StdDev: {self._ivStdDev}")

    # Apply ATR-based stop loss.
    def ApplyAtrStopLoss(self, price):
        # Only apply stop-loss if ATR is available and position exists
        if self._currentAtr <= 0 or self.Position == 0:
            return

        # Calculate stop levels
        if self.Position > 0:  # Long position
            stopLevel = price - (self.StopLossAtr * self._currentAtr)
            if price <= stopLevel:
                self.LogInfo(f"ATR Stop Loss triggered for long position: Current {price} <= Stop {stopLevel}")
                self.SellMarket(Math.Abs(self.Position))
                self._isLong = False
        elif self.Position < 0:  # Short position
            stopLevel = price + (self.StopLossAtr * self._currentAtr)
            if price >= stopLevel:
                self.LogInfo(f"ATR Stop Loss triggered for short position: Current {price} >= Stop {stopLevel}")
                self.BuyMarket(Math.Abs(self.Position))
                self._isShort = False

    def CreateClone(self):
        """!! REQUIRED!! Creates a new instance of the strategy."""
        return hull_ma_implied_volatility_breakout_strategy()