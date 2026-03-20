import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy
from StockSharp.Messages import Unit, UnitTypes


class e_turbo_fx_momentum_strategy(Strategy):
    def __init__(self):
        super(e_turbo_fx_momentum_strategy, self).__init__()

        self._depth_analysis = self.Param("DepthAnalysis", 3) \
            .SetDisplay("Depth Analysis", "Number of finished candles used for pattern detection", "Trading Rules")
        self._take_profit_steps = self.Param("TakeProfitSteps", 120) \
            .SetDisplay("Depth Analysis", "Number of finished candles used for pattern detection", "Trading Rules")
        self._stop_loss_steps = self.Param("StopLossSteps", 70) \
            .SetDisplay("Depth Analysis", "Number of finished candles used for pattern detection", "Trading Rules")
        self._trade_volume = self.Param("TradeVolume", 0.1) \
            .SetDisplay("Depth Analysis", "Number of finished candles used for pattern detection", "Trading Rules")
        self._candle_type = self.Param("CandleType", TimeSpan.FromHours(1) \
            .SetDisplay("Depth Analysis", "Number of finished candles used for pattern detection", "Trading Rules")

        self._bearish_sequence = 0.0
        self._bullish_sequence = 0.0
        self._previous_bearish_body = 0.0
        self._previous_bullish_body = 0.0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(e_turbo_fx_momentum_strategy, self).OnReseted()
        self._bearish_sequence = 0.0
        self._bullish_sequence = 0.0
        self._previous_bearish_body = 0.0
        self._previous_bullish_body = 0.0

    def OnStarted(self, time):
        super(e_turbo_fx_momentum_strategy, self).OnStarted(time)


        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self._process_candle).Start()

    def _process_candle(self, candle, *args):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        # Trading logic placeholder
        pass

    def CreateClone(self):
        return e_turbo_fx_momentum_strategy()
