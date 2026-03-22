import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

import math
from System import TimeSpan, Math
from StockSharp.Messages import DataType, Unit, UnitTypes, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage, AverageTrueRange
from StockSharp.Algo.Strategies import Strategy

class keltner_width_mean_reversion_strategy(Strategy):
    """
    Keltner width mean reversion strategy.
    Trades contractions and expansions of Keltner Channel width around its recent average.
    """

    def __init__(self):
        super(keltner_width_mean_reversion_strategy, self).__init__()

        self._ema_period = self.Param("EmaPeriod", 20) \
            .SetGreaterThanZero() \
            .SetDisplay("EMA Period", "Period for EMA calculation", "Indicators")

        self._atr_period = self.Param("AtrPeriod", 14) \
            .SetGreaterThanZero() \
            .SetDisplay("ATR Period", "Period for ATR calculation", "Indicators")

        self._keltner_multiplier = self.Param("KeltnerMultiplier", 2.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Keltner Multiplier", "Multiplier for Keltner Channel bands", "Indicators")

        self._width_dev_mult = self.Param("WidthDeviationMultiplier", 1.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Width Dev Multiplier", "Multiplier for width deviation threshold", "Strategy Parameters")

        self._width_lookback = self.Param("WidthLookbackPeriod", 20) \
            .SetGreaterThanZero() \
            .SetDisplay("Width Lookback", "Lookback period for width statistics", "Strategy Parameters")

        self._stop_loss_percent = self.Param("StopLossPercent", 2.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Stop Loss %", "Stop loss percentage", "Risk Management")

        self._cooldown_bars = self.Param("CooldownBars", 1200) \
            .SetDisplay("Cooldown Bars", "Bars to wait between orders", "Risk Management")

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")

        self._ema = None
        self._atr = None
        self._width_history = None
        self._current_index = 0
        self._filled_count = 0
        self._cooldown = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(keltner_width_mean_reversion_strategy, self).OnReseted()
        self._ema = None
        self._atr = None
        lb = int(self._width_lookback.Value)
        self._width_history = [0.0] * lb
        self._current_index = 0
        self._filled_count = 0
        self._cooldown = 0

    def OnStarted(self, time):
        super(keltner_width_mean_reversion_strategy, self).OnStarted(time)

        lb = int(self._width_lookback.Value)
        self._width_history = [0.0] * lb
        self._current_index = 0
        self._filled_count = 0
        self._cooldown = 0

        self._ema = ExponentialMovingAverage()
        self._ema.Length = int(self._ema_period.Value)
        self._atr = AverageTrueRange()
        self._atr.Length = int(self._atr_period.Value)

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self._ema, self._atr, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, self._ema)
            self.DrawIndicator(area, self._atr)
            self.DrawOwnTrades(area)

        self.StartProtection(Unit(), Unit(self._stop_loss_percent.Value, UnitTypes.Percent))

    def _process_candle(self, candle, ema_value, atr_value):
        if candle.State != CandleStates.Finished:
            return

        if not self._ema.IsFormed or not self._atr.IsFormed:
            return

        km = float(self._keltner_multiplier.Value)
        width = 2.0 * km * float(atr_value)

        lb = int(self._width_lookback.Value)
        self._width_history[self._current_index] = width
        self._current_index = (self._current_index + 1) % lb

        if self._filled_count < lb:
            self._filled_count += 1

        if self._filled_count < lb:
            return

        avg_width = 0.0
        for i in range(lb):
            avg_width += self._width_history[i]
        avg_width /= float(lb)

        sum_sq = 0.0
        for i in range(lb):
            diff = self._width_history[i] - avg_width
            sum_sq += diff * diff
        std_width = math.sqrt(sum_sq / float(lb))

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        if self._cooldown > 0:
            self._cooldown -= 1
            return

        wdm = float(self._width_dev_mult.Value)
        lower_threshold = avg_width - wdm * std_width
        upper_threshold = avg_width + wdm * std_width

        if self.Position == 0:
            if width < lower_threshold:
                self.BuyMarket()
                self._cooldown = int(self._cooldown_bars.Value)
            elif width > upper_threshold:
                self.SellMarket()
                self._cooldown = int(self._cooldown_bars.Value)
        elif self.Position > 0 and width >= avg_width:
            self.SellMarket(Math.Abs(self.Position))
            self._cooldown = int(self._cooldown_bars.Value)
        elif self.Position < 0 and width <= avg_width:
            self.BuyMarket(Math.Abs(self.Position))
            self._cooldown = int(self._cooldown_bars.Value)

    def CreateClone(self):
        return keltner_width_mean_reversion_strategy()
