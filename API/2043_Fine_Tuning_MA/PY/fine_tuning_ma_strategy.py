import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy


class fine_tuning_ma_strategy(Strategy):

    def __init__(self):
        super(fine_tuning_ma_strategy, self).__init__()

        self._ma_length = self.Param("MaLength", 20) \
            .SetDisplay("MA Length", "Length of the moving average", "Parameters")
        self._take_profit_percent = self.Param("TakeProfitPercent", 1.0) \
            .SetDisplay("Take Profit, %", "Take profit level in percent", "Protection")
        self._stop_loss_percent = self.Param("StopLossPercent", 1.0) \
            .SetDisplay("Stop Loss, %", "Stop loss level in percent", "Protection")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Type of candles for calculations", "Parameters")

        self._prev1 = 0.0
        self._prev2 = 0.0
        self._candle_count = 0

    @property
    def MaLength(self):
        return self._ma_length.Value

    @MaLength.setter
    def MaLength(self, value):
        self._ma_length.Value = value

    @property
    def TakeProfitPercent(self):
        return self._take_profit_percent.Value

    @TakeProfitPercent.setter
    def TakeProfitPercent(self, value):
        self._take_profit_percent.Value = value

    @property
    def StopLossPercent(self):
        return self._stop_loss_percent.Value

    @StopLossPercent.setter
    def StopLossPercent(self, value):
        self._stop_loss_percent.Value = value

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    def OnStarted(self, time):
        super(fine_tuning_ma_strategy, self).OnStarted(time)

        ma = ExponentialMovingAverage()
        ma.Length = self.MaLength

        self.SubscribeCandles(self.CandleType) \
            .Bind(ma, self.ProcessCandle) \
            .Start()

        self.StartProtection(
            stopLoss=Unit(self.StopLossPercent, UnitTypes.Percent),
            takeProfit=Unit(self.TakeProfitPercent, UnitTypes.Percent)
        )

    def ProcessCandle(self, candle, ma_value):
        if candle.State != CandleStates.Finished:
            return

        val = float(ma_value)

        self._candle_count += 1
        if self._candle_count <= 2:
            self._prev2 = self._prev1
            self._prev1 = val
            return

        was_rising = self._prev1 > self._prev2
        was_falling = self._prev1 < self._prev2

        if was_rising and val > self._prev1 and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket()
            self.BuyMarket()
        elif was_falling and val < self._prev1 and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket()
            self.SellMarket()

        self._prev2 = self._prev1
        self._prev1 = val

    def OnReseted(self):
        super(fine_tuning_ma_strategy, self).OnReseted()
        self._prev1 = 0.0
        self._prev2 = 0.0
        self._candle_count = 0

    def CreateClone(self):
        return fine_tuning_ma_strategy()
