import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import AverageTrueRange
from StockSharp.Algo.Strategies import Strategy

class pinball_machine_random_draw_strategy(Strategy):
    def __init__(self):
        super(pinball_machine_random_draw_strategy, self).__init__()
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))).SetDisplay("Candle Type", "Timeframe", "General")
        self._atr_length = self.Param("AtrLength", 14).SetDisplay("ATR Length", "ATR period for stops", "Indicators")

    @property
    def CandleType(self): return self._candle_type.Value
    @CandleType.setter
    def CandleType(self, value): self._candle_type.Value = value

    def OnReseted(self):
        super(pinball_machine_random_draw_strategy, self).OnReseted()
        self._entry_price = 0
        self._candle_count = 0

    def OnStarted2(self, time):
        super(pinball_machine_random_draw_strategy, self).OnStarted2(time)
        self._entry_price = 0
        self._candle_count = 0

        atr = AverageTrueRange()
        atr.Length = self._atr_length.Value

        sub = self.SubscribeCandles(self.CandleType)
        sub.Bind(atr, self.OnProcess).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, sub)
            self.DrawOwnTrades(area)

    def OnProcess(self, candle, atr_val):
        if candle.State != CandleStates.Finished:
            return
        if atr_val <= 0:
            return

        self._candle_count += 1
        close = candle.ClosePrice

        if self.Position > 0:
            if close >= self._entry_price + atr_val * 2 or close <= self._entry_price - atr_val * 1.5:
                self.SellMarket()
                self._entry_price = 0
        elif self.Position < 0:
            if close <= self._entry_price - atr_val * 2 or close >= self._entry_price + atr_val * 1.5:
                self.BuyMarket()
                self._entry_price = 0

        if self.Position == 0:
            h = int(float(close) * 100) ^ self._candle_count
            mod = abs(h) % 10
            if mod < 3:
                self._entry_price = close
                self.BuyMarket()
            elif mod > 6:
                self._entry_price = close
                self.SellMarket()

    def CreateClone(self):
        return pinball_machine_random_draw_strategy()
