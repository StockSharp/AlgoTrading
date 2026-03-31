import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy

class ema_cross_2_strategy(Strategy):
    """
    Counter-trend EMA crossover strategy (EMA_CROSS_2 MetaTrader port).
    Buys when long EMA rises above short EMA, sells on opposite.
    Point-based stop-loss, take-profit, and trailing stop management.
    """

    def __init__(self):
        super(ema_cross_2_strategy, self).__init__()
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(15))) \
            .SetDisplay("Candle Type", "Time frame for EMA calculations", "General")
        self._take_profit_points = self.Param("TakeProfitPoints", 500.0) \
            .SetDisplay("Take Profit (points)", "Distance from entry to take-profit", "Risk")
        self._stop_loss_points = self.Param("StopLossPoints", 500.0) \
            .SetDisplay("Stop Loss (points)", "Distance from entry to stop-loss", "Risk")
        self._trailing_stop_points = self.Param("TrailingStopPoints", 500.0) \
            .SetDisplay("Trailing Stop (points)", "Trailing distance after entry", "Risk")
        self._short_ema_period = self.Param("ShortEmaPeriod", 5) \
            .SetDisplay("Short EMA", "Length of the fast EMA", "Indicators")
        self._long_ema_period = self.Param("LongEmaPeriod", 60) \
            .SetDisplay("Long EMA", "Length of the slow EMA", "Indicators")

        self._skip_first_signal = True
        self._last_direction = 0
        self._stop_loss_price = None
        self._take_profit_price = None
        self._point_size = 0.0
        self._entry_price = 0.0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(ema_cross_2_strategy, self).OnReseted()
        self._skip_first_signal = True
        self._last_direction = 0
        self._stop_loss_price = None
        self._take_profit_price = None
        self._point_size = 0.0
        self._entry_price = 0.0

    def OnStarted2(self, time):
        super(ema_cross_2_strategy, self).OnStarted2(time)
        self._point_size = self._calculate_point_size()

        short_ema = ExponentialMovingAverage()
        short_ema.Length = self._short_ema_period.Value
        long_ema = ExponentialMovingAverage()
        long_ema.Length = self._long_ema_period.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(short_ema, long_ema, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, short_ema)
            self.DrawIndicator(area, long_ema)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, short_val, long_val):
        if candle.State != CandleStates.Finished:
            return

        short_val = float(short_val)
        long_val = float(long_val)

        if self._point_size <= 0.0:
            self._point_size = self._calculate_point_size()

        if self._check_risk(candle):
            return

        if self.Position != 0:
            self._update_trailing_stop(candle)
        elif self._stop_loss_price is not None or self._take_profit_price is not None:
            self._reset_risk_levels()

        signal = self._evaluate_cross(long_val, short_val)

        if signal == 0:
            return

        if self.Position != 0:
            return

        if signal == 1:
            self.BuyMarket()
            self._set_risk_levels(float(candle.ClosePrice), True)
        elif signal == 2:
            self.SellMarket()
            self._set_risk_levels(float(candle.ClosePrice), False)

    def _evaluate_cross(self, long_val, short_val):
        current_direction = 0
        if long_val > short_val:
            current_direction = 1
        elif long_val < short_val:
            current_direction = 2

        if self._skip_first_signal:
            self._skip_first_signal = False
            return 0

        if current_direction != 0 and current_direction != self._last_direction:
            self._last_direction = current_direction
            return self._last_direction

        return 0

    def _check_risk(self, candle):
        if self.Position > 0:
            if self._stop_loss_price is not None and float(candle.LowPrice) <= self._stop_loss_price:
                self.SellMarket()
                self._reset_risk_levels()
                return True
            if self._take_profit_price is not None and float(candle.HighPrice) >= self._take_profit_price:
                self.SellMarket()
                self._reset_risk_levels()
                return True
        elif self.Position < 0:
            if self._stop_loss_price is not None and float(candle.HighPrice) >= self._stop_loss_price:
                self.BuyMarket()
                self._reset_risk_levels()
                return True
            if self._take_profit_price is not None and float(candle.LowPrice) <= self._take_profit_price:
                self.BuyMarket()
                self._reset_risk_levels()
                return True
        elif self._stop_loss_price is not None or self._take_profit_price is not None:
            self._reset_risk_levels()
        return False

    def _update_trailing_stop(self, candle):
        trail_pts = self._trailing_stop_points.Value
        if trail_pts <= 0 or self._point_size <= 0:
            return
        distance = trail_pts * self._point_size
        if distance <= 0:
            return
        entry = self._entry_price if self._entry_price > 0 else float(candle.ClosePrice)
        close = float(candle.ClosePrice)

        if self.Position > 0:
            profit = close - entry
            if profit > distance:
                candidate = close - distance
                if self._stop_loss_price is None or self._stop_loss_price < candidate:
                    self._stop_loss_price = candidate
        elif self.Position < 0:
            profit = entry - close
            if profit > distance:
                candidate = close + distance
                if self._stop_loss_price is None or self._stop_loss_price > candidate:
                    self._stop_loss_price = candidate

    def _set_risk_levels(self, price, is_long):
        if self._point_size <= 0:
            self._reset_risk_levels()
            return
        self._entry_price = price
        sl = self._stop_loss_points.Value
        tp = self._take_profit_points.Value
        direction = 1.0 if is_long else -1.0
        self._stop_loss_price = price - direction * sl * self._point_size if sl > 0 else None
        self._take_profit_price = price + direction * tp * self._point_size if tp > 0 else None

    def _reset_risk_levels(self):
        self._stop_loss_price = None
        self._take_profit_price = None

    def _calculate_point_size(self):
        step = 1.0
        if self.Security is not None and self.Security.PriceStep is not None:
            step = float(self.Security.PriceStep)
        return step if step > 0 else 1.0

    def CreateClone(self):
        return ema_cross_2_strategy()
