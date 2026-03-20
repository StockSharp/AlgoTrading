import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy
from StockSharp.Algo.Indicators import ExponentialMovingAverage


class beer_god_ema_timing_strategy(Strategy):
    """BeerGod EMA Timing: mean-reversion with EMA trend filter."""

    def __init__(self):
        super(beer_god_ema_timing_strategy, self).__init__()

        self._ema_length = self.Param("EmaLength", 60) \
            .SetGreaterThanZero() \
            .SetDisplay("EMA Length", "EMA length for the trend filter", "Indicator")
        self._trigger_minutes = self.Param("TriggerMinutesFromOpen", 3) \
            .SetDisplay("Trigger Minutes", "Minutes after open to check signals", "Timing")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Primary candle type", "General")

        self._current_ema = 0.0
        self._previous_ema = 0.0
        self._current_close = 0.0
        self._previous_close = 0.0

    @property
    def EmaLength(self):
        return int(self._ema_length.Value)
    @property
    def TriggerMinutesFromOpen(self):
        return int(self._trigger_minutes.Value)
    @property
    def CandleType(self):
        return self._candle_type.Value

    def OnStarted(self, time):
        super(beer_god_ema_timing_strategy, self).OnStarted(time)

        self._current_ema = 0.0
        self._previous_ema = 0.0
        self._current_close = 0.0
        self._previous_close = 0.0

        self._ema = ExponentialMovingAverage()
        self._ema.Length = self.EmaLength

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(self._ema, self.process_candle).Start()

    def process_candle(self, candle, ema_value):
        if candle.State != CandleStates.Finished:
            return

        self._previous_ema = self._current_ema
        self._previous_close = self._current_close

        self._current_ema = float(ema_value)
        self._current_close = float(candle.ClosePrice)

        if not self._ema.IsFormed or self._previous_ema == 0:
            return

        price = float(candle.ClosePrice)
        ma_current = self._current_ema
        ma_previous = self._previous_ema
        prev_close = self._previous_close

        new_buy = price < ma_current and ma_current < ma_previous and price < prev_close
        new_sell = price > ma_current and ma_current > ma_previous and price > prev_close

        if not new_buy and not new_sell:
            return

        if new_buy and self.Position <= 0:
            self.BuyMarket()
        elif new_sell and self.Position >= 0:
            self.SellMarket()

    def OnReseted(self):
        super(beer_god_ema_timing_strategy, self).OnReseted()
        self._current_ema = 0.0
        self._previous_ema = 0.0
        self._current_close = 0.0
        self._previous_close = 0.0

    def CreateClone(self):
        return beer_god_ema_timing_strategy()
