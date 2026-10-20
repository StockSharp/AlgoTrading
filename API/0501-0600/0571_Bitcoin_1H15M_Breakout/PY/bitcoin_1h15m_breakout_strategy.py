import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")
clr.AddReference("StockSharp.BusinessEntities")

import math
from collections import deque
from System import TimeSpan, DateTime, DateTimeKind, Decimal, Nullable
from StockSharp.Messages import DataType, CandleStates, Sides, OrderStates, Unit, UnitTypes, Level1Fields
from StockSharp.BusinessEntities import Subscription
from StockSharp.Algo.Strategies import Strategy

class bitcoin_1h15m_breakout_strategy(Strategy):
    def __init__(self):
        super(bitcoin_1h15m_breakout_strategy, self).__init__()
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(15))).SetDisplay(
            "CandleType", "Finished lower timeframe candles", "Parameters")
        self._higher_time_frame = self.Param("HigherTimeFrame", TimeSpan.FromHours(1)).SetDisplay(
            "HigherTimeFrame", "Completed higher timeframe range", "Parameters")
        self._stop_loss_buffer = self.Param("StopLossBuffer", 50.0).SetGreaterThanZero().SetDisplay(
            "StopLossBuffer", "Absolute stop distance from actual entry fills", "Parameters")
        self._risk_reward_ratio = self.Param("RiskRewardRatio", 2.0).SetGreaterThanZero().SetDisplay(
            "RiskRewardRatio", "Profit distance divided by stop distance", "Parameters")
        self._reset_state()
        self.OrderRegistering += self._remember_order

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    @property
    def HigherTimeFrame(self):
        return self._higher_time_frame.Value

    @HigherTimeFrame.setter
    def HigherTimeFrame(self, value):
        self._higher_time_frame.Value = value

    @property
    def StopLossBuffer(self):
        return self._stop_loss_buffer.Value

    @StopLossBuffer.setter
    def StopLossBuffer(self, value):
        self._stop_loss_buffer.Value = value

    @property
    def RiskRewardRatio(self):
        return self._risk_reward_ratio.Value

    @RiskRewardRatio.setter
    def RiskRewardRatio(self, value):
        self._risk_reward_ratio.Value = value

    def _reset_state(self):
        self._bucket = self._last_time = DateTime.MinValue
        self._high = self._low = Decimal(0)
        self._previous_high = self._previous_low = None
        self._count = 0
        self._pending = None

    def _remember_order(self, order):
        self._pending = order

    def OnReseted(self):
        super(bitcoin_1h15m_breakout_strategy, self).OnReseted()
        self._reset_state()

    def GetWorkingSecurities(self):
        return [(self.Security, self.CandleType), (self.Security, DataType.Level1)]

    def OnStarted2(self, time):
        frame = self.CandleType.Arg
        if not isinstance(frame, TimeSpan) or frame <= TimeSpan.Zero or self.HigherTimeFrame <= frame or self.HigherTimeFrame.Ticks % frame.Ticks != 0:
            raise ValueError("HigherTimeFrame must be an integer multiple of the lower time frame.")
        super(bitcoin_1h15m_breakout_strategy, self).OnStarted2(time)
        for field in (Level1Fields.BestBidPrice, Level1Fields.BestAskPrice):
            quote = Subscription(DataType.Level1, self.Security)
            quote.MarketData.BuildField = field
            self.SubscribeLevel1(quote).Start()
        candles = self.SubscribeCandles(self.CandleType)
        candles.Bind(self._process).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, candles)
            self.DrawOwnTrades(area)

    def _process(self, candle):
        if candle.State != CandleStates.Finished or candle.OpenTime <= self._last_time:
            return
        frame = self.CandleType.Arg
        bucket = DateTime(candle.OpenTime.Ticks // self.HigherTimeFrame.Ticks * self.HigherTimeFrame.Ticks, DateTimeKind.Utc)
        if bucket != self._bucket:
            complete = self._bucket != DateTime.MinValue and bucket == self._bucket.Add(self.HigherTimeFrame) and self._count == self.HigherTimeFrame.Ticks // frame.Ticks and self._last_time.Add(frame) == bucket
            self._previous_high = self._high if complete else None
            self._previous_low = self._low if complete else None
            self._bucket, self._high, self._low = bucket, candle.HighPrice, candle.LowPrice
            self._count = 1 if candle.OpenTime == bucket else 0
        else:
            self._count = self._count + 1 if self._count > 0 and candle.OpenTime == self._last_time.Add(frame) else 0
            self._high = max(self._high, candle.HighPrice)
            self._low = min(self._low, candle.LowPrice)
        self._last_time = candle.OpenTime
        if self._previous_high is None or self._previous_low is None or self.Position != 0 or not self.IsFormedAndOnlineAndAllowTrading():
            return
        if self._pending is not None and self._pending.State not in (OrderStates.Done, OrderStates.Failed):
            return
        side = Sides.Buy if candle.ClosePrice > self._previous_high else Sides.Sell if candle.ClosePrice < self._previous_low else None
        if side is None:
            return
        self.StartProtection(Unit(float(self.StopLossBuffer) * float(self.RiskRewardRatio), UnitTypes.Absolute),
                             Unit(float(self.StopLossBuffer), UnitTypes.Absolute), useMarketOrders=True, isLocalStop=True)
        if side == Sides.Buy:
            self.BuyMarket(self.Volume)
        else:
            self.SellMarket(self.Volume)

    def CreateClone(self):
        return bitcoin_1h15m_breakout_strategy()
