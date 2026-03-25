import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import SimpleMovingAverage, DecimalIndicatorValue
from StockSharp.Algo.Strategies import Strategy


class escape_strategy(Strategy):
    def __init__(self):
        super(escape_strategy, self).__init__()

        self._fast_length = self.Param("FastLength", 4)
        self._slow_length = self.Param("SlowLength", 5)
        self._take_profit_long = self.Param("TakeProfitLong", 25.0)
        self._take_profit_short = self.Param("TakeProfitShort", 26.0)
        self._stop_loss_long = self.Param("StopLossLong", 25.0)
        self._stop_loss_short = self.Param("StopLossShort", 3.0)
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5)))

        self._fast_ma = None
        self._slow_ma = None
        self._initialized = False
        self._entry_price = 0.0
        self._stop_price = 0.0
        self._take_price = 0.0
        self._previous_close = 0.0
        self._previous_fast = 0.0
        self._previous_slow = 0.0

    @property
    def FastLength(self):
        return self._fast_length.Value

    @FastLength.setter
    def FastLength(self, value):
        self._fast_length.Value = value

    @property
    def SlowLength(self):
        return self._slow_length.Value

    @SlowLength.setter
    def SlowLength(self, value):
        self._slow_length.Value = value

    @property
    def TakeProfitLong(self):
        return self._take_profit_long.Value

    @TakeProfitLong.setter
    def TakeProfitLong(self, value):
        self._take_profit_long.Value = value

    @property
    def TakeProfitShort(self):
        return self._take_profit_short.Value

    @TakeProfitShort.setter
    def TakeProfitShort(self, value):
        self._take_profit_short.Value = value

    @property
    def StopLossLong(self):
        return self._stop_loss_long.Value

    @StopLossLong.setter
    def StopLossLong(self, value):
        self._stop_loss_long.Value = value

    @property
    def StopLossShort(self):
        return self._stop_loss_short.Value

    @StopLossShort.setter
    def StopLossShort(self, value):
        self._stop_loss_short.Value = value

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    def OnStarted(self, time):
        super(escape_strategy, self).OnStarted(time)

        self._initialized = False
        self._entry_price = 0.0
        self._stop_price = 0.0
        self._take_price = 0.0
        self._previous_close = 0.0
        self._previous_fast = 0.0
        self._previous_slow = 0.0

        self._fast_ma = SimpleMovingAverage()
        self._fast_ma.Length = self.FastLength
        self._slow_ma = SimpleMovingAverage()
        self._slow_ma.Length = self.SlowLength

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(self.ProcessCandle).Start()

        self.StartProtection(
            Unit(2.0, UnitTypes.Percent),
            Unit(1.0, UnitTypes.Percent))

    def ProcessCandle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        t = candle.OpenTime
        fast_input = DecimalIndicatorValue(self._fast_ma, candle.OpenPrice, t)
        fast_input.IsFinal = True
        fast_result = self._fast_ma.Process(fast_input)
        slow_input = DecimalIndicatorValue(self._slow_ma, candle.OpenPrice, t)
        slow_input.IsFinal = True
        slow_result = self._slow_ma.Process(slow_input)

        if not self._fast_ma.IsFormed or not self._slow_ma.IsFormed:
            return

        fast = float(fast_result)
        slow = float(slow_result)

        if not self._initialized:
            self._initialized = True
            self._previous_close = float(candle.ClosePrice)
            self._previous_fast = fast
            self._previous_slow = slow
            return

        close = float(candle.ClosePrice)
        buy_signal = self._previous_close >= self._previous_slow and close < slow
        sell_signal = self._previous_close <= self._previous_fast and close > fast

        if self.Position == 0:
            if buy_signal:
                self.BuyMarket()
            elif sell_signal:
                self.SellMarket()

        self._previous_close = close
        self._previous_fast = fast
        self._previous_slow = slow

    def OnReseted(self):
        super(escape_strategy, self).OnReseted()
        self._fast_ma = None
        self._slow_ma = None
        self._initialized = False
        self._entry_price = 0.0
        self._stop_price = 0.0
        self._take_price = 0.0
        self._previous_close = 0.0
        self._previous_fast = 0.0
        self._previous_slow = 0.0

    def CreateClone(self):
        return escape_strategy()
