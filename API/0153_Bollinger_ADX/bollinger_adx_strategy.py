import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, UnitTypes, Unit, CandleStates
from StockSharp.Algo.Indicators import BollingerBands, ADX, ATR
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *

class bollinger_adx_strategy(Strategy):
    """
    Strategy combining Bollinger Bands and ADX indicators.
    Looks for breakouts with strong trend confirmation.
    """

    def __init__(self):
        super(bollinger_adx_strategy, self).__init__()

        # Initialize strategy.

        # Initialize strategy parameters
        self._bollinger_period = self.Param("BollingerPeriod", 20) \
            .SetGreaterThanZero() \
            .SetDisplay("Bollinger Period", "Period for Bollinger Bands calculation", "Indicators") \
            .SetCanOptimize(True) \
            .SetOptimize(10, 30, 5)

        self._bollinger_deviation = self.Param("BollingerDeviation", 2.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Bollinger Deviation", "Standard deviation multiplier for Bollinger Bands", "Indicators") \
            .SetCanOptimize(True) \
            .SetOptimize(1.5, 2.5, 0.5)

        self._adx_period = self.Param("AdxPeriod", 14) \
            .SetGreaterThanZero() \
            .SetDisplay("ADX Period", "Period for ADX calculation", "Indicators") \
            .SetCanOptimize(True) \
            .SetOptimize(7, 21, 7)

        self._adx_threshold = self.Param("AdxThreshold", 25.0) \
            .SetGreaterThanZero() \
            .SetDisplay("ADX Threshold", "ADX level considered as strong trend", "Trading Levels") \
            .SetCanOptimize(True) \
            .SetOptimize(20, 30, 5)

        self._atr_multiplier = self.Param("AtrMultiplier", 2.0) \
            .SetGreaterThanZero() \
            .SetDisplay("ATR Multiplier", "Multiplier for ATR to set stop-loss", "Risk Management") \
            .SetCanOptimize(True) \
            .SetOptimize(1.0, 3.0, 0.5)

        self._candle_type = self.Param("CandleType", tf(5)) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")

    @property
    def bollinger_period(self):
        """Bollinger Bands period."""
        return self._bollinger_period.Value

    @bollinger_period.setter
    def bollinger_period(self, value):
        self._bollinger_period.Value = value

    @property
    def bollinger_deviation(self):
        """Bollinger Bands standard deviation multiplier."""
        return self._bollinger_deviation.Value

    @bollinger_deviation.setter
    def bollinger_deviation(self, value):
        self._bollinger_deviation.Value = value

    @property
    def adx_period(self):
        """ADX indicator period."""
        return self._adx_period.Value

    @adx_period.setter
    def adx_period(self, value):
        self._adx_period.Value = value

    @property
    def adx_threshold(self):
        """ADX threshold for strong trend."""
        return self._adx_threshold.Value

    @adx_threshold.setter
    def adx_threshold(self, value):
        self._adx_threshold.Value = value

    @property
    def atr_multiplier(self):
        """ATR multiplier for stop-loss."""
        return self._atr_multiplier.Value

    @atr_multiplier.setter
    def atr_multiplier(self, value):
        self._atr_multiplier.Value = value

    @property
    def candle_type(self):
        """Candle type for strategy calculation."""
        return self._candle_type.Value

    @candle_type.setter
    def candle_type(self, value):
        self._candle_type.Value = value

    # Initialize strategy.

    def OnStarted(self, time):
        super(bollinger_adx_strategy, self).OnStarted(time)

        # Create indicators
        bollinger_bands = BollingerBands()
        bollinger_bands.Length = self.bollinger_period
        bollinger_bands.Width = self.bollinger_deviation

        adx = ADX()
        adx.Length = self.adx_period
        atr = ATR()
        atr.Length = self.adx_period

        # Create subscription
        subscription = self.SubscribeCandles(self.candle_type)

        # Bind indicators to candles
        subscription.BindEx(bollinger_bands, adx, atr, self.ProcessCandle).Start()

        # Enable stop-loss using ATR
        self.StartProtection(
            takeProfit=None,
            stopLoss=Unit(self.atr_multiplier, UnitTypes.Absolute),
            isStopTrailing=False,
            useMarketOrders=True
        )
        # Setup chart if available
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, bollinger_bands)

            # Create second area for ADX
            adx_area = self.CreateChartArea()
            self.DrawIndicator(adx_area, adx)

            self.DrawOwnTrades(area)

    def ProcessCandle(self, candle, bollinger_value, adx_value, atr_value):
        # Skip unfinished candles
        if candle.State != CandleStates.Finished:
            return

        # Check if strategy is ready to trade
        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        # Trading logic - only trade when ADX indicates strong trend
        try:
            bollinger_up = float(bollinger_value.UpBand)
            bollinger_down = float(bollinger_value.LowBand)
            middle_band = float(bollinger_value.MovingAverage)
        except Exception:
            return

        if adx_value.MovingAverage is None:
            return
        adx_ma = float(adx_value.MovingAverage)

        if adx_ma > self.adx_threshold:
            # Strong trend detected
            if candle.ClosePrice > bollinger_up and self.Position <= 0:
                # Price breaks above upper Bollinger band - Buy
                volume = self.Volume + Math.Abs(self.Position)
                self.BuyMarket(volume)
            elif candle.ClosePrice < bollinger_down and self.Position >= 0:
                # Price breaks below lower Bollinger band - Sell
                volume = self.Volume + Math.Abs(self.Position)
                self.SellMarket(volume)

        # Exit positions when price returns to middle band
        if (self.Position > 0 and candle.ClosePrice < middle_band) or \
                (self.Position < 0 and candle.ClosePrice > middle_band):
            self.ClosePosition()

    def CreateClone(self):
        """!! REQUIRED!! Creates a new instance of the strategy."""
        return bollinger_adx_strategy()
