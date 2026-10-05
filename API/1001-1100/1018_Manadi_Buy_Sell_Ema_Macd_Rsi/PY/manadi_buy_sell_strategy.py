import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Decimal
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import ExponentialMovingAverage, RelativeStrengthIndex, MovingAverageConvergenceDivergenceSignal
from StockSharp.Algo.Strategies import Strategy


class manadi_buy_sell_strategy(Strategy):
    """
    Manadi Buy Sell EMA MACD RSI strategy.
    The fast EMA crossing above the slow EMA with MACD above its signal line and RSI between RsiLowerLong and RsiUpperLong goes long.
    The fast EMA crossing below the slow EMA with MACD below its signal line and RSI between RsiLowerShort and RsiUpperShort goes short.
    An opposite signal reverses the position. TakeProfitPercent and StopLossPercent are fractions of the entry price (0.03 = 3%).
    """

    def __init__(self):
        super(manadi_buy_sell_strategy, self).__init__()
        self._fast_ema_length = self.Param("FastEmaLength", 9).SetGreaterThanZero().SetDisplay("Fast EMA", "Fast EMA length", "Indicators")
        self._slow_ema_length = self.Param("SlowEmaLength", 21).SetGreaterThanZero().SetDisplay("Slow EMA", "Slow EMA length", "Indicators")
        self._rsi_length = self.Param("RsiLength", 14).SetGreaterThanZero().SetDisplay("RSI Length", "RSI length", "Indicators")
        self._rsi_upper_long = self.Param("RsiUpperLong", 70.0).SetDisplay("RSI Upper Long", "Upper RSI bound for longs", "RSI")
        self._rsi_lower_long = self.Param("RsiLowerLong", 40.0).SetDisplay("RSI Lower Long", "Lower RSI bound for longs", "RSI")
        self._rsi_upper_short = self.Param("RsiUpperShort", 60.0).SetDisplay("RSI Upper Short", "Upper RSI bound for shorts", "RSI")
        self._rsi_lower_short = self.Param("RsiLowerShort", 30.0).SetDisplay("RSI Lower Short", "Lower RSI bound for shorts", "RSI")
        self._macd_fast = self.Param("MacdFast", 12).SetGreaterThanZero().SetDisplay("MACD Fast", "MACD fast EMA length", "MACD")
        self._macd_slow = self.Param("MacdSlow", 26).SetGreaterThanZero().SetDisplay("MACD Slow", "MACD slow EMA length", "MACD")
        self._macd_signal = self.Param("MacdSignal", 9).SetGreaterThanZero().SetDisplay("MACD Signal", "MACD signal line length", "MACD")
        self._take_profit_percent = self.Param("TakeProfitPercent", 0.03).SetNotNegative().SetDisplay("Take Profit", "Take profit as a fraction of the entry price", "Risk")
        self._stop_loss_percent = self.Param("StopLossPercent", 0.015).SetNotNegative().SetDisplay("Stop Loss", "Stop loss as a fraction of the entry price", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(1))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._reset_state()

    @property
    def candle_type(self):
        return self._candle_type.Value

    def _reset_state(self):
        self._prev_fast = None
        self._prev_slow = None

    def OnReseted(self):
        super(manadi_buy_sell_strategy, self).OnReseted()
        self._reset_state()

    def OnStarted2(self, time):
        super(manadi_buy_sell_strategy, self).OnStarted2(time)

        self._reset_state()

        fast_ema = ExponentialMovingAverage()
        fast_ema.Length = self._fast_ema_length.Value
        slow_ema = ExponentialMovingAverage()
        slow_ema.Length = self._slow_ema_length.Value
        rsi = RelativeStrengthIndex()
        rsi.Length = self._rsi_length.Value
        macd = MovingAverageConvergenceDivergenceSignal()
        macd.Macd.ShortMa.Length = self._macd_fast.Value
        macd.Macd.LongMa.Length = self._macd_slow.Value
        macd.SignalMa.Length = self._macd_signal.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(fast_ema, slow_ema, rsi, macd, self._process_candle).Start()

        # The parameters are fractions, so 0.03 is a 3% distance.
        hundred = Decimal(100)
        self.StartProtection(
            Unit(Decimal(self._take_profit_percent.Value) * hundred, UnitTypes.Percent),
            Unit(Decimal(self._stop_loss_percent.Value) * hundred, UnitTypes.Percent),
            useMarketOrders=True)

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, fast_ema)
            self.DrawIndicator(area, slow_ema)
            self.DrawOwnTrades(area)
            oscillators = self.CreateChartArea()
            if oscillators is not None:
                self.DrawIndicator(oscillators, rsi)
                self.DrawIndicator(oscillators, macd)

    def _process_candle(self, candle, fast_value, slow_value, rsi_value, macd_value):
        if candle.State != CandleStates.Finished:
            return

        if not fast_value.IsFormed or not slow_value.IsFormed or not rsi_value.IsFormed or not macd_value.IsFormed:
            return

        macd_line = macd_value.Macd
        signal_line = macd_value.Signal
        if macd_line is None or signal_line is None:
            return

        fast = fast_value.GetValue[Decimal](None)
        slow = slow_value.GetValue[Decimal](None)

        pf = self._prev_fast
        ps = self._prev_slow
        self._prev_fast = fast
        self._prev_slow = slow

        if pf is None or ps is None:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        rsi = rsi_value.GetValue[Decimal](None)

        long_signal = (pf <= ps and fast > slow and macd_line > signal_line
                       and rsi > Decimal(self._rsi_lower_long.Value) and rsi < Decimal(self._rsi_upper_long.Value))
        short_signal = (pf >= ps and fast < slow and macd_line < signal_line
                        and rsi > Decimal(self._rsi_lower_short.Value) and rsi < Decimal(self._rsi_upper_short.Value))

        if long_signal and self.Position <= 0:
            self.BuyMarket(self.Volume + abs(self.Position))
        elif short_signal and self.Position >= 0:
            self.SellMarket(self.Volume + abs(self.Position))

    def CreateClone(self):
        return manadi_buy_sell_strategy()
