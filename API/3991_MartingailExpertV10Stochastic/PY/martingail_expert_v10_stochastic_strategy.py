import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy
from StockSharp.Algo.Indicators import StochasticOscillator

class martingail_expert_v10_stochastic_strategy(Strategy):
    def __init__(self):
        super(martingail_expert_v10_stochastic_strategy, self).__init__()

        self._step_points = self.Param("StepPoints", 500.0) \
            .SetDisplay("Step", "Price step in points before averaging", "Martingale")
        self._step_mode = self.Param("StepMode", 0) \
            .SetDisplay("Step Mode", "0 - fixed step, 1 - step plus extra points per order", "Martingale")
        self._profit_factor_points = self.Param("ProfitFactorPoints", 300.0) \
            .SetDisplay("Profit Factor", "Points multiplied by order count for take profit", "Martingale")
        self._multiplier = self.Param("Multiplier", 1.5) \
            .SetDisplay("Multiplier", "Martingale multiplier for averaging", "Martingale")
        self._k_period = self.Param("KPeriod", 14) \
            .SetDisplay("%K Period", "Stochastic %K lookback", "Indicators")
        self._d_period = self.Param("DPeriod", 3) \
            .SetDisplay("%D Period", "Stochastic %D smoothing", "Indicators")
        self._zone_buy = self.Param("ZoneBuy", 50.0) \
            .SetDisplay("Zone Buy", "%D lower bound to allow buys", "Indicators")
        self._zone_sell = self.Param("ZoneSell", 50.0) \
            .SetDisplay("Zone Sell", "%D upper bound to allow sells", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(10))) \
            .SetDisplay("Candle Type", "Timeframe used for processing", "General")

        self.Volume = 1

        self._point_size = 0.0
        self._prev_k = None
        self._prev_d = None

        self._buy_last_price = 0.0
        self._buy_last_volume = 0.0
        self._buy_total_volume = 0.0
        self._buy_weighted_sum = 0.0
        self._buy_order_count = 0
        self._buy_take_profit = 0.0

        self._sell_last_price = 0.0
        self._sell_last_volume = 0.0
        self._sell_total_volume = 0.0
        self._sell_weighted_sum = 0.0
        self._sell_order_count = 0
        self._sell_take_profit = 0.0

    @property
    def StepPoints(self):
        return self._step_points.Value

    @property
    def StepMode(self):
        return self._step_mode.Value

    @property
    def ProfitFactorPoints(self):
        return self._profit_factor_points.Value

    @property
    def Multiplier(self):
        return self._multiplier.Value

    @property
    def KPeriod(self):
        return self._k_period.Value

    @property
    def DPeriod(self):
        return self._d_period.Value

    @property
    def ZoneBuy(self):
        return self._zone_buy.Value

    @property
    def ZoneSell(self):
        return self._zone_sell.Value

    @property
    def CandleType(self):
        return self._candle_type.Value

    def OnStarted(self, time):
        super(martingail_expert_v10_stochastic_strategy, self).OnStarted(time)

        ps = self.Security.PriceStep if self.Security is not None else None
        self._point_size = float(ps) if ps is not None else 1.0
        if self._point_size <= 0:
            self._point_size = 1.0

        self._stochastic = StochasticOscillator()
        self._stochastic.K.Length = self.KPeriod
        self._stochastic.D.Length = self.DPeriod

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.BindEx(self._stochastic, self.ProcessCandle).Start()

    def ProcessCandle(self, candle, stochastic_value):
        if candle.State != CandleStates.Finished:
            return

        k_val = stochastic_value.K
        d_val = stochastic_value.D

        if k_val is None or d_val is None:
            return

        current_k = float(k_val)
        current_d = float(d_val)

        if not self._stochastic.IsFormed:
            self._prev_k = current_k
            self._prev_d = current_d
            return

        trading_allowed = self.IsFormedAndOnlineAndAllowTrading()

        self._manage_clusters(candle, trading_allowed)

        if not trading_allowed:
            self._prev_k = current_k
            self._prev_d = current_d
            return

        if self.Position == 0 and self._buy_order_count == 0 and self._sell_order_count == 0:
            if self._prev_k is not None and self._prev_d is not None:
                if self._prev_k > self._prev_d and self._prev_d > float(self.ZoneBuy):
                    self._open_long(float(candle.ClosePrice))
                elif self._prev_k < self._prev_d and self._prev_d < float(self.ZoneSell):
                    self._open_short(float(candle.ClosePrice))

        self._prev_k = current_k
        self._prev_d = current_d

    def _manage_clusters(self, candle, trading_allowed):
        if self.Position > 0 and self._buy_order_count > 0:
            self._handle_long_cluster(candle, trading_allowed)
        elif self.Position < 0 and self._sell_order_count > 0:
            self._handle_short_cluster(candle, trading_allowed)
        elif self.Position == 0:
            if self._buy_order_count > 0 or self._sell_order_count > 0:
                self._reset_long_state()
                self._reset_short_state()

    def _handle_long_cluster(self, candle, trading_allowed):
        if not trading_allowed or self._point_size <= 0:
            return

        if self._buy_take_profit > 0 and float(candle.HighPrice) >= self._buy_take_profit:
            self.SellMarket(abs(self.Position))
            self._reset_long_state()
            return

        current_count = max(1, self._buy_order_count)
        step_pts = float(self.StepPoints)
        if self.StepMode != 0:
            step_pts = step_pts + max(0.0, current_count * 2.0 - 2.0)
        add_trigger = self._buy_last_price - step_pts * self._point_size

        if self._buy_last_volume > 0 and float(candle.LowPrice) <= add_trigger:
            next_volume = max(1.0, float(round(float(self._buy_last_volume) * float(self.Multiplier))))
            self.BuyMarket(next_volume)

            execution_price = float(candle.ClosePrice)
            self._buy_last_volume = next_volume
            self._buy_last_price = execution_price
            self._buy_total_volume += next_volume
            self._buy_weighted_sum += execution_price * next_volume
            self._buy_order_count += 1
            self._recalc_long_tp()

    def _handle_short_cluster(self, candle, trading_allowed):
        if not trading_allowed or self._point_size <= 0:
            return

        if self._sell_take_profit > 0 and float(candle.LowPrice) <= self._sell_take_profit:
            self.BuyMarket(abs(self.Position))
            self._reset_short_state()
            return

        current_count = max(1, self._sell_order_count)
        step_pts = float(self.StepPoints)
        if self.StepMode != 0:
            step_pts = step_pts + max(0.0, current_count * 2.0 - 2.0)
        add_trigger = self._sell_last_price + step_pts * self._point_size

        if self._sell_last_volume > 0 and float(candle.HighPrice) >= add_trigger:
            next_volume = max(1.0, float(round(float(self._sell_last_volume) * float(self.Multiplier))))
            self.SellMarket(next_volume)

            execution_price = float(candle.ClosePrice)
            self._sell_last_volume = next_volume
            self._sell_last_price = execution_price
            self._sell_total_volume += next_volume
            self._sell_weighted_sum += execution_price * next_volume
            self._sell_order_count += 1
            self._recalc_short_tp()

    def _open_long(self, price):
        vol = self.Volume
        self.BuyMarket(vol)

        self._buy_last_price = price
        self._buy_last_volume = vol
        self._buy_total_volume = vol
        self._buy_weighted_sum = price * vol
        self._buy_order_count = 1
        self._recalc_long_tp()
        self._reset_short_state()

    def _open_short(self, price):
        vol = self.Volume
        self.SellMarket(vol)

        self._sell_last_price = price
        self._sell_last_volume = vol
        self._sell_total_volume = vol
        self._sell_weighted_sum = price * vol
        self._sell_order_count = 1
        self._recalc_short_tp()
        self._reset_long_state()

    def _recalc_long_tp(self):
        avg = self._buy_weighted_sum / self._buy_total_volume if self._buy_total_volume > 0 else self._buy_last_price
        self._buy_take_profit = avg + float(self.ProfitFactorPoints) * self._point_size

    def _recalc_short_tp(self):
        avg = self._sell_weighted_sum / self._sell_total_volume if self._sell_total_volume > 0 else self._sell_last_price
        self._sell_take_profit = avg - float(self.ProfitFactorPoints) * self._point_size

    def _reset_long_state(self):
        self._buy_last_price = 0.0
        self._buy_last_volume = 0.0
        self._buy_total_volume = 0.0
        self._buy_weighted_sum = 0.0
        self._buy_order_count = 0
        self._buy_take_profit = 0.0

    def _reset_short_state(self):
        self._sell_last_price = 0.0
        self._sell_last_volume = 0.0
        self._sell_total_volume = 0.0
        self._sell_weighted_sum = 0.0
        self._sell_order_count = 0
        self._sell_take_profit = 0.0

    def OnReseted(self):
        super(martingail_expert_v10_stochastic_strategy, self).OnReseted()
        self._point_size = 0.0
        self._prev_k = None
        self._prev_d = None
        self._reset_long_state()
        self._reset_short_state()

    def CreateClone(self):
        return martingail_expert_v10_stochastic_strategy()
