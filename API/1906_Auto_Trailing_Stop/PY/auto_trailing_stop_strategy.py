import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *
from indicator_extensions import *

class auto_trailing_stop_strategy(Strategy):
    """
    Strategy that opens positions using MA crossover and protects
    them with trailing stop loss and take profit.
    """

    def __init__(self):
        super(auto_trailing_stop_strategy, self).__init__()

        self._fast_ma_period = self.Param("FastMaPeriod", 5) \
            .SetGreaterThanZero() \
            .SetDisplay("Fast MA", "Fast moving average period", "Indicators")
        self._slow_ma_period = self.Param("SlowMaPeriod", 20) \
            .SetGreaterThanZero() \
            .SetDisplay("Slow MA", "Slow moving average period", "Indicators")
        self._take_profit_pct = self.Param("TakeProfitPct", 5.0) \
            .SetDisplay("Take Profit %", "Take profit percentage", "Protection")
        self._stop_loss_pct = self.Param("StopLossPct", 3.0) \
            .SetDisplay("Stop Loss %", "Initial stop loss percentage", "Protection")
        self._candle_type = self.Param("CandleType", tf(15)) \
            .SetDisplay("Candle Type", "Candle type for price updates", "General")

        self._slow_ma = None
        self._prev_fast = 0.0
        self._prev_slow = 0.0
        self._is_first = True

    @property
    def FastMaPeriod(self): return self._fast_ma_period.Value
    @FastMaPeriod.setter
    def FastMaPeriod(self, v): self._fast_ma_period.Value = v
    @property
    def SlowMaPeriod(self): return self._slow_ma_period.Value
    @SlowMaPeriod.setter
    def SlowMaPeriod(self, v): self._slow_ma_period.Value = v
    @property
    def TakeProfitPct(self): return self._take_profit_pct.Value
    @TakeProfitPct.setter
    def TakeProfitPct(self, v): self._take_profit_pct.Value = v
    @property
    def StopLossPct(self): return self._stop_loss_pct.Value
    @StopLossPct.setter
    def StopLossPct(self, v): self._stop_loss_pct.Value = v
    @property
    def CandleType(self): return self._candle_type.Value
    @CandleType.setter
    def CandleType(self, v): self._candle_type.Value = v

    def OnReseted(self):
        super(auto_trailing_stop_strategy, self).OnReseted()
        self._prev_fast = 0.0
        self._prev_slow = 0.0
        self._is_first = True

    def OnStarted(self, time):
        super(auto_trailing_stop_strategy, self).OnStarted(time)

        fast_ma = SimpleMovingAverage()
        fast_ma.Length = self.FastMaPeriod
        self._slow_ma = SimpleMovingAverage()
        self._slow_ma.Length = self.SlowMaPeriod

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(fast_ma, self.ProcessCandle).Start()

        self.StartProtection(
            stopLoss=Unit(self.StopLossPct, UnitTypes.Percent),
            takeProfit=Unit(self.TakeProfitPct, UnitTypes.Percent)
        )

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, fast_ma)
            self.DrawIndicator(area, self._slow_ma)
            self.DrawOwnTrades(area)

    def ProcessCandle(self, candle, fast):
        if candle.State != CandleStates.Finished:
            return

        slow_result = self._slow_ma.Process(candle.ClosePrice, candle.OpenTime, True)
        if not slow_result.IsFormed:
            return

        slow = float(slow_result)

        if self._is_first:
            self._prev_fast = fast
            self._prev_slow = slow
            self._is_first = False
            return

        if self._prev_fast <= self._prev_slow and fast > slow and self.Position <= 0:


            self.BuyMarket()


        elif self._prev_fast >= self._prev_slow and fast < slow and self.Position >= 0:


            self.SellMarket()

        self._prev_fast = fast
        self._prev_slow = slow

    def CreateClone(self):
        """!! REQUIRED!! Creates a new instance of the strategy."""
        return auto_trailing_stop_strategy()
