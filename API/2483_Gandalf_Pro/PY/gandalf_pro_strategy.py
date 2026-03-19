import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import WeightedMovingAverage, SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy

class gandalf_pro_strategy(Strategy):
    """
    Gandalf PRO: LWMA/SMA smoothing filter strategy.
    Calculates dynamic targets using recursive smoothing and trades
    when projected level is sufficiently far from current price.
    """

    def __init__(self):
        super(gandalf_pro_strategy, self).__init__()
        self._entry_buffer_steps = self.Param("EntryBufferSteps", 3.0) \
            .SetDisplay("Entry Buffer", "Entry buffer distance in price steps", "General")
        self._buy_length = self.Param("BuyLength", 24) \
            .SetDisplay("Buy Length", "LWMA/SMA length for longs", "General")
        self._buy_price_factor = self.Param("BuyPriceFactor", 0.18) \
            .SetDisplay("Buy Price Factor", "Recursive smoothing weight for price", "General")
        self._buy_trend_factor = self.Param("BuyTrendFactor", 0.18) \
            .SetDisplay("Buy Trend Factor", "Recursive smoothing weight for trend", "General")
        self._buy_stop_loss = self.Param("BuyStopLoss", 62) \
            .SetDisplay("Buy Stop Loss", "Stop distance for longs in price steps", "Risk")
        self._sell_length = self.Param("SellLength", 24) \
            .SetDisplay("Sell Length", "LWMA/SMA length for shorts", "General")
        self._sell_price_factor = self.Param("SellPriceFactor", 0.18) \
            .SetDisplay("Sell Price Factor", "Recursive smoothing weight for price", "General")
        self._sell_trend_factor = self.Param("SellTrendFactor", 0.18) \
            .SetDisplay("Sell Trend Factor", "Recursive smoothing weight for trend", "General")
        self._sell_stop_loss = self.Param("SellStopLoss", 62) \
            .SetDisplay("Sell Stop Loss", "Stop distance for shorts in price steps", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Data type used for calculations", "General")

        self._close_history = []
        self._prev_buy_w = 0.0
        self._prev_buy_s = 0.0
        self._has_prev_buy = False
        self._prev_sell_w = 0.0
        self._prev_sell_s = 0.0
        self._has_prev_sell = False
        self._long_stop = None
        self._long_target = None
        self._short_stop = None
        self._short_target = None
        self._price_step = 1.0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(gandalf_pro_strategy, self).OnReseted()
        self._close_history = []
        self._prev_buy_w = 0.0
        self._prev_buy_s = 0.0
        self._has_prev_buy = False
        self._prev_sell_w = 0.0
        self._prev_sell_s = 0.0
        self._has_prev_sell = False
        self._long_stop = None
        self._long_target = None
        self._short_stop = None
        self._short_target = None
        self._price_step = 1.0

    def OnStarted(self, time):
        super(gandalf_pro_strategy, self).OnStarted(time)

        ps = 1.0
        if self.Security is not None and self.Security.PriceStep is not None:
            ps = float(self.Security.PriceStep)
        if ps <= 0:
            ps = 1.0
        self._price_step = ps

        buy_w = WeightedMovingAverage()
        buy_w.Length = self._buy_length.Value
        buy_s = SimpleMovingAverage()
        buy_s.Length = self._buy_length.Value
        sell_w = WeightedMovingAverage()
        sell_w.Length = self._sell_length.Value
        sell_s = SimpleMovingAverage()
        sell_s.Length = self._sell_length.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(buy_w, buy_s, sell_w, sell_s, self._process_candle).Start()

    def _process_candle(self, candle, buy_w_val, buy_s_val, sell_w_val, sell_s_val):
        if candle.State != CandleStates.Finished:
            return

        buy_w = float(buy_w_val)
        buy_s = float(buy_s_val)
        sell_w = float(sell_w_val)
        sell_s = float(sell_s_val)
        close = float(candle.ClosePrice)
        high = float(candle.HighPrice)
        low = float(candle.LowPrice)

        # Manage open positions
        if self.Position > 0:
            if ((self._long_stop is not None and low <= self._long_stop) or
                (self._long_target is not None and high >= self._long_target)):
                self.SellMarket()
                self._long_stop = None
                self._long_target = None
        else:
            self._long_stop = None
            self._long_target = None

        if self.Position < 0:
            if ((self._short_stop is not None and high >= self._short_stop) or
                (self._short_target is not None and low <= self._short_target)):
                self.BuyMarket()
                self._short_stop = None
                self._short_target = None
        else:
            self._short_stop = None
            self._short_target = None

        buy_length = self._buy_length.Value
        sell_length = self._sell_length.Value
        buf = self._entry_buffer_steps.Value

        if self._has_prev_buy and len(self._close_history) >= buy_length:
            if self.IsFormedAndOnlineAndAllowTrading():
                target = self._calc_target(buy_length, self._buy_price_factor.Value,
                                           self._buy_trend_factor.Value, self._prev_buy_w, self._prev_buy_s)
                if target > close + buf * self._price_step:
                    self.BuyMarket()
                    self._long_target = target
                    sl = self._buy_stop_loss.Value
                    self._long_stop = close - sl * self._price_step if sl > 0 else None

        if self._has_prev_sell and len(self._close_history) >= sell_length:
            if self.IsFormedAndOnlineAndAllowTrading():
                target = self._calc_target(sell_length, self._sell_price_factor.Value,
                                           self._sell_trend_factor.Value, self._prev_sell_w, self._prev_sell_s)
                if target < close - buf * self._price_step:
                    self.SellMarket()
                    self._short_target = target
                    sl = self._sell_stop_loss.Value
                    self._short_stop = close + sl * self._price_step if sl > 0 else None

        self._prev_buy_w = buy_w
        self._prev_buy_s = buy_s
        self._has_prev_buy = True
        self._prev_sell_w = sell_w
        self._prev_sell_s = sell_s
        self._has_prev_sell = True

        self._close_history.insert(0, close)
        max_period = max(buy_length, sell_length)
        while len(self._close_history) > max_period + 2:
            self._close_history.pop()

    def _calc_target(self, length, price_factor, trend_factor, w_prev, s_prev):
        if length <= 1:
            return 0.0

        lm1 = length - 1.0
        trend_comp = (6.0 * w_prev - 6.0 * s_prev) / lm1
        t = [0.0] * (length + 2)
        s = [0.0] * (length + 2)
        t[length] = trend_comp
        s[length] = 4.0 * s_prev - 3.0 * w_prev - trend_comp

        for k in range(length - 1, 0, -1):
            if k < len(self._close_history):
                c = self._close_history[k]
            else:
                c = 0.0
            s[k] = price_factor * c + (1.0 - price_factor) * (s[k + 1] + t[k + 1])
            t[k] = trend_factor * (s[k] - s[k + 1]) + (1.0 - trend_factor) * t[k + 1]

        return s[1] + t[1]

    def CreateClone(self):
        return gandalf_pro_strategy()
