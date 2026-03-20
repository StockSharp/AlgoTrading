import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import SimpleMovingAverage, AverageTrueRange
from StockSharp.Algo.Strategies import Strategy

class sma_pullback_atr_exits_strategy(Strategy):
    def __init__(self):
        super(sma_pullback_atr_exits_strategy, self).__init__()
        self._fast_sma = self.Param("FastSmaLength", 8).SetGreaterThanZero().SetDisplay("Fast SMA", "Fast SMA length", "Indicators")
        self._slow_sma = self.Param("SlowSmaLength", 30).SetGreaterThanZero().SetDisplay("Slow SMA", "Slow SMA length", "Indicators")
        self._atr_length = self.Param("AtrLength", 14).SetGreaterThanZero().SetDisplay("ATR Length", "ATR length", "Indicators")
        self._atr_sl = self.Param("AtrMultiplierSl", 1.2).SetDisplay("ATR SL Mult", "ATR multiplier for SL", "Risk")
        self._atr_tp = self.Param("AtrMultiplierTp", 2.0).SetDisplay("ATR TP Mult", "ATR multiplier for TP", "Risk")
        self._cooldown_bars = self.Param("CooldownBars", 10).SetDisplay("Cooldown Bars", "Bars between trades", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(30))).SetDisplay("Candle Type", "Candle type", "General")

    @property
    def CandleType(self): return self._candle_type.Value
    @CandleType.setter
    def CandleType(self, value): self._candle_type.Value = value

    def OnReseted(self):
        super(sma_pullback_atr_exits_strategy, self).OnReseted()
        self._entry_price = 0
        self._cooldown_remaining = 0

    def OnStarted(self, time):
        super(sma_pullback_atr_exits_strategy, self).OnStarted(time)
        self._entry_price = 0
        self._cooldown_remaining = 0

        fast = SimpleMovingAverage()
        fast.Length = self._fast_sma.Value
        slow = SimpleMovingAverage()
        slow.Length = self._slow_sma.Value
        atr = AverageTrueRange()
        atr.Length = self._atr_length.Value

        sub = self.SubscribeCandles(self.CandleType)
        sub.Bind(fast, slow, atr, self.OnProcess).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, sub)
            self.DrawIndicator(area, fast)
            self.DrawIndicator(area, slow)
            self.DrawOwnTrades(area)

    def OnProcess(self, candle, fast_val, slow_val, atr_val):
        if candle.State != CandleStates.Finished:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        close = float(candle.ClosePrice)

        # SL/TP exits
        if self.Position > 0 and self._entry_price > 0:
            sl = self._entry_price - atr_val * self._atr_sl.Value
            tp = self._entry_price + atr_val * self._atr_tp.Value
            if float(candle.LowPrice) <= sl or float(candle.HighPrice) >= tp:
                self.SellMarket()
                self._entry_price = 0
                self._cooldown_remaining = self._cooldown_bars.Value
                return
        elif self.Position < 0 and self._entry_price > 0:
            sl = self._entry_price + atr_val * self._atr_sl.Value
            tp = self._entry_price - atr_val * self._atr_tp.Value
            if float(candle.HighPrice) >= sl or float(candle.LowPrice) <= tp:
                self.BuyMarket()
                self._entry_price = 0
                self._cooldown_remaining = self._cooldown_bars.Value
                return

        if self._cooldown_remaining > 0:
            self._cooldown_remaining -= 1
            return

        # Buy: pullback in uptrend
        if close < fast_val and fast_val > slow_val and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket()
            self.BuyMarket()
            self._entry_price = close
            self._cooldown_remaining = self._cooldown_bars.Value
        # Sell: pullback in downtrend
        elif close > fast_val and fast_val < slow_val and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket()
            self.SellMarket()
            self._entry_price = close
            self._cooldown_remaining = self._cooldown_bars.Value

    def CreateClone(self):
        return sma_pullback_atr_exits_strategy()
