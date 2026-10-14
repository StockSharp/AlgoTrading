import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Decimal
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage, AverageTrueRange, RelativeStrengthIndex, SuperTrend
from StockSharp.Algo.Strategies import Strategy


class daily_supertrend_ema_crossover_rsi_filter_strategy(Strategy):
    """
    Supertrend EMA crossover strategy with RSI filter.
    A fast EMA crossing above the slow EMA goes long when Supertrend is up and RSI is below RsiOverbought;
    a cross below goes short when Supertrend is down and RSI is above RsiOversold. An opposite signal reverses the position.
    Stop loss and take profit sit at ATR multiples from the entry close, measured with the ATR of the signal bar.
    """

    def __init__(self):
        super(daily_supertrend_ema_crossover_rsi_filter_strategy, self).__init__()
        self._fast_ema_length = self.Param("FastEmaLength", 3).SetGreaterThanZero().SetDisplay("Fast EMA", "Fast EMA length", "Indicators")
        self._slow_ema_length = self.Param("SlowEmaLength", 6).SetGreaterThanZero().SetDisplay("Slow EMA", "Slow EMA length", "Indicators")
        self._atr_length = self.Param("AtrLength", 3).SetGreaterThanZero().SetDisplay("ATR Length", "ATR length used by the stops and by Supertrend", "Indicators")
        self._stop_loss_multiplier = self.Param("StopLossMultiplier", 2.5).SetNotNegative().SetDisplay("Stop Loss ATR", "Stop loss distance in ATR", "Risk")
        self._take_profit_multiplier = self.Param("TakeProfitMultiplier", 4.0).SetNotNegative().SetDisplay("Take Profit ATR", "Take profit distance in ATR", "Risk")
        self._rsi_length = self.Param("RsiLength", 10).SetGreaterThanZero().SetDisplay("RSI Length", "RSI length", "Indicators")
        self._rsi_overbought = self.Param("RsiOverbought", 65.0).SetDisplay("RSI Overbought", "RSI level that blocks longs", "Indicators")
        self._rsi_oversold = self.Param("RsiOversold", 30.0).SetDisplay("RSI Oversold", "RSI level that blocks shorts", "Indicators")
        self._supertrend_multiplier = self.Param("SupertrendMultiplier", 1.0).SetGreaterThanZero().SetDisplay("Supertrend Multiplier", "ATR multiplier of Supertrend", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._reset_state()

    @property
    def candle_type(self):
        return self._candle_type.Value

    def GetWorkingSecurities(self):
        return [(self.Security, self.candle_type)]

    def _reset_state(self):
        self._prev_fast = None
        self._prev_slow = None
        self._stop_price = None
        self._take_price = None

    def OnReseted(self):
        super(daily_supertrend_ema_crossover_rsi_filter_strategy, self).OnReseted()
        self._reset_state()

    def OnStarted2(self, time):
        super(daily_supertrend_ema_crossover_rsi_filter_strategy, self).OnStarted2(time)

        self._reset_state()

        fast_ema = ExponentialMovingAverage()
        fast_ema.Length = self._fast_ema_length.Value
        slow_ema = ExponentialMovingAverage()
        slow_ema.Length = self._slow_ema_length.Value
        atr = AverageTrueRange()
        atr.Length = self._atr_length.Value
        rsi = RelativeStrengthIndex()
        rsi.Length = self._rsi_length.Value
        supertrend = SuperTrend()
        supertrend.Length = self._atr_length.Value
        supertrend.Multiplier = Decimal(self._supertrend_multiplier.Value)

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(fast_ema, slow_ema, atr, rsi, supertrend, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, fast_ema)
            self.DrawIndicator(area, slow_ema)
            self.DrawIndicator(area, supertrend)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, fast_value, slow_value, atr_value, rsi_value, supertrend_value):
        if candle.State != CandleStates.Finished:
            return

        if not fast_value.IsFormed or not slow_value.IsFormed or not atr_value.IsFormed or not rsi_value.IsFormed or not supertrend_value.IsFormed:
            return

        fast = float(fast_value.GetValue[Decimal](None))
        slow = float(slow_value.GetValue[Decimal](None))
        prev_fast = self._prev_fast
        prev_slow = self._prev_slow
        self._prev_fast = fast
        self._prev_slow = slow

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        high = float(candle.HighPrice)
        low = float(candle.LowPrice)

        if self.Position > 0:
            if (self._stop_price is not None and low <= self._stop_price) or (self._take_price is not None and high >= self._take_price):
                self.SellMarket(self.Position)
                self._stop_price = None
                self._take_price = None
                return
        elif self.Position < 0:
            if (self._stop_price is not None and high >= self._stop_price) or (self._take_price is not None and low <= self._take_price):
                self.BuyMarket(-self.Position)
                self._stop_price = None
                self._take_price = None
                return

        if prev_fast is None or prev_slow is None:
            return

        is_up_trend = supertrend_value.IsUpTrend
        rsi = float(rsi_value.GetValue[Decimal](None))
        atr = float(atr_value.GetValue[Decimal](None))
        close = float(candle.ClosePrice)
        sl_mult = float(self._stop_loss_multiplier.Value)
        tp_mult = float(self._take_profit_multiplier.Value)

        cross_up = prev_fast <= prev_slow and fast > slow
        cross_down = prev_fast >= prev_slow and fast < slow

        if cross_up and is_up_trend and rsi < float(self._rsi_overbought.Value) and self.Position <= 0:
            self.BuyMarket(self.Volume + abs(self.Position))
            self._stop_price = close - atr * sl_mult if sl_mult > 0 else None
            self._take_price = close + atr * tp_mult if tp_mult > 0 else None
        elif cross_down and not is_up_trend and rsi > float(self._rsi_oversold.Value) and self.Position >= 0:
            self.SellMarket(self.Volume + abs(self.Position))
            self._stop_price = close + atr * sl_mult if sl_mult > 0 else None
            self._take_price = close - atr * tp_mult if tp_mult > 0 else None

    def CreateClone(self):
        return daily_supertrend_ema_crossover_rsi_filter_strategy()
