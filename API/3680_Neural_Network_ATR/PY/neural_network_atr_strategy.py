import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy


class neural_network_atr_strategy(Strategy):
    def __init__(self):
        super(neural_network_atr_strategy, self).__init__()

        self._max_risk_per_trade = self.Param("MaxRiskPerTrade", 1.0) \
            .SetDisplay("Max Risk %", "Maximum risk per trade as percentage of equity", "Risk Management")
        self._daily_loss_limit = self.Param("DailyLossLimit", 5.0) \
            .SetDisplay("Max Risk %", "Maximum risk per trade as percentage of equity", "Risk Management")
        self._total_loss_limit = self.Param("TotalLossLimit", 10.0) \
            .SetDisplay("Max Risk %", "Maximum risk per trade as percentage of equity", "Risk Management")
        self._daily_profit_target = self.Param("DailyProfitTarget", 1.0) \
            .SetDisplay("Max Risk %", "Maximum risk per trade as percentage of equity", "Risk Management")
        self._initial_learning_rate = self.Param("InitialLearningRate", 0.01) \
            .SetDisplay("Max Risk %", "Maximum risk per trade as percentage of equity", "Risk Management")
        self._hidden_layer_size = self.Param("HiddenLayerSize", 5) \
            .SetDisplay("Max Risk %", "Maximum risk per trade as percentage of equity", "Risk Management")
        self._buy_threshold = self.Param("BuyThreshold", 0.502) \
            .SetDisplay("Max Risk %", "Maximum risk per trade as percentage of equity", "Risk Management")
        self._sell_threshold = self.Param("SellThreshold", 0.498) \
            .SetDisplay("Max Risk %", "Maximum risk per trade as percentage of equity", "Risk Management")
        self._candle_type = self.Param("CandleType", TimeSpan.FromMinutes(15) \
            .SetDisplay("Max Risk %", "Maximum risk per trade as percentage of equity", "Risk Management")
        self._atr_period = self.Param("AtrPeriod", 14) \
            .SetDisplay("Max Risk %", "Maximum risk per trade as percentage of equity", "Risk Management")
        self._max_spread_points = self.Param("MaxSpreadPoints", 20) \
            .SetDisplay("Max Risk %", "Maximum risk per trade as percentage of equity", "Risk Management")
        self._risk_reward_ratio = self.Param("RiskRewardRatio", 2.0) \
            .SetDisplay("Max Risk %", "Maximum risk per trade as percentage of equity", "Risk Management")
        self._fallback_stop_loss_points = self.Param("FallbackStopLossPoints", 50) \
            .SetDisplay("Max Risk %", "Maximum risk per trade as percentage of equity", "Risk Management")
        self._input_size = self.Param("InputSize", 5) \
            .SetDisplay("Max Risk %", "Maximum risk per trade as percentage of equity", "Risk Management")
        self._minimum_learning_rate = self.Param("MinimumLearningRate", 0.0001) \
            .SetDisplay("Max Risk %", "Maximum risk per trade as percentage of equity", "Risk Management")
        self._feature_clamp = self.Param("FeatureClamp", 1) \
            .SetDisplay("Max Risk %", "Maximum risk per trade as percentage of equity", "Risk Management")

        self._account_equity_at_start = 0.0
        self._daily_equity_at_start = 0.0
        self._last_trade_day = None
        self._last_penalty_day = None
        self._trading_halted = False
        self._learning_rate = 0.0
        self._atr_indicator = None
        self._bias_output = 0.0
        self._previous_candle = None
        self._best_bid_price = 0.0
        self._best_ask_price = 0.0
        self._has_best_bid = False
        self._has_best_ask = False

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(neural_network_atr_strategy, self).OnReseted()
        self._account_equity_at_start = 0.0
        self._daily_equity_at_start = 0.0
        self._last_trade_day = None
        self._last_penalty_day = None
        self._trading_halted = False
        self._learning_rate = 0.0
        self._atr_indicator = None
        self._bias_output = 0.0
        self._previous_candle = None
        self._best_bid_price = 0.0
        self._best_ask_price = 0.0
        self._has_best_bid = False
        self._has_best_ask = False

    def OnStarted(self, time):
        super(neural_network_atr_strategy, self).OnStarted(time)
        self.StartProtection(None, None)

        self._atr = ATR()
        self._atr.Length = self.atr_period

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
        return neural_network_atr_strategy()
