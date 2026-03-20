import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage, RelativeStrengthIndex
from StockSharp.Algo.Strategies import Strategy

class divergence_ema_rsi_close_buy_only_strategy(Strategy):
    def __init__(self):
        super(divergence_ema_rsi_close_buy_only_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(30)))
        self._ema_period = self.Param("EmaPeriod", 20)
        self._rsi_period = self.Param("RsiPeriod", 7)
        self._rsi_entry = self.Param("RsiEntry", 35.0)
        self._rsi_exit = self.Param("RsiExit", 65.0)

        self._prev_rsi = None

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    @property
    def EmaPeriod(self):
        return self._ema_period.Value

    @EmaPeriod.setter
    def EmaPeriod(self, value):
        self._ema_period.Value = value

    @property
    def RsiPeriod(self):
        return self._rsi_period.Value

    @RsiPeriod.setter
    def RsiPeriod(self, value):
        self._rsi_period.Value = value

    @property
    def RsiEntry(self):
        return self._rsi_entry.Value

    @RsiEntry.setter
    def RsiEntry(self, value):
        self._rsi_entry.Value = value

    @property
    def RsiExit(self):
        return self._rsi_exit.Value

    @RsiExit.setter
    def RsiExit(self, value):
        self._rsi_exit.Value = value

    def OnReseted(self):
        super(divergence_ema_rsi_close_buy_only_strategy, self).OnReseted()
        self._prev_rsi = None

    def OnStarted(self, time):
        super(divergence_ema_rsi_close_buy_only_strategy, self).OnStarted(time)
        self._prev_rsi = None

        ema = ExponentialMovingAverage()
        ema.Length = self.EmaPeriod
        rsi = RelativeStrengthIndex()
        rsi.Length = self.RsiPeriod

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(ema, rsi, self._process_candle).Start()

    def _process_candle(self, candle, ema_value, rsi_value):
        if candle.State != CandleStates.Finished:
            return

        ema_val = float(ema_value)
        rsi_val = float(rsi_value)
        close = float(candle.ClosePrice)

        if self._prev_rsi is None:
            self._prev_rsi = rsi_val
            return

        rsi_entry = float(self.RsiEntry)
        rsi_exit = float(self.RsiExit)

        # Exit: RSI crosses above exit level
        if self.Position > 0 and self._prev_rsi < rsi_exit and rsi_val >= rsi_exit:
            self.SellMarket()

        # Entry: RSI crosses below entry level and price is near/below EMA
        if self.Position <= 0 and self._prev_rsi > rsi_entry and rsi_val <= rsi_entry and close <= ema_val * 1.005:
            self.BuyMarket()

        self._prev_rsi = rsi_val

    def CreateClone(self):
        return divergence_ema_rsi_close_buy_only_strategy()
