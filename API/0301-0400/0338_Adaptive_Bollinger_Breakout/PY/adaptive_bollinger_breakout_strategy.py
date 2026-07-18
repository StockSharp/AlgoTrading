import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, Unit, UnitTypes, CandleStates
from StockSharp.Algo.Indicators import SimpleMovingAverage, StandardDeviation, AverageTrueRange, CandleIndicatorValue
from StockSharp.Algo.Strategies import Strategy
from indicator_extensions import *

class adaptive_bollinger_breakout_strategy(Strategy):
    """
    Strategy that trades adaptive Bollinger mean reversion selected by ATR volatility regime.
    """

    def __init__(self):
        super(adaptive_bollinger_breakout_strategy, self).__init__()

        self._min_bollinger_period = self.Param("MinBollingerPeriod", 10) \
            .SetGreaterThanZero() \
            .SetDisplay("Min Bollinger Period", "Short Bollinger period for volatile regimes", "Indicator Settings")

        self._max_bollinger_period = self.Param("MaxBollingerPeriod", 30) \
            .SetGreaterThanZero() \
            .SetDisplay("Max Bollinger Period", "Long Bollinger period for quiet regimes", "Indicator Settings")

        self._min_bollinger_deviation = self.Param("MinBollingerDeviation", 1.5) \
            .SetGreaterThanZero() \
            .SetDisplay("Min Bollinger Deviation", "Narrow band width for quiet regimes", "Indicator Settings")

        self._max_bollinger_deviation = self.Param("MaxBollingerDeviation", 2.5) \
            .SetGreaterThanZero() \
            .SetDisplay("Max Bollinger Deviation", "Wide band width for volatile regimes", "Indicator Settings")

        self._atr_period = self.Param("AtrPeriod", 14) \
            .SetGreaterThanZero() \
            .SetDisplay("ATR Period", "Period for ATR volatility calculation", "Indicator Settings")

        self._cooldown_bars = self.Param("CooldownBars", 6) \
            .SetNotNegative() \
            .SetDisplay("Cooldown Bars", "Closed candles to wait before another breakout entry", "Trading")

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(1))) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")

        self._fast_sma = None
        self._slow_sma = None
        self._fast_std = None
        self._slow_std = None
        self._atr = None
        self._atr_sum = 0.0
        self._atr_count = 0
        self._cooldown_remaining = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def GetWorkingSecurities(self):
        return [(self.Security, self.candle_type)]

    def OnReseted(self):
        super(adaptive_bollinger_breakout_strategy, self).OnReseted()
        self._atr_sum = 0.0
        self._atr_count = 0
        self._cooldown_remaining = 0

    def OnStarted2(self, time):
        super(adaptive_bollinger_breakout_strategy, self).OnStarted2(time)

        min_period = int(self._min_bollinger_period.Value)
        max_period = int(self._max_bollinger_period.Value)
        atr_period = int(self._atr_period.Value)

        self._fast_sma = SimpleMovingAverage()
        self._fast_sma.Length = min_period

        self._slow_sma = SimpleMovingAverage()
        self._slow_sma.Length = max_period

        self._fast_std = StandardDeviation()
        self._fast_std.Length = min_period

        self._slow_std = StandardDeviation()
        self._slow_std.Length = max_period

        self._atr = AverageTrueRange()
        self._atr.Length = atr_period

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self.ProcessCandle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, self._fast_sma)
            self.DrawIndicator(area, self._slow_sma)
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

        aiv = CandleIndicatorValue(self._atr, candle)
        aiv.IsFinal = True
        atr_val = self._atr.Process(aiv)

        fast_sma_val = process_float(self._fast_sma, candle.ClosePrice, candle.OpenTime, True)

        slow_sma_val = process_float(self._slow_sma, candle.ClosePrice, candle.OpenTime, True)

        fast_std_val = process_float(self._fast_std, candle.ClosePrice, candle.OpenTime, True)

        slow_std_val = process_float(self._slow_std, candle.ClosePrice, candle.OpenTime, True)

        if not self._atr.IsFormed or not self._fast_sma.IsFormed or not self._slow_sma.IsFormed or \
           not self._fast_std.IsFormed or not self._slow_std.IsFormed:
            return
        if atr_val.IsEmpty or fast_sma_val.IsEmpty or slow_sma_val.IsEmpty or fast_std_val.IsEmpty or slow_std_val.IsEmpty:
            return

        current_atr = float(atr_val)
        self._atr_sum += current_atr
        self._atr_count += 1

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        average_atr = self._atr_sum / self._atr_count if self._atr_count > 0 else current_atr
        use_fast_bands = current_atr >= average_atr

        fast_sma = float(fast_sma_val)
        slow_sma = float(slow_sma_val)
        fast_std = float(fast_std_val)
        slow_std = float(slow_std_val)

        middle_band = fast_sma if use_fast_bands else slow_sma
        std_dev = fast_std if use_fast_bands else slow_std
        band_width = float(self._max_bollinger_deviation.Value) if use_fast_bands else float(self._min_bollinger_deviation.Value)
        upper_band = middle_band + (std_dev * band_width)
        lower_band = middle_band - (std_dev * band_width)

        close = float(candle.ClosePrice)
        cooldown = int(self._cooldown_bars.Value)

        if self.Position > 0 and close >= middle_band:
            self.SellMarket(self.Position)
            self._cooldown_remaining = cooldown
        elif self.Position < 0 and close <= middle_band:
            self.BuyMarket(Math.Abs(self.Position))
            self._cooldown_remaining = cooldown
        elif self._cooldown_remaining == 0 and close < lower_band and self.Position <= 0:
            vol = self.Volume
            if self.Position < 0:
                vol = self.Volume + Math.Abs(self.Position)
            self.BuyMarket(vol)
            self._cooldown_remaining = cooldown
        elif self._cooldown_remaining == 0 and close > upper_band and self.Position >= 0:
            vol = self.Volume
            if self.Position > 0:
                vol = self.Volume + Math.Abs(self.Position)
            self.SellMarket(vol)
            self._cooldown_remaining = cooldown

    def CreateClone(self):
        return adaptive_bollinger_breakout_strategy()
