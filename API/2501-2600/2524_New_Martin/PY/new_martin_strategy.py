import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates, Sides
from StockSharp.Algo.Indicators import SmoothedMovingAverage
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *
from indicator_extensions import *

class new_martin_strategy(Strategy):
    def __init__(self):
        super(new_martin_strategy, self).__init__()
        self._tp_pips = self.Param("TakeProfitPips", 50.0).SetGreaterThanZero().SetDisplay("Take Profit", "Target distance in pips", "Risk")
        self._initial_volume = self.Param("InitialVolume", 0.1).SetGreaterThanZero().SetDisplay("Initial Volume", "Volume per hedge side", "Trading")
        self._slow_period = self.Param("SlowPeriod", 20).SetGreaterThanZero().SetDisplay("Slow MA", "Slow smoothed MA period", "Indicators")
        self._fast_period = self.Param("FastPeriod", 5).SetGreaterThanZero().SetDisplay("Fast MA", "Fast smoothed MA period", "Indicators")
        self._loss_percent = self.Param("LossPercent", 12.0).SetGreaterThanZero().SetDisplay("Equity DD %", "Maximum drawdown before reset", "Risk")
        self._multiplier = self.Param("Multiplier", 1.6).SetGreaterThanZero().SetDisplay("Multiplier", "Martingale growth factor", "Trading")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(1))).SetDisplay("Candle Type", "Time frame", "General")

    @property
    def CandleType(self): return self._candle_type.Value
    @CandleType.setter
    def CandleType(self, value): self._candle_type.Value = value

    def OnReseted(self):
        super(new_martin_strategy, self).OnReseted()
        self._slow_prev1 = None
        self._slow_prev2 = None
        self._fast_prev1 = None
        self._fast_prev2 = None
        self._current_volume = 0
        self._pip_size = 0
        self._initialized = False

    def OnStarted2(self, time):
        super(new_martin_strategy, self).OnStarted2(time)
        self._current_volume = self._initial_volume.Value
        step = 1.0
        if self.Security is not None and self.Security.PriceStep is not None and self.Security.PriceStep > 0:
            step = float(self.Security.PriceStep)
        self._pip_size = step
        self._slow_prev1 = None
        self._slow_prev2 = None
        self._fast_prev1 = None
        self._fast_prev2 = None
        self._initialized = False

        slow_ma = SmoothedMovingAverage()
        slow_ma.Length = self._slow_period.Value
        fast_ma = SmoothedMovingAverage()
        fast_ma.Length = self._fast_period.Value
        self._slow_ma = slow_ma
        self._fast_ma = fast_ma

        sub = self.SubscribeCandles(self.CandleType)
        sub.Bind(slow_ma, fast_ma, self.OnProcess).Start()

    def OnProcess(self, candle, slow, fast):
        if candle.State != CandleStates.Finished:
            return
        if not self._slow_ma.IsFormed or not self._fast_ma.IsFormed:
            self._update_history(slow, fast)
            return

        if not self._initialized:
            self.BuyMarket(self._current_volume)
            self.SellMarket(self._current_volume)
            self._initialized = True

        # Check MA crossover for martingale additions
        if (self._slow_prev2 is not None and self._fast_prev2 is not None and
            self._slow_prev1 is not None and self._fast_prev1 is not None):
            cross = ((self._slow_prev2 > self._fast_prev2 and self._slow_prev1 < self._fast_prev1) or
                     (self._slow_prev2 < self._fast_prev2 and self._slow_prev1 > self._fast_prev1))
            if cross:
                vol = self._current_volume * self._multiplier.Value
                if self.Position > 0:
                    self.SellMarket(vol)
                elif self.Position < 0:
                    self.BuyMarket(vol)

        self._update_history(slow, fast)

    def _update_history(self, slow, fast):
        self._slow_prev2 = self._slow_prev1
        self._slow_prev1 = slow
        self._fast_prev2 = self._fast_prev1
        self._fast_prev1 = fast

    def CreateClone(self):
        return new_martin_strategy()
