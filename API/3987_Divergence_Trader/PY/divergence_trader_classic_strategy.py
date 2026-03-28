import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy
from StockSharp.Algo.Indicators import SimpleMovingAverage

class divergence_trader_classic_strategy(Strategy):
    def __init__(self):
        super(divergence_trader_classic_strategy, self).__init__()

        self._order_volume = self.Param("OrderVolume", 0.1) \
            .SetDisplay("Order Volume", "Volume used when opening a new position", "Trading")
        self._fast_period = self.Param("FastPeriod", 7) \
            .SetDisplay("Fast SMA", "Period for the fast simple moving average", "Indicators")
        self._slow_period = self.Param("SlowPeriod", 88) \
            .SetDisplay("Slow SMA", "Period for the slow simple moving average", "Indicators")
        self._buy_threshold = self.Param("BuyThreshold", 10.0) \
            .SetDisplay("Buy Threshold", "Minimal divergence needed to allow long entries", "Signals")
        self._stay_out_threshold = self.Param("StayOutThreshold", 1000.0) \
            .SetDisplay("Stay Out Threshold", "Upper divergence bound disabling new entries", "Signals")
        self._take_profit_pips = self.Param("TakeProfitPips", 0.0) \
            .SetDisplay("Take Profit (pips)", "Distance in pips used to exit winners", "Risk")
        self._stop_loss_pips = self.Param("StopLossPips", 0.0) \
            .SetDisplay("Stop Loss (pips)", "Maximum adverse excursion tolerated", "Risk")
        self._trailing_stop_pips = self.Param("TrailingStopPips", 9999.0) \
            .SetDisplay("Trailing Stop (pips)", "Trailing distance; 9999 disables trailing", "Risk")
        self._break_even_pips = self.Param("BreakEvenPips", 9999.0) \
            .SetDisplay("Break-Even Trigger (pips)", "Profit in pips before moving stop to break-even", "Risk")
        self._break_even_buffer_pips = self.Param("BreakEvenBufferPips", 2.0) \
            .SetDisplay("Break-Even Buffer (pips)", "Buffer in pips added to the break-even stop", "Risk")
        self._basket_profit_currency = self.Param("BasketProfitCurrency", 75.0) \
            .SetDisplay("Basket Profit", "Floating profit that forces closing all positions", "Basket")
        self._basket_loss_currency = self.Param("BasketLossCurrency", 9999.0) \
            .SetDisplay("Basket Loss", "Floating loss that forces closing all positions", "Basket")
        self._start_hour = self.Param("StartHour", 0) \
            .SetDisplay("Start Hour", "Hour when trading becomes active (0-23)", "Schedule")
        self._stop_hour = self.Param("StopHour", 24) \
            .SetDisplay("Stop Hour", "Hour when trading stops accepting new entries (1-24)", "Schedule")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Timeframe used to calculate signals", "General")

        self._previous_spread = None
        self._pip_size = 0.0
        self._max_basket_pnl = 0.0
        self._min_basket_pnl = 0.0
        self._break_even_price = None
        self._trailing_stop_price = None
        self._highest_price = 0.0
        self._lowest_price = 0.0
        self._entry_price = 0.0

    @property
    def OrderVolume(self):
        return self._order_volume.Value

    @property
    def FastPeriod(self):
        return self._fast_period.Value

    @property
    def SlowPeriod(self):
        return self._slow_period.Value

    @property
    def BuyThreshold(self):
        return self._buy_threshold.Value

    @property
    def StayOutThreshold(self):
        return self._stay_out_threshold.Value

    @property
    def TakeProfitPips(self):
        return self._take_profit_pips.Value

    @property
    def StopLossPips(self):
        return self._stop_loss_pips.Value

    @property
    def TrailingStopPips(self):
        return self._trailing_stop_pips.Value

    @property
    def BreakEvenPips(self):
        return self._break_even_pips.Value

    @property
    def BreakEvenBufferPips(self):
        return self._break_even_buffer_pips.Value

    @property
    def BasketProfitCurrency(self):
        return self._basket_profit_currency.Value

    @property
    def BasketLossCurrency(self):
        return self._basket_loss_currency.Value

    @property
    def StartHour(self):
        return self._start_hour.Value

    @property
    def StopHour(self):
        return self._stop_hour.Value

    @property
    def CandleType(self):
        return self._candle_type.Value

    def OnStarted(self, time):
        super(divergence_trader_classic_strategy, self).OnStarted(time)

        self._pip_size = self._calculate_pip_size()

        self._fast_sma = SimpleMovingAverage()
        self._fast_sma.Length = self.FastPeriod
        self._slow_sma = SimpleMovingAverage()
        self._slow_sma.Length = self.SlowPeriod

        self._previous_spread = None
        self._break_even_price = None
        self._trailing_stop_price = None
        self._highest_price = 0.0
        self._lowest_price = 0.0

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(self._fast_sma, self._slow_sma, self.ProcessCandle).Start()

    def ProcessCandle(self, candle, fast_value, slow_value):
        if candle.State != CandleStates.Finished:
            return

        fast_value = float(fast_value)
        slow_value = float(slow_value)

        self._manage_open_position(candle)

        if self._evaluate_basket_pnl(float(candle.ClosePrice)):
            self._previous_spread = fast_value - slow_value
            return

        if not self._fast_sma.IsFormed or not self._slow_sma.IsFormed:
            self._previous_spread = fast_value - slow_value
            return

        current_spread = fast_value - slow_value
        divergence = current_spread - self._previous_spread if self._previous_spread is not None else 0.0
        self._previous_spread = current_spread

        if not self._is_within_trading_hours(candle.CloseTime):
            return

        ov = float(self.OrderVolume)
        if ov <= 0:
            return

        buy_thr = float(self.BuyThreshold)
        stay_out = float(self.StayOutThreshold)

        if divergence >= buy_thr and divergence <= stay_out:
            if self.Position < 0:
                self.BuyMarket(abs(self.Position))
            if self.Position <= 0:
                self._reset_position_tracking()
                self.BuyMarket(ov)
        elif divergence <= -buy_thr and divergence >= -stay_out:
            if self.Position > 0:
                self.SellMarket(self.Position)
            if self.Position >= 0:
                self._reset_position_tracking()
                self.SellMarket(ov)

    def _manage_open_position(self, candle):
        if self.Position == 0:
            self._reset_position_tracking()
            return

        entry_price = self._entry_price
        if entry_price == 0:
            return

        pip_size = self._ensure_pip_size()
        tp_pips = float(self.TakeProfitPips)
        sl_pips = float(self.StopLossPips)
        be_pips = float(self.BreakEvenPips)
        be_buffer = float(self.BreakEvenBufferPips)
        trail_pips = float(self.TrailingStopPips)

        take_profit_distance = tp_pips * pip_size if tp_pips > 0 else 0.0
        stop_loss_distance = sl_pips * pip_size if sl_pips > 0 else 0.0
        break_even_distance = be_pips * pip_size if be_pips > 0 and be_pips < 9000 else 0.0
        break_even_buffer = be_buffer * pip_size if be_buffer > 0 else 0.0
        trailing_distance = trail_pips * pip_size if trail_pips > 0 and trail_pips < 9000 else 0.0
        abs_position = abs(self.Position)

        high_price = float(candle.HighPrice)
        low_price = float(candle.LowPrice)
        close_price = float(candle.ClosePrice)

        if self.Position > 0:
            if self._highest_price == 0:
                self._highest_price = entry_price
            self._highest_price = max(self._highest_price, high_price)

            profit_distance = close_price - entry_price

            if break_even_distance > 0 and profit_distance >= break_even_distance and self._break_even_price is None:
                self._break_even_price = entry_price + break_even_buffer

            if self._break_even_price is not None and low_price <= self._break_even_price:
                self.SellMarket(abs_position)
                self._reset_position_tracking()
                return

            if trailing_distance > 0 and profit_distance >= trailing_distance:
                candidate = self._highest_price - trailing_distance
                if self._trailing_stop_price is None or candidate > self._trailing_stop_price:
                    self._trailing_stop_price = candidate
                if self._trailing_stop_price is not None and low_price <= self._trailing_stop_price:
                    self.SellMarket(abs_position)
                    self._reset_position_tracking()
                    return

            if take_profit_distance > 0 and profit_distance >= take_profit_distance:
                self.SellMarket(abs_position)
                self._reset_position_tracking()
                return

            if stop_loss_distance > 0 and low_price <= entry_price - stop_loss_distance:
                self.SellMarket(abs_position)
                self._reset_position_tracking()

        elif self.Position < 0:
            if self._lowest_price == 0:
                self._lowest_price = entry_price
            self._lowest_price = min(self._lowest_price, low_price)

            profit_distance = entry_price - close_price

            if break_even_distance > 0 and profit_distance >= break_even_distance and self._break_even_price is None:
                self._break_even_price = entry_price - break_even_buffer

            if self._break_even_price is not None and high_price >= self._break_even_price:
                self.BuyMarket(abs_position)
                self._reset_position_tracking()
                return

            if trailing_distance > 0 and profit_distance >= trailing_distance:
                candidate = self._lowest_price + trailing_distance
                if self._trailing_stop_price is None or candidate < self._trailing_stop_price:
                    self._trailing_stop_price = candidate
                if self._trailing_stop_price is not None and high_price >= self._trailing_stop_price:
                    self.BuyMarket(abs_position)
                    self._reset_position_tracking()
                    return

            if take_profit_distance > 0 and profit_distance >= take_profit_distance:
                self.BuyMarket(abs_position)
                self._reset_position_tracking()
                return

            if stop_loss_distance > 0 and high_price >= entry_price + stop_loss_distance:
                self.BuyMarket(abs_position)
                self._reset_position_tracking()

    def _evaluate_basket_pnl(self, last_price):
        bp = float(self.BasketProfitCurrency)
        bl = float(self.BasketLossCurrency)
        if bp <= 0 and bl <= 0:
            return False
        if self.Position == 0:
            return False
        entry_price = self._entry_price
        if entry_price == 0:
            return False

        step = self._ensure_pip_size()
        price_move = last_price - entry_price if self.Position > 0 else entry_price - last_price
        pip_move = price_move / step if step > 0 else price_move
        currency_pnl = pip_move * step * abs(self.Position)

        self._max_basket_pnl = max(self._max_basket_pnl, currency_pnl)
        self._min_basket_pnl = min(self._min_basket_pnl, currency_pnl)

        should_close_profit = bp > 0 and currency_pnl >= bp
        should_close_loss = bl > 0 and currency_pnl <= -bl

        if should_close_profit or should_close_loss:
            self._close_all_positions()
            return True
        return False

    def _close_all_positions(self):
        if self.Position > 0:
            self.SellMarket(self.Position)
        elif self.Position < 0:
            self.BuyMarket(abs(self.Position))
        self._reset_position_tracking()

    def _reset_position_tracking(self):
        self._break_even_price = None
        self._trailing_stop_price = None
        self._highest_price = 0.0
        self._lowest_price = 0.0

    def _is_within_trading_hours(self, time):
        hour = time.Hour
        start = self.StartHour
        stop = self.StopHour
        if start == stop:
            return True
        if start < stop:
            return hour >= start and hour < stop
        return hour >= start or hour < stop

    def _calculate_pip_size(self):
        ps = self.Security.PriceStep if self.Security is not None else None
        step = float(ps) if ps is not None else 0.0
        return step if step > 0 else 0.0001

    def _ensure_pip_size(self):
        if self._pip_size <= 0:
            self._pip_size = self._calculate_pip_size()
        return self._pip_size

    def OnOwnTradeReceived(self, trade):
        super(divergence_trader_classic_strategy, self).OnOwnTradeReceived(trade)
        if self.Position != 0 and self._entry_price == 0:
            self._entry_price = float(trade.Trade.Price)
        if self.Position == 0:
            self._entry_price = 0.0

    def OnReseted(self):
        super(divergence_trader_classic_strategy, self).OnReseted()
        self._previous_spread = None
        self._pip_size = 0.0
        self._max_basket_pnl = 0.0
        self._min_basket_pnl = 0.0
        self._break_even_price = None
        self._trailing_stop_price = None
        self._highest_price = 0.0
        self._lowest_price = 0.0
        self._entry_price = 0.0

    def CreateClone(self):
        return divergence_trader_classic_strategy()
