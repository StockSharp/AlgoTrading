import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Decimal, DateTimeOffset
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import AverageDirectionalIndex, ExponentialMovingAverage, RelativeStrengthIndex, AverageTrueRange, Highest, Lowest
from StockSharp.Algo.Strategies import Strategy
from indicator_extensions import *

class hybrid_rsi_breakout_dashboard_strategy(Strategy):
    """
    Hybrid RSI breakout strategy.
    When ADX is at or below AdxThreshold the market is ranging: a long opens when RSI drops below RsiBuy with the close above the
    EmaLength EMA, a short when RSI rises above RsiSell with the close below it, and these RSI trades close when RSI crosses back
    over RsiExit. When ADX is above AdxThreshold the market is trending: a long opens when the close breaks above the highest close of
    the previous BreakoutLength candles and a short when it breaks below the lowest close, and these breakout trades are closed by an
    ATR trailing stop of AtrMultiplier * ATR. Trading starts at StartDate. The last trade type and direction are kept for display.
    """

    def __init__(self):
        super(hybrid_rsi_breakout_dashboard_strategy, self).__init__()
        self._adx_length = self.Param("AdxLength", 14).SetGreaterThanZero().SetDisplay("ADX Length", "ADX period", "Indicators")
        self._adx_threshold = self.Param("AdxThreshold", 20.0).SetDisplay("ADX Threshold", "ADX level above which the market is trending", "Indicators")
        self._ema_length = self.Param("EmaLength", 200).SetGreaterThanZero().SetDisplay("EMA Length", "Trend EMA period", "Indicators")
        self._rsi_length = self.Param("RsiLength", 14).SetGreaterThanZero().SetDisplay("RSI Length", "RSI period", "Indicators")
        self._rsi_buy = self.Param("RsiBuy", 40.0).SetDisplay("RSI Buy", "RSI level below which a ranging long opens", "Signals")
        self._rsi_sell = self.Param("RsiSell", 60.0).SetDisplay("RSI Sell", "RSI level above which a ranging short opens", "Signals")
        self._rsi_exit = self.Param("RsiExit", 50.0).SetDisplay("RSI Exit", "RSI level that closes RSI trades", "Signals")
        self._breakout_length = self.Param("BreakoutLength", 20).SetGreaterThanZero().SetDisplay("Breakout Length", "Previous closes the breakout range spans", "Signals")
        self._atr_length = self.Param("AtrLength", 14).SetGreaterThanZero().SetDisplay("ATR Length", "ATR period", "Risk")
        self._atr_multiplier = self.Param("AtrMultiplier", 2.0).SetGreaterThanZero().SetDisplay("ATR Multiplier", "ATR multiplier of the trailing stop", "Risk")
        self._start_date = self.Param("StartDate", DateTimeOffset(2017, 1, 1, 0, 0, 0, TimeSpan.Zero)).SetDisplay("Start Date", "Date trading starts from", "General")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._highest = None
        self._lowest = None
        self._reset_state()

    @property
    def candle_type(self):
        return self._candle_type.Value

    def _reset_state(self):
        self._prev_highest = None
        self._prev_lowest = None
        self._is_breakout_trade = False
        self._trailing_stop = None
        self.last_trade_type = None
        self.last_trade_direction = None

    def OnReseted(self):
        super(hybrid_rsi_breakout_dashboard_strategy, self).OnReseted()
        self._reset_state()

    def OnStarted2(self, time):
        super(hybrid_rsi_breakout_dashboard_strategy, self).OnStarted2(time)

        self._reset_state()

        adx = AverageDirectionalIndex()
        adx.Length = self._adx_length.Value
        ema = ExponentialMovingAverage()
        ema.Length = self._ema_length.Value
        rsi = RelativeStrengthIndex()
        rsi.Length = self._rsi_length.Value
        atr = AverageTrueRange()
        atr.Length = self._atr_length.Value
        self._highest = Highest()
        self._highest.Length = self._breakout_length.Value
        self._lowest = Lowest()
        self._lowest.Length = self._breakout_length.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(adx, ema, rsi, atr, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, ema)
            self.DrawOwnTrades(area)
            oscillators = self.CreateChartArea()
            if oscillators is not None:
                self.DrawIndicator(oscillators, rsi)
                self.DrawIndicator(oscillators, adx)

    def _process_candle(self, candle, adx_value, ema_value, rsi_value, atr_value):
        if candle.State != CandleStates.Finished:
            return

        close = candle.ClosePrice

        # The breakout range is measured on the closes before this candle.
        range_high = self._prev_highest
        range_low = self._prev_lowest

        highest_value = process_value(self._highest, close, candle.OpenTime, True)
        lowest_value = process_value(self._lowest, close, candle.OpenTime, True)

        if self._highest.IsFormed and self._lowest.IsFormed:
            self._prev_highest = to_decimal(highest_value)
            self._prev_lowest = to_decimal(lowest_value)

        if not adx_value.IsFormed or adx_value.MovingAverage is None:
            return

        if not ema_value.IsFormed or not rsi_value.IsFormed or not atr_value.IsFormed or range_high is None or range_low is None:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        if candle.OpenTime < self._start_date.Value.UtcDateTime:
            return

        adx = adx_value.MovingAverage
        ema = to_decimal(ema_value)
        rsi = to_decimal(rsi_value)
        trail = to_decimal(atr_value) * Decimal(self._atr_multiplier.Value)
        rsi_exit = Decimal(self._rsi_exit.Value)

        # Manage the open trade first.
        if self.Position > 0:
            if self._is_breakout_trade:
                level = close - trail
                self._trailing_stop = level if self._trailing_stop is None else max(self._trailing_stop, level)
                if candle.LowPrice <= self._trailing_stop:
                    self.SellMarket(self.Position)
                    self._trailing_stop = None
                    return
            elif rsi > rsi_exit:
                self.SellMarket(self.Position)
                return
        elif self.Position < 0:
            if self._is_breakout_trade:
                level = close + trail
                self._trailing_stop = level if self._trailing_stop is None else min(self._trailing_stop, level)
                if candle.HighPrice >= self._trailing_stop:
                    self.BuyMarket(-self.Position)
                    self._trailing_stop = None
                    return
            elif rsi < rsi_exit:
                self.BuyMarket(-self.Position)
                return

        trending = adx > Decimal(self._adx_threshold.Value)
        bullish = close > ema

        if trending:
            go_long = close > range_high
            go_short = close < range_low
        else:
            go_long = rsi < Decimal(self._rsi_buy.Value) and bullish
            go_short = rsi > Decimal(self._rsi_sell.Value) and not bullish

        if go_long and self.Position <= 0:
            self.BuyMarket(self.Volume + abs(self.Position))
            self._on_entry(trending, True, close, trail)
        elif go_short and self.Position >= 0:
            self.SellMarket(self.Volume + abs(self.Position))
            self._on_entry(trending, False, close, trail)

    def _on_entry(self, breakout, is_long, close, trail):
        self._is_breakout_trade = breakout
        if breakout:
            self._trailing_stop = close - trail if is_long else close + trail
        else:
            self._trailing_stop = None
        self.last_trade_type = "Breakout" if breakout else "RSI"
        self.last_trade_direction = "Long" if is_long else "Short"

    def CreateClone(self):
        return hybrid_rsi_breakout_dashboard_strategy()
