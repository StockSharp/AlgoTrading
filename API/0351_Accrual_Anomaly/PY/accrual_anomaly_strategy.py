import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.BusinessEntities")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import ExponentialMovingAverage, SimpleMovingAverage, StandardDeviation
from StockSharp.Algo.Strategies import Strategy
from StockSharp.BusinessEntities import Security
from indicator_extensions import *

class accrual_anomaly_strategy(Strategy):
    """Cross-sectional accrual anomaly strategy using dual securities."""

    def __init__(self):
        super(accrual_anomaly_strategy, self).__init__()

        self._security2_id = self.Param("Security2Id", "TONUSDT@BNBFT") \
            .SetDisplay("Second Security Id", "Identifier of the secondary security", "General")

        self._accrual_length = self.Param("AccrualLength", 6) \
            .SetRange(2, 30) \
            .SetDisplay("Accrual Length", "Smoothing length for the synthetic accrual proxy", "Indicators")

        self._lookback_period = self.Param("LookbackPeriod", 24) \
            .SetRange(10, 120) \
            .SetDisplay("Lookback Period", "Lookback period for spread normalization", "Indicators")

        self._entry_threshold = self.Param("EntryThreshold", 1.4) \
            .SetRange(0.5, 4.0) \
            .SetDisplay("Entry Threshold", "Z-score threshold required to open a position", "Signals")

        self._exit_threshold = self.Param("ExitThreshold", 0.35) \
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
        self._primary_accrual_avg = None
        self._secondary_accrual_avg = None
        self._spread_average = None
        self._spread_deviation = None
        self._latest_primary_accrual = 0.0
        self._latest_secondary_accrual = 0.0
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
        super(accrual_anomaly_strategy, self).OnReseted()
        self._security2 = None
        self._primary_accrual_avg = None
        self._secondary_accrual_avg = None
        self._spread_average = None
        self._spread_deviation = None
        self._latest_primary_accrual = 0.0
        self._latest_secondary_accrual = 0.0
        self._primary_updated = False
        self._secondary_updated = False
        self._previous_z_score = None
        self._cooldown_remaining = 0

    def OnStarted2(self, time):
        super(accrual_anomaly_strategy, self).OnStarted2(time)

        sec2_id = str(self._security2_id.Value)
        if not sec2_id:
            raise Exception("Secondary security identifier is not specified.")

        s = Security()
        s.Id = sec2_id
        self._security2 = s

        accrual_len = int(self._accrual_length.Value)
        lookback = int(self._lookback_period.Value)

        self._primary_accrual_avg = ExponentialMovingAverage()
        self._primary_accrual_avg.Length = accrual_len
        self._secondary_accrual_avg = ExponentialMovingAverage()
        self._secondary_accrual_avg.Length = accrual_len
        self._spread_average = SimpleMovingAverage()
        self._spread_average.Length = lookback
        self._spread_deviation = StandardDeviation()
        self._spread_deviation.Length = lookback
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

        self._latest_primary_accrual = self.UpdateAccrualAverage(self._primary_accrual_avg, candle)
        self._primary_updated = True
        self.TryProcessSpread(candle.OpenTime)

    def ProcessSecondaryCandle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        self._latest_secondary_accrual = self.UpdateAccrualAverage(self._secondary_accrual_avg, candle)
        self._secondary_updated = True
        self.TryProcessSpread(candle.OpenTime)

    def UpdateAccrualAverage(self, average, candle):
        accrual_proxy = self.CalculateAccrualProxy(candle)
        result = process_float(average, accrual_proxy, candle.OpenTime, True)
        return float(result)

    def CalculateAccrualProxy(self, candle):
        price_base = max(float(candle.OpenPrice), 1.0)
        price_step = float(self.Security.PriceStep) if self.Security is not None and self.Security.PriceStep is not None else 1.0
        range_val = max(float(candle.HighPrice) - float(candle.LowPrice), price_step)
        body = float(candle.ClosePrice) - float(candle.OpenPrice)
        body_ratio = body / price_base
        close_location = ((float(candle.ClosePrice) - float(candle.LowPrice)) - (float(candle.HighPrice) - float(candle.ClosePrice))) / range_val
        volatility_ratio = range_val / price_base

        return (body_ratio * 18.0) + (close_location * 0.8) - (volatility_ratio * 6.0)

    def TryProcessSpread(self, time):
        if not self._primary_updated or not self._secondary_updated:
            return

        self._primary_updated = False
        self._secondary_updated = False

        if not self._primary_accrual_avg.IsFormed or not self._secondary_accrual_avg.IsFormed:
            return

        spread = self._latest_primary_accrual - self._latest_secondary_accrual

        mean_result = process_float(self._spread_average, spread, time, True)
        mean = float(mean_result)

        dev_result = process_float(self._spread_deviation, spread, time, True)
        deviation = float(dev_result)

        if not self._spread_average.IsFormed or not self._spread_deviation.IsFormed or deviation <= 0:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        if self._cooldown_remaining > 0:
            self._cooldown_remaining -= 1

        z_score = (spread - mean) / deviation
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
        return accrual_anomaly_strategy()
