import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import SuperTrend, AverageTrueRange
from StockSharp.Algo.Strategies import Strategy
from indicator_extensions import *


class atr_god_strategy(Strategy):
    """
    ATR GOD strategy.
    A Supertrend flip up goes long and a flip down goes short, reversing an opposite position. Each entry fixes a stop-loss
    RiskMultiplier ATRs from the entry close and a take-profit RewardRiskRatio times that distance on the other side.
    """

    def __init__(self):
        super(atr_god_strategy, self).__init__()
        self._period = self.Param("Period", 10) \
            .SetGreaterThanZero() \
            .SetDisplay("Period", "ATR period of the Supertrend and the stops", "Supertrend")
        self._multiplier = self.Param("Multiplier", 3.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Multiplier", "Supertrend ATR multiplier", "Supertrend")
        self._risk_multiplier = self.Param("RiskMultiplier", 4.5) \
            .SetNotNegative() \
            .SetDisplay("Risk Multiplier", "Stop-loss distance in ATR multiples", "Risk")
        self._reward_risk_ratio = self.Param("RewardRiskRatio", 1.5) \
            .SetNotNegative() \
            .SetDisplay("Reward/Risk", "Take-profit distance relative to the stop distance", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")

        self._reset_state()

    @property
    def CandleType(self):
        return self._candle_type.Value

    def GetWorkingSecurities(self):
        return [(self.Security, self.CandleType)]

    def _reset_state(self):
        self._prev_is_up_trend = None
        self._stop_price = 0.0
        self._take_price = 0.0

    def OnReseted(self):
        super(atr_god_strategy, self).OnReseted()
        self._reset_state()

    def OnStarted2(self, time):
        super(atr_god_strategy, self).OnStarted2(time)

        self._reset_state()

        super_trend = SuperTrend()
        super_trend.Length = self._period.Value
        super_trend.Multiplier = self._multiplier.Value
        atr = AverageTrueRange()
        atr.Length = self._period.Value

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.BindEx(super_trend, atr, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, super_trend)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, super_trend_value, atr_value):
        if candle.State != CandleStates.Finished:
            return

        if not super_trend_value.IsFormed or not atr_value.IsFormed:
            return

        is_up_trend = bool(super_trend_value.IsUpTrend)
        prev_up = self._prev_is_up_trend
        self._prev_is_up_trend = is_up_trend

        if prev_up is None:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        close = float(candle.ClosePrice)
        risk_multiplier = float(self._risk_multiplier.Value)
        reward_ratio = float(self._reward_risk_ratio.Value)
        risk = risk_multiplier * float(to_decimal(atr_value))
        reward = risk * reward_ratio

        if not prev_up and is_up_trend and self.Position <= 0:
            self.BuyMarket(self.Volume + abs(self.Position))
            self._stop_price = close - risk
            self._take_price = close + reward
        elif prev_up and not is_up_trend and self.Position >= 0:
            self.SellMarket(self.Volume + abs(self.Position))
            self._stop_price = close + risk
            self._take_price = close - reward
        elif self.Position != 0:
            use_stop = risk_multiplier > 0
            use_take = risk_multiplier > 0 and reward_ratio > 0
            high = float(candle.HighPrice)
            low = float(candle.LowPrice)

            if self.Position > 0 and ((use_stop and low <= self._stop_price) or (use_take and high >= self._take_price)):
                self.SellMarket(self.Position)
            elif self.Position < 0 and ((use_stop and high >= self._stop_price) or (use_take and low <= self._take_price)):
                self.BuyMarket(-self.Position)

    def CreateClone(self):
        return atr_god_strategy()
