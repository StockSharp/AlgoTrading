import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")
clr.AddReference("StockSharp.BusinessEntities")

from System import TimeSpan, Decimal
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import (ExponentialMovingAverage, SimpleMovingAverage, AverageTrueRange,
                                        MovingAverageConvergenceDivergenceSignal, CandleIndicatorValue)
from StockSharp.Algo.Strategies import Strategy


class long_ema_advanced_exit_strategy(Strategy):
    """
    Long EMA advanced exit strategy.
    Long only: enters when the short MA crosses above (or, with the Above condition, is above) the medium MA while price is above the long MA.
    Exits when the MACD on the higher MacdCandleType timeframe crosses below its signal line, when price closes below the MaCloseExitPeriod MA,
    when the short MA crosses below the medium MA, when a candle range exceeds AtrMultiplier ATRs, or on a percent trailing stop; each exit can be switched off.
    MaType: EMA or SMA. EntryConditionType: Crossover or Above.
    """

    def __init__(self):
        super(long_ema_advanced_exit_strategy, self).__init__()
        self._ma_type = self.Param("MaType", "EMA").SetDisplay("MA Type", "Moving average type", "Indicators")
        self._entry_condition_type = self.Param("EntryConditionType", "Crossover").SetDisplay("Entry Condition", "Crossover or Above", "Entry")
        self._long_term_period = self.Param("LongTermPeriod", 200).SetGreaterThanZero().SetDisplay("Long MA", "Long-term MA period", "Indicators")
        self._short_term_period = self.Param("ShortTermPeriod", 5).SetGreaterThanZero().SetDisplay("Short MA", "Short-term MA period", "Indicators")
        self._mid_term_period = self.Param("MidTermPeriod", 10).SetGreaterThanZero().SetDisplay("Medium MA", "Medium-term MA period", "Indicators")
        self._enable_macd_exit = self.Param("EnableMacdExit", True).SetDisplay("MACD Exit", "Exit on a MACD cross down", "Exit")
        self._macd_candle_type = self.Param("MacdCandleType", DataType.TimeFrame(TimeSpan.FromDays(7))).SetDisplay("MACD Candle Type", "Candle type of the MACD exit", "Exit")
        self._macd_fast_length = self.Param("MacdFastLength", 12).SetGreaterThanZero().SetDisplay("MACD Fast", "MACD fast length", "Exit")
        self._macd_slow_length = self.Param("MacdSlowLength", 26).SetGreaterThanZero().SetDisplay("MACD Slow", "MACD slow length", "Exit")
        self._macd_signal_length = self.Param("MacdSignalLength", 9).SetGreaterThanZero().SetDisplay("MACD Signal", "MACD signal length", "Exit")
        self._use_trailing_stop = self.Param("UseTrailingStop", False).SetDisplay("Use Trailing Stop", "Use the percent trailing stop", "Risk")
        self._trailing_stop_percent = self.Param("TrailingStopPercent", 15.0).SetNotNegative().SetDisplay("Trailing Stop %", "Trailing stop percent", "Risk")
        self._use_ma_close_exit = self.Param("UseMaCloseExit", False).SetDisplay("MA Close Exit", "Exit when price closes below the exit MA", "Exit")
        self._ma_close_exit_period = self.Param("MaCloseExitPeriod", 50).SetGreaterThanZero().SetDisplay("Exit MA Period", "Exit MA period", "Exit")
        self._use_ma_cross_exit = self.Param("UseMaCrossExit", True).SetDisplay("MA Cross Exit", "Exit when the short MA crosses below the medium MA", "Exit")
        self._use_volatility_filter = self.Param("UseVolatilityFilter", False).SetDisplay("Volatility Exit", "Exit when a candle range exceeds the ATR threshold", "Exit")
        self._atr_period = self.Param("AtrPeriod", 14).SetGreaterThanZero().SetDisplay("ATR Period", "ATR period", "Exit")
        self._atr_multiplier = self.Param("AtrMultiplier", 1.5).SetNotNegative().SetDisplay("ATR Multiplier", "ATR multiplier of the volatility exit", "Exit")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._macd = None
        self._reset_state()

    @property
    def candle_type(self):
        return self._candle_type.Value

    def GetWorkingSecurities(self):
        return [(self.Security, self.candle_type), (self.Security, self._macd_candle_type.Value)]

    def _reset_state(self):
        self._prev_short = None
        self._prev_mid = None
        self._prev_macd = None
        self._prev_signal = None

    def OnReseted(self):
        super(long_ema_advanced_exit_strategy, self).OnReseted()
        self._reset_state()

    def _create_ma(self, length):
        ma = SimpleMovingAverage() if str(self._ma_type.Value) == "SMA" else ExponentialMovingAverage()
        ma.Length = length
        return ma

    def OnStarted2(self, time):
        super(long_ema_advanced_exit_strategy, self).OnStarted2(time)

        self._reset_state()

        short_ma = self._create_ma(self._short_term_period.Value)
        mid_ma = self._create_ma(self._mid_term_period.Value)
        long_ma = self._create_ma(self._long_term_period.Value)
        exit_ma = self._create_ma(self._ma_close_exit_period.Value)
        atr = AverageTrueRange()
        atr.Length = self._atr_period.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(short_ma, mid_ma, long_ma, exit_ma, atr, self._process_candle).Start()

        self._macd = MovingAverageConvergenceDivergenceSignal()
        self._macd.Macd.ShortMa.Length = self._macd_fast_length.Value
        self._macd.Macd.LongMa.Length = self._macd_slow_length.Value
        self._macd.SignalMa.Length = self._macd_signal_length.Value

        # The weekly MACD needs months of history, so it is processed manually to keep it from blocking entries until it forms.
        self.SubscribeCandles(self._macd_candle_type.Value).Bind(self._process_macd).Start()

        if self._use_trailing_stop.Value and self._trailing_stop_percent.Value > 0:
            self.StartProtection(Unit(), Unit(Decimal(self._trailing_stop_percent.Value), UnitTypes.Percent), isStopTrailing=True, useMarketOrders=True)

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, short_ma)
            self.DrawIndicator(area, mid_ma)
            self.DrawIndicator(area, long_ma)
            self.DrawOwnTrades(area)

    def _process_macd(self, candle):
        if candle.State != CandleStates.Finished:
            return

        macd_value = self._macd.Process(CandleIndicatorValue(self._macd, candle))

        if not macd_value.IsFormed or macd_value.Macd is None or macd_value.Signal is None:
            return

        macd = macd_value.Macd
        signal = macd_value.Signal
        cross_down = self._prev_macd is not None and self._prev_signal is not None and self._prev_macd >= self._prev_signal and macd < signal
        self._prev_macd = macd
        self._prev_signal = signal

        if cross_down and self._enable_macd_exit.Value and self.Position > 0 and self.IsFormedAndOnlineAndAllowTrading():
            self.SellMarket(self.Position)

    def _process_candle(self, candle, short_value, mid_value, long_value, exit_value, atr_value):
        if candle.State != CandleStates.Finished:
            return

        if not short_value.IsFormed or not mid_value.IsFormed:
            return

        short_ma = short_value.GetValue[Decimal](None)
        mid_ma = mid_value.GetValue[Decimal](None)
        prev_short = self._prev_short
        prev_mid = self._prev_mid
        self._prev_short = short_ma
        self._prev_mid = mid_ma

        if prev_short is None or prev_mid is None:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        close = candle.ClosePrice
        cross_up = prev_short <= prev_mid and short_ma > mid_ma
        cross_down = prev_short >= prev_mid and short_ma < mid_ma

        if self.Position > 0:
            exit_signal = (self._use_ma_cross_exit.Value and cross_down) \
                or (self._use_ma_close_exit.Value and exit_value.IsFormed and close < exit_value.GetValue[Decimal](None)) \
                or (self._use_volatility_filter.Value and atr_value.IsFormed
                    and candle.HighPrice - candle.LowPrice > atr_value.GetValue[Decimal](None) * Decimal(self._atr_multiplier.Value))
            if exit_signal:
                self.SellMarket(self.Position)
            return

        if self.Position < 0 or not long_value.IsFormed:
            return

        entry = cross_up if str(self._entry_condition_type.Value) == "Crossover" else short_ma > mid_ma

        if entry and close > long_value.GetValue[Decimal](None):
            self.BuyMarket(self.Volume)

    def CreateClone(self):
        return long_ema_advanced_exit_strategy()
