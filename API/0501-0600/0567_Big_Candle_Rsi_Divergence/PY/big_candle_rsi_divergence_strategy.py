import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from collections import deque
from System import TimeSpan, Decimal
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import RelativeStrengthIndex
from StockSharp.Algo.Strategies import Strategy

BODY_LOOKBACK = 5


class big_candle_rsi_divergence_strategy(Strategy):
    """
    Big Candle RSI Divergence strategy.
    When flat, a candle whose body is bigger than each of the previous five bodies opens a trade in its direction. An initial stop
    InitialStopLossTicks price steps away protects the trade; once price has moved TrailStartTicks steps in profit a trailing stop
    follows the best price at TrailDistanceTicks steps. Fast RSI(5) and slow RSI(14) are plotted for comparison.
    """

    def __init__(self):
        super(big_candle_rsi_divergence_strategy, self).__init__()
        self._trail_start_ticks = self.Param("TrailStartTicks", 200).SetNotNegative().SetDisplay("Trail Start Ticks", "Profit in price steps that activates the trailing stop", "Risk")
        self._trail_distance_ticks = self.Param("TrailDistanceTicks", 150).SetNotNegative().SetDisplay("Trail Distance Ticks", "Trailing stop distance in price steps", "Risk")
        self._initial_stop_loss_ticks = self.Param("InitialStopLossTicks", 200).SetNotNegative().SetDisplay("Initial Stop Loss Ticks", "Initial stop loss in price steps", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._reset_state()

    @property
    def candle_type(self):
        return self._candle_type.Value

    def _reset_state(self):
        self._bodies = deque()
        self._entry_price = Decimal(0)
        self._best_price = Decimal(0)
        self._trailing_active = False

    def OnReseted(self):
        super(big_candle_rsi_divergence_strategy, self).OnReseted()
        self._reset_state()

    def OnStarted2(self, time):
        super(big_candle_rsi_divergence_strategy, self).OnStarted2(time)

        self._reset_state()

        rsi_fast = RelativeStrengthIndex()
        rsi_fast.Length = 5
        rsi_slow = RelativeStrengthIndex()
        rsi_slow.Length = 14

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(rsi_fast, rsi_slow, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)
            oscillators = self.CreateChartArea()
            if oscillators is not None:
                self.DrawIndicator(oscillators, rsi_fast)
                self.DrawIndicator(oscillators, rsi_slow)

    def _process_candle(self, candle, rsi_fast_value, rsi_slow_value):
        if candle.State != CandleStates.Finished:
            return

        body = abs(candle.ClosePrice - candle.OpenPrice)
        is_big = len(self._bodies) == BODY_LOOKBACK and all(body > b for b in self._bodies)

        self._bodies.append(body)
        while len(self._bodies) > BODY_LOOKBACK:
            self._bodies.popleft()

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        if self.Position != 0:
            self._manage_stops(candle)
            return

        if not is_big:
            return

        if candle.ClosePrice > candle.OpenPrice:
            self.BuyMarket(self.Volume)
            self._start_trade(candle.ClosePrice)
        elif candle.ClosePrice < candle.OpenPrice:
            self.SellMarket(self.Volume)
            self._start_trade(candle.ClosePrice)

    def _start_trade(self, price):
        self._entry_price = price
        self._best_price = price
        self._trailing_active = False

    def _manage_stops(self, candle):
        if self._entry_price <= 0:
            return

        step = self.Security.PriceStep if self.Security is not None and self.Security.PriceStep is not None else Decimal(1)
        stop_ticks = self._initial_stop_loss_ticks.Value
        trail_ticks = self._trail_distance_ticks.Value
        initial_stop = Decimal(stop_ticks) * step
        trail_start = Decimal(self._trail_start_ticks.Value) * step
        trail_distance = Decimal(trail_ticks) * step

        if self.Position > 0:
            stop = self._entry_price - initial_stop if stop_ticks > 0 else None
            if self._trailing_active:
                trail = self._best_price - trail_distance
                stop = trail if stop is None else max(stop, trail)

            if stop is not None and candle.LowPrice <= stop:
                self.SellMarket(self.Position)
                self._entry_price = Decimal(0)
                return

            self._best_price = max(self._best_price, candle.HighPrice)
            if trail_ticks > 0 and self._best_price - self._entry_price >= trail_start:
                self._trailing_active = True
        elif self.Position < 0:
            stop = self._entry_price + initial_stop if stop_ticks > 0 else None
            if self._trailing_active:
                trail = self._best_price + trail_distance
                stop = trail if stop is None else min(stop, trail)

            if stop is not None and candle.HighPrice >= stop:
                self.BuyMarket(-self.Position)
                self._entry_price = Decimal(0)
                return

            self._best_price = min(self._best_price, candle.LowPrice)
            if trail_ticks > 0 and self._entry_price - self._best_price >= trail_start:
                self._trailing_active = True

    def CreateClone(self):
        return big_candle_rsi_divergence_strategy()
