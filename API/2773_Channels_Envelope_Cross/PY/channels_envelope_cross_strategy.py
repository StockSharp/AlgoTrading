import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Math, Decimal
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy
from indicator_extensions import *

class channels_envelope_cross_strategy(Strategy):

    def __init__(self):
        super(channels_envelope_cross_strategy, self).__init__()
        self._order_volume = self.Param("OrderVolume", 0.1)
        self._use_trade_hours = self.Param("UseTradeHours", False)
        self._from_hour = self.Param("FromHour", 0)
        self._to_hour = self.Param("ToHour", 23)
        self._stop_loss_buy_pips = self.Param("StopLossBuyPips", 0)
        self._stop_loss_sell_pips = self.Param("StopLossSellPips", 0)
        self._take_profit_buy_pips = self.Param("TakeProfitBuyPips", 0)
        self._take_profit_sell_pips = self.Param("TakeProfitSellPips", 0)
        self._trailing_stop_buy_pips = self.Param("TrailingStopBuyPips", 30)
        self._trailing_stop_sell_pips = self.Param("TrailingStopSellPips", 30)
        self._trailing_step_pips = self.Param("TrailingStepPips", 1)
        self._envelope003 = self.Param("Envelope003", 0.003)
        self._envelope007 = self.Param("Envelope007", 0.007)
        self._envelope010 = self.Param("Envelope010", 0.01)
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5)))

        self._ema_fast_close = None
        self._ema_fast_open = None
        self._ema_slow = None
        self._has_previous_values = False
        self._prev_fast_close = 0.0
        self._prev_fast_open = 0.0
        self._prev_slow = 0.0
        self._prev_env_lower03 = 0.0
        self._prev_env_upper03 = 0.0
        self._prev_env_lower07 = 0.0
        self._prev_env_upper07 = 0.0
        self._prev_env_lower10 = 0.0
        self._prev_env_upper10 = 0.0
        self._entry_price = None
        self._stop_loss_price = None
        self._take_profit_price = None

    @property
    def OrderVolume(self):
        return self._order_volume.Value

    @property
    def UseTradeHours(self):
        return self._use_trade_hours.Value

    @property
    def FromHour(self):
        return self._from_hour.Value

    @property
    def ToHour(self):
        return self._to_hour.Value

    @property
    def StopLossBuyPips(self):
        return self._stop_loss_buy_pips.Value

    @property
    def StopLossSellPips(self):
        return self._stop_loss_sell_pips.Value

    @property
    def TakeProfitBuyPips(self):
        return self._take_profit_buy_pips.Value

    @property
    def TakeProfitSellPips(self):
        return self._take_profit_sell_pips.Value

    @property
    def TrailingStopBuyPips(self):
        return self._trailing_stop_buy_pips.Value

    @property
    def TrailingStopSellPips(self):
        return self._trailing_stop_sell_pips.Value

    @property
    def TrailingStepPips(self):
        return self._trailing_step_pips.Value

    @property
    def Envelope003(self):
        return self._envelope003.Value

    @property
    def Envelope007(self):
        return self._envelope007.Value

    @property
    def Envelope010(self):
        return self._envelope010.Value

    @property
    def CandleType(self):
        return self._candle_type.Value

    def OnStarted2(self, time):
        super(channels_envelope_cross_strategy, self).OnStarted2(time)
        self._ema_fast_close = ExponentialMovingAverage()
        self._ema_fast_close.Length = 2
        self._ema_fast_open = ExponentialMovingAverage()
        self._ema_fast_open.Length = 2
        self._ema_slow = ExponentialMovingAverage()
        self._ema_slow.Length = 220

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(self._process_candle).Start()

    def _process_candle(self, candle):
        if self.UseTradeHours and not self._is_within_trade_hours(candle.OpenTime):
            return
        if candle.State != CandleStates.Finished:
            return

        t = candle.ServerTime
        fc_val = process_float(self._ema_fast_close, Decimal(float(candle.ClosePrice)), t, True)
        fo_val = process_float(self._ema_fast_open, Decimal(float(candle.OpenPrice)), t, True)
        sl_val = process_float(self._ema_slow, Decimal(float(candle.ClosePrice)), t, True)

        fast_close = float(fc_val.Value)
        fast_open = float(fo_val.Value)
        slow = float(sl_val.Value)

        env003 = float(self.Envelope003)
        env007 = float(self.Envelope007)
        env010 = float(self.Envelope010)

        env_lower03 = slow * (1.0 - env003)
        env_upper03 = slow * (1.0 + env003)
        env_lower07 = slow * (1.0 - env007)
        env_upper07 = slow * (1.0 + env007)
        env_lower10 = slow * (1.0 - env010)
        env_upper10 = slow * (1.0 + env010)

        if not self._ema_slow.IsFormed or not self._ema_fast_close.IsFormed or not self._ema_fast_open.IsFormed:
            self._update_prev(fast_close, fast_open, slow, env_lower03, env_upper03, env_lower07, env_upper07, env_lower10, env_upper10)
            return

        if not self._has_previous_values:
            self._update_prev(fast_close, fast_open, slow, env_lower03, env_upper03, env_lower07, env_upper07, env_lower10, env_upper10)
            self._has_previous_values = True
            return

        buy_signal = (
            (fast_close > env_lower10 and self._prev_fast_close <= self._prev_env_lower10) or
            (fast_close > env_lower07 and self._prev_fast_close <= self._prev_env_lower07) or
            (fast_close < env_lower03 and self._prev_fast_close < self._prev_env_lower03) or
            (fast_close > slow and self._prev_fast_close <= self._prev_slow) or
            (fast_close > env_upper03 and self._prev_fast_close <= self._prev_env_upper03) or
            (fast_close > env_upper07 and self._prev_fast_close <= self._prev_env_upper07)
        )

        sell_signal = (
            (fast_open < env_upper10 and self._prev_fast_open >= self._prev_env_upper10) or
            (fast_open < env_upper07 and self._prev_fast_open >= self._prev_env_upper07) or
            (fast_open < env_upper03 and self._prev_fast_open >= self._prev_env_upper03) or
            (fast_open < slow and self._prev_fast_open >= self._prev_slow) or
            (fast_open < env_lower03 and self._prev_fast_open >= self._prev_env_lower03) or
            (fast_open < env_lower07 and self._prev_fast_open >= self._prev_env_lower07)
        )

        pos = float(self.Position)
        if pos > 0:
            self._manage_long(candle)
        elif pos < 0:
            self._manage_short(candle)

        if float(self.Position) == 0:
            if buy_signal:
                self.BuyMarket(float(self.OrderVolume))
                self._set_entry_state(True, float(candle.ClosePrice))
            elif sell_signal:
                self.SellMarket(float(self.OrderVolume))
                self._set_entry_state(False, float(candle.ClosePrice))

        self._update_prev(fast_close, fast_open, slow, env_lower03, env_upper03, env_lower07, env_upper07, env_lower10, env_upper10)

    def _manage_long(self, candle):
        if self._entry_price is None:
            return
        pip = self._get_pip_size()
        trail_dist = self.TrailingStopBuyPips * pip
        trail_step = self.TrailingStepPips * pip
        profit = float(candle.ClosePrice) - self._entry_price

        if self.TrailingStopBuyPips > 0 and profit > trail_dist + trail_step:
            threshold = float(candle.ClosePrice) - (trail_dist + trail_step)
            if self._stop_loss_price is None or self._stop_loss_price < threshold:
                self._stop_loss_price = float(candle.ClosePrice) - trail_dist

        pos = float(self.Position)
        if self._stop_loss_price is not None and float(candle.LowPrice) <= self._stop_loss_price:
            self.SellMarket(pos)
            self._reset_position_state()
            return
        if self._take_profit_price is not None and float(candle.HighPrice) >= self._take_profit_price:
            self.SellMarket(pos)
            self._reset_position_state()

    def _manage_short(self, candle):
        if self._entry_price is None:
            return
        pip = self._get_pip_size()
        trail_dist = self.TrailingStopSellPips * pip
        trail_step = self.TrailingStepPips * pip
        profit = self._entry_price - float(candle.ClosePrice)

        if self.TrailingStopSellPips > 0 and profit > trail_dist + trail_step:
            threshold = float(candle.ClosePrice) + (trail_dist + trail_step)
            if self._stop_loss_price is None or self._stop_loss_price > threshold:
                self._stop_loss_price = float(candle.ClosePrice) + trail_dist

        pos = abs(float(self.Position))
        if self._stop_loss_price is not None and float(candle.HighPrice) >= self._stop_loss_price:
            self.BuyMarket(pos)
            self._reset_position_state()
            return
        if self._take_profit_price is not None and float(candle.LowPrice) <= self._take_profit_price:
            self.BuyMarket(pos)
            self._reset_position_state()

    def _set_entry_state(self, is_long, entry_price):
        self._entry_price = entry_price
        pip = self._get_pip_size()
        if is_long and self.StopLossBuyPips > 0:
            self._stop_loss_price = entry_price - self.StopLossBuyPips * pip
        elif not is_long and self.StopLossSellPips > 0:
            self._stop_loss_price = entry_price + self.StopLossSellPips * pip
        else:
            self._stop_loss_price = None

        if is_long and self.TakeProfitBuyPips > 0:
            self._take_profit_price = entry_price + self.TakeProfitBuyPips * pip
        elif not is_long and self.TakeProfitSellPips > 0:
            self._take_profit_price = entry_price - self.TakeProfitSellPips * pip
        else:
            self._take_profit_price = None

    def _reset_position_state(self):
        self._entry_price = None
        self._stop_loss_price = None
        self._take_profit_price = None

    def _update_prev(self, fc, fo, sl, el03, eu03, el07, eu07, el10, eu10):
        self._prev_fast_close = fc
        self._prev_fast_open = fo
        self._prev_slow = sl
        self._prev_env_lower03 = el03
        self._prev_env_upper03 = eu03
        self._prev_env_lower07 = el07
        self._prev_env_upper07 = eu07
        self._prev_env_lower10 = el10
        self._prev_env_upper10 = eu10

    def _is_within_trade_hours(self, time):
        hour = time.Hour
        if self.FromHour == self.ToHour:
            return hour == self.FromHour
        if self.FromHour < self.ToHour:
            return hour >= self.FromHour and hour <= self.ToHour
        return hour >= self.FromHour or hour <= self.ToHour

    def _get_pip_size(self):
        sec = self.Security
        step = float(sec.PriceStep) if sec is not None and sec.PriceStep is not None else 0.0001
        if sec is not None and sec.Decimals is not None:
            decimals = int(sec.Decimals)
            if decimals == 3 or decimals == 5:
                return step * 10.0
        return step

    def OnReseted(self):
        super(channels_envelope_cross_strategy, self).OnReseted()
        self._has_previous_values = False
        self._prev_fast_close = 0.0
        self._prev_fast_open = 0.0
        self._prev_slow = 0.0
        self._prev_env_lower03 = 0.0
        self._prev_env_upper03 = 0.0
        self._prev_env_lower07 = 0.0
        self._prev_env_upper07 = 0.0
        self._prev_env_lower10 = 0.0
        self._prev_env_upper10 = 0.0
        self._entry_price = None
        self._stop_loss_price = None
        self._take_profit_price = None

    def CreateClone(self):
        return channels_envelope_cross_strategy()
