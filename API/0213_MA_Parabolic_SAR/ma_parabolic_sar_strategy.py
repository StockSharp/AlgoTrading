import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import SimpleMovingAverage, ParabolicSar
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *


class ma_parabolic_sar_strategy(Strategy):
    """
    Strategy based on Moving Average and Parabolic SAR indicators.
    Enters long when price is above MA and above SAR.
    Enters short when price is below MA and below SAR.
    Uses Parabolic SAR as dynamic stop-loss.
    """

    def __init__(self):
        super(ma_parabolic_sar_strategy, self).__init__()

        # Constructor.
        self._ma_period = self.Param("MaPeriod", 20) \
            .SetGreaterThanZero() \
            .SetDisplay("MA Period", "Period for Moving Average calculation", "Indicators") \
            .SetCanOptimize(True) \
            .SetOptimize(10, 50, 5)

        self._sar_step = self.Param("SarStep", 0.02) \
            .SetGreaterThanZero() \
            .SetDisplay("SAR Step", "Acceleration factor for Parabolic SAR", "Indicators") \
            .SetCanOptimize(True) \
            .SetOptimize(0.01, 0.05, 0.01)

        self._sar_max_step = self.Param("SarMaxStep", 0.2) \
            .SetGreaterThanZero() \
            .SetDisplay("SAR Max Step", "Maximum acceleration factor for Parabolic SAR", "Indicators") \
            .SetCanOptimize(True) \
            .SetOptimize(0.1, 0.3, 0.05)

        self._candle_type = self.Param("CandleType", tf(5)) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")

        self._take_value = self.Param("TakeValue", Unit(0, UnitTypes.Absolute)) \
            .SetDisplay("Take Profit", "Take profit value", "Protection")

        self._stop_value = self.Param("StopValue", Unit(2, UnitTypes.Percent)) \
            .SetDisplay("Stop Loss", "Stop loss value", "Protection")

        self._ma = None
        self._parabolic_sar = None
        self._last_sar_value = 0.0

    @property
    def MaPeriod(self):
        """Moving Average period."""
        return self._ma_period.Value

    @MaPeriod.setter
    def MaPeriod(self, value):
        self._ma_period.Value = value

    @property
    def SarStep(self):
        """Parabolic SAR acceleration factor."""
        return self._sar_step.Value

    @SarStep.setter
    def SarStep(self, value):
        self._sar_step.Value = value

    @property
    def SarMaxStep(self):
        """Parabolic SAR maximum acceleration factor."""
        return self._sar_max_step.Value

    @SarMaxStep.setter
    def SarMaxStep(self, value):
        self._sar_max_step.Value = value

    @property
    def CandleType(self):
        """Candle type parameter."""
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    @property
    def TakeValue(self):
        """Take profit value."""
        return self._take_value.Value

    @TakeValue.setter
    def TakeValue(self, value):
        self._take_value.Value = value

    @property
    def StopValue(self):
        """Stop loss value."""
        return self._stop_value.Value

    @StopValue.setter
    def StopValue(self, value):
        self._stop_value.Value = value

    def GetWorkingSecurities(self):
        """!! REQUIRED !! Returns securities for strategy."""
        return [(self.Security, self.CandleType)]

    def OnStarted(self, time):
        super(ma_parabolic_sar_strategy, self).OnStarted(time)

        self._last_sar_value = 0.0

        # Initialize indicators
        self._ma = SimpleMovingAverage()
        self._ma.Length = self.MaPeriod
        self._parabolic_sar = ParabolicSar()
        self._parabolic_sar.AccelerationStep = self.SarStep
        self._parabolic_sar.AccelerationMax = self.SarMaxStep

        # Create candles subscription
        subscription = self.SubscribeCandles(self.CandleType)

        # Bind indicators to subscription
        subscription.Bind(self._ma, self._parabolic_sar, self.ProcessCandle).Start()

        # Setup chart if available
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, self._ma)
            self.DrawIndicator(area, self._parabolic_sar)
            self.DrawOwnTrades(area)

        # Start protection by take profit and stop loss (like SmaStrategy)
        self.StartProtection(self.TakeValue, self.StopValue)

    def ProcessCandle(self, candle, ma_value, sar_value):
        # Skip unfinished candles
        if candle.State != CandleStates.Finished:
            return

        # Skip if strategy is not ready to trade
        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        # Store current SAR value for stop-loss
        self._last_sar_value = sar_value

        # Trading logic
        is_price_above_ma = candle.ClosePrice > ma_value
        is_price_above_sar = candle.ClosePrice > sar_value

        # Long signal: Price above MA and above SAR
        if is_price_above_ma and is_price_above_sar:
            if self.Position <= 0:
                self.BuyMarket(self.Volume + Math.Abs(self.Position))
                self.LogInfo(f"Long Entry: Price({candle.ClosePrice}) > MA({ma_value}) && Price > SAR({sar_value})")
        # Short signal: Price below MA and below SAR
        elif not is_price_above_ma and not is_price_above_sar:
            if self.Position >= 0:
                self.SellMarket(self.Volume + Math.Abs(self.Position))
                self.LogInfo(f"Short Entry: Price({candle.ClosePrice}) < MA({ma_value}) && Price < SAR({sar_value})")
        # Exit long position: Price falls below SAR
        elif self.Position > 0 and not is_price_above_sar:
            self.SellMarket(Math.Abs(self.Position))
            self.LogInfo(f"Exit Long: Price({candle.ClosePrice}) < SAR({sar_value})")
        # Exit short position: Price rises above SAR
        elif self.Position < 0 and is_price_above_sar:
            self.BuyMarket(Math.Abs(self.Position))
            self.LogInfo(f"Exit Short: Price({candle.ClosePrice}) > SAR({sar_value})")

    def CreateClone(self):
        """!! REQUIRED!! Creates a new instance of the strategy."""
        return ma_parabolic_sar_strategy()
