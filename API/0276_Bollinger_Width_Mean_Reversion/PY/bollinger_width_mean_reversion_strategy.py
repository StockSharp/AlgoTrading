import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

import math
from System import TimeSpan, Math
from StockSharp.Messages import DataType, Unit, UnitTypes, CandleStates
from StockSharp.Algo.Indicators import BollingerBands
from StockSharp.Algo.Strategies import Strategy

class bollinger_width_mean_reversion_strategy(Strategy):
    """
    Bollinger width mean reversion strategy.
    Trades contractions and expansions of normalized Bollinger Bands width around its recent average.
    """

    def __init__(self):
        super(bollinger_width_mean_reversion_strategy, self).__init__()

        self._bollinger_length = self.Param("BollingerLength", 20) \
            .SetGreaterThanZero() \
            .SetDisplay("Bollinger Length", "Period for Bollinger Bands calculation", "Indicators") \
            .SetOptimize(10, 50, 5)

        self._bollinger_deviation = self.Param("BollingerDeviation", 2.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Bollinger Deviation", "Deviation multiplier for Bollinger Bands", "Indicators") \
            .SetOptimize(1.0, 3.0, 0.5)

        self._width_lookback = self.Param("WidthLookbackPeriod", 20) \
            .SetGreaterThanZero() \
            .SetDisplay("Width Lookback", "Lookback for width mean", "Strategy Parameters") \
            .SetOptimize(10, 50, 5)

        self._width_dev_mult = self.Param("WidthDeviationMultiplier", 1.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Width Dev Mult", "Multiplier for width standard deviation threshold", "Strategy Parameters") \
            .SetOptimize(0.5, 3.0, 0.5)

        self._stop_loss_percent = self.Param("StopLossPercent", 2.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Stop Loss %", "Stop loss percentage", "Risk Management")

        self._cooldown_bars = self.Param("CooldownBars", 1200) \
            .SetDisplay("Cooldown Bars", "Bars to wait between orders", "Risk Management")

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")

        self._bollinger = None
        self._width_history = None
        self._current_index = 0
        self._filled_count = 0
        self._cooldown = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(bollinger_width_mean_reversion_strategy, self).OnReseted()
        self._bollinger = None
        lb = int(self._width_lookback.Value)
        self._width_history = [0.0] * lb
        self._current_index = 0
        self._filled_count = 0
        self._cooldown = 0

    def OnStarted2(self, time):
        super(bollinger_width_mean_reversion_strategy, self).OnStarted2(time)

        lb = int(self._width_lookback.Value)
        self._width_history = [0.0] * lb
        self._current_index = 0
        self._filled_count = 0
        self._cooldown = 0

        self._bollinger = BollingerBands()
        self._bollinger.Length = int(self._bollinger_length.Value)
        self._bollinger.Width = self._bollinger_deviation.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(self._bollinger, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, self._bollinger)
            self.DrawOwnTrades(area)

        self.StartProtection(Unit(), Unit(self._stop_loss_percent.Value, UnitTypes.Percent))

    def _process_candle(self, candle, bollinger_value):
        if candle.State != CandleStates.Finished:
            return

        if not self._bollinger.IsFormed:
            return

        upper_band = bollinger_value.UpBand
        lower_band = bollinger_value.LowBand
        middle_band = bollinger_value.MovingAverage
        if upper_band is None or lower_band is None or middle_band is None:
            return

        upper_val = float(upper_band)
        lower_val = float(lower_band)
        middle_val = float(middle_band)

        if middle_val <= 0:
            return

        last_width = (upper_val - lower_val) / middle_val

        lb = int(self._width_lookback.Value)
        self._width_history[self._current_index] = last_width
        self._current_index = (self._current_index + 1) % lb

        if self._filled_count < lb:
            self._filled_count += 1

        if self._filled_count < lb:
            return

        # Calculate statistics
        avg_width = 0.0
        for i in range(lb):
            avg_width += self._width_history[i]
        avg_width /= float(lb)

        if avg_width <= 0:
            return

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
            if last_width < lower_threshold:
                self.BuyMarket()
                self._cooldown = int(self._cooldown_bars.Value)
            elif last_width > upper_threshold:
                self.SellMarket()
                self._cooldown = int(self._cooldown_bars.Value)
        elif self.Position > 0 and last_width >= avg_width:
            self.SellMarket(Math.Abs(self.Position))
            self._cooldown = int(self._cooldown_bars.Value)
        elif self.Position < 0 and last_width <= avg_width:
            self.BuyMarket(Math.Abs(self.Position))
            self._cooldown = int(self._cooldown_bars.Value)

    def CreateClone(self):
        return bollinger_width_mean_reversion_strategy()
