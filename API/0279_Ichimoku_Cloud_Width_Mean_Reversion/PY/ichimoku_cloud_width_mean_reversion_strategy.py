import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

import math
from System import TimeSpan, Math
from StockSharp.Messages import DataType, Unit, UnitTypes, CandleStates
from StockSharp.Algo.Indicators import Ichimoku
from StockSharp.Algo.Strategies import Strategy

class ichimoku_cloud_width_mean_reversion_strategy(Strategy):
    """
    Ichimoku cloud width mean reversion strategy.
    Trades contractions and expansions of the Ichimoku cloud width around its recent average.
    """

    def __init__(self):
        super(ichimoku_cloud_width_mean_reversion_strategy, self).__init__()

        self._tenkan_period = self.Param("TenkanPeriod", 9) \
            .SetGreaterThanZero() \
            .SetDisplay("Tenkan Period", "Tenkan-sen period", "Ichimoku")

        self._kijun_period = self.Param("KijunPeriod", 26) \
            .SetGreaterThanZero() \
            .SetDisplay("Kijun Period", "Kijun-sen period", "Ichimoku")

        self._senkou_span_b_period = self.Param("SenkouSpanBPeriod", 52) \
            .SetGreaterThanZero() \
            .SetDisplay("Senkou Span B Period", "Senkou Span B period", "Ichimoku")

        self._lookback_period = self.Param("LookbackPeriod", 20) \
            .SetGreaterThanZero() \
            .SetDisplay("Lookback Period", "Lookback period for cloud width statistics", "Strategy Parameters")

        self._deviation_multiplier = self.Param("DeviationMultiplier", 1.5) \
            .SetGreaterThanZero() \
            .SetDisplay("Deviation Multiplier", "Deviation multiplier for mean reversion detection", "Strategy Parameters")

        self._stop_loss_percent = self.Param("StopLossPercent", 2.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Stop Loss %", "Stop loss percentage", "Risk Management")

        self._cooldown_bars = self.Param("CooldownBars", 1200) \
            .SetDisplay("Cooldown Bars", "Bars to wait between orders", "Risk Management")

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Candle type for strategy", "General")

        self._ichimoku = None
        self._cloud_width_history = None
        self._current_index = 0
        self._filled_count = 0
        self._cooldown = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(ichimoku_cloud_width_mean_reversion_strategy, self).OnReseted()
        self._ichimoku = None
        lb = int(self._lookback_period.Value)
        self._cloud_width_history = [0.0] * lb
        self._current_index = 0
        self._filled_count = 0
        self._cooldown = 0

    def OnStarted(self, time):
        super(ichimoku_cloud_width_mean_reversion_strategy, self).OnStarted(time)

        lb = int(self._lookback_period.Value)
        self._cloud_width_history = [0.0] * lb
        self._current_index = 0
        self._filled_count = 0
        self._cooldown = 0

        self._ichimoku = Ichimoku()
        self._ichimoku.Tenkan.Length = int(self._tenkan_period.Value)
        self._ichimoku.Kijun.Length = int(self._kijun_period.Value)
        self._ichimoku.SenkouB.Length = int(self._senkou_span_b_period.Value)

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(self._ichimoku, self._process_ichimoku).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, self._ichimoku)
            self.DrawOwnTrades(area)

        self.StartProtection(Unit(), Unit(self._stop_loss_percent.Value, UnitTypes.Percent))

    def _process_ichimoku(self, candle, ichimoku_value):
        if candle.State != CandleStates.Finished:
            return

        if not self._ichimoku.IsFormed:
            return

        senkou_a = ichimoku_value.SenkouA
        senkou_b = ichimoku_value.SenkouB
        if senkou_a is None or senkou_b is None:
            return

        senkou_a_val = float(senkou_a)
        senkou_b_val = float(senkou_b)
        cloud_width = Math.Abs(senkou_a_val - senkou_b_val)

        lb = int(self._lookback_period.Value)
        self._cloud_width_history[self._current_index] = cloud_width
        self._current_index = (self._current_index + 1) % lb

        if self._filled_count < lb:
            self._filled_count += 1

        if self._filled_count < lb:
            return

        avg_width = 0.0
        for i in range(lb):
            avg_width += self._cloud_width_history[i]
        avg_width /= float(lb)

        sum_sq = 0.0
        for i in range(lb):
            diff = self._cloud_width_history[i] - avg_width
            sum_sq += diff * diff
        std_width = math.sqrt(sum_sq / float(lb))

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        if self._cooldown > 0:
            self._cooldown -= 1
            return

        dm = float(self._deviation_multiplier.Value)
        narrow_threshold = avg_width - std_width * dm
        wide_threshold = avg_width + std_width * dm
        upper_cloud = max(senkou_a_val, senkou_b_val)
        lower_cloud = min(senkou_a_val, senkou_b_val)
        close_price = float(candle.ClosePrice)
        price_above_cloud = close_price > upper_cloud
        price_below_cloud = close_price < lower_cloud

        if self.Position == 0:
            if cloud_width < narrow_threshold:
                if price_above_cloud:
                    self.BuyMarket()
                    self._cooldown = int(self._cooldown_bars.Value)
                elif price_below_cloud:
                    self.SellMarket()
                    self._cooldown = int(self._cooldown_bars.Value)
            elif cloud_width > wide_threshold:
                if price_below_cloud:
                    self.SellMarket()
                    self._cooldown = int(self._cooldown_bars.Value)
                elif price_above_cloud:
                    self.BuyMarket()
                    self._cooldown = int(self._cooldown_bars.Value)
        elif self.Position > 0 and cloud_width >= avg_width:
            self.SellMarket(Math.Abs(self.Position))
            self._cooldown = int(self._cooldown_bars.Value)
        elif self.Position < 0 and cloud_width <= avg_width:
            self.BuyMarket(Math.Abs(self.Position))
            self._cooldown = int(self._cooldown_bars.Value)

    def CreateClone(self):
        return ichimoku_cloud_width_mean_reversion_strategy()
