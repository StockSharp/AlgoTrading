import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import RelativeStrengthIndex, ExponentialMovingAverage, AverageTrueRange
from StockSharp.Algo.Strategies import Strategy

class six_indicators_momentum_strategy(Strategy):
    def __init__(self):
        super(six_indicators_momentum_strategy, self).__init__()
        self._rsi_length = self.Param("RsiLength", 14).SetDisplay("RSI Length", "RSI period", "Indicators")
        self._ema_length = self.Param("EmaLength", 30).SetDisplay("EMA Length", "Trend filter", "Indicators")
        self._atr_length = self.Param("AtrLength", 14).SetDisplay("ATR Length", "ATR period", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))).SetDisplay("Candle Type", "Timeframe", "General")

    @property
    def CandleType(self): return self._candle_type.Value
    @CandleType.setter
    def CandleType(self, value): self._candle_type.Value = value

    def OnReseted(self):
        super(six_indicators_momentum_strategy, self).OnReseted()
        self._prev_rsi = 0
        self._entry_price = 0

    def OnStarted2(self, time):
        super(six_indicators_momentum_strategy, self).OnStarted2(time)
        self._prev_rsi = 0
        self._entry_price = 0

        rsi = RelativeStrengthIndex()
        rsi.Length = self._rsi_length.Value
        ema = ExponentialMovingAverage()
        ema.Length = self._ema_length.Value
        atr = AverageTrueRange()
        atr.Length = self._atr_length.Value

        sub = self.SubscribeCandles(self.CandleType)
        sub.Bind(rsi, ema, atr, self.OnProcess).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, sub)
            self.DrawIndicator(area, ema)
            self.DrawOwnTrades(area)

    def OnProcess(self, candle, rsi_val, ema_val, atr_val):
        if candle.State != CandleStates.Finished:
            return

        if self._prev_rsi == 0 or atr_val <= 0:
            self._prev_rsi = rsi_val
            return

        close = float(candle.ClosePrice)

        if self.Position > 0:
            if close >= self._entry_price + atr_val * 2.5 or close <= self._entry_price - atr_val * 1.5 or rsi_val > 70:
                self.SellMarket()
                self._entry_price = 0
        elif self.Position < 0:
            if close <= self._entry_price - atr_val * 2.5 or close >= self._entry_price + atr_val * 1.5 or rsi_val < 30:
                self.BuyMarket()
                self._entry_price = 0

        if self.Position == 0:
            if rsi_val > 55 and self._prev_rsi <= 55 and close > ema_val:
                self._entry_price = close
                self.BuyMarket()
            elif rsi_val < 45 and self._prev_rsi >= 45 and close < ema_val:
                self._entry_price = close
                self.SellMarket()

        self._prev_rsi = rsi_val

    def CreateClone(self):
        return six_indicators_momentum_strategy()
