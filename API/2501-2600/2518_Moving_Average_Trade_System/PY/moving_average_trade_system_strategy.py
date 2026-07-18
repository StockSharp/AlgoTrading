import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy

class moving_average_trade_system_strategy(Strategy):

    def __init__(self):
        super(moving_average_trade_system_strategy, self).__init__()
        self._take_profit_steps = self.Param("TakeProfitSteps", 50.0)
        self._stop_loss_steps = self.Param("StopLossSteps", 50.0)
        self._trailing_stop_steps = self.Param("TrailingStopSteps", 11.0)
        self._slope_threshold_steps = self.Param("SlopeThresholdSteps", 10.0)
        self._fast_period = self.Param("FastPeriod", 5)
        self._medium_period = self.Param("MediumPeriod", 20)
        self._signal_period = self.Param("SignalPeriod", 40)
        self._slow_period = self.Param("SlowPeriod", 60)
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(1)))

        self._prev_signal = None
        self._prev_slow = None
        self._long_entry_price = None
        self._long_take_profit = None
        self._long_stop_loss = None
        self._long_high = 0.0
        self._short_entry_price = None
        self._short_take_profit = None
        self._short_stop_loss = None
        self._short_low = 0.0
        self._sma_fast = None
        self._sma_medium = None
        self._sma_signal = None
        self._sma_slow = None

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(moving_average_trade_system_strategy, self).OnReseted()
        self._prev_signal = None
        self._prev_slow = None
        self._reset_long_state()
        self._reset_short_state()

    def OnStarted2(self, time):
        super(moving_average_trade_system_strategy, self).OnStarted2(time)
        self._sma_fast = SimpleMovingAverage()
        self._sma_fast.Length = self._fast_period.Value
        self._sma_medium = SimpleMovingAverage()
        self._sma_medium.Length = self._medium_period.Value
        self._sma_signal = SimpleMovingAverage()
        self._sma_signal.Length = self._signal_period.Value
        self._sma_slow = SimpleMovingAverage()
        self._sma_slow.Length = self._slow_period.Value
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self._sma_fast, self._sma_medium, self._sma_signal, self._sma_slow, self._process_candle).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, self._sma_fast)
            self.DrawIndicator(area, self._sma_medium)
            self.DrawIndicator(area, self._sma_signal)
            self.DrawIndicator(area, self._sma_slow)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, fast_val, medium_val, signal_val, slow_val):
        if candle.State != CandleStates.Finished:
            return
        fast = float(fast_val)
        medium = float(medium_val)
        sig = float(signal_val)
        slow = float(slow_val)

        if not self._sma_fast.IsFormed or not self._sma_medium.IsFormed or not self._sma_signal.IsFormed or not self._sma_slow.IsFormed:
            self._prev_signal = sig
            self._prev_slow = slow
            return

        prev_signal = self._prev_signal
        prev_slow = self._prev_slow
        self._prev_signal = sig
        self._prev_slow = slow

        if prev_signal is None or prev_slow is None:
            return

        price_step = self._get_price_step()
        slope_threshold = float(self._slope_threshold_steps.Value) * price_step

        bullish_structure = fast > medium and medium > slow
        bearish_structure = fast < medium and medium < slow
        bullish_slope = (sig - slow) >= slope_threshold
        bearish_slope = (slow - sig) >= slope_threshold
        bullish_cross = prev_signal <= prev_slow and sig > slow
        bearish_cross = prev_signal >= prev_slow and sig < slow

        buy_signal = bullish_structure and bullish_slope and bullish_cross
        sell_signal = bearish_structure and bearish_slope and bearish_cross

        if buy_signal and self.Position <= 0:
            self.BuyMarket()
        elif sell_signal and self.Position >= 0:
            self.SellMarket()
        else:
            if self.Position > 0:
                if sig <= slow:
                    self.SellMarket()
                else:
                    self._manage_long(candle, price_step)
            elif self.Position < 0:
                if sig >= slow:
                    self.BuyMarket()
                else:
                    self._manage_short(candle, price_step)

    def _manage_long(self, candle, price_step):
        if self._long_entry_price is None:
            return
        high = float(candle.HighPrice)
        low = float(candle.LowPrice)
        close = float(candle.ClosePrice)
        self._long_high = max(self._long_high, high)

        if self._long_take_profit is not None and high >= self._long_take_profit:
            self.SellMarket()
            return
        if self._long_stop_loss is not None and low <= self._long_stop_loss:
            self.SellMarket()
            return

        trailing = float(self._trailing_stop_steps.Value)
        if trailing <= 0:
            return
        trailing_level = self._long_high - trailing * price_step
        if close <= trailing_level:
            self.SellMarket()

    def _manage_short(self, candle, price_step):
        if self._short_entry_price is None:
            return
        high = float(candle.HighPrice)
        low = float(candle.LowPrice)
        close = float(candle.ClosePrice)
        self._short_low = min(self._short_low, low)

        if self._short_take_profit is not None and low <= self._short_take_profit:
            self.BuyMarket()
            return
        if self._short_stop_loss is not None and high >= self._short_stop_loss:
            self.BuyMarket()
            return

        trailing = float(self._trailing_stop_steps.Value)
        if trailing <= 0:
            return
        trailing_level = self._short_low + trailing * price_step
        if close >= trailing_level:
            self.BuyMarket()

    def OnOwnTradeReceived(self, trade):
        super(moving_average_trade_system_strategy, self).OnOwnTradeReceived(trade)
        t = trade.Trade
        if t is None:
            return
        price = float(t.Price)
        if self.Position > 0:
            self._reset_short_state()
            self._setup_long_state(price)
        elif self.Position < 0:
            self._reset_long_state()
            self._setup_short_state(price)
        else:
            self._reset_long_state()
            self._reset_short_state()

    def _setup_long_state(self, entry_price):
        price_step = self._get_price_step()
        self._long_entry_price = entry_price
        self._long_high = entry_price
        tp = float(self._take_profit_steps.Value)
        sl = float(self._stop_loss_steps.Value)
        self._long_take_profit = entry_price + tp * price_step if tp > 0 else None
        self._long_stop_loss = entry_price - sl * price_step if sl > 0 else None

    def _setup_short_state(self, entry_price):
        price_step = self._get_price_step()
        self._short_entry_price = entry_price
        self._short_low = entry_price
        tp = float(self._take_profit_steps.Value)
        sl = float(self._stop_loss_steps.Value)
        self._short_take_profit = entry_price - tp * price_step if tp > 0 else None
        self._short_stop_loss = entry_price + sl * price_step if sl > 0 else None

    def _reset_long_state(self):
        self._long_entry_price = None
        self._long_take_profit = None
        self._long_stop_loss = None
        self._long_high = 0.0

    def _reset_short_state(self):
        self._short_entry_price = None
        self._short_take_profit = None
        self._short_stop_loss = None
        self._short_low = 0.0

    def _get_price_step(self):
        sec = self.Security
        if sec is not None:
            step = sec.PriceStep
            if step is not None and float(step) != 0.0:
                return float(step)
        return 1.0

    def CreateClone(self):
        return moving_average_trade_system_strategy()
