import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy


class trailing_stop_and_take_strategy(Strategy):
    POS_ALL = 0
    POS_LONG = 1
    POS_SHORT = 2

    def __init__(self):
        super(trailing_stop_and_take_strategy, self).__init__()
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromDays(1)))
        self._position_type = self.Param("PositionType", self.POS_ALL)
        self._initial_stop_loss_points = self.Param("InitialStopLossPoints", 400.0)
        self._initial_take_profit_points = self.Param("InitialTakeProfitPoints", 400.0)
        self._trailing_stop_loss_points = self.Param("TrailingStopLossPoints", 200.0)
        self._trailing_take_profit_points = self.Param("TrailingTakeProfitPoints", 200.0)
        self._trailing_step_points = self.Param("TrailingStepPoints", 10.0)
        self._epsilon = self.Param("Epsilon", 0.0000001)
        self._allow_trailing_loss = self.Param("AllowTrailingLoss", False)
        self._breakeven_points = self.Param("BreakevenPoints", 6.0)
        self._spread_multiplier = self.Param("SpreadMultiplier", 2)
        self._price_step = 1.0
        self._previous_position = 0.0
        self._long_stop = None
        self._long_take = None
        self._short_stop = None
        self._short_take = None
        self._entry_price = 0.0

    @property
    def CandleType(self):
        return self._candle_type.Value

    @property
    def PositionType(self):
        return self._position_type.Value

    @property
    def InitialStopLossPoints(self):
        return self._initial_stop_loss_points.Value

    @property
    def InitialTakeProfitPoints(self):
        return self._initial_take_profit_points.Value

    @property
    def TrailingStopLossPoints(self):
        return self._trailing_stop_loss_points.Value

    @property
    def TrailingTakeProfitPoints(self):
        return self._trailing_take_profit_points.Value

    @property
    def TrailingStepPoints(self):
        return self._trailing_step_points.Value

    @property
    def Epsilon(self):
        return self._epsilon.Value

    @property
    def AllowTrailingLoss(self):
        return self._allow_trailing_loss.Value

    @property
    def BreakevenPoints(self):
        return self._breakeven_points.Value

    @property
    def SpreadMultiplier(self):
        return self._spread_multiplier.Value

    def OnStarted2(self, time):
        super(trailing_stop_and_take_strategy, self).OnStarted2(time)
        sec = self.Security
        self._price_step = float(sec.PriceStep) if sec is not None and sec.PriceStep is not None and float(sec.PriceStep) > 0 else 1.0
        self._previous_position = 0.0
        self._reset_levels()
        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(self._process_candle).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return
        pos = float(self.Position)
        if pos > 0:
            if self.PositionType == self.POS_SHORT:
                self._reset_long_levels()
            else:
                if self._previous_position <= 0:
                    self._reset_short_levels()
                self._ensure_long_initialized()
                self._update_long_trailing(candle)
                self._manage_long_exits(candle)
        elif pos < 0:
            if self.PositionType == self.POS_LONG:
                self._reset_short_levels()
            else:
                if self._previous_position >= 0:
                    self._reset_long_levels()
                self._ensure_short_initialized()
                self._update_short_trailing(candle)
                self._manage_short_exits(candle)
        else:
            self._reset_levels()
            self._entry_price = 0.0
        self._try_enter(candle)
        self._previous_position = float(self.Position)

    def _try_enter(self, candle):
        pos = float(self.Position)
        vol = float(self.Volume)
        if pos != 0 or vol <= 0:
            return
        close = float(candle.ClosePrice)
        if self.PositionType == self.POS_LONG:
            if close > float(candle.OpenPrice):
                self.BuyMarket(vol)
                self._entry_price = close
        elif self.PositionType == self.POS_SHORT:
            if close < float(candle.OpenPrice):
                self.SellMarket(vol)
                self._entry_price = close
        else:
            if close > float(candle.OpenPrice):
                self.BuyMarket(vol)
                self._entry_price = close
            elif close < float(candle.OpenPrice):
                self.SellMarket(vol)
                self._entry_price = close

    def _ensure_long_initialized(self):
        if float(self.Position) <= 0:
            return
        entry = self._entry_price
        if entry <= 0:
            return
        min_dist = self._get_min_stop_distance()
        if self._long_stop is None:
            pts = float(self.InitialStopLossPoints) if float(self.InitialStopLossPoints) > 0 else (float(self.TrailingStopLossPoints) if float(self.TrailingStopLossPoints) > 0 else 0.0)
            if pts > 0:
                candidate = entry - pts * self._price_step
                min_allowed = entry - min_dist
                self._long_stop = min(candidate, min_allowed)
        if self._long_take is None:
            pts = float(self.InitialTakeProfitPoints) if float(self.InitialTakeProfitPoints) > 0 else (float(self.TrailingTakeProfitPoints) if float(self.TrailingTakeProfitPoints) > 0 else 0.0)
            if pts > 0:
                candidate = entry + pts * self._price_step
                min_allowed = entry + min_dist
                self._long_take = max(candidate, min_allowed)

    def _ensure_short_initialized(self):
        if float(self.Position) >= 0:
            return
        entry = self._entry_price
        if entry <= 0:
            return
        min_dist = self._get_min_stop_distance()
        if self._short_stop is None:
            pts = float(self.InitialStopLossPoints) if float(self.InitialStopLossPoints) > 0 else (float(self.TrailingStopLossPoints) if float(self.TrailingStopLossPoints) > 0 else 0.0)
            if pts > 0:
                candidate = entry + pts * self._price_step
                min_allowed = entry + min_dist
                self._short_stop = max(candidate, min_allowed)
        if self._short_take is None:
            pts = float(self.InitialTakeProfitPoints) if float(self.InitialTakeProfitPoints) > 0 else (float(self.TrailingTakeProfitPoints) if float(self.TrailingTakeProfitPoints) > 0 else 0.0)
            if pts > 0:
                candidate = entry - pts * self._price_step
                min_allowed = entry - min_dist
                self._short_take = min(candidate, min_allowed)

    def _update_long_trailing(self, candle):
        entry = self._entry_price
        if entry <= 0:
            return
        breakeven = entry + float(self.BreakevenPoints) * self._price_step
        trailing_step = max(float(self.TrailingStepPoints) * self._price_step, float(self.Epsilon))
        min_dist = self._get_min_stop_distance()
        if float(self.TrailingStopLossPoints) > 0:
            candidate = float(candle.ClosePrice) - float(self.TrailingStopLossPoints) * self._price_step
            min_allowed = float(candle.ClosePrice) - min_dist
            new_stop = min(candidate, min_allowed)
            if not self.AllowTrailingLoss and new_stop < breakeven:
                pass
            elif self._long_stop is None or new_stop > self._long_stop + trailing_step:
                self._long_stop = new_stop
        if float(self.TrailingTakeProfitPoints) > 0:
            candidate = float(candle.ClosePrice) + float(self.TrailingTakeProfitPoints) * self._price_step
            min_allowed = float(candle.ClosePrice) + min_dist
            new_take = max(candidate, min_allowed)
            if not self.AllowTrailingLoss and new_take < breakeven:
                new_take = breakeven
            if self._long_take is None or new_take < self._long_take - trailing_step:
                self._long_take = new_take

    def _update_short_trailing(self, candle):
        entry = self._entry_price
        if entry <= 0:
            return
        breakeven = entry - float(self.BreakevenPoints) * self._price_step
        trailing_step = max(float(self.TrailingStepPoints) * self._price_step, float(self.Epsilon))
        min_dist = self._get_min_stop_distance()
        if float(self.TrailingStopLossPoints) > 0:
            candidate = float(candle.ClosePrice) + float(self.TrailingStopLossPoints) * self._price_step
            min_allowed = float(candle.ClosePrice) + min_dist
            new_stop = max(candidate, min_allowed)
            if not self.AllowTrailingLoss and new_stop > breakeven:
                pass
            elif self._short_stop is None or new_stop < self._short_stop - trailing_step:
                self._short_stop = new_stop
        if float(self.TrailingTakeProfitPoints) > 0:
            candidate = float(candle.ClosePrice) - float(self.TrailingTakeProfitPoints) * self._price_step
            min_allowed = float(candle.ClosePrice) - min_dist
            new_take = min(candidate, min_allowed)
            if not self.AllowTrailingLoss and new_take > breakeven:
                new_take = breakeven
            if self._short_take is None or new_take > self._short_take + trailing_step:
                self._short_take = new_take

    def _manage_long_exits(self, candle):
        if self._long_stop is not None and float(candle.LowPrice) <= self._long_stop:
            self.SellMarket(float(self.Position))
            self._reset_long_levels()
            return
        if self._long_take is not None and float(candle.HighPrice) >= self._long_take:
            self.SellMarket(float(self.Position))
            self._reset_long_levels()

    def _manage_short_exits(self, candle):
        if self._short_stop is not None and float(candle.HighPrice) >= self._short_stop:
            self.BuyMarket(abs(float(self.Position)))
            self._reset_short_levels()
            return
        if self._short_take is not None and float(candle.LowPrice) <= self._short_take:
            self.BuyMarket(abs(float(self.Position)))
            self._reset_short_levels()

    def _get_min_stop_distance(self):
        mult = self.SpreadMultiplier if self.SpreadMultiplier >= 1 else 1
        return self._price_step * mult

    def _reset_levels(self):
        self._reset_long_levels()
        self._reset_short_levels()

    def _reset_long_levels(self):
        self._long_stop = None
        self._long_take = None

    def _reset_short_levels(self):
        self._short_stop = None
        self._short_take = None

    def OnOwnTradeReceived(self, trade):
        super(trailing_stop_and_take_strategy, self).OnOwnTradeReceived(trade)
        if trade is None or trade.Trade is None:
            return
        pos = float(self.Position)
        if pos != 0 and self._entry_price == 0:
            self._entry_price = float(trade.Trade.Price)
        if pos == 0:
            self._entry_price = 0.0

    def OnReseted(self):
        super(trailing_stop_and_take_strategy, self).OnReseted()
        self._price_step = 0.0
        self._previous_position = 0.0
        self._entry_price = 0.0
        self._reset_levels()

    def CreateClone(self):
        return trailing_stop_and_take_strategy()
