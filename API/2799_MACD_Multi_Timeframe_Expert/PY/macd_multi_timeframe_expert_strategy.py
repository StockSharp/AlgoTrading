import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import MovingAverageConvergenceDivergenceSignal
from StockSharp.Algo.Strategies import Strategy


class macd_multi_timeframe_expert_strategy(Strategy):

    def __init__(self):
        super(macd_multi_timeframe_expert_strategy, self).__init__()
        self._order_volume = self.Param("OrderVolume", 0.1)
        self._stop_loss_points = self.Param("StopLossPoints", 1500.0)
        self._take_profit_points = self.Param("TakeProfitPoints", 2500.0)
        self._fast_period = self.Param("FastPeriod", 12)
        self._slow_period = self.Param("SlowPeriod", 26)
        self._signal_period = self.Param("SignalPeriod", 9)
        self._primary_type = self.Param("PrimaryCandleType", DataType.TimeFrame(TimeSpan.FromMinutes(30)))
        self._confirm_type = self.Param("ConfirmCandleType", DataType.TimeFrame(TimeSpan.FromHours(4)))

        self._macd_primary = None
        self._macd_confirm = None
        self._relation_primary = None
        self._relation_confirm = None
        self._last_trade_direction = 0
        self._candles_since_entry = 0
        self._entry_price = 0.0

    @property
    def OrderVolume(self):
        return self._order_volume.Value

    @property
    def StopLossPoints(self):
        return self._stop_loss_points.Value

    @property
    def TakeProfitPoints(self):
        return self._take_profit_points.Value

    @property
    def FastPeriod(self):
        return self._fast_period.Value

    @property
    def SlowPeriod(self):
        return self._slow_period.Value

    @property
    def SignalPeriod(self):
        return self._signal_period.Value

    @property
    def PrimaryCandleType(self):
        return self._primary_type.Value

    @property
    def ConfirmCandleType(self):
        return self._confirm_type.Value

    def OnStarted2(self, time):
        super(macd_multi_timeframe_expert_strategy, self).OnStarted2(time)

        self._macd_primary = self._create_macd()
        self._macd_confirm = self._create_macd()

        primary_subscription = self.SubscribeCandles(self.PrimaryCandleType)
        primary_subscription.BindEx(self._macd_primary, self._process_primary_candle).Start()

        self.SubscribeCandles(self.ConfirmCandleType).BindEx(self._macd_confirm, self._process_confirm_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, primary_subscription)
            self.DrawOwnTrades(area)

    def _create_macd(self):
        macd = MovingAverageConvergenceDivergenceSignal()
        macd.Macd.ShortMa.Length = self.FastPeriod
        macd.Macd.LongMa.Length = self.SlowPeriod
        macd.SignalMa.Length = self.SignalPeriod
        return macd

    def _try_update_relation(self, macd_value):
        if not macd_value.IsFinal:
            return (False, 0)

        try:
            macd_val = float(macd_value.Macd)
            signal_val = float(macd_value.Signal)
        except Exception:
            return (False, 0)

        if macd_val > signal_val:
            return (True, 1)
        elif macd_val < signal_val:
            return (True, -1)
        else:
            return (True, 0)

    def _process_primary_candle(self, candle, macd_value):
        if candle.State != CandleStates.Finished:
            return

        ok, relation = self._try_update_relation(macd_value)
        if not ok:
            return

        self._relation_primary = relation
        self._candles_since_entry += 1

        # Manage protective exits whenever a position is open.
        pos = float(self.Position)
        if pos != 0:
            self._manage_open_position(candle)

            # If position was closed by SL/TP, allow new entry below
            pos = float(self.Position)
            if pos != 0:
                return

        if self._relation_confirm is None:
            return

        if float(self.OrderVolume) <= 0:
            return

        # Cooldown: require at least 6 candles between trades.
        if self._candles_since_entry < 6:
            return

        # Determine aligned direction: both timeframes must agree.
        aligned_direction = 0

        if self._relation_primary == 1 and self._relation_confirm == 1:
            aligned_direction = 1
        elif self._relation_primary == -1 and self._relation_confirm == -1:
            aligned_direction = -1

        if aligned_direction == 0:
            return

        # Avoid repeated entries in the same direction.
        if self._last_trade_direction == aligned_direction:
            return

        self._last_trade_direction = aligned_direction
        self._candles_since_entry = 0

        if aligned_direction > 0:
            self.BuyMarket(float(self.OrderVolume))
            self._entry_price = float(candle.ClosePrice)
        else:
            self.SellMarket(float(self.OrderVolume))
            self._entry_price = float(candle.ClosePrice)

    def _process_confirm_candle(self, candle, macd_value):
        if candle.State != CandleStates.Finished:
            return

        ok, relation = self._try_update_relation(macd_value)
        if ok:
            self._relation_confirm = relation

    def _manage_open_position(self, candle):
        sec = self.Security
        point = float(sec.PriceStep) if sec is not None and sec.PriceStep is not None else 1.0
        if point <= 0:
            point = 1.0

        pos = float(self.Position)

        if pos > 0:
            if float(self.TakeProfitPoints) > 0 and float(candle.HighPrice) >= self._entry_price + float(self.TakeProfitPoints) * point:
                self.SellMarket(pos)
                self._entry_price = 0.0
                self._last_trade_direction = 0
                return
            if float(self.StopLossPoints) > 0 and float(candle.LowPrice) <= self._entry_price - float(self.StopLossPoints) * point:
                self.SellMarket(pos)
                self._entry_price = 0.0
                self._last_trade_direction = 0
        elif pos < 0:
            volume = abs(pos)
            if float(self.TakeProfitPoints) > 0 and float(candle.LowPrice) <= self._entry_price - float(self.TakeProfitPoints) * point:
                self.BuyMarket(volume)
                self._entry_price = 0.0
                self._last_trade_direction = 0
                return
            if float(self.StopLossPoints) > 0 and float(candle.HighPrice) >= self._entry_price + float(self.StopLossPoints) * point:
                self.BuyMarket(volume)
                self._entry_price = 0.0
                self._last_trade_direction = 0
        else:
            self._entry_price = 0.0

    def OnReseted(self):
        super(macd_multi_timeframe_expert_strategy, self).OnReseted()
        self._macd_primary = None
        self._macd_confirm = None
        self._relation_primary = None
        self._relation_confirm = None
        self._last_trade_direction = 0
        self._candles_since_entry = 0
        self._entry_price = 0.0

    def CreateClone(self):
        return macd_multi_timeframe_expert_strategy()
