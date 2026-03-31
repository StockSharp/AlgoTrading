import clr
import math

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan

from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import (
    MovingAverageConvergenceDivergence, ExponentialMovingAverage
)
from StockSharp.Algo.Strategies import Strategy


class dealers_trade_macd_strategy(Strategy):
    def __init__(self):
        super(dealers_trade_macd_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4)))
        self._fixed_volume = self.Param("FixedVolume", 0.1)
        self._risk_percent = self.Param("RiskPercent", 5.0)
        self._stop_loss_points = self.Param("StopLossPoints", 90.0)
        self._take_profit_points = self.Param("TakeProfitPoints", 30.0)
        self._trailing_stop_points = self.Param("TrailingStopPoints", 15.0)
        self._trailing_step_points = self.Param("TrailingStepPoints", 5.0)
        self._max_positions = self.Param("MaxPositions", 2)
        self._interval_points = self.Param("IntervalPoints", 50.0)
        self._secure_profit = self.Param("SecureProfit", 50.0)
        self._account_protection = self.Param("AccountProtection", True)
        self._positions_for_protection = self.Param("PositionsForProtection", 3)
        self._reverse_condition = self.Param("ReverseCondition", False)
        self._macd_fast_period = self.Param("MacdFastPeriod", 14)
        self._macd_slow_period = self.Param("MacdSlowPeriod", 26)
        self._macd_signal_period = self.Param("MacdSignalPeriod", 1)
        self._max_volume = self.Param("MaxVolume", 5.0)
        self._volume_multiplier = self.Param("VolumeMultiplier", 1.6)

        self._macd = None
        self._previous_macd = None
        self._last_entry_price = 0.0
        self._cooldown = 0
        self._long_positions = []
        self._short_positions = []

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    @property
    def FixedVolume(self):
        return self._fixed_volume.Value

    @property
    def RiskPercent(self):
        return self._risk_percent.Value

    @property
    def StopLossPoints(self):
        return self._stop_loss_points.Value

    @property
    def TakeProfitPoints(self):
        return self._take_profit_points.Value

    @property
    def TrailingStopPoints(self):
        return self._trailing_stop_points.Value

    @property
    def TrailingStepPoints(self):
        return self._trailing_step_points.Value

    @property
    def MaxPositions(self):
        return self._max_positions.Value

    @property
    def IntervalPoints(self):
        return self._interval_points.Value

    @property
    def SecureProfit(self):
        return self._secure_profit.Value

    @property
    def AccountProtection(self):
        return self._account_protection.Value

    @property
    def PositionsForProtection(self):
        return self._positions_for_protection.Value

    @property
    def ReverseCondition(self):
        return self._reverse_condition.Value

    @property
    def MacdFastPeriod(self):
        return self._macd_fast_period.Value

    @property
    def MacdSlowPeriod(self):
        return self._macd_slow_period.Value

    @property
    def MacdSignalPeriod(self):
        return self._macd_signal_period.Value

    @property
    def MaxVolume(self):
        return self._max_volume.Value

    @property
    def VolumeMultiplier(self):
        return self._volume_multiplier.Value

    def OnStarted2(self, time):
        super(dealers_trade_macd_strategy, self).OnStarted2(time)

        slow_ema = ExponentialMovingAverage()
        slow_ema.Length = self.MacdSlowPeriod
        fast_ema = ExponentialMovingAverage()
        fast_ema.Length = self.MacdFastPeriod
        self._macd = MovingAverageConvergenceDivergence(slow_ema, fast_ema)

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(self._macd, self._process_candle).Start()

    def _process_candle(self, candle, macd_value):
        if candle.State != CandleStates.Finished:
            return

        self._handle_trailing_and_exits(candle)

        if not self.IsFormedAndOnlineAndAllowTrading():
            self._previous_macd = macd_value
            return

        if self._cooldown > 0:
            self._cooldown -= 1
            self._previous_macd = macd_value
            return

        open_positions = len(self._long_positions) + len(self._short_positions)
        continue_opening = open_positions < self.MaxPositions

        direction = 0
        if self._previous_macd is None:
            self._previous_macd = macd_value
            return

        if macd_value > self._previous_macd:
            direction = 1
        elif macd_value < self._previous_macd:
            direction = -1

        if self.ReverseCondition:
            direction = -direction

        if self.AccountProtection and open_positions > self.PositionsForProtection:
            total_profit = self._calc_total_profit(float(candle.ClosePrice))
            if total_profit >= self.SecureProfit:
                self._close_most_profitable(float(candle.ClosePrice))
                self._previous_macd = macd_value
                return

        if continue_opening and direction > 0 and len(self._short_positions) == 0:
            self._try_open_long(candle)
        elif continue_opening and direction < 0 and len(self._long_positions) == 0:
            self._try_open_short(candle)

        self._previous_macd = macd_value

    def _handle_trailing_and_exits(self, candle):
        step = self._get_price_step()
        trail_dist = self.TrailingStopPoints * step
        trail_act = (self.TrailingStopPoints + self.TrailingStepPoints) * step

        long_exits = []
        for s in list(self._long_positions):
            if s["take"] > 0 and float(candle.HighPrice) >= s["take"]:
                long_exits.append(s); continue
            if s["stop"] > 0 and float(candle.LowPrice) <= s["stop"]:
                long_exits.append(s); continue
            if self.TrailingStopPoints > 0 and float(candle.ClosePrice) - s["entry"] > trail_act:
                cs = float(candle.ClosePrice) - trail_dist
                if s["stop"] == 0 or s["stop"] < float(candle.ClosePrice) - trail_act:
                    s["stop"] = cs
        for s in long_exits:
            self.Volume = s["volume"]
            self.SellMarket()
            self._long_positions.remove(s)
            self._last_entry_price = 0.0

        short_exits = []
        for s in list(self._short_positions):
            if s["take"] > 0 and float(candle.LowPrice) <= s["take"]:
                short_exits.append(s); continue
            if s["stop"] > 0 and float(candle.HighPrice) >= s["stop"]:
                short_exits.append(s); continue
            if self.TrailingStopPoints > 0 and s["entry"] - float(candle.ClosePrice) > trail_act:
                cs = float(candle.ClosePrice) + trail_dist
                if s["stop"] == 0 or s["stop"] > float(candle.ClosePrice) + trail_act:
                    s["stop"] = cs
        for s in short_exits:
            self.Volume = s["volume"]
            self.BuyMarket()
            self._short_positions.remove(s)
            self._last_entry_price = 0.0

    def _try_open_long(self, candle):
        step = self._get_price_step()
        interval = self.IntervalPoints * step
        if self._last_entry_price != 0 and abs(self._last_entry_price - float(candle.ClosePrice)) < interval:
            return

        base_vol = self.FixedVolume if self.FixedVolume > 0 else self._calc_risk_vol(step)
        if base_vol <= 0:
            return

        n = len(self._long_positions) + len(self._short_positions)
        coeff = 1.0 if n == 0 else math.pow(self.VolumeMultiplier, n + 1)
        vol = self._norm_vol(base_vol * coeff)
        if vol <= 0 or vol > self.MaxVolume:
            return

        sd = self.StopLossPoints * step
        td = self.TakeProfitPoints * step
        self.Volume = vol
        self.BuyMarket()
        self._long_positions.append({
            "entry": float(candle.ClosePrice), "volume": vol,
            "stop": float(candle.ClosePrice) - sd if sd > 0 else 0,
            "take": float(candle.ClosePrice) + td if td > 0 else 0
        })
        self._last_entry_price = float(candle.ClosePrice)
        self._cooldown = 3

    def _try_open_short(self, candle):
        step = self._get_price_step()
        interval = self.IntervalPoints * step
        if self._last_entry_price != 0 and abs(self._last_entry_price - float(candle.ClosePrice)) < interval:
            return

        base_vol = self.FixedVolume if self.FixedVolume > 0 else self._calc_risk_vol(step)
        if base_vol <= 0:
            return

        n = len(self._long_positions) + len(self._short_positions)
        coeff = 1.0 if n == 0 else math.pow(self.VolumeMultiplier, n + 1)
        vol = self._norm_vol(base_vol * coeff)
        if vol <= 0 or vol > self.MaxVolume:
            return

        sd = self.StopLossPoints * step
        td = self.TakeProfitPoints * step
        self.Volume = vol
        self.SellMarket()
        self._short_positions.append({
            "entry": float(candle.ClosePrice), "volume": vol,
            "stop": float(candle.ClosePrice) + sd if sd > 0 else 0,
            "take": float(candle.ClosePrice) - td if td > 0 else 0
        })
        self._last_entry_price = float(candle.ClosePrice)
        self._cooldown = 3

    def _calc_risk_vol(self, price_step):
        if self.StopLossPoints <= 0:
            return 0
        sd = self.StopLossPoints * price_step
        if sd <= 0 or self.Portfolio is None:
            return 0
        eq = float(self.Portfolio.CurrentValue) if self.Portfolio.CurrentValue is not None else 0
        if eq <= 0:
            return 0
        return eq * (self.RiskPercent / 100.0) / sd

    def _calc_total_profit(self, price):
        p = 0.0
        for s in self._long_positions:
            p += (price - s["entry"]) * s["volume"]
        for s in self._short_positions:
            p += (s["entry"] - price) * s["volume"]
        return p

    def _close_most_profitable(self, price):
        best = None
        best_long = False
        best_pnl = 0.0
        for s in self._long_positions:
            pnl = (price - s["entry"]) * s["volume"]
            if pnl > best_pnl:
                best_pnl = pnl; best = s; best_long = True
        for s in self._short_positions:
            pnl = (s["entry"] - price) * s["volume"]
            if pnl > best_pnl:
                best_pnl = pnl; best = s; best_long = False
        if best is None or best_pnl <= 0:
            return
        if best_long:
            self.SellMarket()
            self._long_positions.remove(best)
        else:
            self.BuyMarket()
            self._short_positions.remove(best)
        self._last_entry_price = 0.0

    def _norm_vol(self, vol):
        if vol <= 0:
            return 0
        sec = self.Security
        step = float(sec.VolumeStep) if sec is not None and sec.VolumeStep is not None else 0
        if step > 0:
            vol = math.floor(vol / step) * step
        return vol

    def _get_price_step(self):
        sec = self.Security
        step = float(sec.PriceStep) if sec is not None and sec.PriceStep is not None else 0
        if step > 0:
            return step
        d = sec.Decimals if sec is not None and sec.Decimals is not None else 0
        if d > 0:
            return math.pow(10, -d)
        return 0.0001

    def OnReseted(self):
        super(dealers_trade_macd_strategy, self).OnReseted()
        self._previous_macd = None
        self._last_entry_price = 0.0
        self._cooldown = 0
        self._long_positions = []
        self._short_positions = []

    def CreateClone(self):
        return dealers_trade_macd_strategy()
