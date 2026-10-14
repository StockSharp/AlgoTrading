import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Decimal
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import Highest, Lowest
from StockSharp.Algo.Strategies import Strategy


class gold_scalping_bos_choch_strategy(Strategy):
    """
    Gold scalping BOS and CHoCH strategy.
    The last swing high and low are the highest high and lowest low of the SwingLength candles before the current one. A high above the
    last swing high (break of structure) together with a close crossing above the last swing low (change of character) goes long; a low
    below the last swing low with a close crossing below the last swing high goes short. The stop sits at the RecentLength low (high) and
    the target TakeProfitFactor times that risk away.
    """

    def __init__(self):
        super(gold_scalping_bos_choch_strategy, self).__init__()
        self._recent_length = self.Param("RecentLength", 10).SetGreaterThanZero().SetDisplay("Recent Length", "Candles used for the stop level", "Structure")
        self._swing_length = self.Param("SwingLength", 5).SetGreaterThanZero().SetDisplay("Swing Length", "Candles used for the swing levels", "Structure")
        self._take_profit_factor = self.Param("TakeProfitFactor", 2.0).SetGreaterThanZero().SetDisplay("Take Profit Factor", "Target distance as a multiple of the stop distance", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(1))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._reset_state()

    @property
    def candle_type(self):
        return self._candle_type.Value

    def _reset_state(self):
        self._swing_high = None
        self._swing_low = None
        self._prev_swing_high = None
        self._prev_swing_low = None
        self._prev_close = None
        self._stop_price = Decimal(0)
        self._take_price = Decimal(0)

    def OnReseted(self):
        super(gold_scalping_bos_choch_strategy, self).OnReseted()
        self._reset_state()

    def OnStarted2(self, time):
        super(gold_scalping_bos_choch_strategy, self).OnStarted2(time)

        self._reset_state()

        swing_highest = Highest()
        swing_highest.Length = self._swing_length.Value
        swing_lowest = Lowest()
        swing_lowest.Length = self._swing_length.Value
        recent_highest = Highest()
        recent_highest.Length = self._recent_length.Value
        recent_lowest = Lowest()
        recent_lowest.Length = self._recent_length.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(swing_highest, swing_lowest, recent_highest, recent_lowest, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, swing_highest, swing_lowest, recent_highest, recent_lowest):
        if candle.State != CandleStates.Finished:
            return

        # Swing levels of the candles before this one, and the levels one candle earlier for the cross.
        last_swing_high = self._swing_high
        last_swing_low = self._swing_low
        prev_swing_high = self._prev_swing_high
        prev_swing_low = self._prev_swing_low
        prev_close = self._prev_close

        self._prev_swing_high = self._swing_high
        self._prev_swing_low = self._swing_low
        self._swing_high = swing_highest
        self._swing_low = swing_lowest
        self._prev_close = candle.ClosePrice

        if self._manage_position(candle):
            return

        if last_swing_high is None or last_swing_low is None or prev_swing_high is None or prev_swing_low is None or prev_close is None:
            return

        if not self.IsFormedAndOnlineAndAllowTrading() or self.Position != 0:
            return

        close = candle.ClosePrice
        factor = Decimal(self._take_profit_factor.Value)

        if candle.HighPrice > last_swing_high and prev_close <= prev_swing_low and close > last_swing_low and recent_lowest < close:
            self.BuyMarket(self.Volume)
            self._stop_price = recent_lowest
            self._take_price = close + (close - recent_lowest) * factor
        elif candle.LowPrice < last_swing_low and prev_close >= prev_swing_high and close < last_swing_high and recent_highest > close:
            self.SellMarket(self.Volume)
            self._stop_price = recent_highest
            self._take_price = close - (recent_highest - close) * factor

    def _manage_position(self, candle):
        if self.Position > 0 and self._stop_price > 0:
            if candle.LowPrice <= self._stop_price or candle.HighPrice >= self._take_price:
                self.SellMarket(self.Position)
                return True
        elif self.Position < 0 and self._stop_price > 0:
            if candle.HighPrice >= self._stop_price or candle.LowPrice <= self._take_price:
                self.BuyMarket(abs(self.Position))
                return True
        return False

    def CreateClone(self):
        return gold_scalping_bos_choch_strategy()
