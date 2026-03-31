import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from StockSharp.Algo.Indicators import Highest, Lowest
from StockSharp.Algo.Strategies import Strategy
from StockSharp.Messages import DataType, CandleStates
from System import TimeSpan, Math


class night_flat_trade_strategy(Strategy):
    def __init__(self):
        super(night_flat_trade_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5)))
        self._take_profit_pips = self.Param("TakeProfitPips", 50.0)
        self._trailing_stop_pips = self.Param("TrailingStopPips", 15.0)
        self._trailing_step_pips = self.Param("TrailingStepPips", 5.0)
        self._diff_min_pips = self.Param("DiffMinPips", 18.0)
        self._diff_max_pips = self.Param("DiffMaxPips", 28.0)
        self._open_hour = self.Param("OpenHour", 0)
        self._range_length = self.Param("RangeLength", 3)

        self._highest = None
        self._lowest = None
        self._pip_size = 0.0
        self._entry_price = 0.0
        self._stop_price = None
        self._take_profit_price = None

    @property
    def CandleType(self):
        return self._candle_type.Value

    def OnStarted2(self, time):
        super(night_flat_trade_strategy, self).OnStarted2(time)

        self._highest = Highest()
        self._highest.Length = self._range_length.Value
        self._lowest = Lowest()
        self._lowest.Length = self._range_length.Value

        price_step = float(self.Security.PriceStep) if self.Security is not None and self.Security.PriceStep is not None else 0.0
        if price_step <= 0:
            price_step = 0.0001

        self._pip_size = price_step

        # Check if Security has Decimals property for pip adjustment
        decimals = None
        if self.Security is not None:
            try:
                decimals = self.Security.Decimals
            except:
                pass

        if decimals is not None and (decimals == 3 or decimals == 5):
            self._pip_size *= 10.0

        sub = self.SubscribeCandles(self.CandleType)
        sub.Bind(self._highest, self._lowest, self._process_candle).Start()

    def _process_candle(self, candle, highest_value, lowest_value):
        if candle.State != CandleStates.Finished:
            return

        # Manage active trades before scanning for new setups
        self._handle_existing_position(candle)

        if self.Position != 0:
            return

        if self._highest is None or self._lowest is None:
            return

        if not self._highest.IsFormed or not self._lowest.IsFormed:
            return

        hv = float(highest_value)
        lv = float(lowest_value)

        diff = hv - lv
        if diff <= 0:
            return

        quarter = diff / 4.0
        close_price = float(candle.ClosePrice)

        if close_price > lv and close_price <= lv + quarter:
            self.BuyMarket()
            self._entry_price = close_price
            self._stop_price = lv - diff / 3.0
            self._take_profit_price = close_price + self._to_price(self._take_profit_pips.Value) if self._take_profit_pips.Value > 0 else None
            return

        if close_price < hv and close_price >= hv - quarter:
            self.SellMarket()
            self._entry_price = close_price
            self._stop_price = hv + diff / 3.0
            self._take_profit_price = close_price - self._to_price(self._take_profit_pips.Value) if self._take_profit_pips.Value > 0 else None

    def _handle_existing_position(self, candle):
        if self.Position > 0:
            self._update_trailing_for_long(candle)

            if self._stop_price is not None and float(candle.LowPrice) <= self._stop_price:
                self.SellMarket(abs(self.Position))
                self._reset_trade_state()
                return

            if self._take_profit_price is not None and float(candle.HighPrice) >= self._take_profit_price:
                self.SellMarket(abs(self.Position))
                self._reset_trade_state()

        elif self.Position < 0:
            self._update_trailing_for_short(candle)

            if self._stop_price is not None and float(candle.HighPrice) >= self._stop_price:
                self.BuyMarket(abs(self.Position))
                self._reset_trade_state()
                return

            if self._take_profit_price is not None and float(candle.LowPrice) <= self._take_profit_price:
                self.BuyMarket(abs(self.Position))
                self._reset_trade_state()

    def _update_trailing_for_long(self, candle):
        if self._trailing_stop_pips.Value <= 0 or self._trailing_step_pips.Value <= 0 or self._stop_price is None:
            return

        trailing_distance = self._to_price(self._trailing_stop_pips.Value)
        step_distance = self._to_price(self._trailing_step_pips.Value)

        advance = float(candle.HighPrice) - self._entry_price
        if advance < trailing_distance + step_distance:
            return

        new_stop = float(candle.HighPrice) - trailing_distance
        if new_stop <= self._stop_price or new_stop - self._stop_price < step_distance:
            return

        self._stop_price = new_stop

    def _update_trailing_for_short(self, candle):
        if self._trailing_stop_pips.Value <= 0 or self._trailing_step_pips.Value <= 0 or self._stop_price is None:
            return

        trailing_distance = self._to_price(self._trailing_stop_pips.Value)
        step_distance = self._to_price(self._trailing_step_pips.Value)

        advance = self._entry_price - float(candle.LowPrice)
        if advance < trailing_distance + step_distance:
            return

        new_stop = float(candle.LowPrice) + trailing_distance
        if new_stop >= self._stop_price or self._stop_price - new_stop < step_distance:
            return

        self._stop_price = new_stop

    def _to_price(self, pips):
        if pips <= 0:
            return 0.0
        pip = self._pip_size if self._pip_size > 0 else 0.0001
        return pips * pip

    def _reset_trade_state(self):
        self._entry_price = 0.0
        self._stop_price = None
        self._take_profit_price = None

    def OnReseted(self):
        super(night_flat_trade_strategy, self).OnReseted()
        self._highest = None
        self._lowest = None
        self._pip_size = 0.0
        self._entry_price = 0.0
        self._stop_price = None
        self._take_profit_price = None

    def CreateClone(self):
        return night_flat_trade_strategy()
