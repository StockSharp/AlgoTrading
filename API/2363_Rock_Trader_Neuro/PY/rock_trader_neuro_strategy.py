import clr
import math

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import BollingerBands
from StockSharp.Algo.Strategies import Strategy

class rock_trader_neuro_strategy(Strategy):
    def __init__(self):
        super(rock_trader_neuro_strategy, self).__init__()
        self._sl = self.Param("StopLoss", 30.0).SetDisplay("Stop Loss", "SL in price units", "Risk")
        self._tp = self.Param("TakeProfit", 100.0).SetDisplay("Take Profit", "TP in price units", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))).SetDisplay("Candle Type", "Candle type", "General")

    @property
    def CandleType(self): return self._candle_type.Value
    @CandleType.setter
    def CandleType(self, value): self._candle_type.Value = value

    def OnReseted(self):
        super(rock_trader_neuro_strategy, self).OnReseted()
        self._bands = [0.0] * 7
        self._entry_price = 0
        self._stop_price = 0
        self._take_price = 0

    def OnStarted2(self, time):
        super(rock_trader_neuro_strategy, self).OnStarted2(time)
        self._bands = [0.0] * 7
        self._entry_price = 0
        self._stop_price = 0
        self._take_price = 0

        bb = BollingerBands()
        bb.Length = 20
        bb.Width = 2

        sub = self.SubscribeCandles(self.CandleType)
        sub.BindEx(bb, self.OnProcess).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, sub)
            self.DrawOwnTrades(area)

    def OnProcess(self, candle, bb_val):
        if candle.State != CandleStates.Finished:
            return

        upper = bb_val.UpBand
        lower = bb_val.LowBand
        middle = bb_val.MovingAverage

        if upper is None or lower is None or middle is None:
            return
        if middle == 0:
            return

        band_width = float((upper - lower) / middle)

        # Shift previous values
        for i in range(6, 0, -1):
            self._bands[i] = self._bands[i - 1]
        self._bands[0] = band_width

        if self._bands[6] == 0:
            return

        mn = min(self._bands)
        mx = max(self._bands)
        if mx == mn:
            return

        def normalize(x):
            return (x - mn) * 2.0 / (mx - mn) - 1.0

        n = [normalize(b) for b in self._bands]

        # Weighted sum
        weights = [0.8, -0.9, 0.7, 0.9, -1.0, 0.5, 0.0]
        net = sum(n[i] * weights[i] for i in range(7))

        # Tanh activation
        d = max(-20.0, min(20.0, net * 2.0))
        e_pos = math.exp(d)
        e_neg = math.exp(-d)
        output = (e_pos - e_neg) / (e_pos + e_neg)

        close = candle.ClosePrice

        if self.Position > 0:
            if candle.LowPrice <= self._stop_price or candle.HighPrice >= self._take_price:
                self.SellMarket()
        elif self.Position < 0:
            if candle.HighPrice >= self._stop_price or candle.LowPrice <= self._take_price:
                self.BuyMarket()
        else:
            if output < -0.5:
                self.BuyMarket()
                self._entry_price = close
                self._stop_price = self._entry_price - self._sl.Value
                self._take_price = self._entry_price + self._tp.Value
            elif output > 0.5:
                self.SellMarket()
                self._entry_price = close
                self._stop_price = self._entry_price + self._sl.Value
                self._take_price = self._entry_price - self._tp.Value

    def CreateClone(self):
        return rock_trader_neuro_strategy()
