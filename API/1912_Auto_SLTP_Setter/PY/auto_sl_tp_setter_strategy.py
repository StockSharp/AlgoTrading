import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import SimpleMovingAverage, AverageTrueRange, DecimalIndicatorValue, CandleIndicatorValue
from StockSharp.Algo.Strategies import Strategy


class auto_sl_tp_setter_strategy(Strategy):
    def __init__(self):
        super(auto_sl_tp_setter_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
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
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(auto_sl_tp_setter_strategy, self).OnReseted()
        self._prev_fast = 0.0
        self._prev_slow = 0.0
        self._is_first = True

    def OnStarted2(self, time):
        super(auto_sl_tp_setter_strategy, self).OnStarted2(time)

        fast_ma = SimpleMovingAverage()
        fast_ma.Length = self._fast_ma_period.Value
        self._slow_ma = SimpleMovingAverage()
        self._slow_ma.Length = self._slow_ma_period.Value
        self._atr = AverageTrueRange()
        self._atr.Length = self._atr_period.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(fast_ma, self.process_candle).Start()

        self.StartProtection(
            Unit(3.0, UnitTypes.Percent),
            Unit(2.0, UnitTypes.Percent))

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, fast_ma)
            self.DrawIndicator(area, self._slow_ma)
            self.DrawOwnTrades(area)

    def process_candle(self, candle, fast):
        if candle.State != CandleStates.Finished:
            return

        slow_inp = DecimalIndicatorValue(self._slow_ma, candle.ClosePrice, candle.OpenTime)
        slow_inp.IsFinal = True
        slow_result = self._slow_ma.Process(slow_inp)
        atr_inp = CandleIndicatorValue(self._atr, candle)
        self._atr.Process(atr_inp)

        if not slow_result.IsFormed:
            return

        slow = float(slow_result)
        fast_val = float(fast)

        if self._is_first:
            self._prev_fast = fast_val
            self._prev_slow = slow
            self._is_first = False
            return

        if self._prev_fast <= self._prev_slow and fast_val > slow and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket()
            self.BuyMarket()
        elif self._prev_fast >= self._prev_slow and fast_val < slow and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket()
            self.SellMarket()

        self._prev_fast = fast_val
        self._prev_slow = slow

    def CreateClone(self):
        return auto_sl_tp_setter_strategy()
