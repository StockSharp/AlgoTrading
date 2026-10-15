import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Decimal
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy


class g_channel_ema_strategy(Strategy):
    """
    G-Channel with EMA strategy.
    The G-Channel upper band is max(close, upper) minus (upper - lower) / ChannelLength and the lower band min(close, lower) plus the
    same amount. As in the original G-Channel, an upward cross is the close moving from above the lower band to below it and a
    downward cross the close moving from above the upper band to below it. With the last cross downward and the close below the EMA
    the strategy goes long; with the last cross upward and the close above the EMA it goes short. An opposite signal reverses the position.
    """

    def __init__(self):
        super(g_channel_ema_strategy, self).__init__()
        self._channel_length = self.Param("ChannelLength", 100).SetGreaterThanZero().SetDisplay("Channel Length", "G-Channel length", "Indicators")
        self._ema_length = self.Param("EmaLength", 200).SetGreaterThanZero().SetDisplay("EMA Length", "EMA length", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._reset_state()

    @property
    def candle_type(self):
        return self._candle_type.Value

    def _reset_state(self):
        self._upper = None
        self._lower = None
        self._prev_close = None
        self._last_cross = 0

    def OnReseted(self):
        super(g_channel_ema_strategy, self).OnReseted()
        self._reset_state()

    def OnStarted2(self, time):
        super(g_channel_ema_strategy, self).OnStarted2(time)

        self._reset_state()

        ema = ExponentialMovingAverage()
        ema.Length = self._ema_length.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(ema, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, ema)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, ema):
        if candle.State != CandleStates.Finished:
            return

        close = candle.ClosePrice

        if self._upper is None or self._lower is None or self._prev_close is None:
            self._upper = close
            self._lower = close
            self._prev_close = close
            return

        prev_upper = self._upper
        prev_lower = self._lower
        prev_close = self._prev_close

        step = (prev_upper - prev_lower) / Decimal(self._channel_length.Value)
        upper = max(close, prev_upper) - step
        lower = min(close, prev_lower) + step

        if prev_lower < prev_close and lower > close:
            self._last_cross = 1
        elif prev_upper < prev_close and upper > close:
            self._last_cross = -1

        self._upper = upper
        self._lower = lower
        self._prev_close = close

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        if self._last_cross < 0 and close < ema and self.Position <= 0:
            self.BuyMarket(self.Volume + abs(self.Position))
        elif self._last_cross > 0 and close > ema and self.Position >= 0:
            self.SellMarket(self.Volume + abs(self.Position))

    def CreateClone(self):
        return g_channel_ema_strategy()
