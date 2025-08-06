import clr

clr.AddReference("System.Drawing")
clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from System.Drawing import Color
from StockSharp.Messages import UnitTypes, Unit, DataType, CandleStates
from StockSharp.Algo.Indicators import SimpleMovingAverage, AverageDirectionalIndex
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *
from indicator_extensions import *

class adx_weakening_strategy(Strategy):
    """
    ADX Weakening Strategy.
    Enters long when ADX weakens and price is above MA.
    Enters short when ADX weakens and price is below MA.

    """
    def __init__(self):
        """Initializes a new instance of the ``adx_weakening_strategy``."""
        super(adx_weakening_strategy, self).__init__()

        # Initialize strategy parameters
        self._adxPeriod = self.Param("AdxPeriod", 14) \
            .SetDisplay("ADX Period", "Period for ADX calculation", "Indicators") \
            .SetRange(7, 28) \
            .SetCanOptimize(True)

        self._maPeriod = self.Param("MaPeriod", 20) \
            .SetDisplay("MA Period", "Period for moving average", "Indicators") \
            .SetRange(10, 50) \
            .SetCanOptimize(True)

        self._stopLoss = self.Param("StopLoss", Unit(2, UnitTypes.Percent)) \
            .SetDisplay("Stop Loss", "Stop loss as percentage from entry price", "Risk Management") \
            .SetRange(1.0, 3.0) \
            .SetCanOptimize(True)

        self._candleType = self.Param("CandleType", tf(15)) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")

        # Internal state
        self._prevAdxValue = 0.0

    @property
    def AdxPeriod(self):
        """Period for ADX calculation."""
        return self._adxPeriod.Value

    @AdxPeriod.setter
    def AdxPeriod(self, value):
        self._adxPeriod.Value = value

    @property
    def MaPeriod(self):
        """Period for moving average."""
        return self._maPeriod.Value

    @MaPeriod.setter
    def MaPeriod(self, value):
        self._maPeriod.Value = value

    @property
    def StopLoss(self):
        """Stop loss percentage from entry price."""
        return self._stopLoss.Value

    @StopLoss.setter
    def StopLoss(self, value):
        self._stopLoss.Value = value

    @property
    def CandleType(self):
        """Type of candles to use."""
        return self._candleType.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candleType.Value = value

    def OnReseted(self):
        super(adx_weakening_strategy, self).OnReseted()
        self._prevAdxValue = 0.0

    def OnStarted(self, time):
        """Called when the strategy starts."""
        super(adx_weakening_strategy, self).OnStarted(time)

        # Enable position protection using stop-loss
        self.StartProtection(
            takeProfit=None,
            stopLoss=self.StopLoss,
            isStopTrailing=False,
            useMarketOrders=True
        )
        # Create indicators
        ma = SimpleMovingAverage()
        ma.Length = self.MaPeriod

        adx = AverageDirectionalIndex()
        adx.Length = self.AdxPeriod

        # Create subscription
        subscription = self.SubscribeCandles(self.CandleType)

        # Bind indicators and process candles
        subscription.BindEx(ma, adx, self.ProcessCandle).Start()

        # Setup chart visualization
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, ma)
            self.DrawIndicator(area, adx)
            self.DrawOwnTrades(area)

    def ProcessCandle(self, candle, maValue, adxValue):
        """
        Process candle with indicator values.

        :param candle: Candle.
        :param maValue: Moving average value.
        :param adxValue: ADX value.
        """
        # Skip unfinished candles
        if candle.State != CandleStates.Finished:
            return

        # Check if strategy is ready to trade
        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        ma = float(maValue)

        if adxValue.MovingAverage is None:
            return
        adx_val = float(adxValue.MovingAverage)

        dx = adxValue.Dx
        if dx.Plus is None or dx.Minus is None:
            return

        # If this is the first calculation, just store the ADX value
        if self._prevAdxValue == 0.0:
            self._prevAdxValue = adx_val
            return

        # Check if ADX is weakening (decreasing)
        isAdxWeakening = adx_val < self._prevAdxValue

        # Long entry: ADX weakening and price above MA
        if isAdxWeakening and candle.ClosePrice > ma and self.Position <= 0:
            self.BuyMarket(self.Volume + Math.Abs(self.Position))
            self.LogInfo(
                "Long entry: ADX weakening ({0} < {1}) and price above MA".format(adx_val, self._prevAdxValue))
        # Short entry: ADX weakening and price below MA
        elif isAdxWeakening and candle.ClosePrice < ma and self.Position >= 0:
            self.SellMarket(self.Volume + Math.Abs(self.Position))
            self.LogInfo(
                "Short entry: ADX weakening ({0} < {1}) and price below MA".format(adx_val, self._prevAdxValue))

        # Update previous ADX value
        self._prevAdxValue = adx_val

    def CreateClone(self):
        """!! REQUIRED!! Creates a new instance of the strategy."""
        return adx_weakening_strategy()
