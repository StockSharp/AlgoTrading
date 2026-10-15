import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy


class iu_opening_range_breakout_strategy(Strategy):
    """
    IU opening range breakout strategy.
    The first candle of each UTC day sets the opening range. Before EndTime a close crossing above its high goes long and a close
    crossing below its low goes short, reversing an opposite position, with at most MaxTrades entries a day. The stop sits at the
    previous candle's low (long) or high (short), the target RiskReward times the stop distance away, and any open position is
    closed at EndTime.
    """

    def __init__(self):
        super(iu_opening_range_breakout_strategy, self).__init__()
        self._risk_reward = self.Param("RiskReward", 2.0).SetGreaterThanZero().SetDisplay("Risk/Reward", "Target distance as a multiple of the stop distance", "Risk").SetOptimize(1.0, 3.0, 0.5)
        self._max_trades = self.Param("MaxTrades", 2).SetGreaterThanZero().SetDisplay("Max Trades", "Maximum entries per day", "General")
        self._end_time = self.Param("EndTime", TimeSpan(15, 0, 0)).SetDisplay("End Time", "Time of day (UTC) when positions are closed", "General")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(1))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._reset_state()

    @property
    def candle_type(self):
        return self._candle_type.Value

    def _reset_state(self):
        self._current_day = None
        self._range_high = None
        self._range_low = None
        self._prev_high = None
        self._prev_low = None
        self._prev_close = None
        self._stop_price = None
        self._target_price = None
        self._trades_today = 0

    def OnReseted(self):
        super(iu_opening_range_breakout_strategy, self).OnReseted()
        self._reset_state()

    def OnStarted2(self, time):
        super(iu_opening_range_breakout_strategy, self).OnStarted2(time)

        self._reset_state()

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        high = float(candle.HighPrice)
        low = float(candle.LowPrice)
        close = float(candle.ClosePrice)

        prev_high = self._prev_high
        prev_low = self._prev_low
        prev_close = self._prev_close
        self._prev_high = high
        self._prev_low = low
        self._prev_close = close

        day = candle.OpenTime.Date
        if self._current_day != day:
            # The first candle of the day is the opening range.
            self._current_day = day
            self._range_high = high
            self._range_low = low
            self._trades_today = 0
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        if candle.OpenTime.TimeOfDay >= self._end_time.Value:
            if self.Position > 0:
                self.SellMarket(self.Position)
            elif self.Position < 0:
                self.BuyMarket(-self.Position)
            self._stop_price = None
            self._target_price = None
            return

        if self.Position > 0 and self._stop_price is not None and self._target_price is not None:
            if low <= self._stop_price or high >= self._target_price:
                self.SellMarket(self.Position)
                self._stop_price = None
                self._target_price = None
                return
        elif self.Position < 0 and self._stop_price is not None and self._target_price is not None:
            if high >= self._stop_price or low <= self._target_price:
                self.BuyMarket(-self.Position)
                self._stop_price = None
                self._target_price = None
                return

        if self._trades_today >= self._max_trades.Value or self._range_high is None or self._range_low is None \
                or prev_close is None or prev_high is None or prev_low is None:
            return

        rr = float(self._risk_reward.Value)

        if prev_close <= self._range_high and close > self._range_high and self.Position <= 0 and prev_low < close:
            self.BuyMarket(self.Volume + abs(self.Position))
            self._trades_today += 1
            self._stop_price = prev_low
            self._target_price = close + (close - prev_low) * rr
        elif prev_close >= self._range_low and close < self._range_low and self.Position >= 0 and prev_high > close:
            self.SellMarket(self.Volume + abs(self.Position))
            self._trades_today += 1
            self._stop_price = prev_high
            self._target_price = close - (prev_high - close) * rr

    def CreateClone(self):
        return iu_opening_range_breakout_strategy()
