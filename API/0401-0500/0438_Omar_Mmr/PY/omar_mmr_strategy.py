import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Decimal
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import RelativeStrengthIndex, ExponentialMovingAverage, MovingAverageConvergenceDivergenceSignal
from StockSharp.Algo.Strategies import Strategy

RSI_LOWER = 29.0
RSI_UPPER = 70.0


class omar_mmr_strategy(Strategy):
    """
    Omar MMR strategy (long only).
    Buys when the close is above EMA C, EMA A is above EMA B, the MACD line crosses above its signal line and RSI is
    between 29 and 70. Positions are closed only by the percent take-profit and stop-loss.
    """

    def __init__(self):
        super(omar_mmr_strategy, self).__init__()
        self._rsi_length = self.Param("RsiLength", 14) \
            .SetGreaterThanZero() \
            .SetDisplay("RSI Length", "RSI period", "RSI")
        self._ema_a_length = self.Param("EmaALength", 20) \
            .SetGreaterThanZero() \
            .SetDisplay("EMA A Length", "Fast EMA period", "Moving Averages")
        self._ema_b_length = self.Param("EmaBLength", 50) \
            .SetGreaterThanZero() \
            .SetDisplay("EMA B Length", "Medium EMA period", "Moving Averages")
        self._ema_c_length = self.Param("EmaCLength", 200) \
            .SetGreaterThanZero() \
            .SetDisplay("EMA C Length", "Slow EMA period", "Moving Averages")
        self._macd_fast_length = self.Param("MacdFastLength", 12) \
            .SetGreaterThanZero() \
            .SetDisplay("MACD Fast", "MACD fast EMA period", "MACD")
        self._macd_slow_length = self.Param("MacdSlowLength", 26) \
            .SetGreaterThanZero() \
            .SetDisplay("MACD Slow", "MACD slow EMA period", "MACD")
        self._macd_signal_length = self.Param("MacdSignalLength", 9) \
            .SetGreaterThanZero() \
            .SetDisplay("MACD Signal", "MACD signal line period", "MACD")
        self._take_profit_percent = self.Param("TakeProfitPercent", 1.5) \
            .SetNotNegative() \
            .SetDisplay("Take Profit %", "Take-profit percentage", "Risk")
        self._stop_loss_percent = self.Param("StopLossPercent", 2.0) \
            .SetNotNegative() \
            .SetDisplay("Stop Loss %", "Stop-loss percentage", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(30))) \
            .SetDisplay("Candle type", "Candle type for strategy calculation", "General")

        self._prev_macd = None
        self._prev_signal = None

    @property
    def CandleType(self):
        return self._candle_type.Value

    def GetWorkingSecurities(self):
        return [(self.Security, self.CandleType)]

    def OnReseted(self):
        super(omar_mmr_strategy, self).OnReseted()
        self._prev_macd = None
        self._prev_signal = None

    def OnStarted2(self, time):
        super(omar_mmr_strategy, self).OnStarted2(time)

        self._prev_macd = None
        self._prev_signal = None

        rsi = RelativeStrengthIndex()
        rsi.Length = self._rsi_length.Value
        ema_a = ExponentialMovingAverage()
        ema_a.Length = self._ema_a_length.Value
        ema_b = ExponentialMovingAverage()
        ema_b.Length = self._ema_b_length.Value
        ema_c = ExponentialMovingAverage()
        ema_c.Length = self._ema_c_length.Value
        macd = MovingAverageConvergenceDivergenceSignal()
        macd.Macd.ShortMa.Length = self._macd_fast_length.Value
        macd.Macd.LongMa.Length = self._macd_slow_length.Value
        macd.SignalMa.Length = self._macd_signal_length.Value

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.BindEx(rsi, ema_a, ema_b, ema_c, macd, self._process_candle).Start()

        tp = float(self._take_profit_percent.Value)
        sl = float(self._stop_loss_percent.Value)
        self.StartProtection(
            Unit(Decimal(tp), UnitTypes.Percent) if tp > 0 else Unit(),
            Unit(Decimal(sl), UnitTypes.Percent) if sl > 0 else Unit(),
            useMarketOrders=True)

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, ema_a)
            self.DrawIndicator(area, ema_b)
            self.DrawIndicator(area, ema_c)
            self.DrawOwnTrades(area)
            oscillators = self.CreateChartArea()
            if oscillators is not None:
                self.DrawIndicator(oscillators, macd)
                self.DrawIndicator(oscillators, rsi)

    def _process_candle(self, candle, rsi_value, ema_a_value, ema_b_value, ema_c_value, macd_value):
        if candle.State != CandleStates.Finished:
            return

        if not macd_value.IsFormed:
            return

        if macd_value.Macd is None or macd_value.Signal is None:
            return

        macd = float(macd_value.Macd)
        signal = float(macd_value.Signal)

        prev_macd = self._prev_macd
        prev_signal = self._prev_signal
        self._prev_macd = macd
        self._prev_signal = signal

        if not rsi_value.IsFormed or not ema_a_value.IsFormed or not ema_b_value.IsFormed or not ema_c_value.IsFormed:
            return

        if prev_macd is None or prev_signal is None:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        rsi = float(rsi_value.GetValue[Decimal](None))
        ema_a = float(ema_a_value.GetValue[Decimal](None))
        ema_b = float(ema_b_value.GetValue[Decimal](None))
        ema_c = float(ema_c_value.GetValue[Decimal](None))

        macd_cross_up = prev_macd <= prev_signal and macd > signal

        if self.Position == 0 \
                and float(candle.ClosePrice) > ema_c \
                and ema_a > ema_b \
                and macd_cross_up \
                and RSI_LOWER < rsi < RSI_UPPER:
            self.BuyMarket(self.Volume)

    def CreateClone(self):
        return omar_mmr_strategy()
