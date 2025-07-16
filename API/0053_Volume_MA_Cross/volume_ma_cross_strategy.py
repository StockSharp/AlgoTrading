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
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *
from indicator_extensions import *

class volume_ma_cross_strategy(Strategy):
    """
    Volume MA Cross strategy
    Long entry: Fast volume MA crosses above slow volume MA
    Short entry: Fast volume MA crosses below slow volume MA
    Exit: Reverse crossover
    
    See more examples: https://github.com/StockSharp/AlgoTrading
    """
    def __init__(self):
        super(volume_ma_cross_strategy, self).__init__()
        
        # Initialize internal state
        self._previousFastVolumeMA = 0
        self._previousSlowVolumeMA = 0
        self._isFirstValue = True
        self._fastVolumeMA = None
        self._slowVolumeMA = None

        # Initialize strategy parameters
        self._fastVolumeMALength = self.Param("FastVolumeMALength", 10) \
            .SetDisplay("Fast Volume MA Length", "Period for Fast Volume Moving Average", "Strategy Parameters")

        self._slowVolumeMALength = self.Param("SlowVolumeMALength", 50) \
            .SetDisplay("Slow Volume MA Length", "Period for Slow Volume Moving Average", "Strategy Parameters")

        self._candleType = self.Param("CandleType", tf(5)) \
            .SetDisplay("Candle Type", "Type of candles for strategy calculation", "Strategy Parameters")

    @property
    def FastVolumeMALength(self):
        return self._fastVolumeMALength.Value

    @FastVolumeMALength.setter
    def FastVolumeMALength(self, value):
        self._fastVolumeMALength.Value = value

    @property
    def SlowVolumeMALength(self):
        return self._slowVolumeMALength.Value

    @SlowVolumeMALength.setter
    def SlowVolumeMALength(self, value):
        self._slowVolumeMALength.Value = value

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
        super(volume_ma_cross_strategy, self).OnReseted()
        self._previousFastVolumeMA = 0
        self._previousSlowVolumeMA = 0
        self._isFirstValue = True

    def OnStarted(self, time):
        """
        Called when the strategy starts. Sets up indicators, subscriptions, and charting.
        
        :param time: The time when the strategy started.
        """
        super(volume_ma_cross_strategy, self).OnStarted(time)

        self._previousFastVolumeMA = 0
        self._previousSlowVolumeMA = 0
        self._isFirstValue = True

        # Create indicators
        self._fastVolumeMA = SimpleMovingAverage()
        self._fastVolumeMA.Length = self.FastVolumeMALength
        
        self._slowVolumeMA = SimpleMovingAverage()
        self._slowVolumeMA.Length = self.SlowVolumeMALength
        
        priceMA = SimpleMovingAverage()  # Use same period as fast Volume MA
        priceMA.Length = self.FastVolumeMALength

        # Create subscription
        subscription = self.SubscribeCandles(self.CandleType)
        
        # Regular price MA binding for chart visualization
        subscription.Bind(priceMA, self.ProcessCandle).Start()

        # Configure protection
        self.StartProtection(
            Unit(3, UnitTypes.Percent),  # Take profit
            Unit(2, UnitTypes.Percent)   # Stop loss
        )

        # Setup chart visualization
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, priceMA)
            self.DrawOwnTrades(area)

    def ProcessCandle(self, candle, priceMAValue):
        """
        Process candle and execute trading logic
        
        :param candle: The candle message.
        :param priceMAValue: The price Moving Average value.
        """
        if candle.State != CandleStates.Finished:
            return

        # Process volume through MAs
        fastMAValue = to_float(process_float(self._fastVolumeMA, candle.TotalVolume, candle.ServerTime, True))
        slowMAValue = to_float(process_float(self._slowVolumeMA, candle.TotalVolume, candle.ServerTime, True))

        # Check if strategy is ready to trade
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        
        # Skip the first values to initialize previous values
        if self._isFirstValue:
            self._previousFastVolumeMA = fastMAValue
            self._previousSlowVolumeMA = slowMAValue
            self._isFirstValue = False
            return
        
        # Check for crossovers
        crossAbove = (self._previousFastVolumeMA <= self._previousSlowVolumeMA and 
                     fastMAValue > slowMAValue)
        crossBelow = (self._previousFastVolumeMA >= self._previousSlowVolumeMA and 
                     fastMAValue < slowMAValue)
        
        # Log current values
        self.LogInfo("Candle Close: {0}, Price MA: {1}".format(candle.ClosePrice, priceMAValue))
        self.LogInfo("Fast Volume MA: {0}, Slow Volume MA: {1}".format(fastMAValue, slowMAValue))
        self.LogInfo("Cross Above: {0}, Cross Below: {1}".format(crossAbove, crossBelow))

        # Trading logic:
        # Long: Fast volume MA crosses above slow volume MA
        if crossAbove and self.Position <= 0:
            self.LogInfo("Buy Signal: Fast Volume MA crossing above Slow Volume MA")
            self.BuyMarket(self.Volume + Math.Abs(self.Position))
        # Short: Fast volume MA crosses below slow volume MA
        elif crossBelow and self.Position >= 0:
            self.LogInfo("Sell Signal: Fast Volume MA crossing below Slow Volume MA")
            self.SellMarket(self.Volume + Math.Abs(self.Position))
        
        # Exit logic: Reverse crossover
        if self.Position > 0 and crossBelow:
            self.LogInfo("Exit Long: Fast Volume MA crossing below Slow Volume MA")
            self.SellMarket(Math.Abs(self.Position))
        elif self.Position < 0 and crossAbove:
            self.LogInfo("Exit Short: Fast Volume MA crossing above Slow Volume MA")
            self.BuyMarket(Math.Abs(self.Position))

        # Store current values for next comparison
        self._previousFastVolumeMA = fastMAValue
        self._previousSlowVolumeMA = slowMAValue

    def CreateClone(self):
        """
        !! REQUIRED!! Creates a new instance of the strategy.
        """
        return volume_ma_cross_strategy()