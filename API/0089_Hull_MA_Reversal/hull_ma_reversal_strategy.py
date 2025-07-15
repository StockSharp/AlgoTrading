import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from System import Math
from StockSharp.Messages import UnitTypes, Unit, DataType, ICandleMessage, CandleStates
from StockSharp.Algo.Indicators import HullMovingAverage, AverageTrueRange
from StockSharp.Algo.Strategies import Strategy

class hull_ma_reversal_strategy(Strategy):
    """
    Hull MA Reversal Strategy.
    Enters long when Hull MA changes direction from down to up.
    Enters short when Hull MA changes direction from up to down.
    """

    def __init__(self):
        super(hull_ma_reversal_strategy, self).__init__()

        # Initializes a new instance of the HullMaReversalStrategy.
        self._hma_period = self.Param("HmaPeriod", 9) \
            .SetDisplay("HMA Period", "Period for Hull Moving Average", "Indicator Settings")
        self._atr_multiplier = self.Param("AtrMultiplier", Unit(2, UnitTypes.Absolute)) \
            .SetDisplay("ATR Multiplier", "Multiplier for ATR stop-loss", "Risk Management")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(15))) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")
        self._stop_loss_percent = self.Param("StopLossPercent", 1.0) \
            .SetDisplay("Stop Loss %", "Stop loss percentage from entry price", "Risk Management")

        self._prev_hma_value = 0.0
        self._prev_prev_hma_value = 0.0
        self._atr = None

    @property
    def HmaPeriod(self):
        """Period for Hull Moving Average."""
        return self._hma_period.Value

    @HmaPeriod.setter
    def HmaPeriod(self, value):
        self._hma_period.Value = value

    @property
    def AtrMultiplier(self):
        """ATR multiplier for stop-loss calculation."""
        return self._atr_multiplier.Value

    @AtrMultiplier.setter
    def AtrMultiplier(self, value):
        self._atr_multiplier.Value = value

    @property
    def CandleType(self):
        """Type of candles to use."""
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    @property
    def StopLossPercent(self):
        """Stop-loss percentage."""
        return self._stop_loss_percent.Value

    @StopLossPercent.setter
    def StopLossPercent(self, value):
        self._stop_loss_percent.Value = value

    def GetWorkingSecurities(self):
        """See base class for details."""
        return [(self.Security, self.CandleType)]

    def OnStarted(self, time):
        """Called when the strategy starts."""
        super(hull_ma_reversal_strategy, self).OnStarted(time)

        # Initialize previous values
        self._prev_hma_value = 0.0
        self._prev_prev_hma_value = 0.0

        # Create indicators
        hma = HullMovingAverage()
        hma.Length = self.HmaPeriod
        self._atr = AverageTrueRange()
        self._atr.Length = 14

        # Create subscription
        subscription = self.SubscribeCandles(self.CandleType)

        # Bind indicators and process candles
        subscription.Bind(hma, self._atr, self.ProcessCandle).Start()

        # Setup chart visualization
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, hma)
            self.DrawOwnTrades(area)

        self.StartProtection(
            Unit(0),
            Unit(self.StopLossPercent, UnitTypes.Percent),
            useMarketOrders=True
        )

    def ProcessCandle(self, candle: ICandleMessage, hmaValue: float, atrValue: float):
        """
        Process candle with indicator values.

        :param candle: Candle.
        :param hmaValue: Hull MA value.
        :param atrValue: ATR value.
        """
        # Skip unfinished candles
        if candle.State != CandleStates.Finished:
            return

        # Check if strategy is ready to trade
        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        # If this is one of the first calculations, just store the values
        if self._prev_hma_value == 0:
            self._prev_hma_value = hmaValue
            return

        if self._prev_prev_hma_value == 0:
            self._prev_prev_hma_value = self._prev_hma_value
            self._prev_hma_value = hmaValue
            return

        # Check for Hull MA direction change
        directionChangedUp = self._prev_hma_value < self._prev_prev_hma_value and hmaValue > self._prev_hma_value
        directionChangedDown = self._prev_hma_value > self._prev_prev_hma_value and hmaValue < self._prev_hma_value

        # Long entry: Hull MA changed direction from down to up
        if directionChangedUp and self.Position <= 0:
            self.BuyMarket(self.Volume + Math.Abs(self.Position))
            self.LogInfo("Long entry: Hull MA direction changed up ({0} -> {1} -> {2})".format(
                self._prev_prev_hma_value, self._prev_hma_value, hmaValue))
        # Short entry: Hull MA changed direction from up to down
        elif directionChangedDown and self.Position >= 0:
            self.SellMarket(self.Volume + Math.Abs(self.Position))
            self.LogInfo("Short entry: Hull MA direction changed down ({0} -> {1} -> {2})".format(
                self._prev_prev_hma_value, self._prev_hma_value, hmaValue))

        # Update previous values
        self._prev_prev_hma_value = self._prev_hma_value
        self._prev_hma_value = hmaValue

    def CreateClone(self):
        """!! REQUIRED!! Creates a new instance of the strategy."""
        return hull_ma_reversal_strategy()
