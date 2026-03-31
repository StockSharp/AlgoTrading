import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import BullPower, BearPower
from StockSharp.Algo.Strategies import Strategy

class robot_power_m5_strategy(Strategy):
    def __init__(self):
        super(robot_power_m5_strategy, self).__init__()
        self._bb_period = self.Param("BullBearPeriod", 5).SetGreaterThanZero().SetDisplay("Bull/Bear Period", "Bulls and Bears Power period", "Indicators")
        self._trailing_step = self.Param("TrailingStep", 10.0).SetGreaterThanZero().SetDisplay("Trailing Step", "Trailing stop step", "Risk")
        self._tp = self.Param("TakeProfit", 150.0).SetGreaterThanZero().SetDisplay("Take Profit", "TP distance", "Risk")
        self._sl = self.Param("StopLoss", 105.0).SetGreaterThanZero().SetDisplay("Stop Loss", "SL distance", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))).SetDisplay("Candle Type", "Candle timeframe", "General")

    @property
    def CandleType(self): return self._candle_type.Value
    @CandleType.setter
    def CandleType(self, value): self._candle_type.Value = value

    def OnReseted(self):
        super(robot_power_m5_strategy, self).OnReseted()
        self._stop_price = 0
        self._take_price = 0

    def OnStarted2(self, time):
        super(robot_power_m5_strategy, self).OnStarted2(time)
        self._stop_price = 0
        self._take_price = 0

        bulls = BullPower()
        bulls.Length = self._bb_period.Value
        bears = BearPower()
        bears.Length = self._bb_period.Value

        sub = self.SubscribeCandles(self.CandleType)
        sub.Bind(bulls, bears, self.OnProcess).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, sub)
            self.DrawIndicator(area, bulls)
            self.DrawIndicator(area, bears)
            self.DrawOwnTrades(area)

    def OnProcess(self, candle, bulls_val, bears_val):
        if candle.State != CandleStates.Finished:
            return

        total = bulls_val + bears_val
        close = candle.ClosePrice

        if self.Position == 0:
            if total > 0:
                self.BuyMarket()
                self._stop_price = close - self._sl.Value
                self._take_price = close + self._tp.Value
            elif total < 0:
                self.SellMarket()
                self._stop_price = close + self._sl.Value
                self._take_price = close - self._tp.Value
            return

        if self.Position > 0:
            if candle.LowPrice <= self._stop_price or candle.HighPrice >= self._take_price:
                self.SellMarket()
                self._stop_price = 0
                self._take_price = 0
                return
            if close - self._stop_price > 2 * self._trailing_step.Value:
                self._stop_price = close - self._trailing_step.Value
        elif self.Position < 0:
            if candle.HighPrice >= self._stop_price or candle.LowPrice <= self._take_price:
                self.BuyMarket()
                self._stop_price = 0
                self._take_price = 0
                return
            if self._stop_price - close > 2 * self._trailing_step.Value:
                self._stop_price = close + self._trailing_step.Value

    def CreateClone(self):
        return robot_power_m5_strategy()
