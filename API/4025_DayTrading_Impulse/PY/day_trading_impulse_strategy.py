import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy
from StockSharp.Algo.Indicators import (
    ParabolicSar,
    MovingAverageConvergenceDivergenceSignal,
    StochasticOscillator,
    Momentum,
)

class day_trading_impulse_strategy(Strategy):
    def __init__(self):
        super(day_trading_impulse_strategy, self).__init__()

        self._lot_size = self.Param("LotSize", 1.0) \
            .SetDisplay("Order Volume", "Trade volume used for each market entry", "Trading")
        self._trailing_stop_points = self.Param("TrailingStopPoints", 15.0) \
            .SetDisplay("Trailing Stop (points)", "Distance used to trail profitable positions", "Risk")
        self._take_profit_points = self.Param("TakeProfitPoints", 20.0) \
            .SetDisplay("Take Profit (points)", "Fixed profit target measured in points", "Risk")
        self._stop_loss_points = self.Param("StopLossPoints", 0.0) \
            .SetDisplay("Stop Loss (points)", "Protective stop distance measured in points", "Risk")
        self._slippage_points = self.Param("SlippagePoints", 3.0) \
            .SetDisplay("Slippage (points)", "Maximum acceptable execution slippage", "Trading")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Time frame used for indicator calculations", "Data")
        self._macd_fast_period = self.Param("MacdFastPeriod", 12) \
            .SetDisplay("MACD Fast", "Length of the fast EMA in MACD", "Indicators")
        self._macd_slow_period = self.Param("MacdSlowPeriod", 26) \
            .SetDisplay("MACD Slow", "Length of the slow EMA in MACD", "Indicators")
        self._macd_signal_period = self.Param("MacdSignalPeriod", 9) \
            .SetDisplay("MACD Signal", "Length of the MACD signal EMA", "Indicators")
        self._stochastic_length = self.Param("StochasticLength", 5) \
            .SetDisplay("Stochastic %K", "Period of the %K line", "Indicators")
        self._stochastic_signal = self.Param("StochasticSignal", 3) \
            .SetDisplay("Stochastic %D", "Period of the %D smoothing", "Indicators")
        self._stochastic_slow = self.Param("StochasticSlow", 3) \
            .SetDisplay("Stochastic Slowing", "Final smoothing applied to %K", "Indicators")
        self._stochastic_buy_threshold = self.Param("StochasticBuyThreshold", 35.0) \
            .SetDisplay("Stochastic Buy", "Oversold %K threshold for long entries", "Indicators")
        self._stochastic_sell_threshold = self.Param("StochasticSellThreshold", 60.0) \
            .SetDisplay("Stochastic Sell", "Overbought %K threshold for short entries", "Indicators")
        self._momentum_period = self.Param("MomentumPeriod", 14) \
            .SetDisplay("Momentum Period", "Number of candles used for Momentum", "Indicators")
        self._momentum_neutral_level = self.Param("MomentumNeutralLevel", 100.0) \
            .SetDisplay("Momentum Neutral", "Neutral momentum value used for signal confirmation", "Indicators")
        self._sar_acceleration = self.Param("SarAcceleration", 0.02) \
            .SetDisplay("SAR Acceleration", "Initial acceleration factor of Parabolic SAR", "Indicators")
        self._sar_step = self.Param("SarStep", 0.02) \
            .SetDisplay("SAR Step", "Increment applied to the acceleration factor", "Indicators")
        self._sar_maximum = self.Param("SarMaximum", 0.2) \
            .SetDisplay("SAR Maximum", "Maximum acceleration factor of Parabolic SAR", "Indicators")

        self._parabolic_sar = None
        self._macd = None
        self._stochastic = None
        self._momentum = None
        self._previous_sar = None
        self._long_stop_price = None
        self._short_stop_price = None
        self._long_take_profit = None
        self._short_take_profit = None
        self._long_entry_price = None
        self._short_entry_price = None
        self._point_size = 0.0

    @property
    def LotSize(self):
        return self._lot_size.Value

    @property
    def TrailingStopPoints(self):
        return self._trailing_stop_points.Value

    @property
    def TakeProfitPoints(self):
        return self._take_profit_points.Value

    @property
    def StopLossPoints(self):
        return self._stop_loss_points.Value

    @property
    def SlippagePoints(self):
        return self._slippage_points.Value

    @property
    def CandleType(self):
        return self._candle_type.Value

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
    def StochasticLength(self):
        return self._stochastic_length.Value

    @property
    def StochasticSignal(self):
        return self._stochastic_signal.Value

    @property
    def StochasticSlow(self):
        return self._stochastic_slow.Value

    @property
    def StochasticBuyThreshold(self):
        return self._stochastic_buy_threshold.Value

    @property
    def StochasticSellThreshold(self):
        return self._stochastic_sell_threshold.Value

    @property
    def MomentumPeriod(self):
        return self._momentum_period.Value

    @property
    def MomentumNeutralLevel(self):
        return self._momentum_neutral_level.Value

    @property
    def SarAcceleration(self):
        return self._sar_acceleration.Value

    @property
    def SarStep(self):
        return self._sar_step.Value

    @property
    def SarMaximum(self):
        return self._sar_maximum.Value

    def _calculate_point_size(self):
        step = 0.0
        if self.Security is not None and self.Security.PriceStep is not None:
            step = float(self.Security.PriceStep)
        return step if step > 0 else 0.0001

    def _convert_points(self, points):
        pts = float(points)
        if pts <= 0:
            return 0.0
        if self._point_size > 0:
            return pts * self._point_size
        step = 0.0
        if self.Security is not None and self.Security.PriceStep is not None:
            step = float(self.Security.PriceStep)
        return pts * step if step > 0 else pts

    def OnStarted(self, time):
        super(day_trading_impulse_strategy, self).OnStarted(time)

        self.Volume = float(self.LotSize)
        self._point_size = self._calculate_point_size()

        self._parabolic_sar = ParabolicSar()
        self._parabolic_sar.Acceleration = float(self.SarAcceleration)
        self._parabolic_sar.AccelerationStep = float(self.SarStep)
        self._parabolic_sar.AccelerationMax = float(self.SarMaximum)

        self._macd = MovingAverageConvergenceDivergenceSignal()
        self._macd.Macd.ShortMa.Length = self.MacdFastPeriod
        self._macd.Macd.LongMa.Length = self.MacdSlowPeriod
        self._macd.SignalMa.Length = self.MacdSignalPeriod

        self._stochastic = StochasticOscillator()
        self._stochastic.K.Length = self.StochasticLength
        self._stochastic.D.Length = self.StochasticSignal

        self._momentum = Momentum()
        self._momentum.Length = self.MomentumPeriod

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.BindEx(self._parabolic_sar, self._macd, self._stochastic, self._momentum, self.ProcessCandle).Start()

    def ProcessCandle(self, candle, sar_value, macd_value, stochastic_value, momentum_value):
        if candle.State != CandleStates.Finished:
            return

        if not sar_value.IsFinal or not macd_value.IsFinal or not stochastic_value.IsFinal or not momentum_value.IsFinal:
            return

        sar = float(sar_value)
        previous_sar = self._previous_sar
        self._previous_sar = sar

        if previous_sar is None:
            return

        mom = float(momentum_value)
        close_price = float(candle.ClosePrice)

        macd_val = float(macd_value.Macd) if hasattr(macd_value, 'Macd') else 0.0
        signal_val = float(macd_value.Signal) if hasattr(macd_value, 'Signal') else 0.0

        stoch_k = float(stochastic_value.K) if hasattr(stochastic_value, 'K') else 0.0

        mom_neutral = float(self.MomentumNeutralLevel)
        stoch_buy = float(self.StochasticBuyThreshold)
        stoch_sell = float(self.StochasticSellThreshold)

        buy_signal = sar <= close_price and previous_sar > sar and mom < mom_neutral and macd_val < signal_val and stoch_k < stoch_buy
        sell_signal = sar >= close_price and previous_sar < sar and mom > mom_neutral and macd_val > signal_val and stoch_k > stoch_sell

        closed_position = False

        if self.Position > 0:
            if sell_signal:
                self.SellMarket(abs(self.Position))
                self._reset_long_state()
                closed_position = True
            elif self._handle_long_risk(candle):
                closed_position = True

        elif self.Position < 0:
            if buy_signal:
                self.BuyMarket(abs(self.Position))
                self._reset_short_state()
                closed_position = True
            elif self._handle_short_risk(candle):
                closed_position = True

        if closed_position:
            return

        if self.Position == 0:
            if buy_signal:
                entry_price = close_price
                self.BuyMarket(self.Volume)
                self._long_entry_price = entry_price
                sl = float(self.StopLossPoints)
                tp = float(self.TakeProfitPoints)
                self._long_stop_price = entry_price - self._convert_points(sl) if sl > 0 else None
                self._long_take_profit = entry_price + self._convert_points(tp) if tp > 0 else None
            elif sell_signal:
                entry_price = close_price
                self.SellMarket(self.Volume)
                self._short_entry_price = entry_price
                sl = float(self.StopLossPoints)
                tp = float(self.TakeProfitPoints)
                self._short_stop_price = entry_price + self._convert_points(sl) if sl > 0 else None
                self._short_take_profit = entry_price - self._convert_points(tp) if tp > 0 else None

    def _handle_long_risk(self, candle):
        if abs(self.Position) <= 0:
            return False

        if self._long_take_profit is not None and float(candle.HighPrice) >= self._long_take_profit:
            self.SellMarket(abs(self.Position))
            self._reset_long_state()
            return True

        if self._long_stop_price is not None and float(candle.LowPrice) <= self._long_stop_price:
            self.SellMarket(abs(self.Position))
            self._reset_long_state()
            return True

        trailing_distance = self._convert_points(self.TrailingStopPoints)
        if trailing_distance > 0 and self._long_entry_price is not None:
            progressed = float(candle.HighPrice) - self._long_entry_price
            if progressed >= trailing_distance:
                candidate = float(candle.ClosePrice) - trailing_distance
                if self._long_stop_price is None or candidate > self._long_stop_price:
                    self._long_stop_price = candidate

        return False

    def _handle_short_risk(self, candle):
        if abs(self.Position) <= 0:
            return False

        if self._short_take_profit is not None and float(candle.LowPrice) <= self._short_take_profit:
            self.BuyMarket(abs(self.Position))
            self._reset_short_state()
            return True

        if self._short_stop_price is not None and float(candle.HighPrice) >= self._short_stop_price:
            self.BuyMarket(abs(self.Position))
            self._reset_short_state()
            return True

        trailing_distance = self._convert_points(self.TrailingStopPoints)
        if trailing_distance > 0 and self._short_entry_price is not None:
            progressed = self._short_entry_price - float(candle.LowPrice)
            if progressed >= trailing_distance:
                candidate = float(candle.ClosePrice) + trailing_distance
                if self._short_stop_price is None or candidate < self._short_stop_price:
                    self._short_stop_price = candidate

        return False

    def _reset_long_state(self):
        self._long_entry_price = None
        self._long_stop_price = None
        self._long_take_profit = None

    def _reset_short_state(self):
        self._short_entry_price = None
        self._short_stop_price = None
        self._short_take_profit = None

    def OnReseted(self):
        super(day_trading_impulse_strategy, self).OnReseted()
        self._parabolic_sar = None
        self._macd = None
        self._stochastic = None
        self._momentum = None
        self._previous_sar = None
        self._long_stop_price = None
        self._short_stop_price = None
        self._long_take_profit = None
        self._short_take_profit = None
        self._long_entry_price = None
        self._short_entry_price = None
        self._point_size = 0.0

    def CreateClone(self):
        return day_trading_impulse_strategy()
