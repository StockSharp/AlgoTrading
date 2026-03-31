import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import ExponentialMovingAverage, RelativeStrengthIndex, WilliamsR, DeMarker
from StockSharp.Algo.Strategies import Strategy


class polish_layer_strategy(Strategy):
    def __init__(self):
        super(polish_layer_strategy, self).__init__()

        self._short_ema_period = self.Param("ShortEmaPeriod", 9)
        self._long_ema_period = self.Param("LongEmaPeriod", 45)
        self._rsi_period = self.Param("RsiPeriod", 14)
        self._stochastic_k_period = self.Param("StochasticKPeriod", 5)
        self._stochastic_d_period = self.Param("StochasticDPeriod", 3)
        self._stochastic_slowing = self.Param("StochasticSlowing", 3)
        self._williams_r_period = self.Param("WilliamsRPeriod", 14)
        self._de_marker_period = self.Param("DeMarkerPeriod", 14)
        self._take_profit_points = self.Param("TakeProfitPoints", 17)
        self._stop_loss_points = self.Param("StopLossPoints", 77)
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(30)))

        self._prev_short_ema = None
        self._prev_long_ema = None
        self._prev_rsi = None
        self._prev_prev_rsi = None
        self._prev_stoch_k = None
        self._prev_williams_r = None
        self._prev_de_marker = None

    @property
    def ShortEmaPeriod(self):
        return self._short_ema_period.Value

    @ShortEmaPeriod.setter
    def ShortEmaPeriod(self, value):
        self._short_ema_period.Value = value

    @property
    def LongEmaPeriod(self):
        return self._long_ema_period.Value

    @LongEmaPeriod.setter
    def LongEmaPeriod(self, value):
        self._long_ema_period.Value = value

    @property
    def RsiPeriod(self):
        return self._rsi_period.Value

    @RsiPeriod.setter
    def RsiPeriod(self, value):
        self._rsi_period.Value = value

    @property
    def StochasticKPeriod(self):
        return self._stochastic_k_period.Value

    @StochasticKPeriod.setter
    def StochasticKPeriod(self, value):
        self._stochastic_k_period.Value = value

    @property
    def WilliamsRPeriod(self):
        return self._williams_r_period.Value

    @WilliamsRPeriod.setter
    def WilliamsRPeriod(self, value):
        self._williams_r_period.Value = value

    @property
    def DeMarkerPeriod(self):
        return self._de_marker_period.Value

    @DeMarkerPeriod.setter
    def DeMarkerPeriod(self, value):
        self._de_marker_period.Value = value

    @property
    def TakeProfitPoints(self):
        return self._take_profit_points.Value

    @TakeProfitPoints.setter
    def TakeProfitPoints(self, value):
        self._take_profit_points.Value = value

    @property
    def StopLossPoints(self):
        return self._stop_loss_points.Value

    @StopLossPoints.setter
    def StopLossPoints(self, value):
        self._stop_loss_points.Value = value

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    def OnStarted2(self, time):
        super(polish_layer_strategy, self).OnStarted2(time)

        self._prev_short_ema = None
        self._prev_long_ema = None
        self._prev_rsi = None
        self._prev_prev_rsi = None
        self._prev_stoch_k = None
        self._prev_williams_r = None
        self._prev_de_marker = None

        self.Volume = 1

        short_ema = ExponentialMovingAverage()
        short_ema.Length = self.ShortEmaPeriod
        long_ema = ExponentialMovingAverage()
        long_ema.Length = self.LongEmaPeriod
        rsi = RelativeStrengthIndex()
        rsi.Length = self.RsiPeriod
        stoch_rsi = RelativeStrengthIndex()
        stoch_rsi.Length = self.StochasticKPeriod
        williams_r = WilliamsR()
        williams_r.Length = self.WilliamsRPeriod
        de_marker = DeMarker()
        de_marker.Length = self.DeMarkerPeriod

        self._short_ema = short_ema
        self._long_ema = long_ema
        self._rsi = rsi
        self._stoch_rsi = stoch_rsi
        self._williams_r_ind = williams_r
        self._de_marker_ind = de_marker

        self._cur_short_ema = None
        self._cur_long_ema = None
        self._cur_rsi = None
        self._cur_stoch_k = None
        self._cur_williams = None
        self._cur_demarker = None
        self._last_indicators_time = None
        self._last_stoch_time = None
        self._last_processed_time = None

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(short_ema, long_ema, rsi, williams_r, de_marker, self.ProcessMainIndicators) \
            .BindEx(stoch_rsi, self.ProcessStochastic) \
            .Start()

        step = float(self.Security.PriceStep) if self.Security is not None and self.Security.PriceStep is not None else 1.0
        if step <= 0.0:
            step = 1.0

        self.StartProtection(
            stopLoss=Unit(int(self.StopLossPoints) * step, UnitTypes.Absolute),
            takeProfit=Unit(int(self.TakeProfitPoints) * step, UnitTypes.Absolute))

    def ProcessMainIndicators(self, candle, short_ema_val, long_ema_val, rsi_val, williams_val, demarker_val):
        if candle.State != CandleStates.Finished:
            return

        self._cur_short_ema = float(short_ema_val)
        self._cur_long_ema = float(long_ema_val)
        self._cur_rsi = float(rsi_val)
        self._cur_williams = float(williams_val)
        self._cur_demarker = float(demarker_val)
        self._last_indicators_time = candle.OpenTime

        self._try_process_signal(candle)

    def ProcessStochastic(self, candle, stoch_value):
        if candle.State != CandleStates.Finished:
            return

        if not stoch_value.IsFinal or not self._stoch_rsi.IsFormed:
            return

        self._cur_stoch_k = float(stoch_value)
        self._last_stoch_time = candle.OpenTime

        self._try_process_signal(candle)

    def _try_process_signal(self, candle):
        if self._last_indicators_time != candle.OpenTime or self._last_stoch_time != candle.OpenTime:
            return
        if self._last_processed_time == candle.OpenTime:
            return

        if not self._indicators_formed():
            self._update_previous_from_current()
            self._last_processed_time = candle.OpenTime
            return

        self._execute_trading_logic(candle)
        self._update_previous_from_current()
        self._last_processed_time = candle.OpenTime

    def _indicators_formed(self):
        return (self._short_ema.IsFormed and self._long_ema.IsFormed and
                self._rsi.IsFormed and self._stoch_rsi.IsFormed and
                self._williams_r_ind.IsFormed and self._de_marker_ind.IsFormed)

    def _execute_trading_logic(self, candle):
        if self._prev_short_ema is None or self._prev_long_ema is None or \
           self._prev_rsi is None or self._prev_prev_rsi is None or \
           self._prev_stoch_k is None or self._prev_williams_r is None or \
           self._prev_de_marker is None or self._cur_stoch_k is None or \
           self._cur_williams is None or self._cur_demarker is None:
            return

        long_trend = self._prev_short_ema > self._prev_long_ema and self._prev_rsi > self._prev_prev_rsi
        short_trend = self._prev_short_ema < self._prev_long_ema and self._prev_rsi < self._prev_prev_rsi

        if not long_trend and not short_trend:
            return

        stoch_cross_up = self._cur_stoch_k > self._prev_stoch_k and self._cur_stoch_k >= 50.0
        stoch_cross_down = self._cur_stoch_k < self._prev_stoch_k and self._cur_stoch_k <= 50.0

        demarker_cross_up = self._cur_demarker > self._prev_de_marker and self._cur_demarker >= 0.5
        demarker_cross_down = self._cur_demarker < self._prev_de_marker and self._cur_demarker <= 0.5

        williams_cross_up = self._cur_williams > self._prev_williams_r and self._cur_williams >= -50.0
        williams_cross_down = self._cur_williams < self._prev_williams_r and self._cur_williams <= -50.0

        if long_trend and stoch_cross_up and demarker_cross_up and williams_cross_up and self.Position == 0:
            self.BuyMarket()
        elif short_trend and stoch_cross_down and demarker_cross_down and williams_cross_down and self.Position == 0:
            self.SellMarket()

    def _update_previous_from_current(self):
        if self._cur_short_ema is not None:
            self._prev_short_ema = self._cur_short_ema
        if self._cur_long_ema is not None:
            self._prev_long_ema = self._cur_long_ema
        if self._cur_rsi is not None:
            self._prev_prev_rsi = self._prev_rsi
            self._prev_rsi = self._cur_rsi
        if self._cur_stoch_k is not None:
            self._prev_stoch_k = self._cur_stoch_k
        if self._cur_williams is not None:
            self._prev_williams_r = self._cur_williams
        if self._cur_demarker is not None:
            self._prev_de_marker = self._cur_demarker

    def OnReseted(self):
        super(polish_layer_strategy, self).OnReseted()
        self._prev_short_ema = None
        self._prev_long_ema = None
        self._prev_rsi = None
        self._prev_prev_rsi = None
        self._prev_stoch_k = None
        self._prev_williams_r = None
        self._prev_de_marker = None

    def CreateClone(self):
        return polish_layer_strategy()
