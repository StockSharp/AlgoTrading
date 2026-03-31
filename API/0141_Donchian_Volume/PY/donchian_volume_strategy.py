import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy

class donchian_volume_strategy(Strategy):
    """
    Donchian Volume strategy.
    Uses manual Donchian Channels for breakout detection.
    Enters when price breaks above/below the channel.
    """

    def __init__(self):
        super(donchian_volume_strategy, self).__init__()
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._donchian_period = self.Param("DonchianPeriod", 20).SetDisplay("Donchian Period", "Period of the Donchian Channel", "Indicators")
        self._cooldown_bars = self.Param("CooldownBars", 100).SetDisplay("Cooldown Bars", "Bars between trades", "General")

        self._cooldown = 0
        self._highs = []
        self._lows = []

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(donchian_volume_strategy, self).OnReseted()
        self._cooldown = 0
        self._highs = []
        self._lows = []

    def OnStarted2(self, time):
        super(donchian_volume_strategy, self).OnStarted2(time)

        self._cooldown = 0
        self._highs = []
        self._lows = []

        sma = SimpleMovingAverage()
        sma.Length = self._donchian_period.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(sma, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, sma)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, sma_val):
        if candle.State != CandleStates.Finished:
            return

        high = float(candle.HighPrice)
        low = float(candle.LowPrice)
        close = float(candle.ClosePrice)
        cd = self._cooldown_bars.Value
        period = self._donchian_period.Value

        self._highs.append(high)
        self._lows.append(low)

        max_buf = period * 2
        if len(self._highs) > max_buf:
            self._highs = self._highs[-max_buf:]
            self._lows = self._lows[-max_buf:]

        if len(self._highs) < period:
            return

        # Calculate Donchian Channel
        recent_h = self._highs[-period:]
        recent_l = self._lows[-period:]
        highest_high = max(recent_h)
        lowest_low = min(recent_l)
        middle_line = (highest_high + lowest_low) / 2.0

        if self._cooldown > 0:
            self._cooldown -= 1
            return

        # Long entry: price breaks above channel
        if close >= highest_high and self.Position == 0:
            self.BuyMarket()
            self._cooldown = cd
        # Short entry: price breaks below channel
        elif close <= lowest_low and self.Position == 0:
            self.SellMarket()
            self._cooldown = cd

        # Exit long: price crosses below middle line
        if self.Position > 0 and close < middle_line:
            self.SellMarket()
            self._cooldown = cd
        # Exit short: price crosses above middle line
        elif self.Position < 0 and close > middle_line:
            self.BuyMarket()
            self._cooldown = cd

    def CreateClone(self):
        return donchian_volume_strategy()
