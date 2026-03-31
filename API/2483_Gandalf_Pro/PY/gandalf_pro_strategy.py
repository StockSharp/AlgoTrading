import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import WeightedMovingAverage, SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy


class gandalf_pro_strategy(Strategy):
    def __init__(self):
        super(gandalf_pro_strategy, self).__init__()
        self._entry_buffer_steps = self.Param("EntryBufferSteps", 150.0)
        self._enable_buy = self.Param("EnableBuy", True)
        self._buy_length = self.Param("BuyLength", 24)
        self._buy_price_factor = self.Param("BuyPriceFactor", 0.18)
        self._buy_trend_factor = self.Param("BuyTrendFactor", 0.18)
        self._buy_stop_loss = self.Param("BuyStopLoss", 62)
        self._buy_risk_multiplier = self.Param("BuyRiskMultiplier", 0.0)
        self._enable_sell = self.Param("EnableSell", True)
        self._sell_length = self.Param("SellLength", 24)
        self._sell_price_factor = self.Param("SellPriceFactor", 0.18)
        self._sell_trend_factor = self.Param("SellTrendFactor", 0.18)
        self._sell_stop_loss = self.Param("SellStopLoss", 62)
        self._sell_risk_multiplier = self.Param("SellRiskMultiplier", 0.0)
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4)))

        self._close_history = []
        self._available_history = 0
        self._max_period = 0
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
        self._buy_w_ind = None
        self._buy_s_ind = None
        self._sell_w_ind = None
        self._sell_s_ind = None

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    def OnReseted(self):
        super(gandalf_pro_strategy, self).OnReseted()
        self._close_history = []
        self._available_history = 0
        self._max_period = 0
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

    def OnStarted2(self, time):
        super(gandalf_pro_strategy, self).OnStarted2(time)

        ps = 1.0
        if self.Security is not None and self.Security.PriceStep is not None:
            ps = float(self.Security.PriceStep)
        if ps <= 0:
            ps = 1.0
        self._price_step = ps

        buy_len = int(self._buy_length.Value)
        sell_len = int(self._sell_length.Value)
        self._max_period = max(buy_len, sell_len)
        self._close_history = [0.0] * (self._max_period + 2)
        self._available_history = 0

        self._buy_w_ind = WeightedMovingAverage()
        self._buy_w_ind.Length = buy_len
        self._buy_s_ind = SimpleMovingAverage()
        self._buy_s_ind.Length = buy_len
        self._sell_w_ind = WeightedMovingAverage()
        self._sell_w_ind.Length = sell_len
        self._sell_s_ind = SimpleMovingAverage()
        self._sell_s_ind.Length = sell_len

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

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(self._buy_w_ind, self._buy_s_ind, self._sell_w_ind, self._sell_s_ind, self._process_candle).Start()

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
        pos = float(self.Position)
        if pos > 0:
            if ((self._long_stop is not None and low <= self._long_stop) or
                (self._long_target is not None and high >= self._long_target)):
                self.SellMarket(abs(pos))
                self._long_stop = None
                self._long_target = None
        else:
            self._long_stop = None
            self._long_target = None

        pos = float(self.Position)
        if pos < 0:
            if ((self._short_stop is not None and high >= self._short_stop) or
                (self._short_target is not None and low <= self._short_target)):
                self.BuyMarket(abs(pos))
                self._short_stop = None
                self._short_target = None
        else:
            self._short_stop = None
            self._short_target = None

        buy_length = int(self._buy_length.Value)
        sell_length = int(self._sell_length.Value)
        buf = float(self._entry_buffer_steps.Value)

        buy_ready = self._has_prev_buy and self._available_history >= buy_length
        sell_ready = self._has_prev_sell and self._available_history >= sell_length

        if self._enable_buy.Value and buy_ready and self.IsFormedAndOnlineAndAllowTrading():
            target = self._calc_target(buy_length, float(self._buy_price_factor.Value),
                                       float(self._buy_trend_factor.Value), self._prev_buy_w, self._prev_buy_s)
            if target > close + buf * self._price_step:
                volume = self._get_order_volume(float(self._buy_risk_multiplier.Value))
                if volume > 0:
                    self.BuyMarket(volume)
                    self._long_target = target
                    sl = int(self._buy_stop_loss.Value)
                    self._long_stop = close - sl * self._price_step if sl > 0 else None

        if self._enable_sell.Value and sell_ready and self.IsFormedAndOnlineAndAllowTrading():
            target = self._calc_target(sell_length, float(self._sell_price_factor.Value),
                                       float(self._sell_trend_factor.Value), self._prev_sell_w, self._prev_sell_s)
            if target < close - buf * self._price_step:
                volume = self._get_order_volume(float(self._sell_risk_multiplier.Value))
                if volume > 0:
                    self.SellMarket(volume)
                    self._short_target = target
                    sl = int(self._sell_stop_loss.Value)
                    self._short_stop = close + sl * self._price_step if sl > 0 else None

        if self._buy_w_ind.IsFormed and self._buy_s_ind.IsFormed:
            self._prev_buy_w = buy_w
            self._prev_buy_s = buy_s
            self._has_prev_buy = True

        if self._sell_w_ind.IsFormed and self._sell_s_ind.IsFormed:
            self._prev_sell_w = sell_w
            self._prev_sell_s = sell_s
            self._has_prev_sell = True

        self._update_close_history(close)

    def _update_close_history(self, close):
        if len(self._close_history) <= 2:
            return
        i = len(self._close_history) - 1
        while i > 1:
            self._close_history[i] = self._close_history[i - 1]
            i -= 1
        self._close_history[1] = close
        if self._available_history < len(self._close_history) - 1:
            self._available_history += 1

    def _calc_target(self, length, price_factor, trend_factor, w_prev, s_prev):
        if length <= 1:
            return 0.0

        t = [0.0] * (length + 2)
        s = [0.0] * (length + 2)
        lm1 = float(length - 1)
        trend_comp = (6.0 * w_prev - 6.0 * s_prev) / lm1
        t[length] = trend_comp
        s[length] = 4.0 * s_prev - 3.0 * w_prev - trend_comp

        for k in range(length - 1, 0, -1):
            c = self._close_history[k]
            s[k] = price_factor * c + (1.0 - price_factor) * (s[k + 1] + t[k + 1])
            t[k] = trend_factor * (s[k] - s[k + 1]) + (1.0 - trend_factor) * t[k + 1]

        return s[1] + t[1]

    def _get_order_volume(self, risk_multiplier):
        base_vol = float(self.Volume)
        if risk_multiplier <= 0:
            return base_vol
        return base_vol * risk_multiplier

    def CreateClone(self):
        return gandalf_pro_strategy()
