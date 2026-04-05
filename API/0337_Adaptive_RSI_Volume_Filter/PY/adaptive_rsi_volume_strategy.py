import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, Unit, UnitTypes, CandleStates
from StockSharp.Algo.Indicators import RelativeStrengthIndex, AverageTrueRange, SimpleMovingAverage, CandleIndicatorValue
from StockSharp.Algo.Strategies import Strategy
from indicator_extensions import *

class adaptive_rsi_volume_strategy(Strategy):
    """
    Strategy that trades an ATR-adaptive RSI confirmed by relative volume.
    """

    def __init__(self):
        super(adaptive_rsi_volume_strategy, self).__init__()

        self._min_rsi_period = self.Param("MinRsiPeriod", 8) \
            .SetGreaterThanZero() \
            .SetDisplay("Min RSI Period", "Fast RSI period used in high volatility", "Indicator Settings")

        self._max_rsi_period = self.Param("MaxRsiPeriod", 21) \
            .SetGreaterThanZero() \
            .SetDisplay("Max RSI Period", "Slow RSI period used in low volatility", "Indicator Settings")

        self._atr_period = self.Param("AtrPeriod", 14) \
            .SetGreaterThanZero() \
            .SetDisplay("ATR Period", "Period for ATR volatility calculation", "Indicator Settings")

        self._volume_lookback = self.Param("VolumeLookback", 12) \
            .SetGreaterThanZero() \
            .SetDisplay("Volume Lookback", "Periods used for average volume", "Volume Settings")

        self._cooldown_bars = self.Param("CooldownBars", 8) \
            .SetNotNegative() \
            .SetDisplay("Cooldown Bars", "Closed candles to wait before another signal", "Trading")

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(1))) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")

        self._fast_rsi = None
        self._slow_rsi = None
        self._atr = None
        self._volume_sma = None
        self._adaptive_rsi_value = 50.0
        self._avg_volume = 0.0
        self._atr_value = 0.0
        self._cooldown_remaining = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def GetWorkingSecurities(self):
        return [(self.Security, self.candle_type)]

    def OnReseted(self):
        super(adaptive_rsi_volume_strategy, self).OnReseted()
        self._adaptive_rsi_value = 50.0
        self._avg_volume = 0.0
        self._atr_value = 0.0
        self._cooldown_remaining = 0

    def OnStarted2(self, time):
        super(adaptive_rsi_volume_strategy, self).OnStarted2(time)

        self._fast_rsi = RelativeStrengthIndex()
        self._fast_rsi.Length = int(self._min_rsi_period.Value)

        self._slow_rsi = RelativeStrengthIndex()
        self._slow_rsi.Length = int(self._max_rsi_period.Value)

        self._atr = AverageTrueRange()
        self._atr.Length = int(self._atr_period.Value)

        self._volume_sma = SimpleMovingAverage()
        self._volume_sma.Length = int(self._volume_lookback.Value)

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self.ProcessCandle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, self._fast_rsi)
            self.DrawIndicator(area, self._slow_rsi)
            self.DrawOwnTrades(area)

        self.StartProtection(
            takeProfit=Unit(2, UnitTypes.Percent),
            stopLoss=Unit(1, UnitTypes.Percent)
        )

    def ProcessCandle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        if self._cooldown_remaining > 0:
            self._cooldown_remaining -= 1

        fast_rsi_val = process_float(self._fast_rsi, candle.ClosePrice, candle.OpenTime, True)

        slow_rsi_val = process_float(self._slow_rsi, candle.ClosePrice, candle.OpenTime, True)

        aiv = CandleIndicatorValue(self._atr, candle)
        aiv.IsFinal = True
        atr_val = self._atr.Process(aiv)

        volume_val = process_float(self._volume_sma, candle.TotalVolume, candle.OpenTime, True)

        if not self._fast_rsi.IsFormed or not self._slow_rsi.IsFormed or not self._atr.IsFormed or not self._volume_sma.IsFormed:
            return
        if fast_rsi_val.IsEmpty or slow_rsi_val.IsEmpty or atr_val.IsEmpty or volume_val.IsEmpty:
            return

        self._avg_volume = float(volume_val)
        self._atr_value = float(atr_val)

        fast_rsi = float(fast_rsi_val)
        slow_rsi = float(slow_rsi_val)
        close_price = float(candle.ClosePrice)
        normalized_atr = min(max(self._atr_value / max(close_price * 0.02, 1.0), 0.0), 1.0)

        self._adaptive_rsi_value = slow_rsi + ((fast_rsi - slow_rsi) * normalized_atr)

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        total_volume = float(candle.TotalVolume)
        is_high_volume = total_volume >= (self._avg_volume * 0.9)
        oversold_level = 45.0 - (normalized_atr * 5.0)
        overbought_level = 55.0 + (normalized_atr * 5.0)
        cooldown = int(self._cooldown_bars.Value)

        if self._cooldown_remaining == 0 and is_high_volume and self._adaptive_rsi_value <= oversold_level and self.Position <= 0:
            vol = self.Volume
            if self.Position < 0:
                vol = self.Volume + Math.Abs(self.Position)
            self.BuyMarket(vol)
            self._cooldown_remaining = cooldown
        elif self._cooldown_remaining == 0 and is_high_volume and self._adaptive_rsi_value >= overbought_level and self.Position >= 0:
            vol = self.Volume
            if self.Position > 0:
                vol = self.Volume + Math.Abs(self.Position)
            self.SellMarket(vol)
            self._cooldown_remaining = cooldown
        elif self.Position > 0 and self._adaptive_rsi_value >= 52.0:
            self.SellMarket(self.Position)
            self._cooldown_remaining = cooldown
        elif self.Position < 0 and self._adaptive_rsi_value <= 48.0:
            self.BuyMarket(Math.Abs(self.Position))
            self._cooldown_remaining = cooldown

    def CreateClone(self):
        return adaptive_rsi_volume_strategy()
