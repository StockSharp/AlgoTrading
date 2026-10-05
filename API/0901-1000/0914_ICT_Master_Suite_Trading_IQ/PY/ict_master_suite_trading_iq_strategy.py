import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Decimal
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import AverageTrueRange
from StockSharp.Algo.Strategies import Strategy


class ict_master_suite_trading_iq_strategy(Strategy):
    """
    ICT Master Suite session breakout strategy.
    The session is the UTC day. A close above the high of the session's earlier candles goes long and a close below their low goes short,
    reversing an opposite position. An open position is protected by a trailing stop AtrMultiplier ATRs away from the close that only moves
    in the trade's favour; the position closes when a candle touches it.
    """

    def __init__(self):
        super(ict_master_suite_trading_iq_strategy, self).__init__()
        self._atr_period = self.Param("AtrPeriod", 14).SetGreaterThanZero().SetDisplay("ATR Period", "ATR calculation period", "Risk Management")
        self._atr_multiplier = self.Param("AtrMultiplier", 1.5).SetGreaterThanZero().SetDisplay("ATR Multiplier", "ATR multiplier for the trailing stop", "Risk Management")
        self._allow_long = self.Param("AllowLong", True).SetDisplay("Allow Long", "Enable long trades", "General")
        self._allow_short = self.Param("AllowShort", True).SetDisplay("Allow Short", "Enable short trades", "General")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))).SetDisplay("Candle Type", "Type of candles", "General")
        self._reset_state()

    @property
    def candle_type(self):
        return self._candle_type.Value

    def _reset_state(self):
        self._session_date = None
        self._session_high = None
        self._session_low = None
        self._stop_price = None

    def OnReseted(self):
        super(ict_master_suite_trading_iq_strategy, self).OnReseted()
        self._reset_state()

    def OnStarted2(self, time):
        super(ict_master_suite_trading_iq_strategy, self).OnStarted2(time)

        self._reset_state()

        atr = AverageTrueRange()
        atr.Length = self._atr_period.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(atr, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, atr)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, atr_value):
        if candle.State != CandleStates.Finished:
            return

        date = candle.OpenTime.Date

        # The first candle of a session only opens the range; breakouts need earlier candles of the same session.
        if self._session_date is None or self._session_date != date:
            self._session_date = date
            self._session_high = candle.HighPrice
            self._session_low = candle.LowPrice
            self._manage_stop(candle, atr_value)
            return

        session_high = self._session_high
        session_low = self._session_low

        self._session_high = max(self._session_high, candle.HighPrice)
        self._session_low = min(self._session_low, candle.LowPrice)

        if self._manage_stop(candle, atr_value):
            return

        if not atr_value.IsFormed or not self.IsFormedAndOnlineAndAllowTrading():
            return

        atr = atr_value.GetValue[Decimal](None)
        multiplier = Decimal(self._atr_multiplier.Value)
        close = candle.ClosePrice

        if self._allow_long.Value and close > session_high and self.Position <= 0:
            self.BuyMarket(self.Volume + abs(self.Position))
            self._stop_price = close - atr * multiplier
        elif self._allow_short.Value and close < session_low and self.Position >= 0:
            self.SellMarket(self.Volume + abs(self.Position))
            self._stop_price = close + atr * multiplier

    # Returns True when the position was closed by the trailing stop on this candle.
    def _manage_stop(self, candle, atr_value):
        if self.Position == 0:
            self._stop_price = None
            return False

        stop = self._stop_price
        if stop is None:
            return False

        if self.Position > 0 and candle.LowPrice <= stop:
            self.SellMarket(self.Position)
            self._stop_price = None
            return True

        if self.Position < 0 and candle.HighPrice >= stop:
            self.BuyMarket(-self.Position)
            self._stop_price = None
            return True

        if atr_value.IsFormed:
            distance = atr_value.GetValue[Decimal](None) * Decimal(self._atr_multiplier.Value)
            if self.Position > 0:
                self._stop_price = max(stop, candle.ClosePrice - distance)
            else:
                self._stop_price = min(stop, candle.ClosePrice + distance)

        return False

    def CreateClone(self):
        return ict_master_suite_trading_iq_strategy()
