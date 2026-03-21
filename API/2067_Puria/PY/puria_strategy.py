import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import ExponentialMovingAverage, MovingAverageConvergenceDivergence, DecimalIndicatorValue
from StockSharp.Algo.Strategies import Strategy


class puria_strategy(Strategy):

    def __init__(self):
        super(puria_strategy, self).__init__()

        self._ma1_period = self.Param("Ma1Period", 30) \
            .SetDisplay("MA1 Period", "Slow EMA period", "Moving Averages")
        self._ma2_period = self.Param("Ma2Period", 40) \
            .SetDisplay("MA2 Period", "Second slow EMA period", "Moving Averages")
        self._ma3_period = self.Param("Ma3Period", 5) \
            .SetDisplay("MA3 Period", "Fast EMA period", "Moving Averages")
        self._stop_loss_pct = self.Param("StopLossPct", 2.0) \
            .SetDisplay("Stop Loss %", "Stop loss percentage", "Risk")
        self._take_profit_pct = self.Param("TakeProfitPct", 3.0) \
            .SetDisplay("Take Profit %", "Take profit percentage", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Timeframe for strategy", "General")

        self._macd = None
        self._prev_ma75 = 0.0
        self._prev_ma85 = 0.0
        self._prev_ma5 = 0.0
        self._prev_close = 0.0
        self._prev_macd = 0.0
        self._initialized = False

    @property
    def Ma1Period(self):
        return self._ma1_period.Value

    @Ma1Period.setter
    def Ma1Period(self, value):
        self._ma1_period.Value = value

    @property
    def Ma2Period(self):
        return self._ma2_period.Value

    @Ma2Period.setter
    def Ma2Period(self, value):
        self._ma2_period.Value = value

    @property
    def Ma3Period(self):
        return self._ma3_period.Value

    @Ma3Period.setter
    def Ma3Period(self, value):
        self._ma3_period.Value = value

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

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    def OnStarted(self, time):
        super(puria_strategy, self).OnStarted(time)

        self._initialized = False

        ma75 = ExponentialMovingAverage()
        ma75.Length = self.Ma1Period
        ma85 = ExponentialMovingAverage()
        ma85.Length = self.Ma2Period
        ma5 = ExponentialMovingAverage()
        ma5.Length = self.Ma3Period

        self._macd = MovingAverageConvergenceDivergence()
        self._macd.ShortMa.Length = 15
        self._macd.LongMa.Length = 26

        self.SubscribeCandles(self.CandleType) \
            .Bind(ma75, ma85, ma5, self.ProcessCandle) \
            .Start()

        self.StartProtection(
            takeProfit=Unit(float(self.TakeProfitPct), UnitTypes.Percent),
            stopLoss=Unit(float(self.StopLossPct), UnitTypes.Percent)
        )

    def ProcessCandle(self, candle, ma75_val, ma85_val, ma5_val):
        if candle.State != CandleStates.Finished:
            return

        close = float(candle.ClosePrice)
        t = candle.OpenTime

        macd_result = self._macd.Process(DecimalIndicatorValue(self._macd, close, t, True))
        if not macd_result.IsFormed:
            return

        ma75_f = float(ma75_val)
        ma85_f = float(ma85_val)
        ma5_f = float(ma5_val)
        macd_val = float(macd_result.ToDecimal())

        if not self._initialized:
            self._prev_ma75 = ma75_f
            self._prev_ma85 = ma85_f
            self._prev_ma5 = ma5_f
            self._prev_close = close
            self._prev_macd = macd_val
            self._initialized = True
            return

        buy_signal = (self._prev_ma5 > self._prev_ma75 and
                      self._prev_ma5 > self._prev_ma85 and
                      self._prev_close > self._prev_ma5 and
                      self._prev_macd > 0)
        sell_signal = (self._prev_ma5 < self._prev_ma75 and
                       self._prev_ma5 < self._prev_ma85 and
                       self._prev_close < self._prev_ma5 and
                       self._prev_macd < 0)

        if buy_signal and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket()
            self.BuyMarket()
        elif sell_signal and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket()
            self.SellMarket()

        self._prev_ma75 = ma75_f
        self._prev_ma85 = ma85_f
        self._prev_ma5 = ma5_f
        self._prev_close = close
        self._prev_macd = macd_val

    def OnReseted(self):
        super(puria_strategy, self).OnReseted()
        self._macd = None
        self._prev_ma75 = 0.0
        self._prev_ma85 = 0.0
        self._prev_ma5 = 0.0
        self._prev_close = 0.0
        self._prev_macd = 0.0
        self._initialized = False

    def CreateClone(self):
        return puria_strategy()
