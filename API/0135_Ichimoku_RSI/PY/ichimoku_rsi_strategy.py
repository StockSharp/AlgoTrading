import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import RelativeStrengthIndex
from StockSharp.Algo.Strategies import Strategy

class ichimoku_rsi_strategy(Strategy):
    """
    Ichimoku RSI strategy.
    Combines manual Tenkan/Kijun calculation with RSI confirmation.
    Enters on Tenkan/Kijun crossover with RSI filter.
    """

    TENKAN_PERIOD = 9
    KIJUN_PERIOD = 26

    def __init__(self):
        super(ichimoku_rsi_strategy, self).__init__()
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._rsi_period = self.Param("RsiPeriod", 14).SetDisplay("RSI Period", "Period for RSI calculation", "RSI Settings")
        self._rsi_oversold = self.Param("RsiOversold", 30.0).SetDisplay("RSI Oversold", "RSI oversold level", "RSI Settings")
        self._rsi_overbought = self.Param("RsiOverbought", 70.0).SetDisplay("RSI Overbought", "RSI overbought level", "RSI Settings")
        self._cooldown_bars = self.Param("CooldownBars", 100).SetDisplay("Cooldown Bars", "Bars between trades", "General")

        self._rsi_value = 50.0
        self._cooldown = 0
        self._highs = []
        self._lows = []

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(ichimoku_rsi_strategy, self).OnReseted()
        self._rsi_value = 50.0
        self._cooldown = 0
        self._highs = []
        self._lows = []

    def OnStarted2(self, time):
        super(ichimoku_rsi_strategy, self).OnStarted2(time)

        self._rsi_value = 50.0
        self._cooldown = 0
        self._highs = []
        self._lows = []

        rsi = RelativeStrengthIndex()
        rsi.Length = self._rsi_period.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(rsi, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)
            rsi_area = self.CreateChartArea()
            if rsi_area is not None:
                self.DrawIndicator(rsi_area, rsi)

    @staticmethod
    def _get_highest(values, period):
        start = max(0, len(values) - period)
        return max(values[start:])

    @staticmethod
    def _get_lowest(values, period):
        start = max(0, len(values) - period)
        return min(values[start:])

    def _process_candle(self, candle, rsi_val):
        self._rsi_value = float(rsi_val)

        if candle.State != CandleStates.Finished:
            return

        # Track highs and lows
        self._highs.append(float(candle.HighPrice))
        self._lows.append(float(candle.LowPrice))

        # Keep buffer manageable
        max_len = self.KIJUN_PERIOD * 2
        if len(self._highs) > max_len:
            self._highs = self._highs[-max_len:]
            self._lows = self._lows[-max_len:]

        # Need at least KijunPeriod bars
        if len(self._highs) < self.KIJUN_PERIOD:
            return

        # Tenkan-sen = (highest high over 9 + lowest low over 9) / 2
        tenkan = (self._get_highest(self._highs, self.TENKAN_PERIOD) + self._get_lowest(self._lows, self.TENKAN_PERIOD)) / 2.0
        # Kijun-sen = (highest high over 26 + lowest low over 26) / 2
        kijun = (self._get_highest(self._highs, self.KIJUN_PERIOD) + self._get_lowest(self._lows, self.KIJUN_PERIOD)) / 2.0

        cd = self._cooldown_bars.Value
        overbought = float(self._rsi_overbought.Value)
        oversold = float(self._rsi_oversold.Value)

        if self._cooldown > 0:
            self._cooldown -= 1
            return

        # Buy: tenkan > kijun (bullish) + RSI not overbought
        if tenkan > kijun and self._rsi_value < overbought and self.Position == 0:
            self.BuyMarket()
            self._cooldown = cd
        # Sell: tenkan < kijun (bearish) + RSI not oversold
        elif tenkan < kijun and self._rsi_value > oversold and self.Position == 0:
            self.SellMarket()
            self._cooldown = cd

        # Exit long if tenkan crosses below kijun
        if self.Position > 0 and tenkan < kijun:
            self.SellMarket()
            self._cooldown = cd
        # Exit short if tenkan crosses above kijun
        elif self.Position < 0 and tenkan > kijun:
            self.BuyMarket()
            self._cooldown = cd

    def CreateClone(self):
        return ichimoku_rsi_strategy()
