import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy

class fast_slow_ma_crossover_strategy(Strategy):
    def __init__(self):
        super(fast_slow_ma_crossover_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(120)))
        self._fast_ma_period = self.Param("FastMaPeriod", 30)
        self._slow_ma_period = self.Param("SlowMaPeriod", 80)
        self._stop_loss_pct = self.Param("StopLossPct", 2.0)
        self._take_profit_pct = self.Param("TakeProfitPct", 3.0)

        self._prev_fast = 0.0
        self._prev_slow = 0.0
        self._has_prev = False
        self._entry_price = 0.0

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    @property
    def FastMaPeriod(self):
        return self._fast_ma_period.Value

    @FastMaPeriod.setter
    def FastMaPeriod(self, value):
        self._fast_ma_period.Value = value

    @property
    def SlowMaPeriod(self):
        return self._slow_ma_period.Value

    @SlowMaPeriod.setter
    def SlowMaPeriod(self, value):
        self._slow_ma_period.Value = value

    @property
    def StopLossPct(self):
        return self._stop_loss_pct.Value

    @StopLossPct.setter
    def StopLossPct(self, value):
        self._stop_loss_pct.Value = value

    @property
    def TakeProfitPct(self):
        return self._take_profit_pct.Value

    @TakeProfitPct.setter
    def TakeProfitPct(self, value):
        self._take_profit_pct.Value = value

    def OnReseted(self):
        super(fast_slow_ma_crossover_strategy, self).OnReseted()
        self._prev_fast = 0.0
        self._prev_slow = 0.0
        self._has_prev = False
        self._entry_price = 0.0

    def OnStarted(self, time):
        super(fast_slow_ma_crossover_strategy, self).OnStarted(time)
        self._prev_fast = 0.0
        self._prev_slow = 0.0
        self._has_prev = False
        self._entry_price = 0.0

        fast_ma = ExponentialMovingAverage()
        fast_ma.Length = self.FastMaPeriod
        slow_ma = ExponentialMovingAverage()
        slow_ma.Length = self.SlowMaPeriod

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(fast_ma, slow_ma, self._process_candle).Start()

    def _process_candle(self, candle, fast_value, slow_value):
        if candle.State != CandleStates.Finished:
            return

        fast_val = float(fast_value)
        slow_val = float(slow_value)
        close = float(candle.ClosePrice)

        # Check SL/TP on existing position
        if self.Position != 0 and self._entry_price > 0:
            if self.Position > 0:
                pnl_pct = (close - self._entry_price) / self._entry_price * 100.0
                if pnl_pct <= -float(self.StopLossPct) or pnl_pct >= float(self.TakeProfitPct):
                    self.SellMarket()
                    self._entry_price = 0.0
                    self._prev_fast = fast_val
                    self._prev_slow = slow_val
                    self._has_prev = True
                    return
            elif self.Position < 0:
                pnl_pct = (self._entry_price - close) / self._entry_price * 100.0
                if pnl_pct <= -float(self.StopLossPct) or pnl_pct >= float(self.TakeProfitPct):
                    self.BuyMarket()
                    self._entry_price = 0.0
                    self._prev_fast = fast_val
                    self._prev_slow = slow_val
                    self._has_prev = True
                    return

        if self._has_prev:
            cross_up = self._prev_fast <= self._prev_slow and fast_val > slow_val
            cross_down = self._prev_fast >= self._prev_slow and fast_val < slow_val

            if cross_up and self.Position <= 0:
                self.BuyMarket()
                self._entry_price = close
            elif cross_down and self.Position >= 0:
                self.SellMarket()
                self._entry_price = close

        self._prev_fast = fast_val
        self._prev_slow = slow_val
        self._has_prev = True

    def CreateClone(self):
        return fast_slow_ma_crossover_strategy()
