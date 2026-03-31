import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from System import Decimal
from StockSharp.Algo.Indicators import MovingAverageConvergenceDivergenceSignal, CandleIndicatorValue, DecimalIndicatorValue
from StockSharp.Algo.Strategies import Strategy


class macd_multi_timeframe_expert_strategy(Strategy):

    def __init__(self):
        super(macd_multi_timeframe_expert_strategy, self).__init__()
        self._order_volume = self.Param("OrderVolume", 0.1)
        self._stop_loss_points = self.Param("StopLossPoints", 200.0)
        self._take_profit_points = self.Param("TakeProfitPoints", 400.0)
        self._max_spread_points = self.Param("MaxSpreadPoints", 20.0)
        self._fast_period = self.Param("FastPeriod", 12)
        self._slow_period = self.Param("SlowPeriod", 26)
        self._signal_period = self.Param("SignalPeriod", 9)
        self._five_minute_type = self.Param("FiveMinuteCandleType", DataType.TimeFrame(TimeSpan.FromMinutes(30)))
        self._fifteen_minute_type = self.Param("FifteenMinuteCandleType", DataType.TimeFrame(TimeSpan.FromHours(2)))
        self._hour_type = self.Param("HourCandleType", DataType.TimeFrame(TimeSpan.FromHours(8)))
        self._four_hour_type = self.Param("FourHourCandleType", DataType.TimeFrame(TimeSpan.FromDays(1)))

        self._macd_five_minute = None
        self._macd_fifteen_minute = None
        self._macd_hour = None
        self._macd_four_hour = None
        self._relation_five_minute = None
        self._relation_fifteen_minute = None
        self._relation_hour = None
        self._relation_four_hour = None
        self._last_aligned_direction = 0
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
    def MaxSpreadPoints(self):
        return self._max_spread_points.Value

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
    def FiveMinuteCandleType(self):
        return self._five_minute_type.Value

    @property
    def FifteenMinuteCandleType(self):
        return self._fifteen_minute_type.Value

    @property
    def HourCandleType(self):
        return self._hour_type.Value

    @property
    def FourHourCandleType(self):
        return self._four_hour_type.Value

    def OnStarted2(self, time):
        super(macd_multi_timeframe_expert_strategy, self).OnStarted2(time)

        self._macd_five_minute = self._create_macd()
        self._macd_fifteen_minute = self._create_macd()
        self._macd_hour = self._create_macd()
        self._macd_four_hour = self._create_macd()

        five_minute_subscription = self.SubscribeCandles(self.FiveMinuteCandleType)
        five_minute_subscription.Bind(self._process_five_minute_candle).Start()

        self.SubscribeCandles(self.FifteenMinuteCandleType).Bind(self._process_fifteen_minute_candle).Start()
        self.SubscribeCandles(self.HourCandleType).Bind(self._process_hour_candle).Start()
        self.SubscribeCandles(self.FourHourCandleType).Bind(self._process_four_hour_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, five_minute_subscription)
            self.DrawOwnTrades(area)

    def _create_macd(self):
        macd = MovingAverageConvergenceDivergenceSignal()
        macd.Macd.ShortMa.Length = self.FastPeriod
        macd.Macd.LongMa.Length = self.SlowPeriod
        macd.SignalMa.Length = self.SignalPeriod
        return macd

    def _try_update_relation(self, macd_indicator, candle):
        civ = CandleIndicatorValue(macd_indicator, candle)
        civ.IsFinal = True
        macd_value = macd_indicator.Process(civ)

        if not macd_indicator.IsFormed:
            return (False, 0)

        try:
            macd_val = float(macd_value.Macd)
            signal_val = float(macd_value.Signal)
        except Exception:
            # Fallback: use .Value for main line
            try:
                macd_val = float(macd_value.Value)
                signal_val = 0.0
            except:
                return (False, 0)

        if signal_val > macd_val:
            return (True, 1)
        elif signal_val < macd_val:
            return (True, -1)
        else:
            return (True, 0)

    def _process_five_minute_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        ok, relation = self._try_update_relation(self._macd_five_minute, candle)
        if not ok:
            return

        self._relation_five_minute = relation

        if not self._has_all_relations():
            return

        aligned_direction = 0

        if self._all_relations_equal(1):
            aligned_direction = 1
        elif self._all_relations_equal(-1):
            aligned_direction = -1
        else:
            self._last_aligned_direction = 0
            return

        pos = float(self.Position)
        if pos != 0:
            self._manage_open_position(candle)
            return

        if float(self.OrderVolume) <= 0:
            return

        if self._last_aligned_direction == aligned_direction:
            return

        self._last_aligned_direction = aligned_direction

        if aligned_direction > 0:
            self.BuyMarket(float(self.OrderVolume))
            self._entry_price = float(candle.ClosePrice)
        else:
            self.SellMarket(float(self.OrderVolume))
            self._entry_price = float(candle.ClosePrice)

    def _process_fifteen_minute_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        ok, relation = self._try_update_relation(self._macd_fifteen_minute, candle)
        if ok:
            self._relation_fifteen_minute = relation

    def _process_hour_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        ok, relation = self._try_update_relation(self._macd_hour, candle)
        if ok:
            self._relation_hour = relation

    def _process_four_hour_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        ok, relation = self._try_update_relation(self._macd_four_hour, candle)
        if ok:
            self._relation_four_hour = relation

    def _has_all_relations(self):
        return (self._relation_five_minute is not None and
                self._relation_fifteen_minute is not None and
                self._relation_hour is not None and
                self._relation_four_hour is not None)

    def _all_relations_equal(self, value):
        return (self._relation_five_minute == value and
                self._relation_fifteen_minute == value and
                self._relation_hour == value and
                self._relation_four_hour == value)

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
                return
            if float(self.StopLossPoints) > 0 and float(candle.LowPrice) <= self._entry_price - float(self.StopLossPoints) * point:
                self.SellMarket(pos)
                self._entry_price = 0.0
        elif pos < 0:
            volume = abs(pos)
            if float(self.TakeProfitPoints) > 0 and float(candle.LowPrice) <= self._entry_price - float(self.TakeProfitPoints) * point:
                self.BuyMarket(volume)
                self._entry_price = 0.0
                return
            if float(self.StopLossPoints) > 0 and float(candle.HighPrice) >= self._entry_price + float(self.StopLossPoints) * point:
                self.BuyMarket(volume)
                self._entry_price = 0.0
        else:
            self._entry_price = 0.0

    def OnReseted(self):
        super(macd_multi_timeframe_expert_strategy, self).OnReseted()
        self._macd_five_minute = None
        self._macd_fifteen_minute = None
        self._macd_hour = None
        self._macd_four_hour = None
        self._relation_five_minute = None
        self._relation_fifteen_minute = None
        self._relation_hour = None
        self._relation_four_hour = None
        self._last_aligned_direction = 0
        self._entry_price = 0.0

    def CreateClone(self):
        return macd_multi_timeframe_expert_strategy()
