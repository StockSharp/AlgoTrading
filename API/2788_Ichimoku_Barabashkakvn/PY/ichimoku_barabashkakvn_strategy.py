import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import Ichimoku
from StockSharp.Algo.Strategies import Strategy


class ichimoku_barabashkakvn_strategy(Strategy):

    def __init__(self):
        super(ichimoku_barabashkakvn_strategy, self).__init__()
        self._tenkan_period = self.Param("TenkanPeriod", 9)
        self._kijun_period = self.Param("KijunPeriod", 26)
        self._senkou_span_b_period = self.Param("SenkouSpanBPeriod", 52)
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(1)))
        self._order_volume = self.Param("OrderVolume", 1.0)
        self._buy_stop_loss_pips = self.Param("BuyStopLossPips", 100)
        self._buy_take_profit_pips = self.Param("BuyTakeProfitPips", 300)
        self._sell_stop_loss_pips = self.Param("SellStopLossPips", 100)
        self._sell_take_profit_pips = self.Param("SellTakeProfitPips", 300)
        self._buy_trailing_stop_pips = self.Param("BuyTrailingStopPips", 50)
        self._sell_trailing_stop_pips = self.Param("SellTrailingStopPips", 50)
        self._trailing_step_pips = self.Param("TrailingStepPips", 5)
        self._use_trade_hours = self.Param("UseTradeHours", False)
        self._start_hour = self.Param("StartHour", 0)
        self._end_hour = self.Param("EndHour", 23)

        self._ichimoku = None
        self._prev_tenkan = None
        self._entry_price = None
        self._stop_loss_price = None
        self._take_profit_price = None
        self._pip_value = 0.0

    @property
    def TenkanPeriod(self):
        return self._tenkan_period.Value

    @property
    def KijunPeriod(self):
        return self._kijun_period.Value

    @property
    def SenkouSpanBPeriod(self):
        return self._senkou_span_b_period.Value

    @property
    def CandleType(self):
        return self._candle_type.Value

    @property
    def OrderVolume(self):
        return self._order_volume.Value

    @property
    def BuyStopLossPips(self):
        return self._buy_stop_loss_pips.Value

    @property
    def BuyTakeProfitPips(self):
        return self._buy_take_profit_pips.Value

    @property
    def SellStopLossPips(self):
        return self._sell_stop_loss_pips.Value

    @property
    def SellTakeProfitPips(self):
        return self._sell_take_profit_pips.Value

    @property
    def BuyTrailingStopPips(self):
        return self._buy_trailing_stop_pips.Value

    @property
    def SellTrailingStopPips(self):
        return self._sell_trailing_stop_pips.Value

    @property
    def TrailingStepPips(self):
        return self._trailing_step_pips.Value

    @property
    def UseTradeHours(self):
        return self._use_trade_hours.Value

    @property
    def StartHour(self):
        return self._start_hour.Value

    @property
    def EndHour(self):
        return self._end_hour.Value

    def OnStarted(self, time):
        super(ichimoku_barabashkakvn_strategy, self).OnStarted(time)

        self._ichimoku = Ichimoku()
        self._ichimoku.Tenkan.Length = self.TenkanPeriod
        self._ichimoku.Kijun.Length = self.KijunPeriod
        self._ichimoku.SenkouB.Length = self.SenkouSpanBPeriod

        self._pip_value = self._calculate_pip_value()

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        ichimoku_value = self._ichimoku.Process(candle)

        if not ichimoku_value.IsFinal:
            return

        try:
            tenkan = float(ichimoku_value.Tenkan)
            kijun = float(ichimoku_value.Kijun)
            senkou_a = float(ichimoku_value.SenkouA)
            senkou_b = float(ichimoku_value.SenkouB)
        except:
            return

        if not self._ichimoku.IsFormed:
            self._prev_tenkan = tenkan
            return

        if self._prev_tenkan is None:
            self._prev_tenkan = tenkan
            return

        pos = float(self.Position)
        if pos == 0 and (self._entry_price is not None or self._stop_loss_price is not None or self._take_profit_price is not None):
            self._reset_position_state()

        if pos != 0 and self._check_protective_levels(candle):
            self._prev_tenkan = tenkan
            return

        if self.UseTradeHours:
            hour = candle.OpenTime.Hour
            if not (hour >= self.StartHour and hour <= self.EndHour):
                self._prev_tenkan = tenkan
                return

        buy_signal = self._prev_tenkan < kijun and tenkan >= kijun and float(candle.ClosePrice) > senkou_b
        sell_signal = self._prev_tenkan > kijun and tenkan <= kijun and float(candle.ClosePrice) < senkou_a

        if pos == 0:
            if buy_signal:
                self._open_long(candle)
            elif sell_signal:
                self._open_short(candle)
        elif pos < 0:
            if buy_signal:
                self._close_short()
                self._prev_tenkan = tenkan
                return
        elif pos > 0:
            if sell_signal:
                self._close_long()
                self._prev_tenkan = tenkan
                return

        self._update_trailing_stops(candle)
        self._prev_tenkan = tenkan

    def _open_long(self, candle):
        if float(self.OrderVolume) <= 0:
            return
        self.BuyMarket(float(self.OrderVolume))
        self._entry_price = float(candle.ClosePrice)

        stop_offset = self._get_price_offset(self.BuyStopLossPips)
        take_offset = self._get_price_offset(self.BuyTakeProfitPips)

        self._stop_loss_price = float(candle.ClosePrice) - stop_offset if stop_offset > 0 else None
        self._take_profit_price = float(candle.ClosePrice) + take_offset if take_offset > 0 else None

    def _open_short(self, candle):
        if float(self.OrderVolume) <= 0:
            return
        self.SellMarket(float(self.OrderVolume))
        self._entry_price = float(candle.ClosePrice)

        stop_offset = self._get_price_offset(self.SellStopLossPips)
        take_offset = self._get_price_offset(self.SellTakeProfitPips)

        self._stop_loss_price = float(candle.ClosePrice) + stop_offset if stop_offset > 0 else None
        self._take_profit_price = float(candle.ClosePrice) - take_offset if take_offset > 0 else None

    def _close_long(self):
        pos = float(self.Position)
        if pos <= 0:
            return
        self.SellMarket(abs(pos))
        self._reset_position_state()

    def _close_short(self):
        pos = float(self.Position)
        if pos >= 0:
            return
        self.BuyMarket(abs(pos))
        self._reset_position_state()

    def _update_trailing_stops(self, candle):
        if self._entry_price is None:
            return

        pos = float(self.Position)
        if pos > 0:
            trailing_stop = self._get_price_offset(self.BuyTrailingStopPips)
            trailing_step = self._get_price_offset(self.TrailingStepPips)

            if trailing_stop > 0 and trailing_step >= 0:
                profit = float(candle.ClosePrice) - self._entry_price
                if profit > trailing_stop + trailing_step:
                    threshold = float(candle.ClosePrice) - (trailing_stop + trailing_step)
                    if self._stop_loss_price is None or self._stop_loss_price < threshold:
                        self._stop_loss_price = float(candle.ClosePrice) - trailing_stop

            self._check_protective_levels(candle)

        elif pos < 0:
            trailing_stop = self._get_price_offset(self.SellTrailingStopPips)
            trailing_step = self._get_price_offset(self.TrailingStepPips)

            if trailing_stop > 0 and trailing_step >= 0:
                profit = self._entry_price - float(candle.ClosePrice)
                if profit > trailing_stop + trailing_step:
                    threshold = float(candle.ClosePrice) + (trailing_stop + trailing_step)
                    if self._stop_loss_price is None or self._stop_loss_price > threshold:
                        self._stop_loss_price = float(candle.ClosePrice) + trailing_stop

            self._check_protective_levels(candle)

    def _check_protective_levels(self, candle):
        pos = float(self.Position)
        if pos > 0:
            if self._stop_loss_price is not None and float(candle.LowPrice) <= self._stop_loss_price:
                self.SellMarket(abs(pos))
                self._reset_position_state()
                return True
            if self._take_profit_price is not None and float(candle.HighPrice) >= self._take_profit_price:
                self.SellMarket(abs(pos))
                self._reset_position_state()
                return True
        elif pos < 0:
            if self._stop_loss_price is not None and float(candle.HighPrice) >= self._stop_loss_price:
                self.BuyMarket(abs(pos))
                self._reset_position_state()
                return True
            if self._take_profit_price is not None and float(candle.LowPrice) <= self._take_profit_price:
                self.BuyMarket(abs(pos))
                self._reset_position_state()
                return True
        return False

    def _reset_position_state(self):
        self._entry_price = None
        self._stop_loss_price = None
        self._take_profit_price = None

    def _get_price_offset(self, pips):
        if pips <= 0:
            return 0.0
        return pips * self._pip_value

    def _calculate_pip_value(self):
        sec = self.Security
        if sec is None:
            return 1.0
        price_step = float(sec.PriceStep) if sec.PriceStep is not None else 1.0
        if price_step <= 0:
            price_step = 1.0

        decimals = self._get_decimal_places(price_step)
        multiplier = 10.0 if decimals == 3 or decimals == 5 else 1.0
        return price_step * multiplier

    def _get_decimal_places(self, value):
        value = abs(value)
        count = 0
        while value != int(value) and count < 10:
            value *= 10.0
            count += 1
        return count

    def OnReseted(self):
        super(ichimoku_barabashkakvn_strategy, self).OnReseted()
        self._prev_tenkan = None
        self._pip_value = 0.0
        self._reset_position_state()

    def CreateClone(self):
        return ichimoku_barabashkakvn_strategy()
