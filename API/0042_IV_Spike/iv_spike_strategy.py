import clr

clr.AddReference("System.Drawing")
clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from System import Math
from System.Drawing import Color
from StockSharp.Messages import UnitTypes
from StockSharp.Messages import Unit
from StockSharp.Messages import DataType
from StockSharp.Messages import ICandleMessage
from StockSharp.Messages import CandleStates
from StockSharp.Messages import Sides
from StockSharp.Algo.Indicators import SimpleMovingAverage
from StockSharp.Algo.Indicators import StandardDeviation
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *

class iv_spike_strategy(Strategy):
    """
    IV Spike strategy based on implied volatility spikes
    This strategy enters long when IV increases by 50% and price is below MA,
    or short when IV increases by 50% and price is above MA
    
    """
    def __init__(self):
        super(iv_spike_strategy, self).__init__()
        
        # Initialize internal state
        self._previousIV = 0

        # Initialize strategy parameters
        self._maPeriod = self.Param("MAPeriod", 20) \
            .SetDisplay("MA Period", "Period for Moving Average calculation", "Strategy Parameters")

        self._ivPeriod = self.Param("IVPeriod", 20) \
            .SetDisplay("IV Period", "Period for Implied Volatility calculation", "Strategy Parameters")

        self._ivSpikeThreshold = self.Param("IVSpikeThreshold", 1.5) \
            .SetDisplay("IV Spike Threshold", "Minimum IV increase multiplier (e.g., 1.5 = 50% increase)", "Strategy Parameters")

        self._candleType = self.Param("CandleType", tf(5)) \
            .SetDisplay("Candle Type", "Type of candles for strategy calculation", "Strategy Parameters")

    @property
    def MAPeriod(self):
        return self._maPeriod.Value

    @MAPeriod.setter
    def MAPeriod(self, value):
        self._maPeriod.Value = value

    @property
    def IVPeriod(self):
        return self._ivPeriod.Value

    @IVPeriod.setter
    def IVPeriod(self, value):
        self._ivPeriod.Value = value

    @property
    def IVSpikeThreshold(self):
        return self._ivSpikeThreshold.Value

    @IVSpikeThreshold.setter
    def IVSpikeThreshold(self, value):
        self._ivSpikeThreshold.Value = value

    @property
    def CandleType(self):
        return self._candleType.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candleType.Value = value

    def OnReseted(self):
        """
        Resets internal state when strategy is reset.
        """
        super(iv_spike_strategy, self).OnReseted()
        self._previousIV = 0

    def OnStarted(self, time):
        """
        Called when the strategy starts. Sets up indicators, subscriptions, and charting.
        
        :param time: The time when the strategy started.
        """
        super(iv_spike_strategy, self).OnStarted(time)

        self._previousIV = 0

        # Create indicators
        ma = SimpleMovingAverage()
        ma.Length = self.MAPeriod
        
        hv = StandardDeviation()  # Using standard deviation as proxy for IV
        hv.Length = self.IVPeriod

        # Create subscription and bind indicators
        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(ma, hv, self.ProcessCandle).Start()

        # Configure protection
        self.StartProtection(
            takeProfit=Unit(3, UnitTypes.Percent),
            stopLoss=Unit(2, UnitTypes.Percent)
        )
        # Setup chart visualization
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, ma)
            self.DrawIndicator(area, hv)
            self.DrawOwnTrades(area)

    def ProcessCandle(self, candle, maValue, ivValue):
        """
        Process candle and execute trading logic
        
        :param candle: The candle message.
        :param maValue: The Moving Average value.
        :param ivValue: The IV (standard deviation) value.
        """
        # Skip unfinished candles
        if candle.State != CandleStates.Finished:
            return

        # Check if strategy is ready to trade
        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        # Initialize previous IV on first candle
        if self._previousIV == 0 and ivValue > 0:
            self._previousIV = ivValue
            return

        # Calculate IV change
        ivChange = ivValue / self._previousIV if self._previousIV != 0 else 1

        # Log current values
        self.LogInfo("Candle Close: {0}, MA: {1}, IV: {2}, IV Change: {3:P2}".format(
            candle.ClosePrice, maValue, ivValue, ivChange - 1))

        # Trading logic:
        # Check for IV spike
        if ivChange >= self.IVSpikeThreshold:
            self.LogInfo("IV Spike detected: {0:P2}".format(ivChange - 1))

            # Long: IV spike and price below MA
            if candle.ClosePrice < maValue and self.Position <= 0:
                self.LogInfo("Buy Signal: IV Spike ({0:P2}) and Price ({1}) < MA ({2})".format(
                    ivChange - 1, candle.ClosePrice, maValue))
                self.BuyMarket(self.Volume + Math.Abs(self.Position))
            # Short: IV spike and price above MA
            elif candle.ClosePrice > maValue and self.Position >= 0:
                self.LogInfo("Sell Signal: IV Spike ({0:P2}) and Price ({1}) > MA ({2})".format(
                    ivChange - 1, candle.ClosePrice, maValue))
                self.SellMarket(self.Volume + Math.Abs(self.Position))

        # Exit logic: IV declining (IV now < previous IV)
        if ivValue < self._previousIV:
            if self.Position > 0:
                self.LogInfo("Exit Long: IV declining ({0} < {1})".format(ivValue, self._previousIV))
                self.SellMarket(Math.Abs(self.Position))
            elif self.Position < 0:
                self.LogInfo("Exit Short: IV declining ({0} < {1})".format(ivValue, self._previousIV))
                self.BuyMarket(Math.Abs(self.Position))

        # Store current IV for next comparison
        self._previousIV = ivValue

    def CreateClone(self):
        """
        !! REQUIRED!! Creates a new instance of the strategy.
        """
        return iv_spike_strategy()