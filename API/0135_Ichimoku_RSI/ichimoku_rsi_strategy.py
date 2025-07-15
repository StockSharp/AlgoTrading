import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import Ichimoku, RelativeStrengthIndex
from StockSharp.Algo.Strategies import Strategy

class ichimoku_rsi_strategy(Strategy):
    """
    Strategy that combines Ichimoku Cloud and RSI indicators to identify
    potential trading opportunities in trending markets with RSI confirmation.
    """

    def __init__(self):
        super(ichimoku_rsi_strategy, self).__init__()

        # Data type for candles.
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(30))) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")

        # Tenkan-sen (Conversion Line) period.
        self._tenkan_period = self.Param("TenkanPeriod", 9) \
            .SetRange(5, 30) \
            .SetDisplay("Tenkan Period", "Tenkan-sen (Conversion Line) period", "Ichimoku Settings") \
            .SetCanOptimize(True)

        # Kijun-sen (Base Line) period.
        self._kijun_period = self.Param("KijunPeriod", 26) \
            .SetRange(10, 50) \
            .SetDisplay("Kijun Period", "Kijun-sen (Base Line) period", "Ichimoku Settings") \
            .SetCanOptimize(True)

        # Senkou Span B (2nd Leading Span) period.
        self._senkou_span_b_period = self.Param("SenkouSpanBPeriod", 52) \
            .SetRange(30, 100) \
            .SetDisplay("Senkou Span B Period", "Senkou Span B (2nd Leading Span) period", "Ichimoku Settings") \
            .SetCanOptimize(True)

        # Period for RSI calculation.
        self._rsi_period = self.Param("RsiPeriod", 14) \
            .SetRange(5, 30) \
            .SetDisplay("RSI Period", "Period for RSI calculation", "RSI Settings") \
            .SetCanOptimize(True)

        # RSI oversold level.
        self._rsi_oversold = self.Param("RsiOversold", 30) \
            .SetRange(10, 40) \
            .SetDisplay("RSI Oversold", "RSI oversold level", "RSI Settings") \
            .SetCanOptimize(True)

        # RSI overbought level.
        self._rsi_overbought = self.Param("RsiOverbought", 70) \
            .SetRange(60, 90) \
            .SetDisplay("RSI Overbought", "RSI overbought level", "RSI Settings") \
            .SetCanOptimize(True)

        # Stop loss percentage from entry price.
        self._stop_loss_percent = self.Param("StopLossPercent", 2.0) \
            .SetRange(0.5, 5.0) \
            .SetDisplay("Stop Loss %", "Stop loss percentage from entry price", "Risk Management")

    @property
    def CandleType(self):
        """Data type for candles."""
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    @property
    def TenkanPeriod(self):
        """Tenkan-sen (Conversion Line) period."""
        return self._tenkan_period.Value

    @TenkanPeriod.setter
    def TenkanPeriod(self, value):
        self._tenkan_period.Value = value

    @property
    def KijunPeriod(self):
        """Kijun-sen (Base Line) period."""
        return self._kijun_period.Value

    @KijunPeriod.setter
    def KijunPeriod(self, value):
        self._kijun_period.Value = value

    @property
    def SenkouSpanBPeriod(self):
        """Senkou Span B (2nd Leading Span) period."""
        return self._senkou_span_b_period.Value

    @SenkouSpanBPeriod.setter
    def SenkouSpanBPeriod(self, value):
        self._senkou_span_b_period.Value = value

    @property
    def RsiPeriod(self):
        """Period for RSI calculation."""
        return self._rsi_period.Value

    @RsiPeriod.setter
    def RsiPeriod(self, value):
        self._rsi_period.Value = value

    @property
    def RsiOversold(self):
        """RSI oversold level."""
        return self._rsi_oversold.Value

    @RsiOversold.setter
    def RsiOversold(self, value):
        self._rsi_oversold.Value = value

    @property
    def RsiOverbought(self):
        """RSI overbought level."""
        return self._rsi_overbought.Value

    @RsiOverbought.setter
    def RsiOverbought(self, value):
        self._rsi_overbought.Value = value

    @property
    def StopLossPercent(self):
        """Stop loss percentage from entry price."""
        return self._stop_loss_percent.Value

    @StopLossPercent.setter
    def StopLossPercent(self, value):
        self._stop_loss_percent.Value = value

    def GetWorkingSecurities(self):
        return [(self.Security, self.CandleType)]

    def OnStarted(self, time):
        super(ichimoku_rsi_strategy, self).OnStarted(time)

        # Set up stop loss protection
        self.StartProtection(
            Unit(0),  # No take profit
            Unit(self.StopLossPercent, UnitTypes.Percent)  # Stop loss based on parameter
        )

        # Create indicators
        ichimoku = Ichimoku()
        ichimoku.Tenkan.Length = self.TenkanPeriod
        ichimoku.Kijun.Length = self.KijunPeriod
        ichimoku.SenkouB.Length = self.SenkouSpanBPeriod

        rsi = RelativeStrengthIndex()
        rsi.Length = self.RsiPeriod

        # Create candle subscription
        subscription = self.SubscribeCandles(self.CandleType)

        # Bind the indicators and candle processor
        subscription.BindEx(ichimoku, rsi, self.ProcessCandle).Start()

        # Set up chart if available
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, ichimoku)

            # Draw RSI in a separate area
            rsi_area = self.CreateChartArea()
            self.DrawIndicator(rsi_area, rsi)

            self.DrawOwnTrades(area)

    def ProcessCandle(self, candle, ichimoku_value, rsi_value):
        """
        Process incoming candle with indicator values.

        :param candle: Candle to process.
        :param ichimoku_value: Ichimoku value.
        :param rsi_value: RSI value.
        """
        if candle.State != CandleStates.Finished:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        # Extract values from Ichimoku indicator
        try:
            tenkan = float(ichimoku_value.Tenkan)
            kijun = float(ichimoku_value.Kijun)
            senkou_span_a = float(ichimoku_value.SenkouA)
            senkou_span_b = float(ichimoku_value.SenkouB)
        except Exception:
            return

        # Extract RSI value
        rsi_indicator_value = float(rsi_value)

        # Check cloud status (Kumo)
        price_above_cloud = candle.ClosePrice > Math.Max(senkou_span_a, senkou_span_b)
        price_below_cloud = candle.ClosePrice < Math.Min(senkou_span_a, senkou_span_b)
        bullish_cloud = senkou_span_a > senkou_span_b

        # Trading logic for long positions
        if price_above_cloud and tenkan > kijun and bullish_cloud and rsi_indicator_value < self.RsiOverbought:
            # Price above cloud with bullish TK cross and bullish cloud, and RSI not overbought - Long signal
            if self.Position <= 0:
                self.BuyMarket(self.Volume + Math.Abs(self.Position))
                self.LogInfo("Buy signal: Price above cloud, Tenkan > Kijun ({0:F4} > {1:F4}), Bullish cloud, RSI = {2:F2}".format(
                    tenkan, kijun, rsi_indicator_value))
        # Trading logic for short positions
        elif price_below_cloud and tenkan < kijun and not bullish_cloud and rsi_indicator_value > self.RsiOversold:
            # Price below cloud with bearish TK cross and bearish cloud, and RSI not oversold - Short signal
            if self.Position >= 0:
                self.SellMarket(self.Volume + Math.Abs(self.Position))
                self.LogInfo("Sell signal: Price below cloud, Tenkan < Kijun ({0:F4} < {1:F4}), Bearish cloud, RSI = {2:F2}".format(
                    tenkan, kijun, rsi_indicator_value))

        # Exit conditions
        if self.Position > 0:
            # Exit long if price crosses below Kijun-sen (Base Line)
            if candle.ClosePrice < kijun:
                self.SellMarket(Math.Abs(self.Position))
                self.LogInfo("Exit long: Price ({0}) crossed below Kijun-sen ({1:F4})".format(candle.ClosePrice, kijun))
            # Also exit if RSI becomes overbought
            elif rsi_indicator_value > self.RsiOverbought:
                self.SellMarket(Math.Abs(self.Position))
                self.LogInfo("Exit long: RSI overbought ({0:F2})".format(rsi_indicator_value))
        elif self.Position < 0:
            # Exit short if price crosses above Kijun-sen (Base Line)
            if candle.ClosePrice > kijun:
                self.BuyMarket(Math.Abs(self.Position))
                self.LogInfo("Exit short: Price ({0}) crossed above Kijun-sen ({1:F4})".format(candle.ClosePrice, kijun))
            # Also exit if RSI becomes oversold
            elif rsi_indicator_value < self.RsiOversold:
                self.BuyMarket(Math.Abs(self.Position))
                self.LogInfo("Exit short: RSI oversold ({0:F2})".format(rsi_indicator_value))

    def CreateClone(self):
        """!! REQUIRED!! Creates a new instance of the strategy."""
        return ichimoku_rsi_strategy()
