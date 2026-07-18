import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy


class full_candle_strategy(Strategy):
    """Full Candle Strategy. Trades on full body candles with EMA trend filter."""

    def __init__(self):
        super(full_candle_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(15))) \
            .SetDisplay("Candle type", "Candle type for strategy calculation.", "General")
        self._ema_length = self.Param("EmaLength", 20) \
            .SetDisplay("EMA Length", "EMA period", "Moving Averages")
        self._shadow_percent = self.Param("ShadowPercent", 10.0) \
            .SetDisplay("Shadow Percent", "Maximum shadow percentage of candle range", "Strategy")
        self._cooldown_bars = self.Param("CooldownBars", 15) \
            .SetDisplay("Cooldown Bars", "Bars to wait between trades", "Risk")

        self._ema = None
        self._entry_price = None
        self._cooldown_remaining = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(full_candle_strategy, self).OnReseted()
        self._ema = None
        self._entry_price = None
        self._cooldown_remaining = 0

    def OnStarted2(self, time):
        super(full_candle_strategy, self).OnStarted2(time)

        self._ema = ExponentialMovingAverage()
        self._ema.Length = int(self._ema_length.Value)

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self._ema, self._on_process).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, self._ema)
            self.DrawOwnTrades(area)

    def _on_process(self, candle, ema_val):
        if candle.State != CandleStates.Finished:
            return

        if not self._ema.IsFormed:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        if self._cooldown_remaining > 0:
            self._cooldown_remaining -= 1
            return

        close = float(candle.ClosePrice)
        opn = float(candle.OpenPrice)
        high = float(candle.HighPrice)
        low = float(candle.LowPrice)
        ev = float(ema_val)
        cooldown = int(self._cooldown_bars.Value)
        shadow_pct_threshold = float(self._shadow_percent.Value)

        candle_size = high - low
        if candle_size <= 0:
            return

        body_size = abs(close - opn)

        if close > opn:
            upper_shadow = high - close
            lower_shadow = opn - low
        else:
            upper_shadow = high - opn
            lower_shadow = close - low

        total_shadow_pct = ((upper_shadow + lower_shadow) * 100.0) / candle_size
        is_full_candle = total_shadow_pct <= shadow_pct_threshold and body_size > 0

        # Exit conditions
        if self.Position > 0 and self._entry_price is not None and close > self._entry_price * 1.003:
            self.SellMarket(Math.Abs(self.Position))
            self._entry_price = None
            self._cooldown_remaining = cooldown
            return
        elif self.Position < 0 and self._entry_price is not None and close < self._entry_price * 0.997:
            self.BuyMarket(Math.Abs(self.Position))
            self._entry_price = None
            self._cooldown_remaining = cooldown
            return

        # Entry: full bullish candle above EMA
        if is_full_candle and close > opn and close > ev and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket(Math.Abs(self.Position))
            self.BuyMarket(self.Volume)
            self._entry_price = close
            self._cooldown_remaining = cooldown
        # Entry: full bearish candle below EMA
        elif is_full_candle and close < opn and close < ev and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket(Math.Abs(self.Position))
            self.SellMarket(self.Volume)
            self._entry_price = close
            self._cooldown_remaining = cooldown

    def CreateClone(self):
        return full_candle_strategy()
