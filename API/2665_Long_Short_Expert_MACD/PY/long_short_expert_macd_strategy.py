import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy
from StockSharp.Algo.Indicators import MovingAverageConvergenceDivergenceSignal


class long_short_expert_macd_strategy(Strategy):
    """MACD crossover strategy with direction filtering and manual SL/TP."""

    # 0=Both, 1=Long, -1=Short
    def __init__(self):
        super(long_short_expert_macd_strategy, self).__init__()

        self._allowed_position = self.Param("AllowedPosition", 0) \
            .SetDisplay("Allowed Positions", "0=Both, 1=Long only, -1=Short only", "General")
        self._fast_length = self.Param("FastLength", 12) \
            .SetGreaterThanZero() \
            .SetDisplay("Fast EMA", "Fast MACD EMA length", "MACD")
        self._slow_length = self.Param("SlowLength", 24) \
            .SetGreaterThanZero() \
            .SetDisplay("Slow EMA", "Slow MACD EMA length", "MACD")
        self._signal_length = self.Param("SignalLength", 9) \
            .SetGreaterThanZero() \
            .SetDisplay("Signal EMA", "MACD signal EMA length", "MACD")
        self._take_profit_points = self.Param("TakeProfitPoints", 50) \
            .SetDisplay("Take Profit", "Take profit distance in price points", "Risk")
        self._stop_loss_points = self.Param("StopLossPoints", 20) \
            .SetDisplay("Stop Loss", "Stop loss distance in price points", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Type of candles to process", "General")

        self._prev_above = None
        self._entry_price = None
        self._long_stop = 0.0
        self._long_take = 0.0
        self._short_stop = 0.0
        self._short_take = 0.0

    @property
    def AllowedPosition(self):
        return int(self._allowed_position.Value)
    @property
    def FastLength(self):
        return int(self._fast_length.Value)
    @property
    def SlowLength(self):
        return int(self._slow_length.Value)
    @property
    def SignalLength(self):
        return int(self._signal_length.Value)
    @property
    def TakeProfitPoints(self):
        return int(self._take_profit_points.Value)
    @property
    def StopLossPoints(self):
        return int(self._stop_loss_points.Value)
    @property
    def CandleType(self):
        return self._candle_type.Value

    def _get_step(self):
        sec = self.Security
        step = float(sec.PriceStep) if sec is not None and sec.PriceStep is not None and float(sec.PriceStep) > 0 else 1.0
        return step

    def OnStarted2(self, time):
        super(long_short_expert_macd_strategy, self).OnStarted2(time)

        self._prev_above = None
        self._entry_price = None
        self._reset_protection()

        self._macd = MovingAverageConvergenceDivergenceSignal()
        self._macd.Macd.ShortMa.Length = self.FastLength
        self._macd.Macd.LongMa.Length = self.SlowLength
        self._macd.SignalMa.Length = self.SignalLength

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.BindEx(self._macd, self.process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, self._macd)
            self.DrawOwnTrades(area)

    def process_candle(self, candle, macd_value):
        if candle.State != CandleStates.Finished:
            return

        macd_v = macd_value.Macd
        signal_v = macd_value.Signal

        if macd_v is None or signal_v is None:
            return

        macd_val = float(macd_v)
        signal_val = float(signal_v)

        self._update_protection()
        is_above = macd_val > signal_val

        if not self._macd.IsFormed:
            self._prev_above = is_above
            return

        if self._try_exit(candle):
            self._prev_above = is_above
            return

        if self._prev_above is None:
            self._prev_above = is_above
            return

        cross_up = is_above and not self._prev_above
        cross_down = not is_above and self._prev_above
        close = float(candle.ClosePrice)

        can_long = self.AllowedPosition != -1
        can_short = self.AllowedPosition != 1
        allow_reverse = self.AllowedPosition == 0

        if cross_up:
            if can_long:
                if self.Position < 0:
                    if allow_reverse:
                        self._reset_protection()
                        self.BuyMarket()
                        self._entry_price = close
                    else:
                        self.BuyMarket()
                        self._reset_protection()
                        self._entry_price = None
                elif self.Position == 0:
                    self._reset_protection()
                    self.BuyMarket()
                    self._entry_price = close
            elif self.Position < 0:
                self.BuyMarket()
                self._reset_protection()
                self._entry_price = None

        elif cross_down:
            if can_short:
                if self.Position > 0:
                    if allow_reverse:
                        self._reset_protection()
                        self.SellMarket()
                        self._entry_price = close
                    else:
                        self.SellMarket()
                        self._reset_protection()
                        self._entry_price = None
                elif self.Position == 0:
                    self._reset_protection()
                    self.SellMarket()
                    self._entry_price = close
            elif self.Position > 0:
                self.SellMarket()
                self._reset_protection()
                self._entry_price = None

        self._prev_above = is_above

    def _update_protection(self):
        if self._entry_price is None:
            self._reset_protection()
            return

        step = self._get_step()
        entry = self._entry_price

        if self.Position > 0:
            self._long_stop = entry - self.StopLossPoints * step if self.StopLossPoints > 0 else 0.0
            self._long_take = entry + self.TakeProfitPoints * step if self.TakeProfitPoints > 0 else 0.0
            self._short_stop = 0.0
            self._short_take = 0.0
        elif self.Position < 0:
            self._short_stop = entry + self.StopLossPoints * step if self.StopLossPoints > 0 else 0.0
            self._short_take = entry - self.TakeProfitPoints * step if self.TakeProfitPoints > 0 else 0.0
            self._long_stop = 0.0
            self._long_take = 0.0
        else:
            self._reset_protection()

    def _try_exit(self, candle):
        h = float(candle.HighPrice)
        lo = float(candle.LowPrice)

        if self.Position > 0:
            if self.StopLossPoints > 0 and self._long_stop > 0 and lo <= self._long_stop:
                self.SellMarket()
                self._reset_protection()
                self._entry_price = None
                return True
            if self.TakeProfitPoints > 0 and self._long_take > 0 and h >= self._long_take:
                self.SellMarket()
                self._reset_protection()
                self._entry_price = None
                return True
        elif self.Position < 0:
            if self.StopLossPoints > 0 and self._short_stop > 0 and h >= self._short_stop:
                self.BuyMarket()
                self._reset_protection()
                self._entry_price = None
                return True
            if self.TakeProfitPoints > 0 and self._short_take > 0 and lo <= self._short_take:
                self.BuyMarket()
                self._reset_protection()
                self._entry_price = None
                return True
        return False

    def _reset_protection(self):
        self._long_stop = 0.0
        self._long_take = 0.0
        self._short_stop = 0.0
        self._short_take = 0.0

    def OnReseted(self):
        super(long_short_expert_macd_strategy, self).OnReseted()
        self._prev_above = None
        self._entry_price = None
        self._reset_protection()

    def CreateClone(self):
        return long_short_expert_macd_strategy()
