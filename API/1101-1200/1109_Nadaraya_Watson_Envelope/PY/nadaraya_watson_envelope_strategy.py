import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Strategies")

import math

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy


class nadaraya_watson_envelope_strategy(Strategy):
    """
    Nadaraya-Watson envelope strategy.
    The upper and lower envelopes are rational quadratic kernel regressions of log highs and log lows over the last
    StartRegressionBar candles, with LookbackWindow as bandwidth and RelativeWeighting as the relative weight of time frames.
    A close crossing above the lower envelope goes long, a close crossing below the upper envelope exits the long and,
    in LongShort mode, goes short. A short exits when the close crosses above the lower envelope.
    StrategyType: LongOnly or LongShort.
    """

    def __init__(self):
        super(nadaraya_watson_envelope_strategy, self).__init__()
        self._lookback_window = self.Param("LookbackWindow", 8).SetGreaterThanZero().SetDisplay("Lookback Window", "Kernel bandwidth in bars", "Kernel")
        self._relative_weighting = self.Param("RelativeWeighting", 8.0).SetGreaterThanZero().SetDisplay("Relative Weighting", "Relative weighting of time frames", "Kernel")
        self._start_regression_bar = self.Param("StartRegressionBar", 25).SetGreaterThanZero().SetDisplay("Start Regression Bar", "Number of bars used in the regression", "Kernel")
        self._strategy_type = self.Param("StrategyType", "LongOnly").SetDisplay("Strategy Type", "Long only or long and short", "General")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(1))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._weights = []
        self._reset_state()

    @property
    def candle_type(self):
        return self._candle_type.Value

    def GetWorkingSecurities(self):
        return [(self.Security, self.candle_type)]

    def _reset_state(self):
        self._log_highs = []
        self._log_lows = []
        self._prev_close = None
        self._prev_upper = None
        self._prev_lower = None

    def OnReseted(self):
        super(nadaraya_watson_envelope_strategy, self).OnReseted()
        self._reset_state()

    def OnStarted2(self, time):
        super(nadaraya_watson_envelope_strategy, self).OnStarted2(time)

        self._reset_state()

        h = float(self._lookback_window.Value)
        r = float(self._relative_weighting.Value)
        self._weights = [math.pow(1.0 + i * i / (h * h * 2.0 * r), -r) for i in range(self._start_regression_bar.Value)]

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    # Weighted average where index 0 is the latest bar.
    def _estimate(self, values):
        total = 0.0
        weight_sum = 0.0
        last = len(values) - 1
        for i, w in enumerate(self._weights):
            total += values[last - i] * w
            weight_sum += w
        return math.exp(total / weight_sum)

    def _process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        high = float(candle.HighPrice)
        low = float(candle.LowPrice)
        if high <= 0 or low <= 0:
            return

        length = self._start_regression_bar.Value
        self._log_highs.append(math.log(high))
        self._log_lows.append(math.log(low))
        if len(self._log_highs) > length:
            self._log_highs.pop(0)
            self._log_lows.pop(0)

        if len(self._log_highs) < length:
            return

        upper = self._estimate(self._log_highs)
        lower = self._estimate(self._log_lows)
        close = float(candle.ClosePrice)

        prev_close = self._prev_close
        prev_upper = self._prev_upper
        prev_lower = self._prev_lower
        self._prev_close = close
        self._prev_upper = upper
        self._prev_lower = lower

        if prev_close is None or prev_upper is None or prev_lower is None:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        cross_above_lower = prev_close <= prev_lower and close > lower
        cross_below_upper = prev_close >= prev_upper and close < upper

        if cross_above_lower and self.Position <= 0:
            self.BuyMarket(self.Volume + abs(self.Position))
        elif cross_below_upper and self.Position >= 0:
            if str(self._strategy_type.Value) == "LongShort":
                self.SellMarket(self.Volume + abs(self.Position))
            elif self.Position > 0:
                self.SellMarket(self.Position)

    def CreateClone(self):
        return nadaraya_watson_envelope_strategy()
