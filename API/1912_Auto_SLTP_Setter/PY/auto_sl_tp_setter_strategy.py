import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import SimpleMovingAverage, AverageTrueRange
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *
from indicator_extensions import *

class auto_sl_tp_setter_strategy(Strategy):
    """
    Strategy that trades MA crossovers and automatically protects positions
    with ATR-based stop loss and take profit.
    """

    def __init__(self):
        super(auto_sl_tp_setter_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", tf(5)) \
            .SetDisplay("Candle Type", "Candle timeframe", "General")
        self._fast_ma_period = self.Param("FastMaPeriod", 10) \
            .SetGreaterThanZero() \
            .SetDisplay("Fast MA", "Fast MA period", "Indicators")
        self._slow_ma_period = self.Param("SlowMaPeriod", 30) \
            .SetGreaterThanZero() \
            .SetDisplay("Slow MA", "Slow MA period", "Indicators")
        self._stop_loss_atr = self.Param("StopLossAtr", 1.5) \
            .SetDisplay("SL ATR Mult", "ATR multiplier for stop loss", "Risk")
        self._take_profit_atr = self.Param("TakeProfitAtr", 2.5) \
            .SetDisplay("TP ATR Mult", "ATR multiplier for take profit", "Risk")
        self._atr_period = self.Param("AtrPeriod", 14) \
            .SetGreaterThanZero() \
            .SetDisplay("ATR Period", "ATR calculation period", "Indicators")

        self._slow_ma = None
        self._atr = None
        self._prev_fast = 0.0
        self._prev_slow = 0.0
        self._is_first = True

    @property
    def CandleType(self): return self._candle_type.Value
    @CandleType.setter
    def CandleType(self, v): self._candle_type.Value = v
    @property
    def FastMaPeriod(self): return self._fast_ma_period.Value
    @FastMaPeriod.setter
    def FastMaPeriod(self, v): self._fast_ma_period.Value = v
    @property
    def SlowMaPeriod(self): return self._slow_ma_period.Value
    @SlowMaPeriod.setter
    def SlowMaPeriod(self, v): self._slow_ma_period.Value = v
    @property
    def StopLossAtr(self): return self._stop_loss_atr.Value
    @StopLossAtr.setter
    def StopLossAtr(self, v): self._stop_loss_atr.Value = v
    @property
    def TakeProfitAtr(self): return self._take_profit_atr.Value
    @TakeProfitAtr.setter
    def TakeProfitAtr(self, v): self._take_profit_atr.Value = v
    @property
    def AtrPeriod(self): return self._atr_period.Value
    @AtrPeriod.setter
    def AtrPeriod(self, v): self._atr_period.Value = v

    def OnReseted(self):
        super(auto_sl_tp_setter_strategy, self).OnReseted()
        self._prev_fast = 0.0
        self._prev_slow = 0.0
        self._is_first = True

    def OnStarted(self, time):
        super(auto_sl_tp_setter_strategy, self).OnStarted(time)

        fast_ma = SimpleMovingAverage()
        fast_ma.Length = self.FastMaPeriod
        self._slow_ma = SimpleMovingAverage()
        self._slow_ma.Length = self.SlowMaPeriod
        self._atr = AverageTrueRange()
        self._atr.Length = self.AtrPeriod

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(fast_ma, self.ProcessCandle).Start()

        self.StartProtection(
            stopLoss=Unit(2, UnitTypes.Percent),
            takeProfit=Unit(3, UnitTypes.Percent)
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
        self._atr.Process(candle)

        if not slow_result.IsFormed:
            return

        slow = float(slow_result)

        if self._is_first:
            self._prev_fast = fast
            self._prev_slow = slow
            self._is_first = False
            return

        if self._prev_fast <= self._prev_slow and fast > slow and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket()
            self.BuyMarket()
        elif self._prev_fast >= self._prev_slow and fast < slow and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket()
            self.SellMarket()

        self._prev_fast = fast
        self._prev_slow = slow

    def CreateClone(self):
        """!! REQUIRED!! Creates a new instance of the strategy."""
        return auto_sl_tp_setter_strategy()
