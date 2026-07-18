import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage, RelativeStrengthIndex
from StockSharp.Algo.Strategies import Strategy


class alexav_d1_profit_gbp_usd_breakout_strategy(Strategy):
    """Alexav D1 Profit breakout strategy.
    Buys when price breaks above EMA and RSI is rising.
    Sells when price breaks below EMA and RSI is falling."""

    def __init__(self):
        super(alexav_d1_profit_gbp_usd_breakout_strategy, self).__init__()

        self._ema_period = self.Param("EmaPeriod", 20) \
            .SetDisplay("EMA Period", "EMA period", "Indicators")
        self._rsi_period = self.Param("RsiPeriod", 14) \
            .SetDisplay("RSI Period", "RSI period", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Candle timeframe", "General")

        self._prev_close = 0.0
        self._prev_ema = 0.0
        self._prev_rsi = 0.0
        self._has_prev = False

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    @property
    def EmaPeriod(self):
        return self._ema_period.Value

    @property
    def RsiPeriod(self):
        return self._rsi_period.Value

    def OnReseted(self):
        super(alexav_d1_profit_gbp_usd_breakout_strategy, self).OnReseted()
        self._prev_close = 0.0
        self._prev_ema = 0.0
        self._prev_rsi = 0.0
        self._has_prev = False

    def OnStarted2(self, time):
        super(alexav_d1_profit_gbp_usd_breakout_strategy, self).OnStarted2(time)

        self._has_prev = False

        ema = ExponentialMovingAverage()
        ema.Length = self.EmaPeriod
        rsi = RelativeStrengthIndex()
        rsi.Length = self.RsiPeriod

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(ema, rsi, self._process_candle).Start()

    def _process_candle(self, candle, ema_value, rsi_value):
        if candle.State != CandleStates.Finished:
            return

        close = float(candle.ClosePrice)
        ema_val = float(ema_value)
        rsi_val = float(rsi_value)

        if not self._has_prev:
            self._prev_close = close
            self._prev_ema = ema_val
            self._prev_rsi = rsi_val
            self._has_prev = True
            return

        # Breakout above EMA with rising RSI
        if self._prev_close <= self._prev_ema and close > ema_val and rsi_val > self._prev_rsi and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket()
            self.BuyMarket()
        # Breakout below EMA with falling RSI
        elif self._prev_close >= self._prev_ema and close < ema_val and rsi_val < self._prev_rsi and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket()
            self.SellMarket()

        self._prev_close = close
        self._prev_ema = ema_val
        self._prev_rsi = rsi_val

    def CreateClone(self):
        return alexav_d1_profit_gbp_usd_breakout_strategy()
