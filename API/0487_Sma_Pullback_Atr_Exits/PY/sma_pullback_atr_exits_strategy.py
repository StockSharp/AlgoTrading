import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import SimpleMovingAverage, AverageTrueRange
from StockSharp.Algo.Strategies import Strategy


class sma_pullback_atr_exits_strategy(Strategy):
    """SMA Pullback ATR Exits Strategy."""

    def __init__(self):
        super(sma_pullback_atr_exits_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(30))) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")
        self._fast_sma_length = self.Param("FastSmaLength", 8) \
            .SetDisplay("Fast SMA", "Fast SMA length", "Indicators")
        self._slow_sma_length = self.Param("SlowSmaLength", 30) \
            .SetDisplay("Slow SMA", "Slow SMA length", "Indicators")
        self._atr_length = self.Param("AtrLength", 14) \
            .SetDisplay("ATR Length", "ATR calculation length", "Indicators")
        self._atr_multiplier_sl = self.Param("AtrMultiplierSl", 1.2) \
            .SetDisplay("ATR SL Mult", "ATR multiplier for stop-loss", "Risk")
        self._atr_multiplier_tp = self.Param("AtrMultiplierTp", 2.0) \
            .SetDisplay("ATR TP Mult", "ATR multiplier for take-profit", "Risk")
        self._cooldown_bars = self.Param("CooldownBars", 10) \
            .SetDisplay("Cooldown Bars", "Bars between trades", "Risk")

        self._entry_price = 0.0
        self._cooldown_remaining = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(sma_pullback_atr_exits_strategy, self).OnReseted()
        self._entry_price = 0.0
        self._cooldown_remaining = 0

    def OnStarted(self, time):
        super(sma_pullback_atr_exits_strategy, self).OnStarted(time)

        fast_sma = SimpleMovingAverage()
        fast_sma.Length = int(self._fast_sma_length.Value)
        slow_sma = SimpleMovingAverage()
        slow_sma.Length = int(self._slow_sma_length.Value)
        atr = AverageTrueRange()
        atr.Length = int(self._atr_length.Value)

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(fast_sma, slow_sma, atr, self._on_process).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, fast_sma)
            self.DrawIndicator(area, slow_sma)
            self.DrawOwnTrades(area)

    def _on_process(self, candle, fast_sma_value, slow_sma_value, atr_value):
        if candle.State != CandleStates.Finished:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        atr_v = float(atr_value)
        atr_sl = float(self._atr_multiplier_sl.Value)
        atr_tp = float(self._atr_multiplier_tp.Value)
        cooldown = int(self._cooldown_bars.Value)

        # Check stop/TP exits first
        if self.Position > 0 and self._entry_price > 0:
            stop = self._entry_price - atr_v * atr_sl
            target = self._entry_price + atr_v * atr_tp
            if float(candle.LowPrice) <= stop or float(candle.HighPrice) >= target:
                self.SellMarket(Math.Abs(self.Position))
                self._entry_price = 0.0
                self._cooldown_remaining = cooldown
                return
        elif self.Position < 0 and self._entry_price > 0:
            stop = self._entry_price + atr_v * atr_sl
            target = self._entry_price - atr_v * atr_tp
            if float(candle.HighPrice) >= stop or float(candle.LowPrice) <= target:
                self.BuyMarket(Math.Abs(self.Position))
                self._entry_price = 0.0
                self._cooldown_remaining = cooldown
                return

        if self._cooldown_remaining > 0:
            self._cooldown_remaining -= 1
            return

        close = float(candle.ClosePrice)
        fast_v = float(fast_sma_value)
        slow_v = float(slow_sma_value)

        # Buy: pullback in uptrend
        if close < fast_v and fast_v > slow_v and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket(Math.Abs(self.Position))
            self.BuyMarket(self.Volume)
            self._entry_price = close
            self._cooldown_remaining = cooldown
        # Sell: pullback in downtrend
        elif close > fast_v and fast_v < slow_v and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket(Math.Abs(self.Position))
            self.SellMarket(self.Volume)
            self._entry_price = close
            self._cooldown_remaining = cooldown

    def CreateClone(self):
        return sma_pullback_atr_exits_strategy()
