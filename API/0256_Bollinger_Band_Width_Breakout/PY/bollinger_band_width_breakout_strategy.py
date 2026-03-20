import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, Unit, UnitTypes, CandleStates
from StockSharp.Algo.Indicators import BollingerBands, SimpleMovingAverage, AverageTrueRange, DecimalIndicatorValue
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *
from indicator_extensions import *


class bollinger_band_width_breakout_strategy(Strategy):
    """
    Strategy that trades on Bollinger Band width breakouts.
    When Bollinger Band width increases significantly above its average,
    it enters position in the direction determined by price movement.
    """

    def __init__(self):
        super(bollinger_band_width_breakout_strategy, self).__init__()

        self._bollinger_length = self.Param("BollingerLength", 20) \
            .SetGreaterThanZero() \
            .SetDisplay("Bollinger Length", "Period of the Bollinger Bands indicator", "Indicators") \
            .SetCanOptimize(True) \
            .SetOptimize(10, 50, 5)

        self._bollinger_deviation = self.Param("BollingerDeviation", 2.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Bollinger Deviation", "Standard deviation multiplier for Bollinger Bands", "Indicators") \
            .SetCanOptimize(True) \
            .SetOptimize(1.0, 3.0, 0.5)

        self._avg_period = self.Param("AvgPeriod", 20) \
            .SetGreaterThanZero() \
            .SetDisplay("Average Period", "Period for Bollinger width average calculation", "Indicators") \
            .SetCanOptimize(True) \
            .SetOptimize(10, 50, 5)

        self._multiplier = self.Param("Multiplier", 1.5) \
            .SetGreaterThanZero() \
            .SetDisplay("Multiplier", "Standard deviation multiplier for breakout detection", "Indicators") \
            .SetCanOptimize(True) \
            .SetOptimize(1.0, 3.0, 0.5)

        self._candle_type = self.Param("CandleType", tf(5)) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")

        self._stop_multiplier = self.Param("StopMultiplier", 2) \
            .SetGreaterThanZero() \
            .SetDisplay("Stop Multiplier", "ATR multiplier for stop-loss", "Risk Management") \
            .SetCanOptimize(True) \
            .SetOptimize(1, 5, 1)

        self._bollinger = None
        self._width_average = None
        self._atr = None

    @property
    def BollingerLength(self):
        return self._bollinger_length.Value

    @BollingerLength.setter
    def BollingerLength(self, value):
        self._bollinger_length.Value = value

    @property
    def BollingerDeviation(self):
        return self._bollinger_deviation.Value

    @BollingerDeviation.setter
    def BollingerDeviation(self, value):
        self._bollinger_deviation.Value = value

    @property
    def AvgPeriod(self):
        return self._avg_period.Value

    @AvgPeriod.setter
    def AvgPeriod(self, value):
        self._avg_period.Value = value

    @property
    def Multiplier(self):
        return self._multiplier.Value

    @Multiplier.setter
    def Multiplier(self, value):
        self._multiplier.Value = value

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    @property
    def StopMultiplier(self):
        return self._stop_multiplier.Value

    @StopMultiplier.setter
    def StopMultiplier(self, value):
        self._stop_multiplier.Value = value

    def GetWorkingSecurities(self):
        return [(self.Security, self.CandleType)]

    def OnReseted(self):
        super(bollinger_band_width_breakout_strategy, self).OnReseted()

    def OnStarted(self, time):
        super(bollinger_band_width_breakout_strategy, self).OnStarted(time)

        # Create indicators
        self._bollinger = BollingerBands()
        self._bollinger.Length = self.BollingerLength
        self._bollinger.Width = self.BollingerDeviation
        self._width_average = SimpleMovingAverage()
        self._width_average.Length = self.AvgPeriod
        self._atr = AverageTrueRange()
        self._atr.Length = self.BollingerLength

        # Create subscription
        subscription = self.SubscribeCandles(self.CandleType)

        # Bind Bollinger Bands and ATR
        subscription.BindEx(self._bollinger, self._atr, self.ProcessBollinger).Start()

        self.StartProtection(
            takeProfit=Unit(2, UnitTypes.Percent),
            stopLoss=Unit(1, UnitTypes.Percent)
        )

        # Create chart area for visualization
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, self._bollinger)
            self.DrawOwnTrades(area)

    def ProcessBollinger(self, candle, bollinger_value, atr_value):
        if candle.State != CandleStates.Finished:
            return

        if not bollinger_value.IsFinal or not atr_value.IsFinal or bollinger_value.IsEmpty or atr_value.IsEmpty:
            return

        # Calculate Bollinger Band width
        if bollinger_value.UpBand is None or bollinger_value.LowBand is None:
            return

        upper_band = float(bollinger_value.UpBand)
        lower_band = float(bollinger_value.LowBand)
        last_width = upper_band - lower_band

        # Process width through average
        avg_result = process_float(self._width_average, last_width, candle.ServerTime, True)
        avg_width = float(avg_result)

        # Skip if indicators are not formed yet
        if not self._width_average.IsFormed:
            return

        # Check if trading is allowed
        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        # Bollinger width breakout detection
        if last_width > avg_width * (1.0 + self.Multiplier / 10.0):
            # Determine direction based on price and bands
            upper_distance = abs(float(candle.ClosePrice) - upper_band)
            lower_distance = abs(float(candle.ClosePrice) - lower_band)
            price_direction = upper_distance < lower_distance

            if price_direction and self.Position == 0:
                self.BuyMarket()
            elif not price_direction and self.Position == 0:
                self.SellMarket()

    def CreateClone(self):
        return bollinger_band_width_breakout_strategy()
