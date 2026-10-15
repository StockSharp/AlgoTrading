import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")
clr.AddReference("StockSharp.BusinessEntities")

from System import TimeSpan, Decimal
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import (SuperTrend, AverageTrueRange, RelativeStrengthIndex, SimpleMovingAverage,
    ExponentialMovingAverage, WeightedMovingAverage)
from StockSharp.Algo.Strategies import Strategy


class MaTypes:
    """Moving average types of the filter."""
    Simple = 0
    Exponential = 1
    Weighted = 2


class advanced_supertrend_strategy(Strategy):
    """
    Advanced Supertrend strategy.
    A Supertrend flip to bullish opens a long and a flip to bearish opens a short, subject to optional filters: RSI below
    RsiOverbought for longs and above RsiOversold for shorts, the close on the trade side of a moving average, the previous trend
    having lasted at least MinTrendBars bars, and a close beyond the previous candle's high or low. An opposite flip closes the
    position, and optional stop loss and take profit sit SlMultiplier and TpMultiplier ATRs from the entry.
    """

    def __init__(self):
        super(advanced_supertrend_strategy, self).__init__()
        self._atr_length = self.Param("AtrLength", 6).SetGreaterThanZero().SetDisplay("ATR Length", "ATR length of the Supertrend and of the stops", "Supertrend")
        self._multiplier = self.Param("Multiplier", 3.0).SetGreaterThanZero().SetDisplay("Multiplier", "Supertrend ATR multiplier", "Supertrend")
        self._use_rsi_filter = self.Param("UseRsiFilter", False).SetDisplay("Use RSI Filter", "Enable the RSI filter", "Filters")
        self._rsi_length = self.Param("RsiLength", 14).SetGreaterThanZero().SetDisplay("RSI Length", "RSI period", "Filters")
        self._rsi_overbought = self.Param("RsiOverbought", 70.0).SetDisplay("RSI Overbought", "RSI level longs must stay below", "Filters")
        self._rsi_oversold = self.Param("RsiOversold", 30.0).SetDisplay("RSI Oversold", "RSI level shorts must stay above", "Filters")
        self._use_ma_filter = self.Param("UseMaFilter", True).SetDisplay("Use MA Filter", "Enable the moving average filter", "Filters")
        self._ma_length = self.Param("MaLength", 50).SetGreaterThanZero().SetDisplay("MA Length", "Moving average period", "Filters")
        self._ma_type = self.Param("MaType", MaTypes.Weighted).SetDisplay("MA Type", "Moving average type", "Filters")
        self._use_stop_loss = self.Param("UseStopLoss", True).SetDisplay("Use Stop Loss", "Enable the ATR stop loss", "Risk")
        self._sl_multiplier = self.Param("SlMultiplier", 3.0).SetNotNegative().SetDisplay("SL Multiplier", "ATR multiplier of the stop loss", "Risk")
        self._use_take_profit = self.Param("UseTakeProfit", True).SetDisplay("Use Take Profit", "Enable the ATR take profit", "Risk")
        self._tp_multiplier = self.Param("TpMultiplier", 9.0).SetNotNegative().SetDisplay("TP Multiplier", "ATR multiplier of the take profit", "Risk")
        self._use_trend_strength = self.Param("UseTrendStrength", False).SetDisplay("Use Trend Strength", "Enable the trend strength filter", "Filters")
        self._min_trend_bars = self.Param("MinTrendBars", 2).SetGreaterThanZero().SetDisplay("Min Trend Bars", "Bars the previous trend must have lasted", "Filters")
        self._use_breakout_confirmation = self.Param("UseBreakoutConfirmation", True).SetDisplay("Use Breakout Confirmation", "Require a close beyond the previous candle's extreme", "Filters")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(30))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._reset_state()

    @property
    def candle_type(self):
        return self._candle_type.Value

    def _reset_state(self):
        self._prev_up_trend = None
        self._trend_bars = 0
        self._prev_high = None
        self._prev_low = None
        self._stop_price = None
        self._take_price = None

    def OnReseted(self):
        super(advanced_supertrend_strategy, self).OnReseted()
        self._reset_state()

    def OnStarted2(self, time):
        super(advanced_supertrend_strategy, self).OnStarted2(time)

        self._reset_state()

        super_trend = SuperTrend()
        super_trend.Length = self._atr_length.Value
        super_trend.Multiplier = Decimal(self._multiplier.Value)
        atr = AverageTrueRange()
        atr.Length = self._atr_length.Value
        rsi = RelativeStrengthIndex()
        rsi.Length = self._rsi_length.Value
        ma_type = self._ma_type.Value
        if ma_type == MaTypes.Simple:
            ma = SimpleMovingAverage()
        elif ma_type == MaTypes.Exponential:
            ma = ExponentialMovingAverage()
        else:
            ma = WeightedMovingAverage()
        ma.Length = self._ma_length.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(super_trend, atr, rsi, ma, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, super_trend)
            self.DrawIndicator(area, ma)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, super_trend_value, atr_value, rsi_value, ma_value):
        if candle.State != CandleStates.Finished:
            return

        prev_high = self._prev_high
        prev_low = self._prev_low
        self._prev_high = candle.HighPrice
        self._prev_low = candle.LowPrice

        if not super_trend_value.IsFormed or not atr_value.IsFormed or not rsi_value.IsFormed or not ma_value.IsFormed:
            return

        up_trend = super_trend_value.IsUpTrend
        was_up = self._prev_up_trend
        prior_trend_bars = self._trend_bars

        if was_up == up_trend:
            self._trend_bars += 1
        else:
            self._trend_bars = 1

        self._prev_up_trend = up_trend

        if was_up is None or prev_high is None or prev_low is None:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        close = candle.ClosePrice

        if self.Position > 0 and ((self._stop_price is not None and candle.LowPrice <= self._stop_price)
                or (self._take_price is not None and candle.HighPrice >= self._take_price)):
            self.SellMarket(self.Position)
            self._clear_levels()
            return

        if self.Position < 0 and ((self._stop_price is not None and candle.HighPrice >= self._stop_price)
                or (self._take_price is not None and candle.LowPrice <= self._take_price)):
            self.BuyMarket(-self.Position)
            self._clear_levels()
            return

        bullish_flip = up_trend and not was_up
        bearish_flip = not up_trend and was_up

        if not bullish_flip and not bearish_flip:
            return

        rsi = rsi_value.GetValue[Decimal](None)
        ma = ma_value.GetValue[Decimal](None)
        strong_trend = not self._use_trend_strength.Value or prior_trend_bars >= self._min_trend_bars.Value
        use_rsi = self._use_rsi_filter.Value
        use_ma = self._use_ma_filter.Value
        use_breakout = self._use_breakout_confirmation.Value

        long_ok = (bullish_flip and strong_trend
            and (not use_rsi or rsi < Decimal(self._rsi_overbought.Value))
            and (not use_ma or close > ma)
            and (not use_breakout or close > prev_high))

        short_ok = (bearish_flip and strong_trend
            and (not use_rsi or rsi > Decimal(self._rsi_oversold.Value))
            and (not use_ma or close < ma)
            and (not use_breakout or close < prev_low))

        atr = atr_value.GetValue[Decimal](None)

        if long_ok and self.Position <= 0:
            self.BuyMarket(self.Volume + abs(self.Position))
            self._set_levels(close, atr, True)
        elif short_ok and self.Position >= 0:
            self.SellMarket(self.Volume + abs(self.Position))
            self._set_levels(close, atr, False)
        elif bearish_flip and self.Position > 0:
            self.SellMarket(self.Position)
            self._clear_levels()
        elif bullish_flip and self.Position < 0:
            self.BuyMarket(-self.Position)
            self._clear_levels()

    def _set_levels(self, price, atr, is_long):
        stop = atr * Decimal(self._sl_multiplier.Value)
        take = atr * Decimal(self._tp_multiplier.Value)
        self._stop_price = (price - stop if is_long else price + stop) if self._use_stop_loss.Value and stop > 0 else None
        self._take_price = (price + take if is_long else price - take) if self._use_take_profit.Value and take > 0 else None

    def _clear_levels(self):
        self._stop_price = None
        self._take_price = None

    def CreateClone(self):
        return advanced_supertrend_strategy()
