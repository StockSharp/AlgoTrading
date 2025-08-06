import clr

clr.AddReference("Ecng.Common")
clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import ParabolicSar
from StockSharp.Algo.Strategies import Strategy
from Ecng.Common import RandomGen
from datatype_extensions import *
from indicator_extensions import *

class parabolic_sar_sentiment_divergence_strategy(Strategy):
    """Parabolic SAR strategy with sentiment divergence."""

    def __init__(self):
        super(parabolic_sar_sentiment_divergence_strategy, self).__init__()

        # SAR Starting acceleration factor.
        self._start_af = self.Param("StartAf", 0.02) \
            .SetRange(0.01, 0.1) \
            .SetCanOptimize(True) \
            .SetDisplay("Starting AF", "Starting acceleration factor for Parabolic SAR", "SAR Parameters")

        # SAR Maximum acceleration factor.
        self._max_af = self.Param("MaxAf", 0.2) \
            .SetRange(0.1, 0.5) \
            .SetCanOptimize(True) \
            .SetDisplay("Maximum AF", "Maximum acceleration factor for Parabolic SAR", "SAR Parameters")

        # Candle type for strategy calculation.
        self._candle_type = self.Param("CandleType", tf(5)) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")

        self._parabolic_sar = None
        self._is_first_candle = True

    @property
    def StartAf(self):
        return self._start_af.Value

    @StartAf.setter
    def StartAf(self, value):
        self._start_af.Value = value

    @property
    def MaxAf(self):
        return self._max_af.Value

    @MaxAf.setter
    def MaxAf(self, value):
        self._max_af.Value = value

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    def GetWorkingSecurities(self):
        return [(self.Security, self.CandleType)]

    def OnReseted(self):
        super(parabolic_sar_sentiment_divergence_strategy, self).OnReseted()
        self._parabolic_sar = None
        self._prev_sentiment = 0
        self._prev_price = 0
        self._is_first_candle = True

    def OnStarted(self, time):
        super(parabolic_sar_sentiment_divergence_strategy, self).OnStarted(time)

        # Create indicator
        self._parabolic_sar = ParabolicSar()
        self._parabolic_sar.Acceleration = self.StartAf
        self._parabolic_sar.AccelerationMax = self.MaxAf


        # Create subscription
        subscription = self.SubscribeCandles(self.CandleType)

        # Bind indicator and processor
        subscription.BindEx(self._parabolic_sar, self.ProcessCandle).Start()

        # Setup visualization
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, self._parabolic_sar)
            self.DrawOwnTrades(area)

        # Start position protection
        self.StartProtection(
            takeProfit=Unit(2, UnitTypes.Percent),
            stopLoss=Unit(2, UnitTypes.Percent),
            isStopTrailing=True
        )
    def ProcessCandle(self, candle, sar_value):
        # Skip unfinished candles
        if candle.State != CandleStates.Finished:
            return

        # Get SAR value
        sar_price = float(sar_value)

        # Get current price and sentiment
        price = float(candle.ClosePrice)
        sentiment = self.GetSentiment()  # In real implementation, this would come from external API

        # Skip first candle to initialize previous values
        if self._is_first_candle:
            self._prev_price = price
            self._prev_sentiment = sentiment
            self._is_first_candle = False
            return

        # Check if strategy is ready to trade
        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        # Bullish divergence: Price falling but sentiment rising
        bullish_divergence = price < self._prev_price and sentiment > self._prev_sentiment

        # Bearish divergence: Price rising but sentiment falling
        bearish_divergence = price > self._prev_price and sentiment < self._prev_sentiment

        # Entry logic
        if price > sar_price and bullish_divergence and self.Position <= 0:
            # Bullish divergence and price above SAR - Long entry
            self.BuyMarket(self.Volume)
            self.LogInfo(f"Buy Signal: SAR={sar_price}, Price={price}, Sentiment={sentiment}")
        elif price < sar_price and bearish_divergence and self.Position >= 0:
            # Bearish divergence and price below SAR - Short entry
            self.SellMarket(self.Volume)
            self.LogInfo(f"Sell Signal: SAR={sar_price}, Price={price}, Sentiment={sentiment}")

        # Exit logic - handled by Parabolic SAR itself
        if self.Position > 0 and price < sar_price:
            # Long position and price below SAR - Exit
            self.SellMarket(abs(self.Position))
            self.LogInfo(f"Exit Long: SAR={sar_price}, Price={price}")
        elif self.Position < 0 and price > sar_price:
            # Short position and price above SAR - Exit
            self.BuyMarket(abs(self.Position))
            self.LogInfo(f"Exit Short: SAR={sar_price}, Price={price}")

        # Update previous values
        self._prev_price = price
        self._prev_sentiment = sentiment

    def GetSentiment(self):
        # This is a placeholder for a real sentiment analysis
        # In a real implementation, this would connect to a sentiment data provider
        # Returning a random value between -1 and 1 for simulation
        return RandomGen.GetDouble() * 2 - 1

    def CreateClone(self):
        """!! REQUIRED!! Creates a new instance of the strategy."""
        return parabolic_sar_sentiment_divergence_strategy()
