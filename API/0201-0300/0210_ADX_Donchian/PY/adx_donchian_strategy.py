import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import AverageDirectionalIndex, DonchianChannels
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *

class adx_donchian_strategy(Strategy):
    """Strategy based on ADX and Donchian Channel indicators"""

    def __init__(self):
        super(adx_donchian_strategy, self).__init__()

        self._adx_period = self.Param("AdxPeriod", 14) \
            .SetRange(7, 28) \
            .SetDisplay("ADX Period", "Period for ADX indicator", "Indicators")

        self._donchian_period = self.Param("DonchianPeriod", 5) \
            .SetRange(5, 50) \
            .SetDisplay("Donchian Period", "Period for Donchian Channel", "Indicators")

        self._stop_loss_percent = self.Param("StopLossPercent", 2.0) \
            .SetRange(0.5, 5.0) \
            .SetDisplay("Stop-Loss %", "Stop-loss percentage from entry price", "Risk Management")

        self._candle_type = self.Param("CandleType", tf(15)) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")

        self._adx_threshold = self.Param("AdxThreshold", 10) \
            .SetRange(5, 40) \
            .SetDisplay("ADX Threshold", "ADX value for strong trend detection", "Indicators")

        self._multiplier = self.Param("Multiplier", 0.1) \
            .SetRange(0.0, 1.0) \
            .SetDisplay("Multiplier %", "Sensitivity to Donchian Channel border (percent)", "Indicators")

        self._cooldown_bars = self.Param("CooldownBars", 40) \
            .SetRange(1, 200) \
            .SetDisplay("Cooldown Bars", "Bars between trades", "General")

        self._cooldown = 0

    @property
    def CandleType(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(adx_donchian_strategy, self).OnReseted()
        self._cooldown = 0

    def OnStarted2(self, time):
        super(adx_donchian_strategy, self).OnStarted2(time)
        self._cooldown = 0

        adx = AverageDirectionalIndex()
        adx.Length = self._adx_period.Value
        donchian = DonchianChannels()
        donchian.Length = self._donchian_period.Value

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.BindEx(donchian, adx, self.ProcessCandle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, adx)
            self.DrawIndicator(area, donchian)
            self.DrawOwnTrades(area)

    def ProcessCandle(self, candle, donchian_value, adx_value):
        if candle.State != CandleStates.Finished:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        adx_ma = adx_value.MovingAverage
        if adx_ma is None:
            return

        upper_band = donchian_value.UpperBand
        lower_band = donchian_value.LowerBand
        if upper_band is None or lower_band is None:
            return

        price = float(candle.ClosePrice)
        adx_val = float(adx_ma)
        threshold = float(self._adx_threshold.Value)
        mult = float(self._multiplier.Value)

        strong_trend = adx_val > threshold

        upper_border = float(upper_band) * (1 - mult / 100)
        lower_border = float(lower_band) * (1 + mult / 100)

        if self._cooldown > 0:
            self._cooldown -= 1

        cooldown_val = int(self._cooldown_bars.Value)

        if self._cooldown == 0 and strong_trend and price >= upper_border and self.Position <= 0:
            volume = self.Volume + abs(self.Position)
            self.BuyMarket(volume)
            self._cooldown = cooldown_val
        elif self._cooldown == 0 and strong_trend and price <= lower_border and self.Position >= 0:
            volume = self.Volume + abs(self.Position)
            self.SellMarket(volume)
            self._cooldown = cooldown_val
        elif self.Position != 0 and adx_val < threshold - 5:
            if self.Position > 0:
                self.SellMarket(self.Position)
            else:
                self.BuyMarket(abs(self.Position))
            self._cooldown = cooldown_val

    def CreateClone(self):
        return adx_donchian_strategy()
