import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import StochasticOscillator
from StockSharp.Algo.Strategies import Strategy


class bull_row_breakout_strategy(Strategy):
    def __init__(self):
        super(bull_row_breakout_strategy, self).__init__()

        self._candle_time_frame = self.Param("CandleTimeFrame", TimeSpan.FromMinutes(5) \
            .SetDisplay("Timeframe", "Primary candle timeframe", "Market")
        self._stop_loss_lookback = self.Param("StopLossLookback", 10) \
            .SetDisplay("Timeframe", "Primary candle timeframe", "Market")
        self._take_profit_percent = self.Param("TakeProfitPercent", 100) \
            .SetDisplay("Timeframe", "Primary candle timeframe", "Market")
        self._bear_row_size = self.Param("BearRowSize", 3) \
            .SetDisplay("Timeframe", "Primary candle timeframe", "Market")
        self._bear_min_body = self.Param("BearMinBody", 0) \
            .SetDisplay("Timeframe", "Primary candle timeframe", "Market")
        self._bear_row_mode = self.Param("BearRowMode", RowSequenceModes.Normal) \
            .SetDisplay("Timeframe", "Primary candle timeframe", "Market")
        self._bear_shift = self.Param("BearShift", 3) \
            .SetDisplay("Timeframe", "Primary candle timeframe", "Market")
        self._bull_row_size = self.Param("BullRowSize", 2) \
            .SetDisplay("Timeframe", "Primary candle timeframe", "Market")
        self._bull_min_body = self.Param("BullMinBody", 0) \
            .SetDisplay("Timeframe", "Primary candle timeframe", "Market")
        self._bull_row_mode = self.Param("BullRowMode", RowSequenceModes.Normal) \
            .SetDisplay("Timeframe", "Primary candle timeframe", "Market")
        self._bull_shift = self.Param("BullShift", 1) \
            .SetDisplay("Timeframe", "Primary candle timeframe", "Market")
        self._breakout_lookback = self.Param("BreakoutLookback", 10) \
            .SetDisplay("Timeframe", "Primary candle timeframe", "Market")
        self._stochastic_k_period = self.Param("StochasticKPeriod", 40) \
            .SetDisplay("Timeframe", "Primary candle timeframe", "Market")
        self._stochastic_d_period = self.Param("StochasticDPeriod", 8) \
            .SetDisplay("Timeframe", "Primary candle timeframe", "Market")
        self._stochastic_slowing = self.Param("StochasticSlowing", 10) \
            .SetDisplay("Timeframe", "Primary candle timeframe", "Market")
        self._stochastic_range_period = self.Param("StochasticRangePeriod", 3) \
            .SetDisplay("Timeframe", "Primary candle timeframe", "Market")
        self._stochastic_upper_level = self.Param("StochasticUpperLevel", 70) \
            .SetDisplay("Timeframe", "Primary candle timeframe", "Market")
        self._stochastic_lower_level = self.Param("StochasticLowerLevel", 30) \
            .SetDisplay("Timeframe", "Primary candle timeframe", "Market")

        self._candles = new()
        self._stochastic_history = new()
        self._stochastic = null!
        self._stop_price = None
        self._take_profit_price = None

    def OnReseted(self):
        super(bull_row_breakout_strategy, self).OnReseted()
        self._candles = new()
        self._stochastic_history = new()
        self._stochastic = null!
        self._stop_price = None
        self._take_profit_price = None

    def OnStarted(self, time):
        super(bull_row_breakout_strategy, self).OnStarted(time)
        self.StartProtection(None, None)


        subscription = self.SubscribeCandles(self.candle_time_frame.TimeFrame()
        subscription.BindEx(_stochastic, self._process_candle).Start()

    def _process_candle(self, candle, *args):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        # Trading logic placeholder
        pass

    def CreateClone(self):
        return bull_row_breakout_strategy()
