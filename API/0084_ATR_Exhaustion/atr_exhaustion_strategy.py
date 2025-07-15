import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from System import Math
from StockSharp.Messages import DataType, UnitTypes, Unit
from StockSharp.Messages import CandleStates
from StockSharp.Algo.Indicators import SimpleMovingAverage, AverageTrueRange
from StockSharp.Algo.Strategies import Strategy

class atr_exhaustion_strategy(Strategy):
    """
    ATR Exhaustion Strategy.
    Enters long when ATR rises significantly and a bullish candle forms.
    Enters short when ATR rises significantly and a bearish candle forms.
    """

    def __init__(self):
        """Initializes a new instance of the strategy."""
        super(atr_exhaustion_strategy, self).__init__()

        # Initialize strategy parameters
        self._atr_period = self.Param("AtrPeriod", 14) \
            .SetDisplay("ATR Period", "Period for ATR calculation", "Indicators") \
            .SetRange(7, 21) \
            .SetCanOptimize(True)

        self._atr_avg_period = self.Param("AtrAvgPeriod", 20) \
            .SetDisplay("ATR Average Period", "Period for ATR average calculation", "Indicators") \
            .SetRange(10, 30) \
            .SetCanOptimize(True)

        self._atr_multiplier = self.Param("AtrMultiplier", 1.5) \
            .SetDisplay("ATR Multiplier", "Multiplier to determine ATR spike", "Indicators") \
            .SetRange(1.3, 2.0) \
            .SetCanOptimize(True)

        self._ma_period = self.Param("MaPeriod", 20) \
            .SetDisplay("MA Period", "Period for moving average", "Indicators") \
            .SetRange(10, 50) \
            .SetCanOptimize(True)

        self._stop_loss = self.Param("StopLoss", Unit(2, UnitTypes.Percent)) \
            .SetDisplay("Stop Loss", "Stop loss as percentage from entry price", "Risk Management") \
            .SetRange(1.0, 3.0) \
            .SetCanOptimize(True)

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")

        self._atr_avg = None

    @property
    def AtrPeriod(self):
        """Period for ATR calculation."""
        return self._atr_period.Value

    @AtrPeriod.setter
    def AtrPeriod(self, value):
        self._atr_period.Value = value

    @property
    def AtrAvgPeriod(self):
        """Period for ATR average calculation."""
        return self._atr_avg_period.Value

    @AtrAvgPeriod.setter
    def AtrAvgPeriod(self, value):
        self._atr_avg_period.Value = value

    @property
    def AtrMultiplier(self):
        """Multiplier to determine ATR spike."""
        return self._atr_multiplier.Value

    @AtrMultiplier.setter
    def AtrMultiplier(self, value):
        self._atr_multiplier.Value = value

    @property
    def MaPeriod(self):
        """Period for moving average."""
        return self._ma_period.Value

    @MaPeriod.setter
    def MaPeriod(self, value):
        self._ma_period.Value = value

    @property
    def StopLoss(self):
        """Stop loss percentage from entry price."""
        return self._stop_loss.Value

    @StopLoss.setter
    def StopLoss(self, value):
        self._stop_loss.Value = value

    @property
    def CandleType(self):
        """Type of candles to use."""
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    def GetWorkingSecurities(self):
        """Return the security and candle type this strategy works with."""
        return [(self.Security, self.CandleType)]

    def OnStarted(self, time):
        """Called when the strategy starts."""
        super(atr_exhaustion_strategy, self).OnStarted(time)

        # Enable position protection using stop-loss
        self.StartProtection(None, self.StopLoss)

        # Create indicators
        ma = SimpleMovingAverage()
        ma.Length = self.MaPeriod
        atr = AverageTrueRange()
        atr.Length = self.AtrPeriod
        self._atr_avg = SimpleMovingAverage()
        self._atr_avg.Length = self.AtrAvgPeriod

        # Create subscription
        subscription = self.SubscribeCandles(self.CandleType)

        # Bind indicators to candles
        subscription.Bind(ma, atr, self.ProcessCandle).Start()

        # Setup chart visualization
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, ma)
            self.DrawIndicator(area, atr)
            self.DrawOwnTrades(area)

    def ProcessCandle(self, candle, ma_value, atr_value):
        """Process candle with indicator values."""
        # Skip unfinished candles
        if candle.State != CandleStates.Finished:
            return

        # Check if strategy is ready to trade
        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        # Update ATR average
        atr_avg_value = self._atr_avg.Process(atr_value, candle.ServerTime, candle.State == CandleStates.Finished).ToDecimal()

        # Determine candle direction
        is_bullish_candle = candle.ClosePrice > candle.OpenPrice
        is_bearish_candle = candle.ClosePrice < candle.OpenPrice

        # Check for ATR spike
        is_atr_spike = atr_value > atr_avg_value * self.AtrMultiplier

        if not is_atr_spike:
            return

        # Long entry: ATR spike with bullish candle
        if is_atr_spike and is_bullish_candle and self.Position <= 0:
            self.BuyMarket(self.Volume + Math.Abs(self.Position))
            self.LogInfo("Long entry: ATR spike ({0} > {1}) with bullish candle".format(atr_value, atr_avg_value * self.AtrMultiplier))
        # Short entry: ATR spike with bearish candle
        elif is_atr_spike and is_bearish_candle and self.Position >= 0:
            self.SellMarket(self.Volume + Math.Abs(self.Position))
            self.LogInfo("Short entry: ATR spike ({0} > {1}) with bearish candle".format(atr_value, atr_avg_value * self.AtrMultiplier))

    def CreateClone(self):
        """!! REQUIRED!! Creates a new instance of the strategy."""
        return atr_exhaustion_strategy()
