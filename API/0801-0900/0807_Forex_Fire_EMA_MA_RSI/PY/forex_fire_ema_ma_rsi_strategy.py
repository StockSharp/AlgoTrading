import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage, SimpleMovingAverage, WeightedMovingAverage, SmoothedMovingAverage, RelativeStrengthIndex, AverageTrueRange
from StockSharp.Algo.Strategies import Strategy

MA_SIMPLE = 0
MA_EXPONENTIAL = 1
MA_WEIGHTED = 2
MA_SMOOTHED = 3

RSI_MIDDLE = 50.0

class forex_fire_ema_ma_rsi_strategy(Strategy):
    """
    Forex Fire EMA MA RSI strategy.
    On entry candles it goes long when the short EMA is above the long EMA, the close is above the moving average, the fast RSI is
    above the slow RSI and above 50, volume rose against the previous candle and on the confluence timeframe the short EMA is above the
    long EMA; the short entry mirrors every condition. An opposite entry reverses the position. A long closes when the short EMA drops
    below the long EMA or the fast RSI reaches RsiOverbought, a short when the short EMA rises above the long EMA or the fast RSI
    reaches RsiOversold. Optional percent stop loss, take profit and trailing stop, and an ATR exit AtrMultiplier ATRs against the
    entry, close the position as well.
    """

    def __init__(self):
        super(forex_fire_ema_ma_rsi_strategy, self).__init__()
        self._ema_short_length = self.Param("EmaShortLength", 13).SetGreaterThanZero().SetDisplay("EMA Short", "Short EMA length", "Indicators")
        self._ema_long_length = self.Param("EmaLongLength", 62).SetGreaterThanZero().SetDisplay("EMA Long", "Long EMA length", "Indicators")
        self._ma_length = self.Param("MaLength", 200).SetGreaterThanZero().SetDisplay("MA Length", "Trend moving average length", "Indicators")
        self._ma_type = self.Param("MaType", MA_SIMPLE).SetDisplay("MA Type", "Trend moving average type (0 Simple, 1 Exponential, 2 Weighted, 3 Smoothed)", "Indicators")
        self._rsi_slow_length = self.Param("RsiSlowLength", 28).SetGreaterThanZero().SetDisplay("RSI Slow", "Slow RSI length", "Indicators")
        self._rsi_fast_length = self.Param("RsiFastLength", 7).SetGreaterThanZero().SetDisplay("RSI Fast", "Fast RSI length", "Indicators")
        self._rsi_overbought = self.Param("RsiOverbought", 70.0).SetDisplay("RSI Overbought", "Fast RSI level that closes a long", "Indicators")
        self._rsi_oversold = self.Param("RsiOversold", 30.0).SetDisplay("RSI Oversold", "Fast RSI level that closes a short", "Indicators")
        self._use_stop_loss = self.Param("UseStopLoss", True).SetDisplay("Use Stop Loss", "Use the percent stop loss", "Risk")
        self._stop_loss_percent = self.Param("StopLossPercent", 2.0).SetNotNegative().SetDisplay("Stop Loss %", "Stop loss percentage from entry price", "Risk")
        self._use_take_profit = self.Param("UseTakeProfit", True).SetDisplay("Use Take Profit", "Use the percent take profit", "Risk")
        self._take_profit_percent = self.Param("TakeProfitPercent", 4.0).SetNotNegative().SetDisplay("Take Profit %", "Take profit percentage from entry price", "Risk")
        self._use_trailing_stop = self.Param("UseTrailingStop", True).SetDisplay("Use Trailing Stop", "Use the percent trailing stop", "Risk")
        self._trailing_percent = self.Param("TrailingPercent", 1.5).SetNotNegative().SetDisplay("Trailing %", "Trailing stop percentage from the best price", "Risk")
        self._use_atr_exits = self.Param("UseAtrExits", True).SetDisplay("Use ATR Exits", "Use the ATR exit", "Risk")
        self._atr_multiplier = self.Param("AtrMultiplier", 2.0).SetNotNegative().SetDisplay("ATR Multiplier", "ATR multiplier of the exit distance", "Risk")
        self._atr_length = self.Param("AtrLength", 14).SetGreaterThanZero().SetDisplay("ATR Length", "ATR length", "Risk")
        self._entry_candle_type = self.Param("EntryCandleType", DataType.TimeFrame(TimeSpan.FromMinutes(15))).SetDisplay("Entry Candles", "Candles for entries", "General")
        self._confluence_candle_type = self.Param("ConfluenceCandleType", DataType.TimeFrame(TimeSpan.FromHours(4))).SetDisplay("Confluence Candles", "Higher timeframe candles for confluence", "General")
        self._reset_state()

    def _reset_state(self):
        self._htf_trend = 0
        self._prev_volume = None
        self._entry_price = 0.0
        self._best_price = 0.0

    def GetWorkingSecurities(self):
        return [(self.Security, self._entry_candle_type.Value), (self.Security, self._confluence_candle_type.Value)]

    def OnReseted(self):
        super(forex_fire_ema_ma_rsi_strategy, self).OnReseted()
        self._reset_state()

    def OnStarted2(self, time):
        super(forex_fire_ema_ma_rsi_strategy, self).OnStarted2(time)

        self._reset_state()

        ema_short = ExponentialMovingAverage()
        ema_short.Length = self._ema_short_length.Value
        ema_long = ExponentialMovingAverage()
        ema_long.Length = self._ema_long_length.Value
        ma = self._create_ma(int(self._ma_type.Value), self._ma_length.Value)
        rsi_fast = RelativeStrengthIndex()
        rsi_fast.Length = self._rsi_fast_length.Value
        rsi_slow = RelativeStrengthIndex()
        rsi_slow.Length = self._rsi_slow_length.Value
        atr = AverageTrueRange()
        atr.Length = self._atr_length.Value

        htf_ema_short = ExponentialMovingAverage()
        htf_ema_short.Length = self._ema_short_length.Value
        htf_ema_long = ExponentialMovingAverage()
        htf_ema_long.Length = self._ema_long_length.Value

        self.SubscribeCandles(self._confluence_candle_type.Value).Bind(htf_ema_short, htf_ema_long, self._process_confluence).Start()

        subscription = self.SubscribeCandles(self._entry_candle_type.Value)
        subscription.Bind(ema_short, ema_long, ma, rsi_fast, rsi_slow, atr, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, ema_short)
            self.DrawIndicator(area, ema_long)
            self.DrawIndicator(area, ma)
            self.DrawOwnTrades(area)

    def _create_ma(self, ma_type, length):
        if ma_type == MA_EXPONENTIAL:
            ma = ExponentialMovingAverage()
        elif ma_type == MA_WEIGHTED:
            ma = WeightedMovingAverage()
        elif ma_type == MA_SMOOTHED:
            ma = SmoothedMovingAverage()
        else:
            ma = SimpleMovingAverage()
        ma.Length = length
        return ma

    def _process_confluence(self, candle, ema_short_value, ema_long_value):
        if candle.State != CandleStates.Finished:
            return

        ema_short = float(ema_short_value)
        ema_long = float(ema_long_value)
        self._htf_trend = 1 if ema_short > ema_long else (-1 if ema_short < ema_long else 0)

    def _process_candle(self, candle, ema_short_value, ema_long_value, ma_value, rsi_fast_value, rsi_slow_value, atr_value):
        if candle.State != CandleStates.Finished:
            return

        last_volume = self._prev_volume
        volume = float(candle.TotalVolume)
        self._prev_volume = volume

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        ema_short = float(ema_short_value)
        ema_long = float(ema_long_value)
        ma = float(ma_value)
        rsi_fast = float(rsi_fast_value)
        rsi_slow = float(rsi_slow_value)
        atr = float(atr_value)
        close = float(candle.ClosePrice)

        if self.Position > 0:
            self._best_price = max(self._best_price, float(candle.HighPrice))
            if ema_short < ema_long or rsi_fast >= float(self._rsi_overbought.Value) or self._long_stop_hit(candle, atr):
                self.SellMarket(self.Position)
                return
        elif self.Position < 0:
            self._best_price = float(candle.LowPrice) if self._best_price == 0 else min(self._best_price, float(candle.LowPrice))
            if ema_short > ema_long or rsi_fast <= float(self._rsi_oversold.Value) or self._short_stop_hit(candle, atr):
                self.BuyMarket(-self.Position)
                return

        if last_volume is None:
            return

        volume_rising = volume > last_volume

        long_signal = ema_short > ema_long and close > ma and rsi_fast > rsi_slow and rsi_fast > RSI_MIDDLE and volume_rising and self._htf_trend > 0
        short_signal = ema_short < ema_long and close < ma and rsi_fast < rsi_slow and rsi_fast < RSI_MIDDLE and volume_rising and self._htf_trend < 0

        if long_signal and self.Position <= 0:
            self.BuyMarket(self.Volume + abs(self.Position))
            self._entry_price = close
            self._best_price = close
        elif short_signal and self.Position >= 0:
            self.SellMarket(self.Volume + abs(self.Position))
            self._entry_price = close
            self._best_price = close

    def _long_stop_hit(self, candle, atr):
        if self._entry_price <= 0:
            return False

        low = float(candle.LowPrice)
        high = float(candle.HighPrice)
        sl = float(self._stop_loss_percent.Value)
        tp = float(self._take_profit_percent.Value)
        trail = float(self._trailing_percent.Value)
        mult = float(self._atr_multiplier.Value)

        if self._use_stop_loss.Value and sl > 0 and low <= self._entry_price * (1.0 - sl / 100.0):
            return True
        if self._use_take_profit.Value and tp > 0 and high >= self._entry_price * (1.0 + tp / 100.0):
            return True
        if self._use_trailing_stop.Value and trail > 0 and low <= self._best_price * (1.0 - trail / 100.0):
            return True
        return self._use_atr_exits.Value and mult > 0 and float(candle.ClosePrice) <= self._entry_price - mult * atr

    def _short_stop_hit(self, candle, atr):
        if self._entry_price <= 0:
            return False

        low = float(candle.LowPrice)
        high = float(candle.HighPrice)
        sl = float(self._stop_loss_percent.Value)
        tp = float(self._take_profit_percent.Value)
        trail = float(self._trailing_percent.Value)
        mult = float(self._atr_multiplier.Value)

        if self._use_stop_loss.Value and sl > 0 and high >= self._entry_price * (1.0 + sl / 100.0):
            return True
        if self._use_take_profit.Value and tp > 0 and low <= self._entry_price * (1.0 - tp / 100.0):
            return True
        if self._use_trailing_stop.Value and trail > 0 and high >= self._best_price * (1.0 + trail / 100.0):
            return True
        return self._use_atr_exits.Value and mult > 0 and float(candle.ClosePrice) >= self._entry_price + mult * atr

    def CreateClone(self):
        return forex_fire_ema_ma_rsi_strategy()
