import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from StockSharp.Algo.Indicators import ExponentialMovingAverage, DecimalIndicatorValue
from StockSharp.Algo.Strategies import Strategy
from StockSharp.Messages import DataType, CandleStates
from System import TimeSpan, Math, Decimal


class ea_moving_average_strategy(Strategy):
    def __init__(self):
        super(ea_moving_average_strategy, self).__init__()

        self._buy_open_period = self.Param("BuyOpenPeriod", 30)
        self._buy_open_shift = self.Param("BuyOpenShift", 3)
        self._buy_close_period = self.Param("BuyClosePeriod", 14)
        self._buy_close_shift = self.Param("BuyCloseShift", 3)
        self._sell_open_period = self.Param("SellOpenPeriod", 30)
        self._sell_open_shift = self.Param("SellOpenShift", 0)
        self._sell_close_period = self.Param("SellClosePeriod", 20)
        self._sell_close_shift = self.Param("SellCloseShift", 2)
        self._use_buy = self.Param("UseBuy", True)
        self._use_sell = self.Param("UseSell", True)
        self._consider_price_last_out = self.Param("ConsiderPriceLastOut", True)
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(1)))

        self._buy_open_ma = None
        self._buy_close_ma = None
        self._sell_open_ma = None
        self._sell_close_ma = None

        self._buy_open_buffer = []
        self._buy_close_buffer = []
        self._sell_open_buffer = []
        self._sell_close_buffer = []

        self._last_exit_price = 0.0
        self._last_entry_price = 0.0
        self._last_entry_side = None

    @property
    def CandleType(self):
        return self._candle_type.Value

    def OnStarted2(self, time):
        super(ea_moving_average_strategy, self).OnStarted2(time)

        self._buy_open_ma = ExponentialMovingAverage()
        self._buy_open_ma.Length = self._buy_open_period.Value
        self._buy_close_ma = ExponentialMovingAverage()
        self._buy_close_ma.Length = self._buy_close_period.Value
        self._sell_open_ma = ExponentialMovingAverage()
        self._sell_open_ma.Length = self._sell_open_period.Value
        self._sell_close_ma = ExponentialMovingAverage()
        self._sell_close_ma.Length = self._sell_close_period.Value

        self._buy_open_buffer = []
        self._buy_close_buffer = []
        self._sell_open_buffer = []
        self._sell_close_buffer = []

        sub = self.SubscribeCandles(self.CandleType)
        sub.Bind(self._process_candle).Start()

    def _process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        price = float(candle.ClosePrice)
        t = candle.OpenTime

        buy_open_val = self._process_ma(self._buy_open_ma, self._buy_open_buffer, self._buy_open_shift.Value, price, t)
        buy_close_val = self._process_ma(self._buy_close_ma, self._buy_close_buffer, self._buy_close_shift.Value, price, t)
        sell_open_val = self._process_ma(self._sell_open_ma, self._sell_open_buffer, self._sell_open_shift.Value, price, t)
        sell_close_val = self._process_ma(self._sell_close_ma, self._sell_close_buffer, self._sell_close_shift.Value, price, t)

        if buy_open_val is None or buy_close_val is None or sell_open_val is None or sell_close_val is None:
            return

        if self.Position != 0:
            self._process_close_signal(candle, buy_close_val, sell_close_val)
        else:
            self._process_open_signal(candle, buy_open_val, sell_open_val)

    def _process_open_signal(self, candle, buy_ma, sell_ma):
        open_price = float(candle.OpenPrice)
        close_price = float(candle.ClosePrice)

        if self._use_buy.Value and open_price < buy_ma and close_price > buy_ma and self._can_re_enter(True, close_price):
            self.BuyMarket()
            self._last_entry_side = 'buy'
            self._last_entry_price = close_price
        elif self._use_sell.Value and open_price > sell_ma and close_price < sell_ma and self._can_re_enter(False, close_price):
            self.SellMarket()
            self._last_entry_side = 'sell'
            self._last_entry_price = close_price

    def _process_close_signal(self, candle, buy_ma, sell_ma):
        open_price = float(candle.OpenPrice)
        close_price = float(candle.ClosePrice)

        if self.Position > 0 and open_price > buy_ma and close_price < buy_ma:
            self.SellMarket(self.Position)
            self._last_exit_price = close_price
            self._last_entry_side = None
            self._last_entry_price = 0.0
        elif self.Position < 0 and open_price < sell_ma and close_price > sell_ma:
            self.BuyMarket(abs(self.Position))
            self._last_exit_price = close_price
            self._last_entry_side = None
            self._last_entry_price = 0.0

    def _can_re_enter(self, is_buy, price):
        if not self._consider_price_last_out.Value:
            return True
        if self._last_exit_price == 0.0:
            return True
        if is_buy:
            return self._last_exit_price >= price
        else:
            return self._last_exit_price <= price

    def _process_ma(self, indicator, buffer, shift, price, time):
        if indicator is None:
            return None

        div = DecimalIndicatorValue(indicator, Decimal(float(price)), time)
        div.IsFinal = True
        result = indicator.Process(div)

        if not indicator.IsFormed:
            return None

        ma_value = float(result.Value)

        buffer.append(ma_value)
        max_size = shift + 1
        while len(buffer) > max_size:
            buffer.pop(0)

        if len(buffer) < max_size:
            return None

        return ma_value if shift == 0 else buffer[0]

    def OnReseted(self):
        super(ea_moving_average_strategy, self).OnReseted()
        self._buy_open_ma = None
        self._buy_close_ma = None
        self._sell_open_ma = None
        self._sell_close_ma = None
        self._buy_open_buffer = []
        self._buy_close_buffer = []
        self._sell_open_buffer = []
        self._sell_close_buffer = []
        self._last_exit_price = 0.0
        self._last_entry_price = 0.0
        self._last_entry_side = None

    def CreateClone(self):
        return ea_moving_average_strategy()
