import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import RelativeStrengthIndex, ExponentialMovingAverage, AverageTrueRange
from StockSharp.Algo.Strategies import Strategy

class vortex_oscillator_system_momentum_strategy(Strategy):
    """RSI momentum breakout with EMA filter and ATR-based stop/take profit."""
    def __init__(self):
        super(vortex_oscillator_system_momentum_strategy, self).__init__()
        self._rsi_len = self.Param("RsiLength", 14).SetDisplay("RSI Length", "RSI period", "Indicators")
        self._ema_len = self.Param("EmaLength", 50).SetDisplay("EMA Length", "Trend filter", "Indicators")
        self._atr_len = self.Param("AtrLength", 14).SetDisplay("ATR Length", "ATR period", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))).SetDisplay("Candle Type", "Timeframe", "General")

    @property
    def CandleType(self): return self._candle_type.Value
    @CandleType.setter
    def CandleType(self, value): self._candle_type.Value = value

    def OnReseted(self):
        super(vortex_oscillator_system_momentum_strategy, self).OnReseted()
        self._prev_rsi = 0
        self._entry_price = 0

    def OnStarted2(self, time):
        super(vortex_oscillator_system_momentum_strategy, self).OnStarted2(time)
        self._prev_rsi = 0
        self._entry_price = 0

        rsi = RelativeStrengthIndex()
        rsi.Length = self._rsi_len.Value
        ema = ExponentialMovingAverage()
        ema.Length = self._ema_len.Value
        atr = AverageTrueRange()
        atr.Length = self._atr_len.Value

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

        rv = float(rsi_val)
        ev = float(ema_val)
        av = float(atr_val)
        close = float(candle.ClosePrice)

        if self._prev_rsi == 0 or av <= 0:
            self._prev_rsi = rv
            return

        # Exit conditions
        if self.Position > 0:
            if close >= self._entry_price + av * 2.5 or close <= self._entry_price - av * 1.5 or rv < 40:
                self.SellMarket()
                self._entry_price = 0
        elif self.Position < 0:
            if close <= self._entry_price - av * 2.5 or close >= self._entry_price + av * 1.5 or rv > 60:
                self.BuyMarket()
                self._entry_price = 0

        # Entry conditions
        if self.Position == 0:
            if rv > 55 and self._prev_rsi <= 55 and close > ev:
                self._entry_price = close
                self.BuyMarket()
            elif rv < 45 and self._prev_rsi >= 45 and close < ev:
                self._entry_price = close
                self.SellMarket()

        self._prev_rsi = rv

    def CreateClone(self):
        return vortex_oscillator_system_momentum_strategy()
