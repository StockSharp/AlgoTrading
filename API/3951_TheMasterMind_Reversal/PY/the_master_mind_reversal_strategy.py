import clr
clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates, UnitTypes, Unit
from StockSharp.Algo.Strategies import Strategy
from StockSharp.Algo.Indicators import StochasticOscillator, WilliamsR

class the_master_mind_reversal_strategy(Strategy):
    def __init__(self):
        super(the_master_mind_reversal_strategy, self).__init__()

        self._trade_volume = self.Param("TradeVolume", 1.0).SetDisplay("Volume", "Base order size", "Trading")
        self._stochastic_period = self.Param("StochasticPeriod", 100).SetDisplay("Stochastic Length", "Total lookback for stochastic", "Indicators")
        self._k_period = self.Param("KPeriod", 3).SetDisplay("%K Smoothing", "Stochastic %K smoothing length", "Indicators")
        self._d_period = self.Param("DPeriod", 3).SetDisplay("%D Signal", "Stochastic %D signal length", "Indicators")
        self._williams_period = self.Param("WilliamsPeriod", 100).SetDisplay("Williams %R Length", "Lookback period for Williams %R", "Indicators")
        self._stochastic_buy_threshold = self.Param("StochasticBuyThreshold", 3.0).SetDisplay("Stoch Buy Threshold", "%D level required to buy", "Signals")
        self._stochastic_sell_threshold = self.Param("StochasticSellThreshold", 97.0).SetDisplay("Stoch Sell Threshold", "%D level required to sell", "Signals")
        self._williams_buy_level = self.Param("WilliamsBuyLevel", -99.5).SetDisplay("Williams Buy Level", "Williams %R oversold level", "Signals")
        self._williams_sell_level = self.Param("WilliamsSellLevel", -0.5).SetDisplay("Williams Sell Level", "Williams %R overbought level", "Signals")
        self._stop_loss = self.Param("StopLoss", 0.0).SetDisplay("Stop Loss", "Protective stop distance", "Risk")
        self._take_profit = self.Param("TakeProfit", 0.0).SetDisplay("Take Profit", "Target distance", "Risk")
        self._use_trailing_stop = self.Param("UseTrailingStop", False).SetDisplay("Use Trailing", "Enable trailing stop management", "Risk")
        self._trailing_stop = self.Param("TrailingStop", 0.0).SetDisplay("Trailing Stop", "Trailing stop distance", "Risk")
        self._trailing_step = self.Param("TrailingStep", 0.0).SetDisplay("Trailing Step", "Trailing step distance", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(15))).SetDisplay("Candle Type", "Primary candle series", "Trading")

        self._stochastic = None
        self._williams = None

    @property
    def TradeVolume(self): return self._trade_volume.Value
    @property
    def StochasticPeriod(self): return self._stochastic_period.Value
    @property
    def KPeriod(self): return self._k_period.Value
    @property
    def DPeriod(self): return self._d_period.Value
    @property
    def WilliamsPeriod(self): return self._williams_period.Value
    @property
    def StochasticBuyThreshold(self): return self._stochastic_buy_threshold.Value
    @property
    def StochasticSellThreshold(self): return self._stochastic_sell_threshold.Value
    @property
    def WilliamsBuyLevel(self): return self._williams_buy_level.Value
    @property
    def WilliamsSellLevel(self): return self._williams_sell_level.Value
    @property
    def StopLoss(self): return self._stop_loss.Value
    @property
    def TakeProfit(self): return self._take_profit.Value
    @property
    def UseTrailingStop(self): return self._use_trailing_stop.Value
    @property
    def TrailingStop(self): return self._trailing_stop.Value
    @property
    def TrailingStep(self): return self._trailing_step.Value
    @property
    def CandleType(self): return self._candle_type.Value

    def OnStarted(self, time):
        super(the_master_mind_reversal_strategy, self).OnStarted(time)

        self.Volume = self.TradeVolume

        self._stochastic = StochasticOscillator()
        self._stochastic.K.Length = self.StochasticPeriod
        self._stochastic.D.Length = self.DPeriod

        self._williams = WilliamsR()
        self._williams.Length = self.WilliamsPeriod

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.BindEx(self._stochastic, self._williams, self.ProcessSignals).Start()

        tp = float(self.TakeProfit)
        sl = float(self.StopLoss)
        tp_unit = Unit(tp, UnitTypes.Absolute) if tp > 0 else None
        sl_unit = Unit(sl, UnitTypes.Absolute) if sl > 0 else None

        if tp_unit is not None or sl_unit is not None:
            self.StartProtection(takeProfit=tp_unit, stopLoss=sl_unit, isStopTrailing=self.UseTrailingStop)

    def ProcessSignals(self, candle, stochastic_value, williams_raw_value):
        if candle.State != CandleStates.Finished:
            return

        signal_value = stochastic_value.D
        if signal_value is None:
            return

        signal_val = float(signal_value)

        williams_value = None
        if not williams_raw_value.IsEmpty:
            williams_value = float(williams_raw_value.ToDecimal())

        if williams_value is None:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        buy_threshold = float(self.StochasticBuyThreshold)
        sell_threshold = float(self.StochasticSellThreshold)
        williams_buy = float(self.WilliamsBuyLevel)
        williams_sell = float(self.WilliamsSellLevel)

        buy_signal = signal_val <= buy_threshold and williams_value <= williams_buy
        sell_signal = signal_val >= sell_threshold and williams_value >= williams_sell

        if buy_signal:
            if self.Position < 0:
                self.BuyMarket(abs(self.Position))
            if self.Position <= 0:
                self.BuyMarket(self.Volume)
            return

        if sell_signal:
            if self.Position > 0:
                self.SellMarket(abs(self.Position))
            if self.Position >= 0:
                self.SellMarket(self.Volume)

    def CreateClone(self):
        return the_master_mind_reversal_strategy()
