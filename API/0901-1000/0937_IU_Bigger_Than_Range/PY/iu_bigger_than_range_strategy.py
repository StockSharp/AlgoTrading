import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import AverageTrueRange, Highest, Lowest
from StockSharp.Algo.Strategies import Strategy
from indicator_extensions import *


class iu_bigger_than_range_strategy(Strategy):
    """
    IU bigger than range strategy.
    The previous range spans the highest open/close and lowest open/close of the LookbackPeriod candles before the current one.
    A candle whose body is larger than that range enters in its direction, reversing an opposite position. The stop is placed by
    StopLossMethod (PreviousHighLow, Atr or Swing) and the target sits RiskToReward times the stop distance away; touching either
    closes the trade.
    """

    def __init__(self):
        super(iu_bigger_than_range_strategy, self).__init__()
        self._lookback_period = self.Param("LookbackPeriod", 22).SetGreaterThanZero().SetDisplay("Lookback Period", "Candles the previous range spans", "Parameters")
        self._risk_to_reward = self.Param("RiskToReward", 3).SetGreaterThanZero().SetDisplay("Risk To Reward", "Target distance as a multiple of the stop distance", "Risk")
        self._stop_loss_method = self.Param("StopLossMethod", "PreviousHighLow").SetDisplay("Stop Loss Method", "How the stop loss is placed: PreviousHighLow, Atr or Swing", "Risk")
        self._atr_length = self.Param("AtrLength", 14).SetGreaterThanZero().SetDisplay("ATR Length", "ATR period for the ATR stop", "Risk")
        self._atr_factor = self.Param("AtrFactor", 2.0).SetGreaterThanZero().SetDisplay("ATR Factor", "ATR multiplier for the ATR stop", "Risk")
        self._swing_length = self.Param("SwingLength", 10).SetGreaterThanZero().SetDisplay("Swing Length", "Candles the swing stop looks back over", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(1))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._body_high = None
        self._body_low = None
        self._swing_high = None
        self._swing_low = None
        self._reset_state()

    @property
    def candle_type(self):
        return self._candle_type.Value

    def _reset_state(self):
        self._prev_range_high = None
        self._prev_range_low = None
        self._prev_candle_high = None
        self._prev_candle_low = None
        self._stop_price = None
        self._target_price = None

    def OnReseted(self):
        super(iu_bigger_than_range_strategy, self).OnReseted()
        self._reset_state()

    def OnStarted2(self, time):
        super(iu_bigger_than_range_strategy, self).OnStarted2(time)

        self._reset_state()

        self._body_high = Highest()
        self._body_high.Length = self._lookback_period.Value
        self._body_low = Lowest()
        self._body_low.Length = self._lookback_period.Value
        self._swing_high = Highest()
        self._swing_high.Length = self._swing_length.Value
        self._swing_low = Lowest()
        self._swing_low.Length = self._swing_length.Value
        atr = AverageTrueRange()
        atr.Length = self._atr_length.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(atr, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, atr_value):
        if candle.State != CandleStates.Finished:
            return

        time = candle.OpenTime
        open_p = float(candle.OpenPrice)
        close = float(candle.ClosePrice)
        high = float(candle.HighPrice)
        low = float(candle.LowPrice)
        atr = float(atr_value)
        body_top = max(open_p, close)
        body_bottom = min(open_p, close)

        # The range is measured on the candles before this one.
        range_high = self._prev_range_high
        range_low = self._prev_range_low
        prev_high = self._prev_candle_high
        prev_low = self._prev_candle_low

        body_high_value = float(process_float(self._body_high, body_top, time, True))
        body_low_value = float(process_float(self._body_low, body_bottom, time, True))
        swing_high_value = float(process_float(self._swing_high, high, time, True))
        swing_low_value = float(process_float(self._swing_low, low, time, True))

        self._prev_range_high = body_high_value if self._body_high.IsFormed else None
        self._prev_range_low = body_low_value if self._body_low.IsFormed else None
        self._prev_candle_high = high
        self._prev_candle_low = low

        if not self.IsFormedAndOnlineAndAllowTrading():
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

        if range_high is None or range_low is None or prev_high is None or prev_low is None:
            return

        if body_top - body_bottom <= range_high - range_low:
            return

        method = str(self._stop_loss_method.Value)
        rr = float(self._risk_to_reward.Value)
        factor = float(self._atr_factor.Value)

        if close > open_p and self.Position <= 0:
            if method == "Atr":
                stop = close - atr * factor
            elif method == "Swing":
                stop = swing_low_value if self._swing_low.IsFormed else None
            else:
                stop = prev_low

            if stop is None or stop >= close:
                return

            self.BuyMarket(self.Volume + abs(self.Position))
            self._stop_price = stop
            self._target_price = close + (close - stop) * rr
        elif close < open_p and self.Position >= 0:
            if method == "Atr":
                stop = close + atr * factor
            elif method == "Swing":
                stop = swing_high_value if self._swing_high.IsFormed else None
            else:
                stop = prev_high

            if stop is None or stop <= close:
                return

            self.SellMarket(self.Volume + abs(self.Position))
            self._stop_price = stop
            self._target_price = close - (stop - close) * rr

    def CreateClone(self):
        return iu_bigger_than_range_strategy()
