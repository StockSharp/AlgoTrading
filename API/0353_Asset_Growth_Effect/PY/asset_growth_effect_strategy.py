import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.BusinessEntities")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Math, Decimal
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import ExponentialMovingAverage, SimpleMovingAverage, StandardDeviation, DecimalIndicatorValue
from StockSharp.Algo.Strategies import Strategy
from StockSharp.BusinessEntities import Security


class asset_growth_effect_strategy(Strategy):
    """Relative asset-growth strategy using dual securities."""

    def __init__(self):
        super(asset_growth_effect_strategy, self).__init__()

        self._security2_id = self.Param("Security2Id", "TONUSDT@BNBFT") \
            .SetDisplay("Second Security Id", "Identifier of the secondary benchmark security", "General")

        self._asset_length = self.Param("AssetLength", 8) \
            .SetRange(2, 40) \
            .SetDisplay("Asset Length", "Smoothing length for the synthetic asset base", "Indicators")

        self._lookback_period = self.Param("LookbackPeriod", 24) \
            .SetRange(10, 150) \
            .SetDisplay("Lookback Period", "Lookback period used to normalize growth spread", "Indicators")

        self._entry_threshold = self.Param("EntryThreshold", 1.35) \
            .SetRange(0.5, 4.0) \
            .SetDisplay("Entry Threshold", "Z-score threshold required to open a position", "Signals")

        self._exit_threshold = self.Param("ExitThreshold", 0.3) \
            .SetRange(0.0, 2.0) \
            .SetDisplay("Exit Threshold", "Z-score threshold required to close a position", "Signals")

        self._cooldown_bars = self.Param("CooldownBars", 12) \
            .SetRange(0, 100) \
            .SetDisplay("Cooldown Bars", "Closed candles to wait before another position change", "Risk")

        self._stop_loss = self.Param("StopLoss", 2.5) \
            .SetRange(0.5, 10.0) \
            .SetDisplay("Stop Loss %", "Stop loss percentage", "Risk")

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Candle series for both instruments", "General")

        self._security2 = None
        self._primary_asset_base = None
        self._secondary_asset_base = None
        self._growth_spread_average = None
        self._growth_spread_deviation = None
        self._prev_primary_asset_base = 0.0
        self._prev_secondary_asset_base = 0.0
        self._latest_primary_growth = 0.0
        self._latest_secondary_growth = 0.0
        self._primary_updated = False
        self._secondary_updated = False
        self._previous_z_score = None
        self._cooldown_remaining = 0

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
        super(asset_growth_effect_strategy, self).OnReseted()
        self._security2 = None
        self._primary_asset_base = None
        self._secondary_asset_base = None
        self._growth_spread_average = None
        self._growth_spread_deviation = None
        self._prev_primary_asset_base = 0.0
        self._prev_secondary_asset_base = 0.0
        self._latest_primary_growth = 0.0
        self._latest_secondary_growth = 0.0
        self._primary_updated = False
        self._secondary_updated = False
        self._previous_z_score = None
        self._cooldown_remaining = 0

    def OnStarted2(self, time):
        super(asset_growth_effect_strategy, self).OnStarted2(time)

        sec2_id = str(self._security2_id.Value)
        if not sec2_id:
            raise Exception("Secondary security identifier is not specified.")

        s = Security()
        s.Id = sec2_id
        self._security2 = s

        asset_len = int(self._asset_length.Value)
        lookback = int(self._lookback_period.Value)

        self._primary_asset_base = ExponentialMovingAverage()
        self._primary_asset_base.Length = asset_len
        self._secondary_asset_base = ExponentialMovingAverage()
        self._secondary_asset_base.Length = asset_len
        self._growth_spread_average = SimpleMovingAverage()
        self._growth_spread_average.Length = lookback
        self._growth_spread_deviation = StandardDeviation()
        self._growth_spread_deviation.Length = lookback
        self._cooldown_remaining = 0

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

        self._latest_primary_growth, self._prev_primary_asset_base = self.UpdateGrowth(
            self._primary_asset_base, candle, self._prev_primary_asset_base)
        self._primary_updated = True
        self.TryProcessGrowthSpread(candle.OpenTime)

    def ProcessSecondaryCandle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        self._latest_secondary_growth, self._prev_secondary_asset_base = self.UpdateGrowth(
            self._secondary_asset_base, candle, self._prev_secondary_asset_base)
        self._secondary_updated = True
        self.TryProcessGrowthSpread(candle.OpenTime)

    def UpdateGrowth(self, average, candle, previous_value):
        synthetic_assets = self.CalculateSyntheticAssets(candle)
        iv = DecimalIndicatorValue(average, synthetic_assets, candle.OpenTime)
        iv.IsFinal = True
        result = average.Process(iv)
        asset_base = float(result)

        if previous_value == 0.0:
            return 0.0, asset_base

        growth = (asset_base - previous_value) / max(abs(previous_value), 1.0)
        return growth, asset_base

    def CalculateSyntheticAssets(self, candle):
        open_p = candle.OpenPrice
        high_p = candle.HighPrice
        low_p = candle.LowPrice
        close_p = candle.ClosePrice

        price_base = Math.Max(open_p, Decimal(1))
        price_step = self.Security.PriceStep if self.Security is not None and self.Security.PriceStep is not None else Decimal(1)
        range_val = Math.Max(high_p - low_p, price_step)

        turnover_proxy = close_p * (Decimal(1) + ((range_val / price_base) * Decimal(5)))
        balance_sheet_proxy = (high_p + low_p + close_p) / Decimal(3)

        return float(turnover_proxy + balance_sheet_proxy)

    def TryProcessGrowthSpread(self, time):
        if not self._primary_updated or not self._secondary_updated:
            return

        self._primary_updated = False
        self._secondary_updated = False

        if not self._primary_asset_base.IsFormed or not self._secondary_asset_base.IsFormed:
            return

        growth_spread = self._latest_primary_growth - self._latest_secondary_growth

        mean_iv = DecimalIndicatorValue(self._growth_spread_average, growth_spread, time)
        mean_iv.IsFinal = True
        mean_result = self._growth_spread_average.Process(mean_iv)
        mean = float(mean_result)

        dev_iv = DecimalIndicatorValue(self._growth_spread_deviation, growth_spread, time)
        dev_iv.IsFinal = True
        dev_result = self._growth_spread_deviation.Process(dev_iv)
        deviation = float(dev_result)

        if not self._growth_spread_average.IsFormed or not self._growth_spread_deviation.IsFormed or deviation <= 0:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        if self._cooldown_remaining > 0:
            self._cooldown_remaining -= 1

        z_score = (growth_spread - mean) / deviation
        entry_thresh = float(self._entry_threshold.Value)
        exit_thresh = float(self._exit_threshold.Value)
        cooldown = int(self._cooldown_bars.Value)

        bullish_entry = self._previous_z_score is not None and self._previous_z_score > -entry_thresh and z_score <= -entry_thresh
        bearish_entry = self._previous_z_score is not None and self._previous_z_score < entry_thresh and z_score >= entry_thresh

        if self._cooldown_remaining == 0 and self.Position == 0:
            if bullish_entry:
                self.BuyMarket()
                self._cooldown_remaining = cooldown
            elif bearish_entry:
                self.SellMarket()
                self._cooldown_remaining = cooldown
        elif self.Position > 0 and z_score >= -exit_thresh:
            self.SellMarket(self.Position)
            self._cooldown_remaining = cooldown
        elif self.Position < 0 and z_score <= exit_thresh:
            self.BuyMarket(Math.Abs(self.Position))
            self._cooldown_remaining = cooldown

        self._previous_z_score = z_score

    def CreateClone(self):
        return asset_growth_effect_strategy()
