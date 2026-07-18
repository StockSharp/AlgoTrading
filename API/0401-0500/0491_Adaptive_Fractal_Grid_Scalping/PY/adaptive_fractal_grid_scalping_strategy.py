import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import AverageTrueRange, SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy


class adaptive_fractal_grid_scalping_strategy(Strategy):
    """Adaptive Fractal Grid Scalping Strategy."""

    def __init__(self):
        super(adaptive_fractal_grid_scalping_strategy, self).__init__()

        self._atr_length = self.Param("AtrLength", 14) \
            .SetDisplay("ATR Length", "ATR period", "Parameters")
        self._sma_length = self.Param("SmaLength", 50) \
            .SetDisplay("SMA Length", "SMA period", "Parameters")
        self._stop_multiplier = self.Param("StopMultiplier", 2.0) \
            .SetDisplay("Stop Multiplier", "ATR multiplier for stop/TP", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(30))) \
            .SetDisplay("Candle Type", "Type of candles", "Data")
        self._cooldown_bars = self.Param("CooldownBars", 10) \
            .SetDisplay("Cooldown Bars", "Bars between trades", "Risk")

        self._h1 = 0.0
        self._h2 = 0.0
        self._h3 = 0.0
        self._h4 = 0.0
        self._h5 = 0.0
        self._l1 = 0.0
        self._l2 = 0.0
        self._l3 = 0.0
        self._l4 = 0.0
        self._l5 = 0.0
        self._fractal_high = None
        self._fractal_low = None
        self._entry_price = 0.0
        self._cooldown_remaining = 0
        self._bar_count = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(adaptive_fractal_grid_scalping_strategy, self).OnReseted()
        self._h1 = self._h2 = self._h3 = self._h4 = self._h5 = 0.0
        self._l1 = self._l2 = self._l3 = self._l4 = self._l5 = 0.0
        self._fractal_high = None
        self._fractal_low = None
        self._entry_price = 0.0
        self._cooldown_remaining = 0
        self._bar_count = 0

    def OnStarted2(self, time):
        super(adaptive_fractal_grid_scalping_strategy, self).OnStarted2(time)

        atr = AverageTrueRange()
        atr.Length = int(self._atr_length.Value)
        sma = SimpleMovingAverage()
        sma.Length = int(self._sma_length.Value)

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(atr, sma, self._on_process).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, sma)
            self.DrawOwnTrades(area)

    def _on_process(self, candle, atr_value, sma_value):
        if candle.State != CandleStates.Finished:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        self._bar_count += 1

        # Update fractal buffers
        self._h1 = self._h2
        self._h2 = self._h3
        self._h3 = self._h4
        self._h4 = self._h5
        self._h5 = float(candle.HighPrice)
        self._l1 = self._l2
        self._l2 = self._l3
        self._l3 = self._l4
        self._l4 = self._l5
        self._l5 = float(candle.LowPrice)

        if self._bar_count < 5:
            return

        # Detect fractals
        if self._h3 > self._h1 and self._h3 > self._h2 and self._h3 > self._h4 and self._h3 > self._h5:
            self._fractal_high = self._h3
        if self._l3 < self._l1 and self._l3 < self._l2 and self._l3 < self._l4 and self._l3 < self._l5:
            self._fractal_low = self._l3

        if self._cooldown_remaining > 0:
            self._cooldown_remaining -= 1
            return

        atr_v = float(atr_value)
        sma_v = float(sma_value)
        close = float(candle.ClosePrice)
        stop_mult = float(self._stop_multiplier.Value)
        cooldown = int(self._cooldown_bars.Value)

        # Exit on ATR-based stop/TP
        if self.Position > 0 and self._entry_price > 0:
            stop_loss = self._entry_price - atr_v * stop_mult
            take_profit = self._entry_price + atr_v * stop_mult * 2
            if close <= stop_loss or close >= take_profit:
                self.SellMarket(Math.Abs(self.Position))
                self._cooldown_remaining = cooldown
                self._entry_price = 0.0
                return
        elif self.Position < 0 and self._entry_price > 0:
            stop_loss = self._entry_price + atr_v * stop_mult
            take_profit = self._entry_price - atr_v * stop_mult * 2
            if close >= stop_loss or close <= take_profit:
                self.BuyMarket(Math.Abs(self.Position))
                self._cooldown_remaining = cooldown
                self._entry_price = 0.0
                return

        # Entry signals
        is_bullish = close > sma_v
        is_bearish = close < sma_v

        if is_bullish and self._fractal_high is not None and close > self._fractal_high and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket(Math.Abs(self.Position))
            self.BuyMarket(self.Volume)
            self._entry_price = close
            self._cooldown_remaining = cooldown
        elif is_bearish and self._fractal_low is not None and close < self._fractal_low and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket(Math.Abs(self.Position))
            self.SellMarket(self.Volume)
            self._entry_price = close
            self._cooldown_remaining = cooldown

    def CreateClone(self):
        return adaptive_fractal_grid_scalping_strategy()
