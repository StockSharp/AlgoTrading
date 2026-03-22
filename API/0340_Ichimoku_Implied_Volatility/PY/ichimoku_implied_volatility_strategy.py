import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from System.Collections.Generic import Queue
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import Ichimoku
from StockSharp.Algo.Strategies import Strategy


class ichimoku_implied_volatility_strategy(Strategy):
    """
    Ichimoku with Implied Volatility strategy.
    """

    def __init__(self):
        super(ichimoku_implied_volatility_strategy, self).__init__()

        self._tenkan_period = self.Param("TenkanPeriod", 9) \
            .SetGreaterThanZero() \
            .SetDisplay("Tenkan-Sen Period", "Tenkan-Sen (Conversion Line) period", "Ichimoku Settings")

        self._kijun_period = self.Param("KijunPeriod", 26) \
            .SetGreaterThanZero() \
            .SetDisplay("Kijun-Sen Period", "Kijun-Sen (Base Line) period", "Ichimoku Settings")

        self._senkou_span_b_period = self.Param("SenkouSpanBPeriod", 52) \
            .SetGreaterThanZero() \
            .SetDisplay("Senkou Span B Period", "Senkou Span B (2nd Leading Span) period", "Ichimoku Settings")

        self._iv_period = self.Param("IVPeriod", 20) \
            .SetGreaterThanZero() \
            .SetDisplay("IV Period", "Implied Volatility averaging period", "Volatility Settings")

        self._cooldown_bars = self.Param("CooldownBars", 24) \
            .SetNotNegative() \
            .SetDisplay("Cooldown Bars", "Closed candles to wait before another position change", "General")

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")

        self._iv_history = []
        self._avg_iv = 0.0
        self._iv_sum = 0.0
        self._current_iv = 0.0
        self._prev_price = 0.0
        self._prev_above_kumo = False
        self._prev_tenkan_above_kijun = False
        self._cooldown_remaining = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def GetWorkingSecurities(self):
        return [(self.Security, self.candle_type)]

    def OnReseted(self):
        super(ichimoku_implied_volatility_strategy, self).OnReseted()
        self._iv_history = []
        self._prev_above_kumo = False
        self._prev_tenkan_above_kijun = False
        self._prev_price = 0.0
        self._avg_iv = 0.0
        self._iv_sum = 0.0
        self._current_iv = 0.0
        self._cooldown_remaining = 0

    def OnStarted(self, time):
        super(ichimoku_implied_volatility_strategy, self).OnStarted(time)

        ichimoku = Ichimoku()
        ichimoku.Tenkan.Length = int(self._tenkan_period.Value)
        ichimoku.Kijun.Length = int(self._kijun_period.Value)
        ichimoku.SenkouB.Length = int(self._senkou_span_b_period.Value)

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(ichimoku, self.ProcessCandle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, ichimoku)
            self.DrawOwnTrades(area)

        self.StartProtection(
            Unit(0),
            Unit(0)
        )

    def ProcessCandle(self, candle, ichimoku_value):
        if candle.State != CandleStates.Finished:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        tenkan_val = ichimoku_value.Tenkan
        if tenkan_val is None:
            return
        tenkan = float(tenkan_val)

        kijun_val = ichimoku_value.Kijun
        if kijun_val is None:
            return
        kijun = float(kijun_val)

        senkou_a_val = ichimoku_value.SenkouA
        if senkou_a_val is None:
            return
        senkou_a = float(senkou_a_val)

        senkou_b_val = ichimoku_value.SenkouB
        if senkou_b_val is None:
            return
        senkou_b = float(senkou_b_val)

        kumo_top = max(senkou_a, senkou_b)
        kumo_bottom = min(senkou_a, senkou_b)
        close = float(candle.ClosePrice)
        price_above_kumo = close > kumo_top
        price_below_kumo = close < kumo_bottom

        tenkan_above_kijun = tenkan > kijun

        self.UpdateImpliedVolatility(candle)

        iv_higher = self._current_iv > self._avg_iv

        if self._cooldown_remaining > 0:
            self._cooldown_remaining -= 1

        iv_period = int(self._iv_period.Value)
        cooldown = int(self._cooldown_bars.Value)

        if self._prev_price == 0.0:
            self._prev_price = close
            self._prev_above_kumo = price_above_kumo
            self._prev_tenkan_above_kijun = tenkan_above_kijun
            return

        bullish_setup = price_above_kumo and tenkan_above_kijun and iv_higher
        bearish_setup = price_below_kumo and (not tenkan_above_kijun) and iv_higher
        bullish_transition = bullish_setup and ((not self._prev_above_kumo) or (not self._prev_tenkan_above_kijun))
        bearish_transition = bearish_setup and (self._prev_above_kumo or self._prev_tenkan_above_kijun)

        if self._cooldown_remaining == 0 and bullish_transition and self.Position <= 0:
            vol = self.Volume
            if self.Position < 0:
                vol = self.Volume + Math.Abs(self.Position)
            self.BuyMarket(vol)
            self._cooldown_remaining = cooldown
        elif self._cooldown_remaining == 0 and bearish_transition and self.Position >= 0:
            vol = self.Volume
            if self.Position > 0:
                vol = self.Volume + Math.Abs(self.Position)
            self.SellMarket(vol)
            self._cooldown_remaining = cooldown

        if self.Position > 0 and not price_above_kumo:
            self.SellMarket(Math.Abs(self.Position))
            self._cooldown_remaining = cooldown
        elif self.Position < 0 and not price_below_kumo:
            self.BuyMarket(Math.Abs(self.Position))
            self._cooldown_remaining = cooldown

        self.ApplyKijunAsStop(candle.ClosePrice, kijun)

        self._prev_price = close
        self._prev_above_kumo = price_above_kumo
        self._prev_tenkan_above_kijun = tenkan_above_kijun

    def UpdateImpliedVolatility(self, candle):
        iv = float((candle.HighPrice - candle.LowPrice) / candle.OpenPrice * 100)

        self._current_iv = iv
        self._iv_history.append(iv)
        self._iv_sum += iv

        iv_period = int(self._iv_period.Value)
        if len(self._iv_history) > iv_period:
            self._iv_sum -= self._iv_history.pop(0)

        if len(self._iv_history) > 0:
            self._avg_iv = self._iv_sum / len(self._iv_history)
        else:
            self._avg_iv = 0.0

        self.LogInfo("IV: {0}, Avg IV: {1}".format(iv, self._avg_iv))

    def ApplyKijunAsStop(self, price, kijun):
        if self.Position > 0 and float(price) < kijun:
            self.SellMarket(Math.Abs(self.Position))
        elif self.Position < 0 and float(price) > kijun:
            self.BuyMarket(Math.Abs(self.Position))

    def CreateClone(self):
        return ichimoku_implied_volatility_strategy()
