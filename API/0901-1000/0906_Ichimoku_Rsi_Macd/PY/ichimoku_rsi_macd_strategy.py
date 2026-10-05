import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Decimal
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import Ichimoku, RelativeStrengthIndex, MovingAverageConvergenceDivergenceSignal
from StockSharp.Algo.Strategies import Strategy

class ichimoku_rsi_macd_strategy(Strategy):
    """
    Ichimoku RSI MACD strategy.
    A long opens when the close is above the Ichimoku cloud, RSI is below RsiOverbought and the MACD line crosses above its signal line;
    a short opens when the close is below the cloud, RSI is above RsiOversold and MACD crosses below its signal line.
    The opposite MACD crossover closes the position (or reverses it when the opposite entry conditions also hold). There are no stops.
    """

    def __init__(self):
        super(ichimoku_rsi_macd_strategy, self).__init__()
        self._tenkan_period = self.Param("TenkanPeriod", 9).SetGreaterThanZero().SetDisplay("Tenkan Period", "Tenkan-sen period", "Ichimoku")
        self._kijun_period = self.Param("KijunPeriod", 26).SetGreaterThanZero().SetDisplay("Kijun Period", "Kijun-sen period", "Ichimoku")
        self._senkou_span_b_period = self.Param("SenkouSpanBPeriod", 52).SetGreaterThanZero().SetDisplay("Senkou Span B Period", "Senkou Span B period", "Ichimoku")
        self._rsi_length = self.Param("RsiLength", 14).SetGreaterThanZero().SetDisplay("RSI Length", "RSI period", "RSI")
        self._rsi_overbought = self.Param("RsiOverbought", 70.0).SetDisplay("RSI Overbought", "RSI level above which longs are not opened", "RSI")
        self._rsi_oversold = self.Param("RsiOversold", 30.0).SetDisplay("RSI Oversold", "RSI level below which shorts are not opened", "RSI")
        self._macd_fast = self.Param("MacdFast", 12).SetGreaterThanZero().SetDisplay("MACD Fast", "Fast EMA period of MACD", "MACD")
        self._macd_slow = self.Param("MacdSlow", 26).SetGreaterThanZero().SetDisplay("MACD Slow", "Slow EMA period of MACD", "MACD")
        self._macd_signal = self.Param("MacdSignal", 9).SetGreaterThanZero().SetDisplay("MACD Signal", "Signal line period of MACD", "MACD")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(1))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._reset_state()

    @property
    def candle_type(self):
        return self._candle_type.Value

    def _reset_state(self):
        self._prev_macd = None
        self._prev_signal = None

    def OnReseted(self):
        super(ichimoku_rsi_macd_strategy, self).OnReseted()
        self._reset_state()

    def OnStarted2(self, time):
        super(ichimoku_rsi_macd_strategy, self).OnStarted2(time)

        self._reset_state()

        ichimoku = Ichimoku()
        ichimoku.Tenkan.Length = self._tenkan_period.Value
        ichimoku.Kijun.Length = self._kijun_period.Value
        ichimoku.SenkouB.Length = self._senkou_span_b_period.Value
        rsi = RelativeStrengthIndex()
        rsi.Length = self._rsi_length.Value
        macd = MovingAverageConvergenceDivergenceSignal()
        macd.Macd.ShortMa.Length = self._macd_fast.Value
        macd.Macd.LongMa.Length = self._macd_slow.Value
        macd.SignalMa.Length = self._macd_signal.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(ichimoku, rsi, macd, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, ichimoku)
            self.DrawOwnTrades(area)
            oscillators = self.CreateChartArea()
            if oscillators is not None:
                self.DrawIndicator(oscillators, rsi)
                self.DrawIndicator(oscillators, macd)

    def _process_candle(self, candle, ichimoku_value, rsi_value, macd_value):
        if candle.State != CandleStates.Finished:
            return

        if not macd_value.IsFormed or macd_value.Macd is None or macd_value.Signal is None:
            return

        macd = macd_value.Macd
        signal = macd_value.Signal
        prev_macd = self._prev_macd
        prev_signal = self._prev_signal
        self._prev_macd = macd
        self._prev_signal = signal

        if prev_macd is None or prev_signal is None:
            return

        if not ichimoku_value.IsFormed or ichimoku_value.SenkouA is None or ichimoku_value.SenkouB is None:
            return

        if not rsi_value.IsFormed:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        rsi = rsi_value.GetValue[Decimal](None)
        close = candle.ClosePrice
        senkou_a = ichimoku_value.SenkouA
        senkou_b = ichimoku_value.SenkouB
        cloud_top = max(senkou_a, senkou_b)
        cloud_bottom = min(senkou_a, senkou_b)

        cross_up = prev_macd <= prev_signal and macd > signal
        cross_down = prev_macd >= prev_signal and macd < signal

        if cross_up:
            if close > cloud_top and rsi < Decimal(self._rsi_overbought.Value) and self.Position <= 0:
                self.BuyMarket(self.Volume + abs(self.Position))
            elif self.Position < 0:
                self.BuyMarket(-self.Position)
        elif cross_down:
            if close < cloud_bottom and rsi > Decimal(self._rsi_oversold.Value) and self.Position >= 0:
                self.SellMarket(self.Volume + abs(self.Position))
            elif self.Position > 0:
                self.SellMarket(self.Position)

    def CreateClone(self):
        return ichimoku_rsi_macd_strategy()
