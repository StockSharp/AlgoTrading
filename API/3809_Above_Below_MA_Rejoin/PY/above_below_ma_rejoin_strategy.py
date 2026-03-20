import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy


class above_below_ma_rejoin_strategy(Strategy):
    """Above/Below MA Rejoin strategy.
    Buys when price rejoins from below a rising EMA (pullback in uptrend).
    Sells when price rejoins from above a falling EMA (pullback in downtrend)."""

    def __init__(self):
        super(above_below_ma_rejoin_strategy, self).__init__()

        self._ema_length = self.Param("EmaLength", 20) \
            .SetDisplay("EMA Period", "EMA lookback period", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Candle timeframe", "General")

        self._prev_ema = 0.0
        self._prev_close = 0.0
        self._has_prev = False

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    @property
    def EmaLength(self):
        return self._ema_length.Value

    def OnReseted(self):
        super(above_below_ma_rejoin_strategy, self).OnReseted()
        self._prev_ema = 0.0
        self._prev_close = 0.0
        self._has_prev = False

    def OnStarted(self, time):
        super(above_below_ma_rejoin_strategy, self).OnStarted(time)

        self._has_prev = False

        ema = ExponentialMovingAverage()
        ema.Length = self.EmaLength

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(ema, self._process_candle).Start()

    def _process_candle(self, candle, ema_value):
        if candle.State != CandleStates.Finished:
            return

        close = float(candle.ClosePrice)
        ema_val = float(ema_value)

        if not self._has_prev:
            self._prev_ema = ema_val
            self._prev_close = close
            self._has_prev = True
            return

        ema_rising = ema_val > self._prev_ema
        ema_falling = ema_val < self._prev_ema

        # Price rejoins from below in uptrend - buy
        if ema_rising and self._prev_close < self._prev_ema and close >= ema_val and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket()
            self.BuyMarket()
        # Price rejoins from above in downtrend - sell
        elif ema_falling and self._prev_close > self._prev_ema and close <= ema_val and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket()
            self.SellMarket()

        self._prev_ema = ema_val
        self._prev_close = close

    def CreateClone(self):
        return above_below_ma_rejoin_strategy()
