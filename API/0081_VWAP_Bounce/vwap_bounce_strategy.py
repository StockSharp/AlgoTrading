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
from StockSharp.Algo.Strategies import Strategy

class vwap_bounce_strategy(Strategy):
    """
    VWAP Bounce Strategy.
    Enters long when price is below VWAP and a bullish candle forms.
    Enters short when price is above VWAP and a bearish candle forms.
    
    See more examples: https://github.com/StockSharp/AlgoTrading
    """
    def __init__(self):
        super(vwap_bounce_strategy, self).__init__()
        
        # Initialize strategy parameters
        self._candleTypeParam = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")
        
        self._stopLossParam = self.Param("StopLoss", Unit(2, UnitTypes.Percent)) \
            .SetDisplay("Stop Loss", "Stop loss as percentage from entry price", "Risk Management")
        
        # VWAP calculation variables
        self._prevVwap = 0.0

    @property
    def CandleType(self):
        return self._candleTypeParam.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candleTypeParam.Value = value

    @property
    def StopLoss(self):
        return self._stopLossParam.Value

    @StopLoss.setter
    def StopLoss(self, value):
        self._stopLossParam.Value = value

    def OnStarted(self, time):
        """
        Called when the strategy starts.
        """
        super(vwap_bounce_strategy, self).OnStarted(time)

        # Enable position protection using stop-loss
        self.StartProtection(
            None,  # No take profit
            self.StopLoss,  # Stop loss
            False,  # Not trailing
            True   # Use market orders
        )

        # Initialize VWAP
        self._prevVwap = 0.0

        # Create subscription to candles
        subscription = self.SubscribeCandles(self.CandleType)
        
        # Bind candle handler
        subscription.Bind(self.ProcessCandle).Start()

        # Setup chart if available
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def ProcessCandle(self, candle):
        """
        Process new candle.
        """
        # Skip unfinished candles
        if candle.State != CandleStates.Finished:
            return

        # Check if strategy is ready to trade
        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        # Calculate VWAP for current candle
        if candle.TotalVolume != 0:
            vwap = candle.TotalPrice / candle.TotalVolume
        else:
            vwap = candle.ClosePrice

        # If VWAP is not initialized yet
        if self._prevVwap == 0:
            self._prevVwap = vwap
            return
        
        # Bullish candle condition (Close > Open)
        isBullishCandle = candle.ClosePrice > candle.OpenPrice
        
        # Bearish candle condition (Close < Open)
        isBearishCandle = candle.ClosePrice < candle.OpenPrice
        
        # Long entry: Price below VWAP and bullish candle
        if candle.ClosePrice < vwap and isBullishCandle and self.Position <= 0:
            self.BuyMarket(self.Volume + abs(self.Position))
            self.LogInfo("Long entry: Close {0}, VWAP {1}, Bullish Candle", 
                        candle.ClosePrice, vwap)

        # Short entry: Price above VWAP and bearish candle
        elif candle.ClosePrice > vwap and isBearishCandle and self.Position >= 0:
            self.SellMarket(self.Volume + abs(self.Position))
            self.LogInfo("Short entry: Close {0}, VWAP {1}, Bearish Candle", 
                        candle.ClosePrice, vwap)

        # Update previous VWAP value
        self._prevVwap = vwap

    def CreateClone(self):
        """
        !! REQUIRED!! Creates a new instance of the strategy.
        """
        return vwap_bounce_strategy()