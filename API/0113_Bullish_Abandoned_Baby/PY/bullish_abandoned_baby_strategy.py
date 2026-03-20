import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy

class bullish_abandoned_baby_strategy(Strategy):
    """
    Strategy based on Bullish Abandoned Baby candlestick pattern.
    Detects a bearish candle followed by a small-body candle near lows,
    then a bullish confirmation candle. Uses SMA for trend filter.
    Also detects the bearish mirror pattern for short entries.
    """

    def __init__(self):
        super(bullish_abandoned_baby_strategy, self).__init__()
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))).SetDisplay("Candle Type", "Candle timeframe", "General")
        self._ma_period = self.Param("MaPeriod", 20).SetDisplay("MA Period", "SMA period for exit", "Indicators")
        self._cooldown_bars = self.Param("CooldownBars", 400).SetDisplay("Cooldown Bars", "Bars between trades", "General")

        self._prev2_open = 0.0
        self._prev2_close = 0.0
        self._prev2_high = 0.0
        self._prev2_low = 0.0
        self._prev1_open = 0.0
        self._prev1_close = 0.0
        self._prev1_high = 0.0
        self._prev1_low = 0.0
        self._prev_ma = 0.0
        self._candle_count = 0
        self._cooldown = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(bullish_abandoned_baby_strategy, self).OnReseted()
        self._prev2_open = 0.0
        self._prev2_close = 0.0
        self._prev2_high = 0.0
        self._prev2_low = 0.0
        self._prev1_open = 0.0
        self._prev1_close = 0.0
        self._prev1_high = 0.0
        self._prev1_low = 0.0
        self._prev_ma = 0.0
        self._candle_count = 0
        self._cooldown = 0

    def OnStarted(self, time):
        super(bullish_abandoned_baby_strategy, self).OnStarted(time)

        self._prev2_open = 0.0
        self._prev2_close = 0.0
        self._prev2_high = 0.0
        self._prev2_low = 0.0
        self._prev1_open = 0.0
        self._prev1_close = 0.0
        self._prev1_high = 0.0
        self._prev1_low = 0.0
        self._prev_ma = 0.0
        self._candle_count = 0
        self._cooldown = 0

        sma = SimpleMovingAverage()
        sma.Length = self._ma_period.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(sma, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, sma)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, ma_val):
        if candle.State != CandleStates.Finished:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        self._candle_count += 1

        close = float(candle.ClosePrice)
        opn = float(candle.OpenPrice)
        high = float(candle.HighPrice)
        low = float(candle.LowPrice)
        ma = float(ma_val)
        cd = self._cooldown_bars.Value

        if self._cooldown > 0:
            self._cooldown -= 1
            # Shift candles even during cooldown
            self._prev2_open = self._prev1_open
            self._prev2_close = self._prev1_close
            self._prev2_high = self._prev1_high
            self._prev2_low = self._prev1_low
            self._prev1_open = opn
            self._prev1_close = close
            self._prev1_high = high
            self._prev1_low = low
            self._prev_ma = ma
            return

        # Exit logic: MA cross
        if self.Position > 0 and close < ma and self._prev_ma > 0 and self._prev1_close >= self._prev_ma:
            self.SellMarket()
            self._cooldown = cd
        elif self.Position < 0 and close > ma and self._prev_ma > 0 and self._prev1_close <= self._prev_ma:
            self.BuyMarket()
            self._cooldown = cd

        # Entry logic
        if self.Position == 0 and self._candle_count >= 3 and self._prev2_close != 0:
            prev2_body = abs(self._prev2_close - self._prev2_open)
            prev1_body = abs(self._prev1_close - self._prev1_open)
            prev2_range = self._prev2_high - self._prev2_low

            # Small body (doji-like) for middle candle
            is_small_body = prev1_body < prev2_body * 0.4 and prev2_range > 0

            # Bullish abandoned baby (relaxed)
            first_bearish = self._prev2_close < self._prev2_open
            middle_near_low = self._prev1_close <= self._prev2_low + prev2_range * 0.3
            current_bullish = close > opn

            # Bearish abandoned baby (relaxed)
            first_bullish = self._prev2_close > self._prev2_open
            middle_near_high = self._prev1_close >= self._prev2_high - prev2_range * 0.3
            current_bearish = close < opn

            if is_small_body and first_bearish and middle_near_low and current_bullish and close > ma:
                self.BuyMarket()
                self._cooldown = cd
            elif is_small_body and first_bullish and middle_near_high and current_bearish and close < ma:
                self.SellMarket()
                self._cooldown = cd

        # Shift candle history
        self._prev2_open = self._prev1_open
        self._prev2_close = self._prev1_close
        self._prev2_high = self._prev1_high
        self._prev2_low = self._prev1_low
        self._prev1_open = opn
        self._prev1_close = close
        self._prev1_high = high
        self._prev1_low = low
        self._prev_ma = ma

    def CreateClone(self):
        return bullish_abandoned_baby_strategy()
