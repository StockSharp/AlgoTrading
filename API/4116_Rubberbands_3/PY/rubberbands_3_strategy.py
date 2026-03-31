import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import SimpleMovingAverage, AverageTrueRange
from StockSharp.Algo.Strategies import Strategy

class rubberbands_3_strategy(Strategy):
    def __init__(self):
        super(rubberbands_3_strategy, self).__init__()
        self._sma_length = self.Param("SmaLength", 20).SetDisplay("SMA Length", "SMA period", "Indicators")
        self._atr_length = self.Param("AtrLength", 14).SetDisplay("ATR Length", "ATR period", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(15))).SetDisplay("Candle Type", "Timeframe", "General")

    @property
    def CandleType(self): return self._candle_type.Value
    @CandleType.setter
    def CandleType(self, value): self._candle_type.Value = value

    def OnReseted(self):
        super(rubberbands_3_strategy, self).OnReseted()
        self._entry_price = 0
        self._grid_count = 0

    def OnStarted2(self, time):
        super(rubberbands_3_strategy, self).OnStarted2(time)
        self._entry_price = 0
        self._grid_count = 0

        sma = SimpleMovingAverage()
        sma.Length = self._sma_length.Value
        atr = AverageTrueRange()
        atr.Length = self._atr_length.Value

        sub = self.SubscribeCandles(self.CandleType)
        sub.Bind(sma, atr, self.OnProcess).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, sub)
            self.DrawIndicator(area, sma)
            self.DrawOwnTrades(area)

    def OnProcess(self, candle, sma_val, atr_val):
        if candle.State != CandleStates.Finished:
            return

        if atr_val <= 0:
            return

        close = float(candle.ClosePrice)
        upper = sma_val + atr_val * 1.5
        lower = sma_val - atr_val * 1.5

        if self.Position > 0:
            if close >= sma_val:
                self.SellMarket()
                self._entry_price = 0
                self._grid_count = 0
            elif close <= self._entry_price - atr_val * 5.0:
                self.SellMarket()
                self._entry_price = 0
                self._grid_count = 0
            elif self._grid_count < 4 and close <= self._entry_price - atr_val * 0.8:
                self._entry_price = (self._entry_price + close) / 2.0
                self._grid_count += 1
                self.BuyMarket()
        elif self.Position < 0:
            if close <= sma_val:
                self.BuyMarket()
                self._entry_price = 0
                self._grid_count = 0
            elif close >= self._entry_price + atr_val * 5.0:
                self.BuyMarket()
                self._entry_price = 0
                self._grid_count = 0
            elif self._grid_count < 4 and close >= self._entry_price + atr_val * 0.8:
                self._entry_price = (self._entry_price + close) / 2.0
                self._grid_count += 1
                self.SellMarket()

        if self.Position == 0:
            if close <= lower:
                self._entry_price = close
                self._grid_count = 0
                self.BuyMarket()
            elif close >= upper:
                self._entry_price = close
                self._grid_count = 0
                self.SellMarket()

    def CreateClone(self):
        return rubberbands_3_strategy()
