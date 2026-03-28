import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates, UnitTypes, Unit
from StockSharp.Algo.Strategies import Strategy
from StockSharp.Algo.Indicators import SimpleMovingAverage, AwesomeOscillator, DecimalIndicatorValue

class ravi_iao_strategy(Strategy):
    def __init__(self):
        super(ravi_iao_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(10))) \
            .SetDisplay("Candle Type", "Time-frame for analysis", "General")
        self._fast_length = self.Param("FastLength", 12) \
            .SetDisplay("Fast Length", "Fast SMA period inside RAVI", "RAVI")
        self._slow_length = self.Param("SlowLength", 72) \
            .SetDisplay("Slow Length", "Slow SMA period inside RAVI", "RAVI")
        self._threshold = self.Param("Threshold", 0.3) \
            .SetDisplay("RAVI Threshold", "Minimum absolute RAVI value to confirm the trend", "Signals")
        self._stop_loss_points = self.Param("StopLossPoints", 500.0) \
            .SetDisplay("Stop Loss", "Stop-loss distance in price units", "Risk")
        self._take_profit_points = self.Param("TakeProfitPoints", 500.0) \
            .SetDisplay("Take Profit", "Take-profit distance in price units", "Risk")

        self._prev_ravi = None
        self._prev_prev_ravi = None
        self._prev_ac = None
        self._prev_prev_ac = None
        self._ao_average = None

    @property
    def CandleType(self):
        return self._candle_type.Value

    @property
    def FastLength(self):
        return self._fast_length.Value

    @property
    def SlowLength(self):
        return self._slow_length.Value

    @property
    def Threshold(self):
        return self._threshold.Value

    @property
    def StopLossPoints(self):
        return self._stop_loss_points.Value

    @property
    def TakeProfitPoints(self):
        return self._take_profit_points.Value

    def OnStarted(self, time):
        super(ravi_iao_strategy, self).OnStarted(time)

        fast_ma = SimpleMovingAverage()
        fast_ma.Length = self.FastLength
        slow_ma = SimpleMovingAverage()
        slow_ma.Length = self.SlowLength
        ao = AwesomeOscillator()
        self._ao_average = SimpleMovingAverage()
        self._ao_average.Length = 5

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.BindEx(fast_ma, slow_ma, ao, self.ProcessCandle).Start()

        tp = Unit(float(self.TakeProfitPoints), UnitTypes.Absolute) if float(self.TakeProfitPoints) > 0 else None
        sl = Unit(float(self.StopLossPoints), UnitTypes.Absolute) if float(self.StopLossPoints) > 0 else None
        self.StartProtection(tp, sl)

    def ProcessCandle(self, candle, fast_val, slow_val, ao_val):
        if candle.State != CandleStates.Finished:
            return

        if fast_val.IsEmpty or slow_val.IsEmpty or ao_val.IsEmpty:
            return

        fast_value = float(fast_val)
        slow_value = float(slow_val)
        ao_value = float(ao_val)

        ao_input = DecimalIndicatorValue(self._ao_average, ao_value, candle.OpenTime)
        ao_input.IsFinal = True
        ao_avg_result = self._ao_average.Process(ao_input)
        if ao_avg_result.IsEmpty:
            return

        ao_avg_value = float(ao_avg_result)
        ac = ao_value - ao_avg_value

        if slow_value == 0:
            self._update_history(None, ac)
            return

        ravi = 100.0 * (fast_value - slow_value) / slow_value

        if (self._prev_ravi is not None and
            self._prev_prev_ravi is not None and
            self._prev_ac is not None and
            self._prev_prev_ac is not None and
            self.Position == 0 and
            self.IsFormedAndOnlineAndAllowTrading()):

            threshold = float(self.Threshold)
            bullish = (self._prev_ac > self._prev_prev_ac and self._prev_prev_ac > 0
                       and self._prev_ravi > self._prev_prev_ravi and self._prev_ravi > threshold)
            bearish = (self._prev_ac < self._prev_prev_ac and self._prev_prev_ac < 0
                       and self._prev_ravi < self._prev_prev_ravi and self._prev_ravi < -threshold)

            if bullish:
                self.BuyMarket(self.Volume)
            elif bearish:
                self.SellMarket(self.Volume)

        self._update_history(ravi, ac)

    def _update_history(self, ravi, ac):
        self._prev_prev_ravi = self._prev_ravi
        self._prev_ravi = ravi
        self._prev_prev_ac = self._prev_ac
        self._prev_ac = ac

    def OnReseted(self):
        super(ravi_iao_strategy, self).OnReseted()
        self._prev_ravi = None
        self._prev_prev_ravi = None
        self._prev_ac = None
        self._prev_prev_ac = None

    def CreateClone(self):
        return ravi_iao_strategy()
