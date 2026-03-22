import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.BusinessEntities")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import SimpleMovingAverage, CandleIndicatorValue
from StockSharp.Algo.Strategies import Strategy
from StockSharp.BusinessEntities import Security


class asset_class_trend_following_strategy(Strategy):
    """Relative asset class trend-following strategy using dual securities."""

    def __init__(self):
        super(asset_class_trend_following_strategy, self).__init__()

        self._security2_id = self.Param("Security2Id", "TONUSDT@BNBFT") \
            .SetDisplay("Second Security Id", "Identifier of the secondary benchmark security", "General")

        self._sma_length = self.Param("SmaLength", 36) \
            .SetRange(10, 200) \
            .SetDisplay("SMA Length", "Trend moving average length", "Indicators")

        self._min_trend_strength = self.Param("MinTrendStrength", 0.004) \
            .SetRange(0.001, 0.05) \
            .SetDisplay("Min Trend Strength", "Minimum absolute trend strength required to hold the primary instrument", "Signals")

        self._relative_strength_threshold = self.Param("RelativeStrengthThreshold", 0.002) \
            .SetRange(0.0, 0.05) \
            .SetDisplay("Relative Strength Threshold", "Minimum relative outperformance of the primary instrument", "Signals")

        self._rebalance_interval_bars = self.Param("RebalanceIntervalBars", 18) \
            .SetRange(1, 200) \
            .SetDisplay("Rebalance Bars", "Number of paired candles between rebalancing decisions", "General")

        self._stop_loss = self.Param("StopLoss", 2.0) \
            .SetRange(0.5, 10.0) \
            .SetDisplay("Stop Loss %", "Stop loss percentage", "Risk")

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Candle series for both instruments", "General")

        self._security2 = None
        self._primary_sma = None
        self._secondary_sma = None
        self._latest_primary_price = 0.0
        self._latest_secondary_price = 0.0
        self._latest_primary_sma = 0.0
        self._latest_secondary_sma = 0.0
        self._primary_updated = False
        self._secondary_updated = False
        self._bars_since_rebalance = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def GetWorkingSecurities(self):
        result = []
        if self.Security is not None:
            result.append((self.Security, self.candle_type))
        sec2_id = str(self._security2_id.Value)
        if sec2_id:
            s = Security()
            s.Id = sec2_id
            result.append((s, self.candle_type))
        return result

    def OnReseted(self):
        super(asset_class_trend_following_strategy, self).OnReseted()
        self._security2 = None
        self._primary_sma = None
        self._secondary_sma = None
        self._latest_primary_price = 0.0
        self._latest_secondary_price = 0.0
        self._latest_primary_sma = 0.0
        self._latest_secondary_sma = 0.0
        self._primary_updated = False
        self._secondary_updated = False
        self._bars_since_rebalance = 0

    def OnStarted(self, time):
        super(asset_class_trend_following_strategy, self).OnStarted(time)

        sec2_id = str(self._security2_id.Value)
        if not sec2_id:
            raise Exception("Secondary security identifier is not specified.")

        s = Security()
        s.Id = sec2_id
        self._security2 = s

        sma_len = int(self._sma_length.Value)
        self._primary_sma = SimpleMovingAverage()
        self._primary_sma.Length = sma_len
        self._secondary_sma = SimpleMovingAverage()
        self._secondary_sma.Length = sma_len
        self._bars_since_rebalance = int(self._rebalance_interval_bars.Value)

        primary_subscription = self.SubscribeCandles(self.candle_type, True, self.Security)
        secondary_subscription = self.SubscribeCandles(self.candle_type, True, self._security2)

        primary_subscription.Bind(self.ProcessPrimaryCandle).Start()
        secondary_subscription.Bind(self.ProcessSecondaryCandle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, primary_subscription)
            self.DrawCandles(area, secondary_subscription)
            self.DrawOwnTrades(area)

        self.StartProtection(
            Unit(2, UnitTypes.Percent),
            Unit(float(self._stop_loss.Value), UnitTypes.Percent)
        )

    def ProcessPrimaryCandle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        self._latest_primary_price = float(candle.ClosePrice)
        civ = CandleIndicatorValue(self._primary_sma, candle)
        civ.IsFinal = True
        result = self._primary_sma.Process(civ)
        self._latest_primary_sma = float(result)
        self._primary_updated = True
        self.TryRebalance()

    def ProcessSecondaryCandle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        self._latest_secondary_price = float(candle.ClosePrice)
        civ = CandleIndicatorValue(self._secondary_sma, candle)
        civ.IsFinal = True
        result = self._secondary_sma.Process(civ)
        self._latest_secondary_sma = float(result)
        self._secondary_updated = True
        self.TryRebalance()

    def TryRebalance(self):
        if not self._primary_updated or not self._secondary_updated:
            return

        self._primary_updated = False
        self._secondary_updated = False

        if not self._primary_sma.IsFormed or not self._secondary_sma.IsFormed:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        rebalance_interval = int(self._rebalance_interval_bars.Value)
        if self._bars_since_rebalance < rebalance_interval:
            self._bars_since_rebalance += 1
            return

        self._bars_since_rebalance = 0

        primary_sma_val = max(self._latest_primary_sma, 1.0)
        secondary_sma_val = max(self._latest_secondary_sma, 1.0)

        primary_trend = (self._latest_primary_price - self._latest_primary_sma) / primary_sma_val
        secondary_trend = (self._latest_secondary_price - self._latest_secondary_sma) / secondary_sma_val
        relative_strength = primary_trend - secondary_trend

        min_strength = float(self._min_trend_strength.Value)
        rel_thresh = float(self._relative_strength_threshold.Value)

        should_hold_long = primary_trend >= min_strength and relative_strength >= rel_thresh

        if should_hold_long and self.Position <= 0:
            vol = self.Volume
            if self.Position < 0:
                vol = self.Volume + Math.Abs(self.Position)
            self.BuyMarket(vol)
        elif not should_hold_long and self.Position > 0:
            self.SellMarket(self.Position)

    def CreateClone(self):
        return asset_class_trend_following_strategy()
