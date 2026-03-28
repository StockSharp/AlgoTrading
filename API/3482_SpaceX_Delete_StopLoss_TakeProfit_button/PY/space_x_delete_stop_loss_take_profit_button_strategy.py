import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import SimpleMovingAverage, StandardDeviation
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *
from indicator_extensions import *

class space_x_delete_stop_loss_take_profit_button_strategy(Strategy):
    def __init__(self):
        super(space_x_delete_stop_loss_take_profit_button_strategy, self).__init__()
        self._sma_period = self.Param("SmaPeriod", 20).SetGreaterThanZero().SetDisplay("SMA Period", "SMA period for baseline", "Indicators")
        self._std_period = self.Param("StdDevPeriod", 20).SetGreaterThanZero().SetDisplay("StdDev Period", "Standard Deviation period", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))).SetDisplay("Candle Type", "Candle timeframe", "General")

    @property
    def CandleType(self): return self._candle_type.Value
    @CandleType.setter
    def CandleType(self, value): self._candle_type.Value = value

    def OnStarted(self, time):
        super(space_x_delete_stop_loss_take_profit_button_strategy, self).OnStarted(time)

        sma = SimpleMovingAverage()
        sma.Length = self._sma_period.Value
        std = StandardDeviation()
        std.Length = self._std_period.Value

        sub = self.SubscribeCandles(self.CandleType)
        sub.Bind(sma, std, self.OnProcess).Start()

    def OnProcess(self, candle, sma_val, std_val):
        if candle.State != CandleStates.Finished:
            return

        upper = sma_val + 2 * std_val
        lower = sma_val - 2 * std_val
        close = float(candle.ClosePrice)

        if close > upper and self.Position <= 0:
            self.BuyMarket()
        elif close < lower and self.Position >= 0:
            self.SellMarket()

    def CreateClone(self):
        return space_x_delete_stop_loss_take_profit_button_strategy()
