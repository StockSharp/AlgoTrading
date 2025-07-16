import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import Ichimoku
from StockSharp.Algo.Strategies import Strategy

class ichimoku_hurst_exponent_strategy(Strategy):
    """
    Strategy based on Ichimoku Kinko Hyo indicator with Hurst exponent filter.
    """

    def __init__(self):
        """Initializes a new instance of the strategy."""
        super(ichimoku_hurst_exponent_strategy, self).__init__()

        # Tenkan-sen (conversion line) period.
        self._tenkan_period = self.Param("TenkanPeriod", 9) \
            .SetDisplay("Tenkan Period", "Tenkan-sen (conversion line) period", "Ichimoku") \
            .SetCanOptimize(True) \
            .SetOptimize(5, 15, 1)

        # Kijun-sen (base line) period.
        self._kijun_period = self.Param("KijunPeriod", 26) \
            .SetDisplay("Kijun Period", "Kijun-sen (base line) period", "Ichimoku") \
            .SetCanOptimize(True) \
            .SetOptimize(20, 40, 2)

        # Senkou Span B (leading span B) period.
        self._senkou_spanb_period = self.Param("SenkouSpanBPeriod", 52) \
            .SetDisplay("Senkou Span B Period", "Senkou Span B (leading span B) period", "Ichimoku") \
            .SetCanOptimize(True) \
            .SetOptimize(40, 70, 5)

        # Hurst exponent calculation period.
        self._hurst_period = self.Param("HurstPeriod", 100) \
            .SetDisplay("Hurst Period", "Hurst exponent calculation period", "Hurst Exponent") \
            .SetCanOptimize(True) \
            .SetOptimize(50, 200, 10)

        # Hurst exponent threshold for trend strength.
        self._hurst_threshold = self.Param("HurstThreshold", 0.5) \
            .SetDisplay("Hurst Threshold", "Hurst exponent threshold for trend strength", "Hurst Exponent") \
            .SetCanOptimize(True) \
            .SetOptimize(0.45, 0.6, 0.05)

        # Candle type to use for the strategy.
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(15))) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")

        # Data for Hurst exponent calculations
        self._prices = []
        self._hurst_exponent = 0.5
        self._ichimoku = None

    @property
    def TenkanPeriod(self):
        return self._tenkan_period.Value

    @TenkanPeriod.setter
    def TenkanPeriod(self, value):
        self._tenkan_period.Value = value

    @property
    def KijunPeriod(self):
        return self._kijun_period.Value

    @KijunPeriod.setter
    def KijunPeriod(self, value):
        self._kijun_period.Value = value

    @property
    def SenkouSpanBPeriod(self):
        return self._senkou_spanb_period.Value

    @SenkouSpanBPeriod.setter
    def SenkouSpanBPeriod(self, value):
        self._senkou_spanb_period.Value = value

    @property
    def HurstPeriod(self):
        return self._hurst_period.Value

    @HurstPeriod.setter
    def HurstPeriod(self, value):
        self._hurst_period.Value = value

    @property
    def HurstThreshold(self):
        return self._hurst_threshold.Value

    @HurstThreshold.setter
    def HurstThreshold(self, value):
        self._hurst_threshold.Value = value

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    def GetWorkingSecurities(self):
        """!! REQUIRED !! Return securities used by the strategy."""
        return [(self.Security, self.CandleType)]

    def OnStarted(self, time):
        super(ichimoku_hurst_exponent_strategy, self).OnStarted(time)

        self._prices.clear()
        self._hurst_exponent = 0.5  # Default Hurst exponent value

        # Create Ichimoku indicator
        self._ichimoku = Ichimoku()
        self._ichimoku.Tenkan.Length = self.TenkanPeriod
        self._ichimoku.Kijun.Length = self.KijunPeriod
        self._ichimoku.SenkouB.Length = self.SenkouSpanBPeriod

        # Create subscription and bind indicator
        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(self._ichimoku, self.ProcessCandle).Start()

        # Setup chart visualization if available
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, self._ichimoku)
            self.DrawOwnTrades(area)

    def ProcessCandle(self, candle, ichimoku_value):
        # Skip unfinished candles
        if candle.State != CandleStates.Finished:
            return

        # Store Ichimoku values
        ichimoku_typed = ichimoku_value
        try:
            tenkan = float(ichimoku_typed.Tenkan)
            kijun = float(ichimoku_typed.Kijun)
            senkou_a = float(ichimoku_typed.SenkouA)
            senkou_b = float(ichimoku_typed.SenkouB)
        except Exception:
            return

        # Update price data for Hurst exponent calculation
        self._prices.append(candle.ClosePrice)

        # Keep only the number of prices needed for Hurst calculation
        while len(self._prices) > self.HurstPeriod:
            self._prices.pop(0)

        # Calculate Hurst exponent when we have enough data
        if len(self._prices) >= self.HurstPeriod:
            self.CalculateHurstExponent()

        # Continue with position checks
        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        # Check if price is above/below Kumo (cloud)
        isPriceAboveKumo = candle.ClosePrice > max(senkou_a, senkou_b)
        isPriceBelowKumo = candle.ClosePrice < min(senkou_a, senkou_b)

        # Trading logic
        # Buy when price is above the cloud, Tenkan > Kijun, and Hurst > threshold (trending market)
        if isPriceAboveKumo and tenkan > kijun and self._hurst_exponent > self.HurstThreshold and self.Position <= 0:
            self.BuyMarket(self.Volume)
            self.LogInfo("Buy Signal: Price {0:F2} above Kumo, Tenkan {1:F2} > Kijun {2:F2}, Hurst {3:F3}".format(
                candle.ClosePrice, tenkan, kijun, self._hurst_exponent))
        # Sell when price is below the cloud, Tenkan < Kijun, and Hurst > threshold (trending market)
        elif isPriceBelowKumo and tenkan < kijun and self._hurst_exponent > self.HurstThreshold and self.Position >= 0:
            self.SellMarket(self.Volume + Math.Abs(self.Position))
            self.LogInfo("Sell Signal: Price {0:F2} below Kumo, Tenkan {1:F2} < Kijun {2:F2}, Hurst {3:F3}".format(
                candle.ClosePrice, tenkan, kijun, self._hurst_exponent))
        # Exit long position when price falls below the cloud
        elif self.Position > 0 and isPriceBelowKumo:
            self.SellMarket(self.Position)
            self.LogInfo("Exit Long: Price {0:F2} fell below Kumo".format(candle.ClosePrice))
        # Exit short position when price rises above the cloud
        elif self.Position < 0 and isPriceAboveKumo:
            self.BuyMarket(Math.Abs(self.Position))
            self.LogInfo("Exit Short: Price {0:F2} rose above Kumo".format(candle.ClosePrice))

    def CalculateHurstExponent(self):
        # This is a simplified Hurst exponent calculation using R/S analysis
        # Note: A full implementation would use multiple time scales

        # Calculate log returns
        log_returns = []
        for i in range(1, len(self._prices)):
            if self._prices[i-1] != 0:
                log_returns.append(Math.Log(float(self._prices[i] / self._prices[i-1])))

        if len(log_returns) < 10:
            return

        # Calculate mean
        mean = sum(log_returns) / len(log_returns)

        # Calculate cumulative deviation series
        cumulative_deviation = []
        sum_val = 0.0
        for log_return in log_returns:
            sum_val += (log_return - mean)
            cumulative_deviation.append(sum_val)

        # Calculate range (max - min of cumulative deviation)
        range_val = max(cumulative_deviation) - min(cumulative_deviation)

        # Calculate standard deviation
        sum_squares = sum((x - mean) * (x - mean) for x in log_returns)
        std_dev = Math.Sqrt(sum_squares / len(log_returns))

        if std_dev == 0:
            return

        # Calculate R/S statistic
        rs = range_val / std_dev

        # Hurst = log(R/S) / log(N)
        logN = Math.Log(float(len(log_returns)))
        if logN != 0:
            self._hurst_exponent = Math.Log(rs) / logN

        self.LogInfo("Calculated Hurst Exponent: {0:F3} (R/S: {1:F3}, N: {2})".format(
            self._hurst_exponent, rs, len(log_returns)))

    def CreateClone(self):
        """!! REQUIRED!! Creates a new instance of the strategy."""
        return ichimoku_hurst_exponent_strategy()
