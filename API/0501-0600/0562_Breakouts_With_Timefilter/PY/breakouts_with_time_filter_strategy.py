import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Decimal
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import SimpleMovingAverage, AverageTrueRange
from StockSharp.Algo.Strategies import Strategy

SESSION_START = TimeSpan(14, 30, 0)
SESSION_END = TimeSpan(15, 0, 0)


class StopLossTypes:
    """Stop loss placement methods."""
    Atr = 0
    Candle = 1
    Points = 2


class breakouts_with_time_filter_strategy(Strategy):
    """
    Breakouts with time filter strategy.
    A close above the highest high of the previous Length candles goes long and a close below their lowest low goes short, only inside
    the 14:30-15:00 UTC window when UseTimeFilter is set and only on the matching side of the moving average when UseMaFilter is set.
    The stop is placed by ATR, by the candle extremes or by fixed points, and the target sits RiskReward times the stop distance away.
    """

    def __init__(self):
        super(breakouts_with_time_filter_strategy, self).__init__()
        self._length = self.Param("Length", 5).SetGreaterThanZero().SetDisplay("Length", "Previous candles the breakout levels span", "Breakout")
        self._ma_length = self.Param("MaLength", 99).SetGreaterThanZero().SetDisplay("MA Length", "Period of the moving average filter", "Filters")
        self._use_ma_filter = self.Param("UseMaFilter", False).SetDisplay("Use MA Filter", "Require the close on the trade side of the moving average", "Filters")
        self._use_time_filter = self.Param("UseTimeFilter", True).SetDisplay("Use Time Filter", "Allow entries only between 14:30 and 15:00 UTC", "Filters")
        self._sl_type = self.Param("SlType", StopLossTypes.Atr).SetDisplay("SL Type", "Stop loss placement method", "Risk")
        self._sl_length = self.Param("SlLength", 0).SetNotNegative().SetDisplay("SL Length", "Earlier candles whose extreme sets a candle stop", "Risk")
        self._atr_length = self.Param("AtrLength", 14).SetGreaterThanZero().SetDisplay("ATR Length", "ATR period", "Risk")
        self._atr_multiplier = self.Param("AtrMultiplier", 0.5).SetGreaterThanZero().SetDisplay("ATR Multiplier", "ATR multiple of the ATR stop", "Risk")
        self._points_stop = self.Param("PointsStop", 50.0).SetGreaterThanZero().SetDisplay("Points Stop", "Stop distance in price steps", "Risk")
        self._risk_reward = self.Param("RiskReward", 3.0).SetGreaterThanZero().SetDisplay("Risk Reward", "Target distance as a multiple of the stop distance", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._reset_state()

    @property
    def candle_type(self):
        return self._candle_type.Value

    def _reset_state(self):
        self._bars = []
        self._stop_level = Decimal(0)
        self._target_level = Decimal(0)

    def OnReseted(self):
        super(breakouts_with_time_filter_strategy, self).OnReseted()
        self._reset_state()

    def OnStarted2(self, time):
        super(breakouts_with_time_filter_strategy, self).OnStarted2(time)

        self._reset_state()

        ma = SimpleMovingAverage()
        ma.Length = self._ma_length.Value
        atr = AverageTrueRange()
        atr.Length = self._atr_length.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(ma, atr, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, ma)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, ma_value, atr_value):
        if candle.State != CandleStates.Finished:
            return

        length = self._length.Value
        sl_length = self._sl_length.Value

        # Breakout levels come from the candles before this one.
        history = list(self._bars[-length:]) if len(self._bars) >= length else None

        self._bars.append((candle.HighPrice, candle.LowPrice))
        keep = max(length, sl_length + 1)
        while len(self._bars) > keep:
            self._bars.pop(0)

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        if self.Position > 0:
            if candle.LowPrice <= self._stop_level or candle.HighPrice >= self._target_level:
                self.SellMarket(self.Position)
                return
        elif self.Position < 0:
            if candle.HighPrice >= self._stop_level or candle.LowPrice <= self._target_level:
                self.BuyMarket(-self.Position)
                return

        use_ma = self._use_ma_filter.Value
        if history is None or not atr_value.IsFormed or (use_ma and not ma_value.IsFormed):
            return

        if self._use_time_filter.Value:
            tod = candle.OpenTime.TimeOfDay
            if tod < SESSION_START or tod >= SESSION_END:
                return

        close = candle.ClosePrice
        highest = max(b[0] for b in history)
        lowest = min(b[1] for b in history)
        ma = ma_value.GetValue[Decimal](None) if use_ma else Decimal(0)
        atr = atr_value.GetValue[Decimal](None)
        rr = Decimal(self._risk_reward.Value)

        if close > highest and (not use_ma or close > ma) and self.Position <= 0:
            stop = self._get_stop(True, close, atr)
            if stop >= close:
                return
            self._stop_level = stop
            self._target_level = close + rr * (close - stop)
            self.BuyMarket(self.Volume + abs(self.Position))
        elif close < lowest and (not use_ma or close < ma) and self.Position >= 0:
            stop = self._get_stop(False, close, atr)
            if stop <= close:
                return
            self._stop_level = stop
            self._target_level = close - rr * (stop - close)
            self.SellMarket(self.Volume + abs(self.Position))

    def _get_stop(self, is_long, close, atr):
        sl_type = self._sl_type.Value

        if sl_type == StopLossTypes.Atr:
            offset = atr * Decimal(self._atr_multiplier.Value)
            return close - offset if is_long else close + offset

        if sl_type == StopLossTypes.Candle:
            # The entry candle plus SlLength earlier candles.
            bars = self._bars[-(self._sl_length.Value + 1):]
            return min(b[1] for b in bars) if is_long else max(b[0] for b in bars)

        step = self.Security.PriceStep if self.Security is not None and self.Security.PriceStep is not None else Decimal(1)
        distance = Decimal(self._points_stop.Value) * step
        return close - distance if is_long else close + distance

    def CreateClone(self):
        return breakouts_with_time_filter_strategy()
