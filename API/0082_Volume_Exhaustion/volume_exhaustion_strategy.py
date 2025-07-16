import clr

clr.AddReference("System.Drawing")
clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from System.Drawing import Color
from StockSharp.Messages import UnitTypes, Unit, DataType, ICandleMessage, CandleStates, Sides
from StockSharp.Algo.Indicators import SimpleMovingAverage, AverageTrueRange
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *

class volume_exhaustion_strategy(Strategy):
    """
    Volume Exhaustion Strategy.
    Looks for volume spikes with corresponding bullish/bearish candles.
    
    """
    def __init__(self):
        super(volume_exhaustion_strategy, self).__init__()
        
        # Initialize strategy parameters
        self._volumePeriodParam = self.Param("VolumePeriod", 20) \
            .SetDisplay("Volume Average Period", "Period for volume average calculation", "Volume Settings")
        
        self._volumeMultiplierParam = self.Param("VolumeMultiplier", 2.0) \
            .SetDisplay("Volume Multiplier", "Multiplier to determine volume spike", "Volume Settings")
        
        self._maPeriodParam = self.Param("MAPeriod", 20) \
            .SetDisplay("MA Period", "Period for moving average", "Trend Settings")
        
        self._atrMultiplierParam = self.Param("AtrMultiplier", Unit(2, UnitTypes.Absolute)) \
            .SetDisplay("ATR Multiplier", "Multiplier for ATR stop-loss", "Risk Management")
        
        self._candleTypeParam = self.Param("CandleType", tf(5)) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")
        
        self._stopLossPercentParam = self.Param("StopLossPercent", 1.0) \
            .SetDisplay("Stop Loss %", "Stop loss percentage from entry price", "Risk Management")
        
        # Indicators
        self._ma = None
        self._atr = None
        self._volumeAvg = None

    @property
    def VolumePeriod(self):
        return self._volumePeriodParam.Value

    @VolumePeriod.setter
    def VolumePeriod(self, value):
        self._volumePeriodParam.Value = value

    @property
    def VolumeMultiplier(self):
        return self._volumeMultiplierParam.Value

    @VolumeMultiplier.setter
    def VolumeMultiplier(self, value):
        self._volumeMultiplierParam.Value = value

    @property
    def MAPeriod(self):
        return self._maPeriodParam.Value

    @MAPeriod.setter
    def MAPeriod(self, value):
        self._maPeriodParam.Value = value

    @property
    def AtrMultiplier(self):
        return self._atrMultiplierParam.Value

    @AtrMultiplier.setter
    def AtrMultiplier(self, value):
        self._atrMultiplierParam.Value = value

    @property
    def CandleType(self):
        return self._candleTypeParam.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candleTypeParam.Value = value

    @property
    def StopLossPercent(self):
        return self._stopLossPercentParam.Value

    @StopLossPercent.setter
    def StopLossPercent(self, value):
        self._stopLossPercentParam.Value = value

    def OnStarted(self, time):
        """
        Called when the strategy starts.
        """
        super(volume_exhaustion_strategy, self).OnStarted(time)

        # Create indicators
        self._ma = SimpleMovingAverage()
        self._ma.Length = self.MAPeriod
        
        self._atr = AverageTrueRange()
        self._atr.Length = 14
        
        self._volumeAvg = SimpleMovingAverage()
        self._volumeAvg.Length = self.VolumePeriod

        # Create subscription
        subscription = self.SubscribeCandles(self.CandleType)
        
        # Bind indicators and process candles
        subscription.Bind(self._ma, self._atr, self._volumeAvg, self.ProcessCandle).Start()

        # Setup chart visualization
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, self._ma)
            self.DrawOwnTrades(area)

        # Start position protection
        self.StartProtection(
            takeProfit=Unit(),
            stopLoss=Unit(self.StopLossPercent, UnitTypes.Percent),
            isStopTrailing=True
        )
    def ProcessCandle(self, candle, maValue, atrValue, volumeAvgValue):
        """
        Process candle with indicator values.
        """
        # Skip unfinished candles
        if candle.State != CandleStates.Finished:
            return

        # Check if strategy is ready to trade
        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        # Determine candle direction
        isBullishCandle = candle.ClosePrice > candle.OpenPrice
        isBearishCandle = candle.ClosePrice < candle.OpenPrice
        
        # Check for volume spike
        isVolumeSpike = candle.TotalVolume > volumeAvgValue * self.VolumeMultiplier
        
        if not isVolumeSpike:
            return

        # Long entry: Volume spike with bullish candle
        if (isVolumeSpike and isBullishCandle and 
            candle.ClosePrice > maValue and self.Position <= 0):
            self.BuyMarket(self.Volume + abs(self.Position))
            self.LogInfo("Long entry: Volume spike ({0} > {1}) with bullish candle", 
                        candle.TotalVolume, volumeAvgValue * self.VolumeMultiplier)

        # Short entry: Volume spike with bearish candle
        elif (isVolumeSpike and isBearishCandle and 
              candle.ClosePrice < maValue and self.Position >= 0):
            self.SellMarket(self.Volume + abs(self.Position))
            self.LogInfo("Short entry: Volume spike ({0} > {1}) with bearish candle", 
                        candle.TotalVolume, volumeAvgValue * self.VolumeMultiplier)

    def CreateClone(self):
        """
        !! REQUIRED!! Creates a new instance of the strategy.
        """
        return volume_exhaustion_strategy()