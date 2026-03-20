import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import RelativeStrengthIndex, SimpleMovingAverage, StochasticOscillator, WeightedMovingAverage, WilliamsR
from StockSharp.Algo.Strategies import Strategy
from StockSharp.Messages import Sides
from StockSharp.Messages import Unit, UnitTypes


class polish_layer_expert_advisor_system_efficient_strategy(Strategy):
    def __init__(self):
        super(polish_layer_expert_advisor_system_efficient_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", TimeSpan.FromMinutes(5) \
            .SetDisplay("Candle Type", "Primary timeframe for calculations", "General")
        self._rsi_period = self.Param("RsiPeriod", 14) \
            .SetDisplay("Candle Type", "Primary timeframe for calculations", "General")
        self._short_price_period = self.Param("ShortPricePeriod", 9) \
            .SetDisplay("Candle Type", "Primary timeframe for calculations", "General")
        self._long_price_period = self.Param("LongPricePeriod", 45) \
            .SetDisplay("Candle Type", "Primary timeframe for calculations", "General")
        self._short_rsi_period = self.Param("ShortRsiPeriod", 9) \
            .SetDisplay("Candle Type", "Primary timeframe for calculations", "General")
        self._long_rsi_period = self.Param("LongRsiPeriod", 45) \
            .SetDisplay("Candle Type", "Primary timeframe for calculations", "General")
        self._stochastic_k_period = self.Param("StochasticKPeriod", 5) \
            .SetDisplay("Candle Type", "Primary timeframe for calculations", "General")
        self._stochastic_d_period = self.Param("StochasticDPeriod", 3) \
            .SetDisplay("Candle Type", "Primary timeframe for calculations", "General")
        self._stochastic_slowing = self.Param("StochasticSlowing", 3) \
            .SetDisplay("Candle Type", "Primary timeframe for calculations", "General")
        self._demarker_period = self.Param("DemarkerPeriod", 14) \
            .SetDisplay("Candle Type", "Primary timeframe for calculations", "General")
        self._williams_period = self.Param("WilliamsPeriod", 14) \
            .SetDisplay("Candle Type", "Primary timeframe for calculations", "General")
        self._stochastic_oversold_level = self.Param("StochasticOversoldLevel", 19) \
            .SetDisplay("Candle Type", "Primary timeframe for calculations", "General")
        self._stochastic_overbought_level = self.Param("StochasticOverboughtLevel", 81) \
            .SetDisplay("Candle Type", "Primary timeframe for calculations", "General")
        self._demarker_buy_level = self.Param("DemarkerBuyLevel", 0.35) \
            .SetDisplay("Candle Type", "Primary timeframe for calculations", "General")
        self._demarker_sell_level = self.Param("DemarkerSellLevel", 0.63) \
            .SetDisplay("Candle Type", "Primary timeframe for calculations", "General")
        self._williams_buy_level = self.Param("WilliamsBuyLevel", -81) \
            .SetDisplay("Candle Type", "Primary timeframe for calculations", "General")
        self._williams_sell_level = self.Param("WilliamsSellLevel", -19) \
            .SetDisplay("Candle Type", "Primary timeframe for calculations", "General")
        self._stop_loss_pips = self.Param("StopLossPips", 7777) \
            .SetDisplay("Candle Type", "Primary timeframe for calculations", "General")
        self._take_profit_pips = self.Param("TakeProfitPips", 17) \
            .SetDisplay("Candle Type", "Primary timeframe for calculations", "General")

        self._short_price_ma = null!
        self._long_price_ma = null!
        self._rsi = null!
        self._short_rsi_average = null!
        self._long_rsi_average = null!
        self._stochastic = null!
        self._de_marker = null!
        self._williams = null!
        self._previous_stochastic_main = None
        self._previous_stochastic_signal = None
        self._previous_de_marker = None
        self._previous_williams = None
        self._long_stop_price = None
        self._long_take_price = None
        self._short_stop_price = None
        self._short_take_price = None
        self._price_step = 0.0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(polish_layer_expert_advisor_system_efficient_strategy, self).OnReseted()
        self._short_price_ma = null!
        self._long_price_ma = null!
        self._rsi = null!
        self._short_rsi_average = null!
        self._long_rsi_average = null!
        self._stochastic = null!
        self._de_marker = null!
        self._williams = null!
        self._previous_stochastic_main = None
        self._previous_stochastic_signal = None
        self._previous_de_marker = None
        self._previous_williams = None
        self._long_stop_price = None
        self._long_take_price = None
        self._short_stop_price = None
        self._short_take_price = None
        self._price_step = 0.0

    def OnStarted(self, time):
        super(polish_layer_expert_advisor_system_efficient_strategy, self).OnStarted(time)

        self.__short_price_ma = SimpleMovingAverage()
        self.__short_price_ma.Length = self.short_price_period
        self.__long_price_ma = WeightedMovingAverage()
        self.__long_price_ma.Length = self.long_price_period
        self.__rsi = RelativeStrengthIndex()
        self.__rsi.Length = self.rsi_period
        self.__short_rsi_average = SimpleMovingAverage()
        self.__short_rsi_average.Length = Shortself.rsi_period
        self.__long_rsi_average = SimpleMovingAverage()
        self.__long_rsi_average.Length = Longself.rsi_period
        self.__de_marker = DeMarker()
        self.__de_marker.Length = self.demarker_period
        self.__williams = WilliamsR()
        self.__williams.Length = self.williams_period

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self.__short_price_ma, self.__long_price_ma, self.__rsi, self._process_candle).Start()

    def _process_candle(self, candle, *args):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        # Trading logic placeholder
        pass

    def CreateClone(self):
        return polish_layer_expert_advisor_system_efficient_strategy()
