import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import SimpleMovingAverage, ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy


class pin_bar_magic_strategy(Strategy):
    """Pin Bar Magic Strategy."""

    def __init__(self):
        super(pin_bar_magic_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(30))) \
            .SetDisplay("Candle type", "Candle type for strategy calculation.", "General")
        self._slow_sma_length = self.Param("SlowSmaLength", 50) \
            .SetDisplay("Slow SMA Period", "Slow SMA period", "Indicators")
        self._medium_ema_length = self.Param("MediumEmaLength", 18) \
            .SetDisplay("Medium EMA Period", "Medium EMA period", "Indicators")
        self._fast_ema_length = self.Param("FastEmaLength", 6) \
            .SetDisplay("Fast EMA Period", "Fast EMA period", "Indicators")
        self._cooldown_bars = self.Param("CooldownBars", 10) \
            .SetDisplay("Cooldown Bars", "Bars to wait between trades", "Risk")

        self._slow_sma = None
        self._medium_ema = None
        self._fast_ema = None
        self._cooldown_remaining = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(pin_bar_magic_strategy, self).OnReseted()
        self._slow_sma = None
        self._medium_ema = None
        self._fast_ema = None
        self._cooldown_remaining = 0

    def OnStarted2(self, time):
        super(pin_bar_magic_strategy, self).OnStarted2(time)

        self._slow_sma = SimpleMovingAverage()
        self._slow_sma.Length = int(self._slow_sma_length.Value)

        self._medium_ema = ExponentialMovingAverage()
        self._medium_ema.Length = int(self._medium_ema_length.Value)

        self._fast_ema = ExponentialMovingAverage()
        self._fast_ema.Length = int(self._fast_ema_length.Value)

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self._slow_sma, self._medium_ema, self._fast_ema, self._on_process).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, self._slow_sma)
            self.DrawIndicator(area, self._medium_ema)
            self.DrawIndicator(area, self._fast_ema)
            self.DrawOwnTrades(area)

    def _on_process(self, candle, slow_sma, medium_ema, fast_ema):
        if candle.State != CandleStates.Finished:
            return

        if not self._slow_sma.IsFormed or not self._medium_ema.IsFormed or not self._fast_ema.IsFormed:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        if self._cooldown_remaining > 0:
            self._cooldown_remaining -= 1
            return

        candle_range = float(candle.HighPrice - candle.LowPrice)
        if candle_range == 0:
            return

        bullish_pin_bar = False
        bearish_pin_bar = False

        if float(candle.ClosePrice) > float(candle.OpenPrice):
            lower_wick = float(candle.OpenPrice - candle.LowPrice)
            bullish_pin_bar = lower_wick > 0.60 * candle_range

            upper_wick = float(candle.HighPrice - candle.ClosePrice)
            bearish_pin_bar = upper_wick > 0.60 * candle_range
        else:
            lower_wick = float(candle.ClosePrice - candle.LowPrice)
            bullish_pin_bar = lower_wick > 0.60 * candle_range

            upper_wick = float(candle.HighPrice - candle.OpenPrice)
            bearish_pin_bar = upper_wick > 0.60 * candle_range

        slow_sma_val = float(slow_sma)
        medium_ema_val = float(medium_ema)
        fast_ema_val = float(fast_ema)

        fan_up_trend = fast_ema_val > medium_ema_val and medium_ema_val > slow_sma_val
        fan_dn_trend = fast_ema_val < medium_ema_val and medium_ema_val < slow_sma_val

        close = float(candle.ClosePrice)
        low = float(candle.LowPrice)
        high = float(candle.HighPrice)

        bull_pierce = (low < fast_ema_val and close > fast_ema_val) or \
                      (low < medium_ema_val and close > medium_ema_val) or \
                      (low < slow_sma_val and close > slow_sma_val)

        bear_pierce = (high > fast_ema_val and close < fast_ema_val) or \
                      (high > medium_ema_val and close < medium_ema_val) or \
                      (high > slow_sma_val and close < slow_sma_val)

        cooldown = int(self._cooldown_bars.Value)

        if fan_up_trend and bullish_pin_bar and bull_pierce and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket(Math.Abs(self.Position))
            self.BuyMarket(self.Volume)
            self._cooldown_remaining = cooldown
        elif fan_dn_trend and bearish_pin_bar and bear_pierce and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket(Math.Abs(self.Position))
            self.SellMarket(self.Volume)
            self._cooldown_remaining = cooldown
        elif self.Position > 0 and fast_ema_val < medium_ema_val:
            self.SellMarket(Math.Abs(self.Position))
            self._cooldown_remaining = cooldown
        elif self.Position < 0 and fast_ema_val > medium_ema_val:
            self.BuyMarket(Math.Abs(self.Position))
            self._cooldown_remaining = cooldown

    def CreateClone(self):
        return pin_bar_magic_strategy()
