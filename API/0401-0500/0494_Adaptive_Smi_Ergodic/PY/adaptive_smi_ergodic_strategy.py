import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")
clr.AddReference("StockSharp.BusinessEntities")

from System import TimeSpan, Decimal
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import TrueStrengthIndex
from StockSharp.Algo.Strategies import Strategy


class adaptive_smi_ergodic_strategy(Strategy):
    """
    Adaptive SMI Ergodic strategy.
    The True Strength Index (long smoothing LongLength, short smoothing ShortLength) is compared with its EMA signal line. A long opens
    when TSI crosses above OversoldThreshold while above the signal line, a short when it crosses below OverboughtThreshold while below
    the signal line; the opposite signal reverses the position. Thresholds are on the -1..1 ergodic scale.
    """

    def __init__(self):
        super(adaptive_smi_ergodic_strategy, self).__init__()
        self._long_length = self.Param("LongLength", 12).SetGreaterThanZero().SetDisplay("Long Length", "Long smoothing length of TSI", "TSI")
        self._short_length = self.Param("ShortLength", 5).SetGreaterThanZero().SetDisplay("Short Length", "Short smoothing length of TSI", "TSI")
        self._signal_length = self.Param("SignalLength", 5).SetGreaterThanZero().SetDisplay("Signal Length", "EMA length of the signal line", "TSI")
        self._oversold_threshold = self.Param("OversoldThreshold", -0.4).SetDisplay("Oversold Threshold", "Oversold level on the -1..1 scale", "Levels")
        self._overbought_threshold = self.Param("OverboughtThreshold", 0.4).SetDisplay("Overbought Threshold", "Overbought level on the -1..1 scale", "Levels")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(30))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._prev_tsi = None

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(adaptive_smi_ergodic_strategy, self).OnReseted()
        self._prev_tsi = None

    def OnStarted2(self, time):
        super(adaptive_smi_ergodic_strategy, self).OnStarted2(time)

        self._prev_tsi = None

        tsi = TrueStrengthIndex()
        tsi.FirstLength = self._long_length.Value
        tsi.SecondLength = self._short_length.Value
        tsi.SignalLength = self._signal_length.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(tsi, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)
            oscillators = self.CreateChartArea()
            if oscillators is not None:
                self.DrawIndicator(oscillators, tsi)

    def _process_candle(self, candle, tsi_value):
        if candle.State != CandleStates.Finished:
            return

        if tsi_value.Tsi is None or tsi_value.Signal is None:
            return

        # The indicator reports TSI in percent; the thresholds use the -1..1 ergodic scale.
        tsi = tsi_value.Tsi / Decimal(100)
        signal = tsi_value.Signal / Decimal(100)

        prev = self._prev_tsi
        self._prev_tsi = tsi

        if prev is None or not self.IsFormedAndOnlineAndAllowTrading():
            return

        oversold = Decimal(self._oversold_threshold.Value)
        overbought = Decimal(self._overbought_threshold.Value)

        cross_above_oversold = prev <= oversold and tsi > oversold
        cross_below_overbought = prev >= overbought and tsi < overbought

        if cross_above_oversold and tsi > signal and self.Position <= 0:
            self.BuyMarket(self.Volume + abs(self.Position))
        elif cross_below_overbought and tsi < signal and self.Position >= 0:
            self.SellMarket(self.Volume + abs(self.Position))

    def CreateClone(self):
        return adaptive_smi_ergodic_strategy()
