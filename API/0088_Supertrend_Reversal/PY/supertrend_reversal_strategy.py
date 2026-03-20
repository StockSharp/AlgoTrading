import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import SuperTrend
from StockSharp.Algo.Strategies import Strategy

class supertrend_reversal_strategy(Strategy):
    """
    Supertrend Reversal strategy.
    Enters long when SuperTrend flips to uptrend (below price).
    Enters short when SuperTrend flips to downtrend (above price).
    Uses cooldown to control trade frequency.
    """

    def __init__(self):
        super(supertrend_reversal_strategy, self).__init__()
        self._period = self.Param("Period", 10).SetDisplay("Period", "ATR period for SuperTrend", "SuperTrend")
        self._multiplier = self.Param("Multiplier", 3.0).SetDisplay("Multiplier", "ATR multiplier for SuperTrend", "SuperTrend")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(1))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._cooldown_bars = self.Param("CooldownBars", 500).SetDisplay("Cooldown Bars", "Bars to wait between trades", "General")

        self._prev_is_up_trend = None
        self._cooldown = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(supertrend_reversal_strategy, self).OnReseted()
        self._prev_is_up_trend = None
        self._cooldown = 0

    def OnStarted(self, time):
        super(supertrend_reversal_strategy, self).OnStarted(time)

        self._prev_is_up_trend = None
        self._cooldown = 0

        st = SuperTrend()
        st.Length = self._period.Value
        st.Multiplier = self._multiplier.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(st, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, st)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, st_iv):
        if candle.State != CandleStates.Finished:
            return

        if not st_iv.IsFormed:
            return

        is_up_trend = st_iv.IsUpTrend

        if self._prev_is_up_trend is None:
            self._prev_is_up_trend = is_up_trend
            return

        if self._cooldown > 0:
            self._cooldown -= 1
            self._prev_is_up_trend = is_up_trend
            return

        cd = self._cooldown_bars.Value

        # SuperTrend flipped to uptrend = bullish
        flipped_up = self._prev_is_up_trend == False and is_up_trend
        # SuperTrend flipped to downtrend = bearish
        flipped_down = self._prev_is_up_trend == True and not is_up_trend

        if self.Position == 0 and flipped_up:
            self.BuyMarket()
            self._cooldown = cd
        elif self.Position == 0 and flipped_down:
            self.SellMarket()
            self._cooldown = cd
        elif self.Position > 0 and flipped_down:
            self.SellMarket()
            self._cooldown = cd
        elif self.Position < 0 and flipped_up:
            self.BuyMarket()
            self._cooldown = cd

        self._prev_is_up_trend = is_up_trend

    def CreateClone(self):
        return supertrend_reversal_strategy()
