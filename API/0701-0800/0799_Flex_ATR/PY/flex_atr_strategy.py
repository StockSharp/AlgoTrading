import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage, RelativeStrengthIndex, AverageTrueRange
from StockSharp.Algo.Strategies import Strategy

RSI_MIDDLE = 50.0

class flex_atr_strategy(Strategy):
    """
    Flex ATR strategy.
    EMA, RSI and ATR periods are chosen from the candle timeframe: up to 5 minutes 8/21 EMAs, RSI 9 and ATR 10; up to 30 minutes
    12/26, RSI 14, ATR 14; up to 4 hours 20/50, RSI 14, ATR 14; longer 50/200, RSI 14, ATR 20. A fast EMA crossing above the slow
    one with RSI above 50 goes long and the opposite cross with RSI below 50 goes short, reversing an opposite position. A position
    closes at a stop AtrStopMult ATRs away or a target AtrProfitMult ATRs away, both set at entry; with EnableTrailingStop the stop
    also follows the best close at AtrTrailMult ATRs.
    """

    def __init__(self):
        super(flex_atr_strategy, self).__init__()
        self._atr_stop_mult = self.Param("AtrStopMult", 3.0).SetNotNegative().SetDisplay("ATR Stop Mult", "Stop distance in ATRs", "Risk")
        self._atr_profit_mult = self.Param("AtrProfitMult", 1.5).SetNotNegative().SetDisplay("ATR Profit Mult", "Target distance in ATRs", "Risk")
        self._enable_trailing_stop = self.Param("EnableTrailingStop", True).SetDisplay("Trailing Stop", "Let the stop follow the best close", "Risk")
        self._atr_trail_mult = self.Param("AtrTrailMult", 1.0).SetNotNegative().SetDisplay("ATR Trail Mult", "Trailing distance in ATRs", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(30))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._reset_state()

    @property
    def candle_type(self):
        return self._candle_type.Value

    def _reset_state(self):
        self._prev_fast = None
        self._prev_slow = None
        self._stop_price = 0.0
        self._target_price = 0.0

    def OnReseted(self):
        super(flex_atr_strategy, self).OnReseted()
        self._reset_state()

    def OnStarted2(self, time):
        super(flex_atr_strategy, self).OnStarted2(time)

        self._reset_state()

        arg = self.candle_type.Arg
        minutes = float(arg.TotalMinutes) if hasattr(arg, "TotalMinutes") else 0.0
        fast_length, slow_length, rsi_length, atr_length = self._select_periods(minutes)

        fast = ExponentialMovingAverage()
        fast.Length = fast_length
        slow = ExponentialMovingAverage()
        slow.Length = slow_length
        rsi = RelativeStrengthIndex()
        rsi.Length = rsi_length
        atr = AverageTrueRange()
        atr.Length = atr_length

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(fast, slow, rsi, atr, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, fast)
            self.DrawIndicator(area, slow)
            self.DrawOwnTrades(area)

    def _select_periods(self, minutes):
        if minutes <= 5:
            return 8, 21, 9, 10
        if minutes <= 30:
            return 12, 26, 14, 14
        if minutes <= 240:
            return 20, 50, 14, 14
        return 50, 200, 14, 20

    def _process_candle(self, candle, fast_value, slow_value, rsi_value, atr_value):
        if candle.State != CandleStates.Finished:
            return

        fast = float(fast_value)
        slow = float(slow_value)
        rsi = float(rsi_value)
        atr = float(atr_value)

        last_fast = self._prev_fast
        last_slow = self._prev_slow
        self._prev_fast = fast
        self._prev_slow = slow

        if last_fast is None or last_slow is None:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        close = float(candle.ClosePrice)
        stop_mult = float(self._atr_stop_mult.Value)
        profit_mult = float(self._atr_profit_mult.Value)
        trail_mult = float(self._atr_trail_mult.Value)

        cross_up = last_fast <= last_slow and fast > slow
        cross_down = last_fast >= last_slow and fast < slow

        if cross_up and rsi > RSI_MIDDLE and self.Position <= 0:
            self.BuyMarket(self.Volume + abs(self.Position))
            self._stop_price = close - stop_mult * atr if stop_mult > 0 else 0.0
            self._target_price = close + profit_mult * atr if profit_mult > 0 else 0.0
            return

        if cross_down and rsi < RSI_MIDDLE and self.Position >= 0:
            self.SellMarket(self.Volume + abs(self.Position))
            self._stop_price = close + stop_mult * atr if stop_mult > 0 else 0.0
            self._target_price = close - profit_mult * atr if profit_mult > 0 else 0.0
            return

        high = float(candle.HighPrice)
        low = float(candle.LowPrice)

        if self.Position > 0:
            if (self._stop_price > 0 and low <= self._stop_price) or (self._target_price > 0 and high >= self._target_price):
                self.SellMarket(self.Position)
                return
            if self._enable_trailing_stop.Value:
                trail = close - trail_mult * atr
                if trail > self._stop_price:
                    self._stop_price = trail
        elif self.Position < 0:
            if (self._stop_price > 0 and high >= self._stop_price) or (self._target_price > 0 and low <= self._target_price):
                self.BuyMarket(-self.Position)
                return
            if self._enable_trailing_stop.Value:
                trail = close + trail_mult * atr
                if self._stop_price <= 0 or trail < self._stop_price:
                    self._stop_price = trail

    def CreateClone(self):
        return flex_atr_strategy()
