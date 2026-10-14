import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Decimal
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage, RelativeStrengthIndex, AverageTrueRange
from StockSharp.Algo.Strategies import Strategy


class gold_scalping_with_precise_entries_strategy(Strategy):
    """
    Gold scalping strategy with precise entries.
    The trend is up when the EmaFastPeriod EMA is above the EmaSlowPeriod EMA. With RSI between RsiLower and RsiUpper, a bullish
    engulfing candle that touches the fast EMA in an uptrend goes long and a bearish engulfing candle touching it in a downtrend goes
    short. The stop is one ATR from the entry and the target PipTarget price steps away.
    """

    def __init__(self):
        super(gold_scalping_with_precise_entries_strategy, self).__init__()
        self._ema_fast_period = self.Param("EmaFastPeriod", 50).SetGreaterThanZero().SetDisplay("EMA Fast Period", "Fast EMA period", "Indicators")
        self._ema_slow_period = self.Param("EmaSlowPeriod", 200).SetGreaterThanZero().SetDisplay("EMA Slow Period", "Slow EMA period", "Indicators")
        self._rsi_period = self.Param("RsiPeriod", 14).SetGreaterThanZero().SetDisplay("RSI Period", "RSI period", "Indicators")
        self._atr_period = self.Param("AtrPeriod", 14).SetGreaterThanZero().SetDisplay("ATR Period", "ATR period for the stop", "Risk")
        self._rsi_lower = self.Param("RsiLower", 45.0).SetDisplay("RSI Lower", "Lower bound of the RSI range", "Indicators")
        self._rsi_upper = self.Param("RsiUpper", 55.0).SetDisplay("RSI Upper", "Upper bound of the RSI range", "Indicators")
        self._pip_target = self.Param("PipTarget", 2.0).SetNotNegative().SetDisplay("Pip Target", "Take profit distance in price steps", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(1))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._reset_state()

    @property
    def candle_type(self):
        return self._candle_type.Value

    def _reset_state(self):
        self._prev_open = None
        self._prev_close = None
        self._stop_price = Decimal(0)
        self._take_price = Decimal(0)

    def OnReseted(self):
        super(gold_scalping_with_precise_entries_strategy, self).OnReseted()
        self._reset_state()

    def OnStarted2(self, time):
        super(gold_scalping_with_precise_entries_strategy, self).OnStarted2(time)

        self._reset_state()

        ema_fast = ExponentialMovingAverage()
        ema_fast.Length = self._ema_fast_period.Value
        ema_slow = ExponentialMovingAverage()
        ema_slow.Length = self._ema_slow_period.Value
        rsi = RelativeStrengthIndex()
        rsi.Length = self._rsi_period.Value
        atr = AverageTrueRange()
        atr.Length = self._atr_period.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(ema_fast, ema_slow, rsi, atr, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, ema_fast)
            self.DrawIndicator(area, ema_slow)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, ema_fast, ema_slow, rsi, atr):
        if candle.State != CandleStates.Finished:
            return

        prev_open = self._prev_open
        prev_close = self._prev_close
        self._prev_open = candle.OpenPrice
        self._prev_close = candle.ClosePrice

        if self._manage_position(candle):
            return

        if prev_open is None or prev_close is None:
            return

        if not self.IsFormedAndOnlineAndAllowTrading() or self.Position != 0:
            return

        open_price = candle.OpenPrice
        close = candle.ClosePrice
        rsi_in_range = Decimal(self._rsi_lower.Value) <= rsi <= Decimal(self._rsi_upper.Value)
        touches_ema = candle.LowPrice <= ema_fast and candle.HighPrice >= ema_fast
        bullish_engulfing = prev_close < prev_open and close > open_price and open_price <= prev_close and close >= prev_open
        bearish_engulfing = prev_close > prev_open and close < open_price and open_price >= prev_close and close <= prev_open
        step = self.Security.PriceStep if self.Security is not None and self.Security.PriceStep is not None else Decimal(1)
        target = Decimal(self._pip_target.Value) * step

        if ema_fast > ema_slow and rsi_in_range and touches_ema and bullish_engulfing:
            self.BuyMarket(self.Volume)
            self._stop_price = close - atr
            self._take_price = close + target
        elif ema_fast < ema_slow and rsi_in_range and touches_ema and bearish_engulfing:
            self.SellMarket(self.Volume)
            self._stop_price = close + atr
            self._take_price = close - target

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
        return gold_scalping_with_precise_entries_strategy()
