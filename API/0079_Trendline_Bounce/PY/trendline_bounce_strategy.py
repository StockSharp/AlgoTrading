import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy

class trendline_bounce_strategy(Strategy):
    """
    Trendline Bounce strategy.
    Calculates linear regression of recent lows (support) and highs (resistance).
    Buys on bounce off support trendline, sells on bounce off resistance.
    Uses SMA for exit signals.
    """

    def __init__(self):
        super(trendline_bounce_strategy, self).__init__()
        self._trendline_period = self.Param("TrendlinePeriod", 20).SetDisplay("Trendline Period", "Lookback for trendline", "Indicators")
        self._ma_period = self.Param("MAPeriod", 20).SetDisplay("MA Period", "Period for SMA", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(1))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._cooldown_bars = self.Param("CooldownBars", 500).SetDisplay("Cooldown Bars", "Bars to wait between trades", "General")

        self._highs = []
        self._lows = []
        self._cooldown = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(trendline_bounce_strategy, self).OnReseted()
        self._highs = []
        self._lows = []
        self._cooldown = 0

    def OnStarted(self, time):
        super(trendline_bounce_strategy, self).OnStarted(time)

        self._highs = []
        self._lows = []
        self._cooldown = 0

        sma = SimpleMovingAverage()
        sma.Length = self._ma_period.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(sma, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, sma)
            self.DrawOwnTrades(area)

    def _get_lin_reg_value(self, values):
        n = len(values)
        if n == 0:
            return 0.0

        sum_x = 0.0
        sum_y = 0.0
        sum_xy = 0.0
        sum_x2 = 0.0

        for i in range(n):
            sum_x += i
            sum_y += values[i]
            sum_xy += i * values[i]
            sum_x2 += i * i

        denom = n * sum_x2 - sum_x * sum_x
        if denom == 0:
            return sum_y / n

        slope = (n * sum_xy - sum_x * sum_y) / denom
        intercept = (sum_y - slope * sum_x) / n

        return slope * (n - 1) + intercept

    def _process_candle(self, candle, sma_val):
        if candle.State != CandleStates.Finished:
            return

        self._highs.append(float(candle.HighPrice))
        self._lows.append(float(candle.LowPrice))

        tp = self._trendline_period.Value

        if len(self._highs) > tp:
            self._highs.pop(0)
            self._lows.pop(0)

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        if len(self._highs) < tp:
            return

        if self._cooldown > 0:
            self._cooldown -= 1
            return

        # Calculate linear regression for support (lows) and resistance (highs)
        support_level = self._get_lin_reg_value(self._lows)
        resistance_level = self._get_lin_reg_value(self._highs)
        buffer = (resistance_level - support_level) * 0.05

        if buffer <= 0:
            return

        is_bullish = candle.ClosePrice > candle.OpenPrice
        is_bearish = candle.ClosePrice < candle.OpenPrice

        close = float(candle.ClosePrice)
        sv = float(sma_val)
        cd = self._cooldown_bars.Value

        # Bounce off support (buy)
        if self.Position == 0 and float(candle.LowPrice) <= support_level + buffer and is_bullish:
            self.BuyMarket()
            self._cooldown = cd
        # Bounce off resistance (sell)
        elif self.Position == 0 and float(candle.HighPrice) >= resistance_level - buffer and is_bearish:
            self.SellMarket()
            self._cooldown = cd
        # Exit using SMA
        elif self.Position > 0 and close < sv:
            self.SellMarket()
            self._cooldown = cd
        elif self.Position < 0 and close > sv:
            self.BuyMarket()
            self._cooldown = cd

    def CreateClone(self):
        return trendline_bounce_strategy()
