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

from enum import IntEnum
from System import Array
from StockSharp.Messages import Level1Fields
from StockSharp.Algo.Indicators import BollingerBands, IIndicator

class TpSlModes(IntEnum):
    None_ = 0
    TP = 1 << 0
    SL = 1 << 1
    Both = TP | SL

class bbtrend_supertrend_decision_strategy(Strategy):
    def __init__(self):
        super(bbtrend_supertrend_decision_strategy, self).__init__()
        self._short_bb_length = self.Param("ShortBbLength", 20).SetGreaterThanZero().SetDisplay(
            "ShortBbLength", "Short Bollinger close window", "Parameters")
        self._long_bb_length = self.Param("LongBbLength", 50).SetGreaterThanZero().SetDisplay(
            "LongBbLength", "Long Bollinger close window", "Parameters")
        self._std_dev = self.Param("StdDev", 2.0).SetGreaterThanZero().SetDisplay(
            "StdDev", "Population deviation multiplier", "Parameters")
        self._supertrend_length = self.Param("SupertrendLength", 10).SetGreaterThanZero().SetDisplay(
            "SupertrendLength", "Wilder true range period on BBTrend", "Parameters")
        self._supertrend_multiplier = self.Param("SupertrendMultiplier", 7.0).SetGreaterThanZero().SetDisplay(
            "SupertrendMultiplier", "SuperTrend band multiplier", "Parameters")
        self._trade_direction = self.Param[Nullable[Sides]]("TradeDirection", Nullable[Sides]()).SetDisplay(
            "TradeDirection", "Allowed entry side; null allows both", "Parameters")
        self._tp_sl_condition = self.Param("TpSlCondition", int(TpSlModes.None_)).SetDisplay(
            "TpSlCondition", "Optional percentage protection mode", "Parameters")
        self._take_profit_perc = self.Param("TakeProfitPerc", 30.0).SetGreaterThanZero().SetDisplay(
            "TakeProfitPerc", "Optional profit distance in percent", "Parameters")
        self._stop_loss_perc = self.Param("StopLossPerc", 20.0).SetGreaterThanZero().SetDisplay(
            "StopLossPerc", "Optional loss distance in percent", "Parameters")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))).SetDisplay(
            "CandleType", "Finished signal candle interval", "Parameters")
        self._reset_state()
        self.OrderRegistering += self._remember_order

    @property
    def ShortBbLength(self):
        return self._short_bb_length.Value

    @ShortBbLength.setter
    def ShortBbLength(self, value):
        self._short_bb_length.Value = value

    @property
    def LongBbLength(self):
        return self._long_bb_length.Value

    @LongBbLength.setter
    def LongBbLength(self, value):
        self._long_bb_length.Value = value

    @property
    def StdDev(self):
        return self._std_dev.Value

    @StdDev.setter
    def StdDev(self, value):
        self._std_dev.Value = value

    @property
    def SupertrendLength(self):
        return self._supertrend_length.Value

    @SupertrendLength.setter
    def SupertrendLength(self, value):
        self._supertrend_length.Value = value

    @property
    def SupertrendMultiplier(self):
        return self._supertrend_multiplier.Value

    @SupertrendMultiplier.setter
    def SupertrendMultiplier(self, value):
        self._supertrend_multiplier.Value = value

    @property
    def TradeDirection(self):
        return self._trade_direction.Value

    @TradeDirection.setter
    def TradeDirection(self, value):
        self._trade_direction.Value = value

    @property
    def TpSlCondition(self):
        return self._tp_sl_condition.Value

    @TpSlCondition.setter
    def TpSlCondition(self, value):
        self._tp_sl_condition.Value = value

    @property
    def TakeProfitPerc(self):
        return self._take_profit_perc.Value

    @TakeProfitPerc.setter
    def TakeProfitPerc(self, value):
        self._take_profit_perc.Value = value

    @property
    def StopLossPerc(self):
        return self._stop_loss_perc.Value

    @StopLossPerc.setter
    def StopLossPerc(self, value):
        self._stop_loss_perc.Value = value

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    def _reset_state(self):
        self._ranges = []
        self._previous = self._up = self._down = self._atr = self._line = None
        self._last_time = DateTime.MinValue
        self._pending = None

    def _remember_order(self, order):
        self._pending = order

    def OnReseted(self):
        super(bbtrend_supertrend_decision_strategy, self).OnReseted()
        self._reset_state()

    def GetWorkingSecurities(self):
        return [(self.Security, self.CandleType), (self.Security, DataType.Level1)]

    def OnStarted2(self, time):
        mode = TpSlModes(int(self.TpSlCondition))
        if self.ShortBbLength >= self.LongBbLength:
            raise ValueError("The short BB window must be smaller than the long window.")
        super(bbtrend_supertrend_decision_strategy, self).OnStarted2(time)
        self.StartProtection(Unit(float(self.TakeProfitPerc), UnitTypes.Percent) if mode in (TpSlModes.TP, TpSlModes.Both) else Unit(),
                             Unit(float(self.StopLossPerc), UnitTypes.Percent) if mode in (TpSlModes.SL, TpSlModes.Both) else Unit(),
                             useMarketOrders=True, isLocalStop=True)
        for field in (Level1Fields.BestBidPrice, Level1Fields.BestAskPrice):
            quote = Subscription(DataType.Level1, self.Security)
            quote.MarketData.BuildField = field
            self.SubscribeLevel1(quote).Start()
        short_bb = BollingerBands()
        short_bb.Length, short_bb.Width = self.ShortBbLength, self.StdDev
        long_bb = BollingerBands()
        long_bb.Length, long_bb.Width = self.LongBbLength, self.StdDev
        candles = self.SubscribeCandles(self.CandleType)
        candles.BindEx(Array[IIndicator]([short_bb, long_bb]), self._process).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, candles)
            self.DrawIndicator(area, short_bb)
            self.DrawIndicator(area, long_bb)
            self.DrawOwnTrades(area)

    def _process(self, candle, values):
        if candle.State != CandleStates.Finished or candle.OpenTime <= self._last_time or not all(v.IsFormed for v in values):
            return
        a, b = values[0], values[1]
        if a.MovingAverage is None or a.LowBand is None or a.UpBand is None or b.LowBand is None or b.UpBand is None or a.MovingAverage <= 0:
            return
        self._last_time = candle.OpenTime
        score = (abs(float(a.LowBand) - float(b.LowBand)) - abs(float(a.UpBand) - float(b.UpBand))) / float(a.MovingAverage) * 100.0
        previous = self._previous
        self._previous = score
        if previous is None:
            return
        change = abs(score - previous)
        if self._atr is None:
            self._ranges.append(change)
            if len(self._ranges) < self.SupertrendLength:
                return
            self._atr = sum(self._ranges) / self.SupertrendLength
        else:
            self._atr = (self._atr * (self.SupertrendLength - 1) + change) / self.SupertrendLength
        upper = (score + previous) / 2.0 + float(self.SupertrendMultiplier) * self._atr
        lower = (score + previous) / 2.0 - float(self.SupertrendMultiplier) * self._atr
        if self._up is not None and upper >= self._up and previous <= self._up:
            upper = self._up
        if self._down is not None and lower <= self._down and previous >= self._down:
            lower = self._down
        down = self._line is None or (score <= upper if self._line == self._up else score < lower)
        self._up, self._down, self._line = upper, lower, upper if down else lower
        if not self.IsFormedAndOnlineAndAllowTrading() or (self._pending is not None and self._pending.State not in (OrderStates.Done, OrderStates.Failed)):
            return
        side = Sides.Sell if down else Sides.Buy
        allowed = self.TradeDirection is None or self.TradeDirection == side
        target = (self.Volume if side == Sides.Buy else -self.Volume) if allowed else Decimal(0)
        delta = target - self.Position
        if delta > 0:
            self.BuyMarket(delta)
        elif delta < 0:
            self.SellMarket(-delta)

    def CreateClone(self):
        return bbtrend_supertrend_decision_strategy()
