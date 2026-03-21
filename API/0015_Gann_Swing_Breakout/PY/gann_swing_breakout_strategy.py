import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy

class gann_swing_breakout_strategy(Strategy):
    """
    Gann Swing Breakout: Donchian channel breakout with SMA trend filter.
    Buys when price breaks above previous channel high and is above SMA.
    Sells when price breaks below previous channel low and is below SMA.
    """

    def __init__(self):
        super(gann_swing_breakout_strategy, self).__init__()
        self._swing_lookback = self.Param("SwingLookback", 40) \
            .SetDisplay("Swing Lookback", "Lookback period for swing high/low", "Trading parameters")
        self._ma_period = self.Param("MaPeriod", 60) \
            .SetDisplay("MA Period", "Period for trend filter MA", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")

        self._prev_channel_high = 0.0
        self._prev_channel_low = 0.0
        self._has_prev_values = False
        self._candles_since_last_trade = 0
        self._highs = []
        self._lows = []

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(gann_swing_breakout_strategy, self).OnReseted()
        self._prev_channel_high = 0.0
        self._prev_channel_low = 0.0
        self._has_prev_values = False
        self._candles_since_last_trade = 0
        self._highs = []
        self._lows = []

    def OnStarted(self, time):
        super(gann_swing_breakout_strategy, self).OnStarted(time)

        ma = SimpleMovingAverage()
        ma.Length = self._ma_period.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(ma, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, ma)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, ma_value):
        if candle.State != CandleStates.Finished:
            return

        ma = float(ma_value)
        if ma == 0:
            return

        high = float(candle.HighPrice)
        low = float(candle.LowPrice)
        close = float(candle.ClosePrice)

        lookback = self._swing_lookback.Value
        self._highs.append(high)
        self._lows.append(low)
        while len(self._highs) > lookback:
            self._highs.pop(0)
        while len(self._lows) > lookback:
            self._lows.pop(0)

        if len(self._highs) < lookback:
            return

        channel_high = max(self._highs)
        channel_low = min(self._lows)

        if channel_high == 0 or channel_low == 0:
            return

        if not self._has_prev_values:
            self._has_prev_values = True
            self._prev_channel_high = channel_high
            self._prev_channel_low = channel_low
            return

        self._candles_since_last_trade += 1

        if self._candles_since_last_trade >= 10 and close > self._prev_channel_high and close > ma and self.Position <= 0:
            self.BuyMarket(self.Volume + abs(self.Position))
            self._candles_since_last_trade = 0
        elif self._candles_since_last_trade >= 10 and close < self._prev_channel_low and close < ma and self.Position >= 0:
            self.SellMarket(self.Volume + abs(self.Position))
            self._candles_since_last_trade = 0

        self._prev_channel_high = channel_high
        self._prev_channel_low = channel_low

    def CreateClone(self):
        return gann_swing_breakout_strategy()
