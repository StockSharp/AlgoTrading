import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage, SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *
from indicator_extensions import *

class ai_volume_strategy(Strategy):
    """
    AI Volume Strategy - trades volume spikes in trend direction.
    Uses EMA for trend and volume SMA for spike detection.
    """

    def __init__(self):
        super(ai_volume_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", tf(30)) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")

        self._price_ema_length = self.Param("PriceEmaLength", 20) \
            .SetGreaterThanZero() \
            .SetDisplay("Price EMA Length", "Length for price EMA", "Parameters")

        self._volume_ema_length = self.Param("VolumeEmaLength", 20) \
            .SetGreaterThanZero() \
            .SetDisplay("Volume EMA Length", "Length for volume EMA", "Parameters")

        self._volume_multiplier = self.Param("VolumeMultiplier", 1.0) \
            .SetDisplay("Volume Multiplier", "Multiplier for volume spike detection", "Parameters")

        self._exit_bars = self.Param("ExitBars", 20) \
            .SetGreaterThanZero() \
            .SetDisplay("Exit Bars", "Exit position after this many bars", "Risk")

        self._cooldown_bars = self.Param("CooldownBars", 15) \
            .SetDisplay("Cooldown Bars", "Bars between trades", "Risk")

        self._volume_sma = None
        self._bars_in_position = 0
        self._cooldown_remaining = 0

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    @property
    def PriceEmaLength(self):
        return self._price_ema_length.Value

    @PriceEmaLength.setter
    def PriceEmaLength(self, value):
        self._price_ema_length.Value = value

    @property
    def VolumeEmaLength(self):
        return self._volume_ema_length.Value

    @VolumeEmaLength.setter
    def VolumeEmaLength(self, value):
        self._volume_ema_length.Value = value

    @property
    def VolumeMultiplier(self):
        return self._volume_multiplier.Value

    @VolumeMultiplier.setter
    def VolumeMultiplier(self, value):
        self._volume_multiplier.Value = value

    @property
    def ExitBars(self):
        return self._exit_bars.Value

    @ExitBars.setter
    def ExitBars(self, value):
        self._exit_bars.Value = value

    @property
    def CooldownBars(self):
        return self._cooldown_bars.Value

    @CooldownBars.setter
    def CooldownBars(self, value):
        self._cooldown_bars.Value = value

    def OnReseted(self):
        super(ai_volume_strategy, self).OnReseted()
        self._volume_sma = None
        self._bars_in_position = 0
        self._cooldown_remaining = 0

    def OnStarted(self, time):
        super(ai_volume_strategy, self).OnStarted(time)

        price_ema = ExponentialMovingAverage()
        price_ema.Length = self.PriceEmaLength
        self._volume_sma = SimpleMovingAverage()
        self._volume_sma.Length = self.VolumeEmaLength

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(price_ema, self.ProcessCandle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, price_ema)
            self.DrawOwnTrades(area)

    def ProcessCandle(self, candle, price_ema_value):
        if candle.State != CandleStates.Finished:
            return

        volume_result = process_float(self._volume_sma, float(candle.TotalVolume), candle.ServerTime, candle.State == CandleStates.Finished)

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        # Time-based exit
        if self.Position != 0:
            self._bars_in_position += 1
            if self._bars_in_position >= self.ExitBars:
                if self.Position > 0:
                    self.SellMarket(Math.Abs(self.Position))
                else:
                    self.BuyMarket(Math.Abs(self.Position))
                self._bars_in_position = 0
                self._cooldown_remaining = self.CooldownBars
                return

        if self._cooldown_remaining > 0:
            self._cooldown_remaining -= 1
            return

        avg_volume = float(volume_result) if self._volume_sma.IsFormed else 0.0
        volume_spike = avg_volume > 0 and float(candle.TotalVolume) > avg_volume * self.VolumeMultiplier
        use_volume_filter = avg_volume > 0

        trend_up = float(candle.ClosePrice) > price_ema_value
        trend_down = float(candle.ClosePrice) < price_ema_value
        is_bullish = float(candle.ClosePrice) > float(candle.OpenPrice)
        is_bearish = float(candle.ClosePrice) < float(candle.OpenPrice)

        long_ok = trend_up and is_bullish and (not use_volume_filter or volume_spike)
        short_ok = trend_down and is_bearish and (not use_volume_filter or volume_spike)

        if long_ok and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket(Math.Abs(self.Position))
            self.BuyMarket(self.Volume)
            self._bars_in_position = 0
            self._cooldown_remaining = self.CooldownBars
        elif short_ok and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket(Math.Abs(self.Position))
            self.SellMarket(self.Volume)
            self._bars_in_position = 0
            self._cooldown_remaining = self.CooldownBars

    def CreateClone(self):
        """!! REQUIRED!! Creates a new instance of the strategy."""
        return ai_volume_strategy()
