import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Decimal
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy


class donky_ma_tp_sl_strategy(Strategy):
    """
    Donky MA TP SL strategy.
    The fast SMA crossing above the slow SMA goes long and crossing below goes short, reversing an opposite position.
    Half of the position closes at the first take-profit level and the remainder at the second level or at the stop-loss.
    The levels are fractions of the entry price (0.03 means 3%).
    """

    def __init__(self):
        super(donky_ma_tp_sl_strategy, self).__init__()
        self._fast_length = self.Param("FastLength", 10).SetGreaterThanZero().SetDisplay("Fast Length", "Fast SMA length", "Indicators")
        self._slow_length = self.Param("SlowLength", 30).SetGreaterThanZero().SetDisplay("Slow Length", "Slow SMA length", "Indicators")
        self._take_profit1_pct = self.Param("TakeProfit1Pct", 0.03).SetNotNegative().SetDisplay("Take Profit 1", "First target as a fraction of the entry price", "Risk")
        self._take_profit2_pct = self.Param("TakeProfit2Pct", 0.06).SetNotNegative().SetDisplay("Take Profit 2", "Second target as a fraction of the entry price", "Risk")
        self._stop_loss_pct = self.Param("StopLossPct", 0.01).SetNotNegative().SetDisplay("Stop Loss", "Stop distance as a fraction of the entry price", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(1))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._reset_state()

    @property
    def candle_type(self):
        return self._candle_type.Value

    def _reset_state(self):
        self._prev_fast = None
        self._prev_slow = None
        self._entry_price = Decimal(0)
        self._first_target_done = False

    def OnReseted(self):
        super(donky_ma_tp_sl_strategy, self).OnReseted()
        self._reset_state()

    def OnStarted2(self, time):
        super(donky_ma_tp_sl_strategy, self).OnStarted2(time)

        self._reset_state()

        fast = SimpleMovingAverage()
        fast.Length = self._fast_length.Value
        slow = SimpleMovingAverage()
        slow.Length = self._slow_length.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(fast, slow, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, fast)
            self.DrawIndicator(area, slow)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, fast, slow):
        if candle.State != CandleStates.Finished:
            return

        prev_fast = self._prev_fast
        prev_slow = self._prev_slow
        self._prev_fast = fast
        self._prev_slow = slow

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        if self._manage_position(candle):
            return

        if prev_fast is None or prev_slow is None:
            return

        cross_up = prev_fast <= prev_slow and fast > slow
        cross_down = prev_fast >= prev_slow and fast < slow

        if cross_up and self.Position <= 0:
            self.BuyMarket(self.Volume + abs(self.Position))
            self._entry_price = candle.ClosePrice
            self._first_target_done = False
        elif cross_down and self.Position >= 0:
            self.SellMarket(self.Volume + abs(self.Position))
            self._entry_price = candle.ClosePrice
            self._first_target_done = False

    # Returns True when the position was closed completely on this candle.
    def _manage_position(self, candle):
        if self.Position == 0 or self._entry_price <= 0:
            return False

        tp1 = Decimal(self._take_profit1_pct.Value)
        tp2 = Decimal(self._take_profit2_pct.Value)
        sl = Decimal(self._stop_loss_pct.Value)
        one = Decimal(1)
        two = Decimal(2)

        if self.Position > 0:
            stop = self._entry_price * (one - sl)
            target1 = self._entry_price * (one + tp1)
            target2 = self._entry_price * (one + tp2)

            if sl > 0 and candle.LowPrice <= stop:
                self.SellMarket(self.Position)
                return True

            if tp2 > 0 and candle.HighPrice >= target2:
                self.SellMarket(self.Position)
                return True

            if not self._first_target_done and tp1 > 0 and candle.HighPrice >= target1:
                self._first_target_done = True
                self.SellMarket(self.Position / two)
        else:
            stop = self._entry_price * (one + sl)
            target1 = self._entry_price * (one - tp1)
            target2 = self._entry_price * (one - tp2)
            volume = abs(self.Position)

            if sl > 0 and candle.HighPrice >= stop:
                self.BuyMarket(volume)
                return True

            if tp2 > 0 and candle.LowPrice <= target2:
                self.BuyMarket(volume)
                return True

            if not self._first_target_done and tp1 > 0 and candle.LowPrice <= target1:
                self._first_target_done = True
                self.BuyMarket(volume / two)

        return False

    def CreateClone(self):
        return donky_ma_tp_sl_strategy()
