import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Decimal
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import SimpleMovingAverage, ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy


class btcusd_adjustable_sltp_strategy(Strategy):
    """
    BTCUSD Adjustable SLTP strategy.
    A bullish SMA(FastSmaLength)/SMA(SlowSmaLength) crossover arms a long setup at close * (1 - RetracementPercentage); the long opens
    when the close crosses back above that level. A bearish crossover cancels the setup, closes a long while the close is below
    EMA(EmaFilterLength) and in that case also opens a short. Take profit, stop loss and break-even trigger are distances in price
    steps from the entry close; once the trigger is reached the stop moves to the entry price.
    """

    def __init__(self):
        super(btcusd_adjustable_sltp_strategy, self).__init__()
        self._fast_sma_length = self.Param("FastSmaLength", 10).SetGreaterThanZero().SetDisplay("Fast SMA Length", "Fast SMA period", "Indicators")
        self._slow_sma_length = self.Param("SlowSmaLength", 25).SetGreaterThanZero().SetDisplay("Slow SMA Length", "Slow SMA period", "Indicators")
        self._ema_filter_length = self.Param("EmaFilterLength", 150).SetGreaterThanZero().SetDisplay("EMA Filter Length", "EMA filter period", "Indicators")
        self._take_profit_distance = self.Param("TakeProfitDistance", 1000.0).SetNotNegative().SetDisplay("Take Profit Distance", "Take profit distance in price steps", "Risk")
        self._stop_loss_distance = self.Param("StopLossDistance", 250.0).SetNotNegative().SetDisplay("Stop Loss Distance", "Stop loss distance in price steps", "Risk")
        self._break_even_trigger = self.Param("BreakEvenTrigger", 500.0).SetNotNegative().SetDisplay("Break Even Trigger", "Profit in price steps that moves the stop to the entry price", "Risk")
        self._retracement_percentage = self.Param("RetracementPercentage", 0.01).SetNotNegative().SetDisplay("Retracement Percentage", "Retracement below the crossover close as a fraction", "Entry")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(1))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._reset_state()

    @property
    def candle_type(self):
        return self._candle_type.Value

    def _reset_state(self):
        self._prev_fast = None
        self._prev_slow = None
        self._prev_close = None
        self._retracement_level = None
        self._entry_price = Decimal(0)
        self._break_even = False

    def OnReseted(self):
        super(btcusd_adjustable_sltp_strategy, self).OnReseted()
        self._reset_state()

    def OnStarted2(self, time):
        super(btcusd_adjustable_sltp_strategy, self).OnStarted2(time)

        self._reset_state()

        fast = SimpleMovingAverage()
        fast.Length = self._fast_sma_length.Value
        slow = SimpleMovingAverage()
        slow.Length = self._slow_sma_length.Value
        ema = ExponentialMovingAverage()
        ema.Length = self._ema_filter_length.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(fast, slow, ema, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, fast)
            self.DrawIndicator(area, slow)
            self.DrawIndicator(area, ema)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, fast, slow, ema):
        if candle.State != CandleStates.Finished:
            return

        close = candle.ClosePrice
        prev_fast = self._prev_fast
        prev_slow = self._prev_slow
        prev_close = self._prev_close

        self._prev_fast = fast
        self._prev_slow = slow
        self._prev_close = close

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        if self._check_stops(candle):
            return

        if prev_fast is None or prev_slow is None or prev_close is None:
            return

        bull_cross = prev_fast <= prev_slow and fast > slow
        bear_cross = prev_fast >= prev_slow and fast < slow

        if bull_cross:
            self._retracement_level = close * (Decimal(1) - Decimal(self._retracement_percentage.Value))
        elif bear_cross:
            self._retracement_level = None
            if close < ema:
                if self.Position >= 0:
                    self._enter(False, close)
                return

        level = self._retracement_level
        if level is not None and prev_close <= level and close > level and self.Position <= 0:
            self._retracement_level = None
            self._enter(True, close)

    def _enter(self, is_long, price):
        volume = self.Volume + abs(self.Position)
        if is_long:
            self.BuyMarket(volume)
        else:
            self.SellMarket(volume)
        self._entry_price = price
        self._break_even = False

    def _check_stops(self, candle):
        if self.Position == 0 or self._entry_price <= 0:
            return False

        step = self.Security.PriceStep if self.Security is not None and self.Security.PriceStep is not None else Decimal(1)
        tp_steps = Decimal(self._take_profit_distance.Value)
        sl_steps = Decimal(self._stop_loss_distance.Value)
        be_steps = Decimal(self._break_even_trigger.Value)
        take = tp_steps * step
        stop_distance = sl_steps * step
        trigger = be_steps * step

        if self.Position > 0:
            if self._break_even:
                stop = self._entry_price
            elif sl_steps > 0:
                stop = self._entry_price - stop_distance
            else:
                stop = None

            if (stop is not None and candle.LowPrice <= stop) or (tp_steps > 0 and candle.HighPrice >= self._entry_price + take):
                self.SellMarket(self.Position)
                self._entry_price = Decimal(0)
                return True

            if be_steps > 0 and candle.HighPrice - self._entry_price >= trigger:
                self._break_even = True
        else:
            if self._break_even:
                stop = self._entry_price
            elif sl_steps > 0:
                stop = self._entry_price + stop_distance
            else:
                stop = None

            if (stop is not None and candle.HighPrice >= stop) or (tp_steps > 0 and candle.LowPrice <= self._entry_price - take):
                self.BuyMarket(-self.Position)
                self._entry_price = Decimal(0)
                return True

            if be_steps > 0 and self._entry_price - candle.LowPrice >= trigger:
                self._break_even = True

        return False

    def CreateClone(self):
        return btcusd_adjustable_sltp_strategy()
