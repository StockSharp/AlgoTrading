import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import BollingerBands, SimpleMovingAverage, AverageTrueRange
from StockSharp.Algo.Strategies import Strategy
from indicator_extensions import *


class bollinger_band_width_breakout_strategy(Strategy):
    """
    Strategy that trades on Bollinger Band width breakouts.
    When Bollinger Band width increases significantly above its average,
    it enters position in the direction determined by price movement.
    """

    def __init__(self):
        super(bollinger_band_width_breakout_strategy, self).__init__()

        # Bollinger Bands period.
        self._bollinger_length = self.Param("BollingerLength", 20) \
            .SetDisplay("Bollinger Length", "Period of the Bollinger Bands indicator", "Indicators")

        # Bollinger Bands standard deviation multiplier.
        self._bollinger_deviation = self.Param("BollingerDeviation", 2.0) \
            .SetDisplay("Bollinger Deviation", "Standard deviation multiplier for Bollinger Bands", "Indicators")

        # Period for width average calculation.
        self._avg_period = self.Param("AvgPeriod", 20) \
            .SetDisplay("Average Period", "Period for Bollinger width average calculation", "Indicators")

        # Standard deviation multiplier for breakout detection.
        self._multiplier = self.Param("Multiplier", 2.0) \
            .SetDisplay("Multiplier", "Standard deviation multiplier for breakout detection", "Indicators")

        # Candle type for strategy.
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")

        # Stop-loss ATR multiplier.
        self._stop_multiplier = self.Param("StopMultiplier", 2) \
            .SetDisplay("Stop Multiplier", "ATR multiplier for stop-loss", "Risk Management")

        # Internal state
        self._bollinger = None
        self._width_average = None
        self._atr = None
        self._best_bid_price = 0.0
        self._best_ask_price = 0.0

    @property
    def BollingerLength(self):
        return self._bollinger_length.Value

    @BollingerLength.setter
    def BollingerLength(self, value):
        self._bollinger_length.Value = value

    @property
    def BollingerDeviation(self):
        return self._bollinger_deviation.Value

    @BollingerDeviation.setter
    def BollingerDeviation(self, value):
        self._bollinger_deviation.Value = value

    @property
    def AvgPeriod(self):
        return self._avg_period.Value

    @AvgPeriod.setter
    def AvgPeriod(self, value):
        self._avg_period.Value = value

    @property
    def Multiplier(self):
        return self._multiplier.Value

    @Multiplier.setter
    def Multiplier(self, value):
        self._multiplier.Value = value

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    @property
    def StopMultiplier(self):
        return self._stop_multiplier.Value

    @StopMultiplier.setter
    def StopMultiplier(self, value):
        self._stop_multiplier.Value = value

    def _update_best_prices(self, depth):
        best_bid = depth.GetBestBid()
        if best_bid is not None:
            self._best_bid_price = best_bid.Price
        best_ask = depth.GetBestAsk()
        if best_ask is not None:
            self._best_ask_price = best_ask.Price

    def OnStarted(self, time):
        super(bollinger_band_width_breakout_strategy, self).OnStarted(time)

        self._best_bid_price = 0.0
        self._best_ask_price = 0.0

        # Create indicators
        self._bollinger = BollingerBands()
        self._bollinger.Length = self.BollingerLength
        self._bollinger.Width = self.BollingerDeviation

        self._width_average = SimpleMovingAverage()
        self._width_average.Length = self.AvgPeriod
        self._atr = AverageTrueRange()
        self._atr.Length = self.BollingerLength

        # Create subscription
        subscription = self.SubscribeCandles(self.CandleType)

        # Bind Bollinger Bands
        subscription.BindEx(self._bollinger, self._atr, self.ProcessBollinger).Start()

        self.SubscribeOrderBook().Bind(self._update_best_prices).Start()

        # Create chart area for visualization
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, self._bollinger)
            self.DrawOwnTrades(area)

    def ProcessBollinger(self, candle, bollinger_value, atr_value):
        if candle.State != CandleStates.Finished:
            return

        # Process candle through ATR
        current_atr = to_float(atr_value)

        # Calculate Bollinger Band width
        bollinger_typed = bollinger_value
        if bollinger_typed.UpBand is None:
            return
        if bollinger_typed.LowBand is None:
            return
        upper_band = bollinger_typed.UpBand
        lower_band = bollinger_typed.LowBand

        last_width = upper_band - lower_band

        # Process width through average
        width_avg_value = self._width_average.Process(last_width, candle.ServerTime, candle.State == CandleStates.Finished)
        avg_width = to_float(width_avg_value)

        # Calculate width standard deviation (simplified approach)
        std_dev = Math.Abs(last_width - avg_width) * 1.5

        # Skip if indicators are not formed yet
        if not self._bollinger.IsFormed or not self._width_average.IsFormed or not self._atr.IsFormed:
            return

        # Check if trading is allowed
        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        # Bollinger width breakout detection
        if last_width > avg_width + self.Multiplier * std_dev:
            # Determine direction based on price and bands
            price_direction = False

            # If price is closer to upper band, go long. If closer to lower band, go short.
            upper_distance = Math.Abs(candle.ClosePrice - upper_band)
            lower_distance = Math.Abs(candle.ClosePrice - lower_band)

            if upper_distance < lower_distance:
                # Price is closer to upper band, likely bullish
                price_direction = True

            # Cancel active orders before placing new ones
            self.CancelActiveOrders()

            # Calculate stop-loss based on current ATR
            stop_offset = self.StopMultiplier * current_atr

            # Trade in the determined direction
            if price_direction and self.Position <= 0:
                # Bullish direction - Buy
                self.BuyMarket(self.Volume + Math.Abs(self.Position))
            elif not price_direction and self.Position >= 0:
                # Bearish direction - Sell
                self.SellMarket(self.Volume + Math.Abs(self.Position))
        elif (self.Position > 0 or self.Position < 0) and last_width < avg_width:
            # Exit position
            self.ClosePosition()

    def CreateClone(self):
        """!! REQUIRED!! Creates a new instance of the strategy."""
        return bollinger_band_width_breakout_strategy()
