import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, Unit, UnitTypes, CandleStates
from StockSharp.Algo.Indicators import HullMovingAverage, AverageDirectionalIndex, AverageTrueRange
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *
from indicator_extensions import *

class hull_ma_adx_strategy(Strategy):
    """
    Strategy based on Hull Moving Average and ADX.
    Enters long when HMA increases and ADX > 25 (strong trend).
    Enters short when HMA decreases and ADX > 25 (strong trend).
    Exits when ADX < 20 (weakening trend).

    """

    def __init__(self):
        super(hull_ma_adx_strategy, self).__init__()

        # Initialize strategy parameters
        self._hma_period = self.Param("HmaPeriod", 9) \
            .SetDisplay("HMA Period", "Period for Hull Moving Average calculation", "Indicators") \
            .SetCanOptimize(True) \
            .SetOptimize(5, 15, 2)

        self._adx_period = self.Param("AdxPeriod", 14) \
            .SetDisplay("ADX Period", "Period for Average Directional Movement Index", "Indicators") \
            .SetCanOptimize(True) \
            .SetOptimize(10, 20, 2)

        self._atr_multiplier = self.Param("AtrMultiplier", 2.0) \
            .SetDisplay("ATR Multiplier", "ATR multiplier for stop loss calculation", "Risk Management")

        self._candle_type = self.Param("CandleType", tf(15)) \
            .SetDisplay("Candle Type", "Timeframe of data for strategy", "General")

        self._stop_loss_percent = self.Param("StopLossPercent", 1.0) \
            .SetNotNegative() \
            .SetDisplay("Stop Loss %", "Stop loss percentage from entry price", "Risk Management") \
            .SetCanOptimize(True) \
            .SetOptimize(0.5, 2.0, 0.5)

        # Indicators
        self._hma = None
        self._adx = None
        self._atr = None

        # Previous indicator values
        self._prev_hma_value = 0.0
        self._prev_adx_value = 0.0

    @property
    def hma_period(self):
        """Hull Moving Average period."""
        return self._hma_period.Value

    @hma_period.setter
    def hma_period(self, value):
        self._hma_period.Value = value

    @property
    def adx_period(self):
        """ADX indicator period."""
        return self._adx_period.Value

    @adx_period.setter
    def adx_period(self, value):
        self._adx_period.Value = value

    @property
    def atr_multiplier(self):
        """ATR multiplier for stop loss calculation."""
        return self._atr_multiplier.Value

    @atr_multiplier.setter
    def atr_multiplier(self, value):
        self._atr_multiplier.Value = value

    @property
    def candle_type(self):
        """Candle type for strategy."""
        return self._candle_type.Value

    @candle_type.setter
    def candle_type(self, value):
        self._candle_type.Value = value

    @property
    def stop_loss_percent(self):
        """Stop-loss percentage."""
        return self._stop_loss_percent.Value

    @stop_loss_percent.setter
    def stop_loss_percent(self, value):
        self._stop_loss_percent.Value = value

    def GetWorkingSecurities(self):
        """Return the security and candle type used by the strategy."""
        return [(self.Security, self.candle_type)]

    def OnReseted(self):
        """Resets internal state when strategy is reset."""
        super(hull_ma_adx_strategy, self).OnReseted()
        self._prev_hma_value = 0.0
        self._prev_adx_value = 0.0

    def OnStarted(self, time):
        super(hull_ma_adx_strategy, self).OnStarted(time)

        # Create indicators
        self._hma = HullMovingAverage()
        self._hma.Length = self.hma_period
        self._adx = AverageDirectionalIndex()
        self._adx.Length = self.adx_period
        self._atr = AverageTrueRange()
        self._atr.Length = 14

        # Initialize variables
        self._prev_hma_value = 0.0
        self._prev_adx_value = 0.0

        # Create subscription
        subscription = self.SubscribeCandles(self.candle_type)

        # Process candles with indicators
        subscription.BindEx(self._hma, self._adx, self._atr, self.ProcessCandle).Start()

        # Setup chart visualization
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, self._hma)
            self.DrawOwnTrades(area)

            # ADX in separate area
            adx_area = self.CreateChartArea()
            if adx_area is not None:
                self.DrawIndicator(adx_area, self._adx)

        self.StartProtection(
            takeProfit=Unit(0, UnitTypes.Absolute),
            stopLoss=Unit(self.stop_loss_percent, UnitTypes.Absolute)
        )
    def ProcessCandle(self, candle, hma_value, adx_value, atr_value):
        # Skip unfinished candles
        if candle.State != CandleStates.Finished:
            return

        typed_adx = adx_value
        if typed_adx.MovingAverage is None:
            return
        adx = float(typed_adx.MovingAverage)

        hma = float(hma_value)

        # Detect HMA direction
        hma_increasing = hma > self._prev_hma_value
        hma_decreasing = hma < self._prev_hma_value

        # Check if strategy is ready for trading
        if not self.IsFormedAndOnlineAndAllowTrading():
            # Store current values for next candle
            self._prev_hma_value = hma
            self._prev_adx_value = adx
            return

        # Trading logic
        if adx > 25:
            # Strong trend detected
            if hma_increasing and self.Position <= 0:
                # HMA rising with strong trend - go long
                self.BuyMarket(self.Volume + Math.Abs(self.Position))
            elif hma_decreasing and self.Position >= 0:
                # HMA falling with strong trend - go short
                self.SellMarket(self.Volume + Math.Abs(self.Position))
        elif adx < 20 and self._prev_adx_value >= 20:
            # Trend weakening - close position
            self.ClosePosition()

        # Store current values for next candle
        self._prev_hma_value = hma
        self._prev_adx_value = adx

    def CreateClone(self):
        """!! REQUIRED!! Creates a new instance of the strategy."""
        return hull_ma_adx_strategy()
