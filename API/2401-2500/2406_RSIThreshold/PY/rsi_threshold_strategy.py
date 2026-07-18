import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import RelativeStrengthIndex
from StockSharp.Algo.Strategies import Strategy


class rsi_threshold_strategy(Strategy):
    DIRECT = 0
    REVERSE = 1

    def __init__(self):
        super(rsi_threshold_strategy, self).__init__()
        self._rsi_period = self.Param("RsiPeriod", 14)
        self._high_level = self.Param("HighLevel", 70.0)
        self._low_level = self.Param("LowLevel", 30.0)
        self._trend = self.Param("Trend", 0)
        self._buy_open = self.Param("BuyOpen", True)
        self._sell_open = self.Param("SellOpen", True)
        self._buy_close = self.Param("BuyClose", True)
        self._sell_close = self.Param("SellClose", True)
        self._stop_loss = self.Param("StopLoss", 1000.0)
        self._take_profit = self.Param("TakeProfit", 2000.0)
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(1)))
        self._prev_rsi = None

    @property
    def RsiPeriod(self): return self._rsi_period.Value
    @RsiPeriod.setter
    def RsiPeriod(self, v): self._rsi_period.Value = v
    @property
    def HighLevel(self): return self._high_level.Value
    @HighLevel.setter
    def HighLevel(self, v): self._high_level.Value = v
    @property
    def LowLevel(self): return self._low_level.Value
    @LowLevel.setter
    def LowLevel(self, v): self._low_level.Value = v
    @property
    def Trend(self): return self._trend.Value
    @Trend.setter
    def Trend(self, v): self._trend.Value = v
    @property
    def BuyOpen(self): return self._buy_open.Value
    @BuyOpen.setter
    def BuyOpen(self, v): self._buy_open.Value = v
    @property
    def SellOpen(self): return self._sell_open.Value
    @SellOpen.setter
    def SellOpen(self, v): self._sell_open.Value = v
    @property
    def BuyClose(self): return self._buy_close.Value
    @BuyClose.setter
    def BuyClose(self, v): self._buy_close.Value = v
    @property
    def SellClose(self): return self._sell_close.Value
    @SellClose.setter
    def SellClose(self, v): self._sell_close.Value = v
    @property
    def StopLoss(self): return self._stop_loss.Value
    @StopLoss.setter
    def StopLoss(self, v): self._stop_loss.Value = v
    @property
    def TakeProfit(self): return self._take_profit.Value
    @TakeProfit.setter
    def TakeProfit(self, v): self._take_profit.Value = v
    @property
    def CandleType(self): return self._candle_type.Value
    @CandleType.setter
    def CandleType(self, v): self._candle_type.Value = v

    def OnStarted2(self, time):
        super(rsi_threshold_strategy, self).OnStarted2(time)
        self._prev_rsi = None
        rsi = RelativeStrengthIndex()
        rsi.Length = self.RsiPeriod
        sub = self.SubscribeCandles(self.CandleType)
        sub.Bind(rsi, self.ProcessCandle).Start()
        self.StartProtection(
            Unit(float(self.TakeProfit), UnitTypes.Absolute),
            Unit(float(self.StopLoss), UnitTypes.Absolute))

    def ProcessCandle(self, candle, rsi):
        if candle.State != CandleStates.Finished:
            return
        r = float(rsi)
        if self._prev_rsi is None:
            self._prev_rsi = r
            return
        if self.Trend == self.DIRECT:
            if self._prev_rsi <= float(self.LowLevel) and r > float(self.LowLevel):
                if self.SellClose and self.Position < 0:
                    self.BuyMarket()
                if self.BuyOpen and self.Position <= 0:
                    self.BuyMarket()
            if self._prev_rsi >= float(self.HighLevel) and r < float(self.HighLevel):
                if self.BuyClose and self.Position > 0:
                    self.SellMarket()
                if self.SellOpen and self.Position >= 0:
                    self.SellMarket()
        else:
            if self._prev_rsi <= float(self.LowLevel) and r > float(self.LowLevel):
                if self.BuyClose and self.Position > 0:
                    self.SellMarket()
                if self.SellOpen and self.Position >= 0:
                    self.SellMarket()
            if self._prev_rsi >= float(self.HighLevel) and r < float(self.HighLevel):
                if self.SellClose and self.Position < 0:
                    self.BuyMarket()
                if self.BuyOpen and self.Position <= 0:
                    self.BuyMarket()
        self._prev_rsi = r

    def OnReseted(self):
        super(rsi_threshold_strategy, self).OnReseted()
        self._prev_rsi = None

    def CreateClone(self):
        return rsi_threshold_strategy()
