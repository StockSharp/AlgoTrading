import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")
clr.AddReference("StockSharp.BusinessEntities")

import math
from collections import deque
from System import TimeSpan, DateTime, DateTimeKind, Decimal, Nullable
from StockSharp.Messages import DataType, CandleStates, Sides, OrderStates, Unit, UnitTypes
from StockSharp.BusinessEntities import Subscription
from StockSharp.Algo.Strategies import Strategy

class btcusd_momentum_after_abnormal_days_strategy(Strategy):
    def __init__(self):
        super(btcusd_momentum_after_abnormal_days_strategy, self).__init__()
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromDays(1))).SetDisplay(
            "CandleType", "Finished daily signal candle interval", "Parameters")
        self._lookback_period = self.Param("LookbackPeriod", 5).SetGreaterThanZero().SetDisplay(
            "LookbackPeriod", "Prior completed daily returns in the baseline", "Parameters")
        self._abnormal_threshold = self.Param("AbnormalThreshold", 1.6).SetGreaterThanZero().SetDisplay(
            "AbnormalThreshold", "Population standard deviation multiplier", "Parameters")
        self._capital_per_trade = self.Param("CapitalPerTrade", 1000.0).SetGreaterThanZero().SetDisplay(
            "CapitalPerTrade", "Signal-close quote currency budget", "Parameters")
        self._reset_state()
        self.OrderRegistering += self._remember_order

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    @property
    def LookbackPeriod(self):
        return self._lookback_period.Value

    @LookbackPeriod.setter
    def LookbackPeriod(self, value):
        self._lookback_period.Value = value

    @property
    def AbnormalThreshold(self):
        return self._abnormal_threshold.Value

    @AbnormalThreshold.setter
    def AbnormalThreshold(self, value):
        self._abnormal_threshold.Value = value

    @property
    def CapitalPerTrade(self):
        return self._capital_per_trade.Value

    @CapitalPerTrade.setter
    def CapitalPerTrade(self, value):
        self._capital_per_trade.Value = value

    def _reset_state(self):
        self._returns = deque()
        self._last_date = DateTime.MinValue
        self._entry_day = None
        self._pending = None

    def _remember_order(self, order):
        self._pending = order

    def OnReseted(self):
        super(btcusd_momentum_after_abnormal_days_strategy, self).OnReseted()
        self._reset_state()

    def GetWorkingSecurities(self):
        return [(self.Security, self.CandleType)]

    def OnStarted2(self, time):
        if self.CandleType != DataType.TimeFrame(TimeSpan.FromDays(1)):
            raise ValueError("Daily candles are required for daily anomaly and exit deadlines.")
        super(btcusd_momentum_after_abnormal_days_strategy, self).OnStarted2(time)
        candles = self.SubscribeCandles(self.CandleType)
        candles.Bind(self._process).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, candles)
            self.DrawOwnTrades(area)

    def _process(self, candle):
        if candle.State != CandleStates.Finished or candle.OpenPrice <= 0 or candle.ClosePrice <= 0 or candle.OpenTime.Date <= self._last_date:
            return
        self._last_date = candle.OpenTime.Date
        sample = list(self._returns)
        value = float((candle.ClosePrice - candle.OpenPrice) / candle.OpenPrice)
        self._returns.append(value)
        while len(self._returns) > self.LookbackPeriod:
            self._returns.popleft()
        if not self.IsFormedAndOnlineAndAllowTrading() or (self._pending is not None and self._pending.State not in (OrderStates.Done, OrderStates.Failed)):
            return
        if self.Position != 0:
            if self._entry_day is not None and candle.OpenTime.Date > self._entry_day:
                if self.Position > 0:
                    self.SellMarket(self.Position)
                else:
                    self.BuyMarket(-self.Position)
            return
        if len(sample) < self.LookbackPeriod:
            return
        mean = sum(sample) / len(sample)
        deviation = math.sqrt(sum((r - mean) ** 2 for r in sample) / len(sample))
        side = Sides.Buy if value > mean + float(self.AbnormalThreshold) * deviation else Sides.Sell if value < mean - float(self.AbnormalThreshold) * deviation else None
        if side is None:
            return
        step = float(self.Security.VolumeStep or 1)
        quantity = math.floor(float(self.CapitalPerTrade) / float(candle.ClosePrice) / step) * step
        if quantity < float(self.Security.MinVolume or step):
            return
        if self.Security.MaxVolume is not None and quantity > float(self.Security.MaxVolume):
            raise ValueError("CapitalPerTrade exceeds the instrument's maximum order volume.")
        self._entry_day = candle.OpenTime.Date
        if side == Sides.Buy:
            self.BuyMarket(Decimal(quantity))
        else:
            self.SellMarket(Decimal(quantity))

    def CreateClone(self):
        return btcusd_momentum_after_abnormal_days_strategy()
