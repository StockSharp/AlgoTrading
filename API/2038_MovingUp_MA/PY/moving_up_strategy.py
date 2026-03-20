import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy


class moving_up_strategy(Strategy):

    def __init__(self):
        super(moving_up_strategy, self).__init__()

        self._fast_length = self.Param("FastLength", 13) \
            .SetDisplay("Fast MA", "Fast MA period", "MA")
        self._slow_length = self.Param("SlowLength", 21) \
            .SetDisplay("Slow MA", "Slow MA period", "MA")
        self._stop_loss = self.Param("StopLoss", 250.0) \
            .SetDisplay("SL", "Stop loss distance", "Risk")
        self._take_profit = self.Param("TakeProfit", 500.0) \
            .SetDisplay("TP", "Take profit distance", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(1))) \
            .SetDisplay("Candle", "Candle type", "General")

        self._is_initialized = False
        self._was_fast_below_slow = False

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
    def StopLoss(self):
        return self._stop_loss.Value

    @StopLoss.setter
    def StopLoss(self, value):
        self._stop_loss.Value = value

    @property
    def TakeProfit(self):
        return self._take_profit.Value

    @TakeProfit.setter
    def TakeProfit(self, value):
        self._take_profit.Value = value

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    def OnStarted(self, time):
        super(moving_up_strategy, self).OnStarted(time)

        fast_ma = ExponentialMovingAverage()
        fast_ma.Length = self.FastLength
        slow_ma = ExponentialMovingAverage()
        slow_ma.Length = self.SlowLength

        self.SubscribeCandles(self.CandleType) \
            .Bind(fast_ma, slow_ma, self.ProcessCandle) \
            .Start()

        self.StartProtection(
            stopLoss=Unit(self.StopLoss, UnitTypes.Absolute),
            takeProfit=Unit(self.TakeProfit, UnitTypes.Absolute)
        )

    def ProcessCandle(self, candle, fast_value, slow_value):
        if candle.State != CandleStates.Finished:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        fast = float(fast_value)
        slow = float(slow_value)

        if not self._is_initialized:
            self._was_fast_below_slow = fast < slow
            self._is_initialized = True
            return

        is_fast_below_slow = fast < slow

        if self._was_fast_below_slow != is_fast_below_slow:
            if not is_fast_below_slow and self.Position <= 0:
                if self.Position < 0:
                    self.BuyMarket()
                self.BuyMarket()
            elif is_fast_below_slow and self.Position >= 0:
                if self.Position > 0:
                    self.SellMarket()
                self.SellMarket()

            self._was_fast_below_slow = is_fast_below_slow

    def OnReseted(self):
        super(moving_up_strategy, self).OnReseted()
        self._is_initialized = False
        self._was_fast_below_slow = False

    def CreateClone(self):
        return moving_up_strategy()
