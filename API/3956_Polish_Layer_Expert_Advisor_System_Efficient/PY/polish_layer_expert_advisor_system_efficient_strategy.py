import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates, UnitTypes
from StockSharp.Algo.Strategies import Strategy
from StockSharp.Algo.Indicators import (
    SimpleMovingAverage,
    WeightedMovingAverage,
    RelativeStrengthIndex,
    StochasticOscillator,
    DeMarker,
    WilliamsR,
    DecimalIndicatorValue,
    CandleIndicatorValue,
)

class polish_layer_expert_advisor_system_efficient_strategy(Strategy):
    def __init__(self):
        super(polish_layer_expert_advisor_system_efficient_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Primary timeframe for calculations", "General")
        self._rsi_period = self.Param("RsiPeriod", 14) \
            .SetDisplay("RSI Period", "Base RSI length", "RSI")
        self._short_price_period = self.Param("ShortPricePeriod", 9) \
            .SetDisplay("Fast Price MA", "Length of the fast price moving average", "Trend")
        self._long_price_period = self.Param("LongPricePeriod", 45) \
            .SetDisplay("Slow Price MA", "Length of the slow price moving average", "Trend")
        self._short_rsi_period = self.Param("ShortRsiPeriod", 9) \
            .SetDisplay("Fast RSI MA", "Length of the fast RSI moving average", "RSI")
        self._long_rsi_period = self.Param("LongRsiPeriod", 45) \
            .SetDisplay("Slow RSI MA", "Length of the slow RSI moving average", "RSI")
        self._stochastic_k_period = self.Param("StochasticKPeriod", 5) \
            .SetDisplay("%K Period", "Stochastic %K period", "Stochastic")
        self._stochastic_d_period = self.Param("StochasticDPeriod", 3) \
            .SetDisplay("%D Period", "Stochastic %D period", "Stochastic")
        self._stochastic_slowing = self.Param("StochasticSlowing", 3) \
            .SetDisplay("Slowing", "Stochastic slowing factor", "Stochastic")
        self._demarker_period = self.Param("DemarkerPeriod", 14) \
            .SetDisplay("DeMarker Period", "DeMarker averaging period", "DeMarker")
        self._williams_period = self.Param("WilliamsPeriod", 14) \
            .SetDisplay("Williams %R Period", "Williams %R lookback", "Williams %R")
        self._stochastic_oversold_level = self.Param("StochasticOversoldLevel", 19.0) \
            .SetDisplay("%K Oversold", "Oversold level for %K", "Thresholds")
        self._stochastic_overbought_level = self.Param("StochasticOverboughtLevel", 81.0) \
            .SetDisplay("%K Overbought", "Overbought level for %K", "Thresholds")
        self._demarker_buy_level = self.Param("DemarkerBuyLevel", 0.35) \
            .SetDisplay("DeMarker Buy Level", "Minimum DeMarker value for longs", "Thresholds")
        self._demarker_sell_level = self.Param("DemarkerSellLevel", 0.63) \
            .SetDisplay("DeMarker Sell Level", "Maximum DeMarker value for shorts", "Thresholds")
        self._williams_buy_level = self.Param("WilliamsBuyLevel", -81.0) \
            .SetDisplay("Williams Buy Level", "Williams %R level for longs", "Thresholds")
        self._williams_sell_level = self.Param("WilliamsSellLevel", -19.0) \
            .SetDisplay("Williams Sell Level", "Williams %R level for shorts", "Thresholds")
        self._stop_loss_pips = self.Param("StopLossPips", 7777.0) \
            .SetDisplay("Stop Loss", "Stop-loss distance in pips", "Risk")
        self._take_profit_pips = self.Param("TakeProfitPips", 17.0) \
            .SetDisplay("Take Profit", "Take-profit distance in pips", "Risk")

        self._previous_stochastic_main = None
        self._previous_stochastic_signal = None
        self._previous_demarker = None
        self._previous_williams = None

        self._short_price_ma = None
        self._long_price_ma = None
        self._rsi = None
        self._short_rsi_average = None
        self._long_rsi_average = None
        self._stochastic = None
        self._de_marker = None
        self._williams = None

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    @property
    def RsiPeriod(self):
        return self._rsi_period.Value

    @property
    def ShortPricePeriod(self):
        return self._short_price_period.Value

    @property
    def LongPricePeriod(self):
        return self._long_price_period.Value

    @property
    def ShortRsiPeriod(self):
        return self._short_rsi_period.Value

    @property
    def LongRsiPeriod(self):
        return self._long_rsi_period.Value

    @property
    def StochasticKPeriod(self):
        return self._stochastic_k_period.Value

    @property
    def StochasticDPeriod(self):
        return self._stochastic_d_period.Value

    @property
    def DemarkerPeriod(self):
        return self._demarker_period.Value

    @property
    def WilliamsPeriod(self):
        return self._williams_period.Value

    @property
    def StochasticOversoldLevel(self):
        return self._stochastic_oversold_level.Value

    @property
    def StochasticOverboughtLevel(self):
        return self._stochastic_overbought_level.Value

    def OnStarted(self, time):
        super(polish_layer_expert_advisor_system_efficient_strategy, self).OnStarted(time)

        from StockSharp.Messages import Unit
        self.StartProtection(Unit(2, UnitTypes.Percent), Unit(1, UnitTypes.Percent))

        self._short_price_ma = SimpleMovingAverage()
        self._short_price_ma.Length = self.ShortPricePeriod
        self._long_price_ma = WeightedMovingAverage()
        self._long_price_ma.Length = self.LongPricePeriod
        self._rsi = RelativeStrengthIndex()
        self._rsi.Length = self.RsiPeriod
        self._short_rsi_average = SimpleMovingAverage()
        self._short_rsi_average.Length = self.ShortRsiPeriod
        self._long_rsi_average = SimpleMovingAverage()
        self._long_rsi_average.Length = self.LongRsiPeriod
        self._stochastic = StochasticOscillator()
        self._stochastic.K.Length = self.StochasticKPeriod
        self._stochastic.D.Length = self.StochasticDPeriod
        self._de_marker = DeMarker()
        self._de_marker.Length = self.DemarkerPeriod
        self._williams = WilliamsR()
        self._williams.Length = self.WilliamsPeriod

        self.Indicators.Add(self._stochastic)
        self.Indicators.Add(self._de_marker)
        self.Indicators.Add(self._williams)

        subscription = self.SubscribeCandles(self.CandleType)
        subscription \
            .Bind(self._short_price_ma, self._long_price_ma, self._rsi, self.ProcessCandle) \
            .Start()

    def ProcessCandle(self, candle, fast_price, slow_price, rsi):
        if candle.State != CandleStates.Finished:
            return

        fast_price = float(fast_price)
        slow_price = float(slow_price)
        rsi_val = float(rsi)

        fast_rsi_input = DecimalIndicatorValue(self._short_rsi_average, rsi, candle.OpenTime)
        fast_rsi_input.IsFinal = True
        fast_rsi_result = self._short_rsi_average.Process(fast_rsi_input)
        slow_rsi_input = DecimalIndicatorValue(self._long_rsi_average, rsi, candle.OpenTime)
        slow_rsi_input.IsFinal = True
        slow_rsi_result = self._long_rsi_average.Process(slow_rsi_input)

        stoch_input = CandleIndicatorValue(self._stochastic, candle)
        stoch_input.IsFinal = True
        stochastic_result = self._stochastic.Process(stoch_input)
        dm_input = CandleIndicatorValue(self._de_marker, candle)
        dm_input.IsFinal = True
        demarker_result = self._de_marker.Process(dm_input)
        wr_input = CandleIndicatorValue(self._williams, candle)
        wr_input.IsFinal = True
        williams_result = self._williams.Process(wr_input)

        if not self._stochastic.IsFormed or not self._de_marker.IsFormed or not self._williams.IsFormed:
            return

        if not self._short_rsi_average.IsFormed or not self._long_rsi_average.IsFormed:
            return

        fast_rsi = float(fast_rsi_result)
        slow_rsi = float(slow_rsi_result)

        stoch_val = stochastic_result
        current_stochastic_main = stoch_val.K
        current_stochastic_signal = stoch_val.D

        if current_stochastic_main is None or current_stochastic_signal is None:
            return

        current_stochastic_main = float(current_stochastic_main)
        current_stochastic_signal = float(current_stochastic_signal)

        demarker = float(demarker_result)
        williams = float(williams_result)

        if (self._previous_stochastic_main is None or
            self._previous_stochastic_signal is None or
            self._previous_demarker is None or
            self._previous_williams is None):
            self._update_previous(current_stochastic_main, current_stochastic_signal, demarker, williams)
            return

        if self.Position != 0:
            self._update_previous(current_stochastic_main, current_stochastic_signal, demarker, williams)
            return

        long_trend = fast_price > slow_price and fast_rsi > slow_rsi
        short_trend = fast_price < slow_price and fast_rsi < slow_rsi

        stoch_cross_up = (self._previous_stochastic_main < float(self.StochasticOversoldLevel)
                          and current_stochastic_main >= float(self.StochasticOversoldLevel))
        stoch_cross_down = (self._previous_stochastic_main > float(self.StochasticOverboughtLevel)
                            and current_stochastic_main <= float(self.StochasticOverboughtLevel))

        long_signal = long_trend and stoch_cross_up
        short_signal = short_trend and stoch_cross_down

        if long_signal:
            self.BuyMarket()
        elif short_signal:
            self.SellMarket()

        self._update_previous(current_stochastic_main, current_stochastic_signal, demarker, williams)

    def _update_previous(self, stoch_main, stoch_signal, demarker, williams):
        self._previous_stochastic_main = stoch_main
        self._previous_stochastic_signal = stoch_signal
        self._previous_demarker = demarker
        self._previous_williams = williams

    def OnReseted(self):
        super(polish_layer_expert_advisor_system_efficient_strategy, self).OnReseted()
        self._previous_stochastic_main = None
        self._previous_stochastic_signal = None
        self._previous_demarker = None
        self._previous_williams = None

    def CreateClone(self):
        return polish_layer_expert_advisor_system_efficient_strategy()
