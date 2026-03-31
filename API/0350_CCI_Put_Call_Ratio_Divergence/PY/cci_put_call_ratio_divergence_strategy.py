import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import CommodityChannelIndex, AverageTrueRange
from StockSharp.Algo.Strategies import Strategy


class cci_put_call_ratio_divergence_strategy(Strategy):
    """CCI reversal strategy filtered by deterministic put/call ratio divergence."""

    def __init__(self):
        super(cci_put_call_ratio_divergence_strategy, self).__init__()

        self._cci_period = self.Param("CciPeriod", 20) \
            .SetRange(10, 50) \
            .SetDisplay("CCI Period", "Period for CCI calculation", "Indicators")

        self._atr_multiplier = self.Param("AtrMultiplier", 2.0) \
            .SetRange(1.0, 5.0) \
            .SetDisplay("ATR Multiplier", "Multiplier for ATR-based stop loss", "Risk Management")

        self._cooldown_bars = self.Param("CooldownBars", 24) \
            .SetNotNegative() \
            .SetDisplay("Cooldown Bars", "Closed candles to wait before another position change", "General")

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")

        self._cci = None
        self._atr = None
        self._prev_pcr = 0.0
        self._current_pcr = 0.0
        self._prev_price = 0.0
        self._prev_cci = None
        self._cooldown_remaining = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def GetWorkingSecurities(self):
        return [(self.Security, self.candle_type)]

    def OnReseted(self):
        super(cci_put_call_ratio_divergence_strategy, self).OnReseted()
        self._cci = None
        self._atr = None
        self._prev_pcr = 0.0
        self._current_pcr = 0.0
        self._prev_price = 0.0
        self._prev_cci = None
        self._cooldown_remaining = 0

    def OnStarted2(self, time):
        super(cci_put_call_ratio_divergence_strategy, self).OnStarted2(time)

        self._cci = CommodityChannelIndex()
        self._cci.Length = int(self._cci_period.Value)

        self._atr = AverageTrueRange()
        self._atr.Length = 14

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self._cci, self._atr, self.ProcessCandle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, self._cci)
            self.DrawIndicator(area, self._atr)
            self.DrawOwnTrades(area)

        self.StartProtection(
            Unit(2, UnitTypes.Percent),
            Unit(2, UnitTypes.Percent)
        )

    def ProcessCandle(self, candle, cci, atr):
        if candle.State != CandleStates.Finished:
            return

        price = float(candle.ClosePrice)
        cci_val = float(cci)
        self.UpdatePutCallRatio(candle)

        if self._prev_price == 0.0:
            self._prev_price = price
            self._prev_pcr = self._current_pcr
            self._prev_cci = cci_val
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            self._prev_price = price
            self._prev_pcr = self._current_pcr
            self._prev_cci = cci_val
            return

        if self._cooldown_remaining > 0:
            self._cooldown_remaining -= 1

        cooldown = int(self._cooldown_bars.Value)

        bullish_divergence = price < self._prev_price and self._current_pcr > self._prev_pcr
        bearish_divergence = price > self._prev_price and self._current_pcr < self._prev_pcr
        oversold_cross = self._prev_cci is not None and self._prev_cci >= -100.0 and cci_val < -100.0
        overbought_cross = self._prev_cci is not None and self._prev_cci <= 100.0 and cci_val > 100.0

        if self._cooldown_remaining == 0 and oversold_cross and bullish_divergence and self.Position <= 0:
            vol = self.Volume
            if self.Position < 0:
                vol = self.Volume + Math.Abs(self.Position)
            self.BuyMarket(vol)
            self._cooldown_remaining = cooldown
        elif self._cooldown_remaining == 0 and overbought_cross and bearish_divergence and self.Position >= 0:
            vol = self.Volume
            if self.Position > 0:
                vol = self.Volume + Math.Abs(self.Position)
            self.SellMarket(vol)
            self._cooldown_remaining = cooldown
        elif self.Position > 0 and cci_val >= 20.0:
            self.SellMarket(self.Position)
            self._cooldown_remaining = cooldown
        elif self.Position < 0 and cci_val <= -20.0:
            self.BuyMarket(Math.Abs(self.Position))
            self._cooldown_remaining = cooldown

        self._prev_price = price
        self._prev_pcr = self._current_pcr
        self._prev_cci = cci_val

    def UpdatePutCallRatio(self, candle):
        price_change = float((candle.ClosePrice - candle.OpenPrice) / max(float(candle.OpenPrice), 1.0))
        range_val = float((candle.HighPrice - candle.LowPrice) / max(float(candle.OpenPrice), 1.0))
        skew = min(0.2, range_val * 5.0)

        if price_change >= 0:
            self._current_pcr = 0.8 - price_change + skew
        else:
            self._current_pcr = 1.1 + abs(price_change) + skew

        self._current_pcr = max(0.5, min(2.0, self._current_pcr))

    def CreateClone(self):
        return cci_put_call_ratio_divergence_strategy()
