import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, Unit, UnitTypes, CandleStates
from StockSharp.Algo.Indicators import AverageDirectionalIndex, VolumeWeightedMovingAverage
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *
from indicator_extensions import *

class vwap_adx_strategy(Strategy):
    """
    Strategy based on VWAP and ADX indicators.
    Enters long when price is above VWAP and ADX > 25.
    Enters short when price is below VWAP and ADX > 25.
    Exits when ADX < 20.

    """

    def __init__(self):
        super(vwap_adx_strategy, self).__init__()

        # Stop loss percentage value.
        self._stop_loss_percent = self.Param("StopLossPercent", 2.0) \
            .SetDisplay("Stop loss (%)", "Stop loss percentage from entry price", "Risk Management")

        # ADX indicator period.
        self._adx_period = self.Param("AdxPeriod", 14) \
            .SetDisplay("ADX Period", "Period for Average Directional Movement Index", "Indicators") \
            .SetCanOptimize(True) \
            .SetOptimize(10, 20, 1)

        # Candle type for strategy.
        self._candle_type = self.Param("CandleType", tf(5)) \
            .SetDisplay("Candle Type", "Timeframe of data for strategy", "General")

        self._adx = None
        self._vwap = None
        self._prev_adx_value = 0

    @property
    def StopLossPercent(self):
        return self._stop_loss_percent.Value

    @StopLossPercent.setter
    def StopLossPercent(self, value):
        self._stop_loss_percent.Value = value

    @property
    def AdxPeriod(self):
        return self._adx_period.Value

    @AdxPeriod.setter
    def AdxPeriod(self, value):
        self._adx_period.Value = value

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    def GetWorkingSecurities(self):
        return [(self.Security, self.CandleType)]

    def OnStarted(self, time):
        super(vwap_adx_strategy, self).OnStarted(time)

        self._prev_adx_value = 0

        # Create ADX indicator
        self._adx = AverageDirectionalIndex()
        self._adx.Length = self.AdxPeriod
        self._vwap = VolumeWeightedMovingAverage()
        self._vwap.Length = self.AdxPeriod

        # Enable position protection
        self.StartProtection(
            takeProfit=Unit(self.StopLossPercent, UnitTypes.Percent),
            stopLoss=Unit(self.StopLossPercent, UnitTypes.Percent)
        )
        # Create subscription and subscribe to VWAP
        subscription = self.SubscribeCandles(self.CandleType)

        # Process candles with ADX
        subscription.BindEx(self._adx, self.ProcessCandle).Start()

        # Setup chart visualization
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, self._adx)
            self.DrawOwnTrades(area)

    def ProcessCandle(self, candle, adx_value):
        # Skip unfinished candles
        if candle.State != CandleStates.Finished:
            return

        vwap = to_float(process_candle(self._vwap, candle))

        # Get current ADX value
        typed_adx = adx_value
        try:
            current_adx_value = typed_adx.MovingAverage
        except AttributeError:
            return

        # Skip if not formed or online
        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        # Trading logic
        if current_adx_value > 25:
            # Strong trend detected
            if candle.ClosePrice > vwap and self.Position <= 0:
                # Price above VWAP - go long
                self.BuyMarket(self.Volume + Math.Abs(self.Position))
            elif candle.ClosePrice < vwap and self.Position >= 0:
                # Price below VWAP - go short
                self.SellMarket(self.Volume + Math.Abs(self.Position))
        elif current_adx_value < 20 and self._prev_adx_value > 20:
            # Trend weakening - close position
            self.ClosePosition()

        # Store current ADX value for next candle
        self._prev_adx_value = current_adx_value

    def CreateClone(self):
        """!! REQUIRED!! Creates a new instance of the strategy."""
        return vwap_adx_strategy()
