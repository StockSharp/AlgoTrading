import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")
clr.AddReference("StockSharp.BusinessEntities")

from System import TimeSpan, Decimal
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes, Level1Fields
from StockSharp.BusinessEntities import Subscription
from StockSharp.Algo.Indicators import BollingerBands, StochasticOscillator
from StockSharp.Algo.Strategies import Strategy

class bollinger_stochastic_strategy(Strategy):
    """
    Bollinger Stochastic strategy.
    A candle that touches the lower band while stochastic %K is below StochOversold goes long; a candle that touches the upper band
    while %K is above StochOverbought goes short. An opposite signal reverses the position, and a percent stop limits the loss.
    """

    def __init__(self):
        super(bollinger_stochastic_strategy, self).__init__()
        self._bollinger_period = self.Param("BollingerPeriod", 20).SetGreaterThanZero().SetDisplay("BB Period", "Period of the Bollinger Bands", "Bollinger")
        self._bollinger_deviation = self.Param("BollingerDeviation", 2.0).SetGreaterThanZero().SetDisplay("BB Deviation", "Standard deviation multiplier of the bands", "Bollinger")
        self._stoch_period = self.Param("StochPeriod", 14).SetGreaterThanZero().SetDisplay("Stochastic Period", "Lookback period of stochastic %K", "Stochastic")
        self._stoch_d_period = self.Param("StochDPeriod", 3).SetGreaterThanZero().SetDisplay("Stochastic %D", "Smoothing period of stochastic %D", "Stochastic")
        self._stoch_oversold = self.Param("StochOversold", 20.0).SetDisplay("Oversold Level", "Stochastic level for longs", "Stochastic")
        self._stoch_overbought = self.Param("StochOverbought", 80.0).SetDisplay("Overbought Level", "Stochastic level for shorts", "Stochastic")
        self._stop_loss_percent = self.Param("StopLossPercent", 2.0).SetNotNegative().SetDisplay("Stop Loss %", "Stop loss percentage from entry price", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(15))).SetDisplay("Candle Type", "Type of candles to use", "General")

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnStarted2(self, time):
        super(bollinger_stochastic_strategy, self).OnStarted2(time)

        bollinger = BollingerBands()
        bollinger.Length = self._bollinger_period.Value
        bollinger.Width = Decimal(self._bollinger_deviation.Value)
        stochastic = StochasticOscillator()
        stochastic.K.Length = self._stoch_period.Value
        stochastic.D.Length = self._stoch_d_period.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(bollinger, stochastic, self._process_candle).Start()

        self.StartProtection(Unit(), Unit(Decimal(self._stop_loss_percent.Value), UnitTypes.Percent), useMarketOrders=True, isLocalStop=True)

        # The stop has to see prices between candles, not only at their close.
        for field in (Level1Fields.BestBidPrice, Level1Fields.BestAskPrice):
            quotes = Subscription(DataType.Level1, self.Security)
            quotes.MarketData.BuildField = field
            self.SubscribeLevel1(quotes).Bind(self._observe_protection_quote).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, bollinger)
            self.DrawOwnTrades(area)
            oscillators = self.CreateChartArea()
            if oscillators is not None:
                self.DrawIndicator(oscillators, stochastic)

    def _observe_protection_quote(self, quote):
        # The high-level handler activates native protection before the callback, including between bars.
        pass

    def _process_candle(self, candle, bollinger_value, stochastic_value):
        if candle.State != CandleStates.Finished:
            return

        if not bollinger_value.IsFormed or not stochastic_value.IsFormed:
            return
        if bollinger_value.UpBand is None or bollinger_value.LowBand is None or stochastic_value.K is None:
            return

        upper = bollinger_value.UpBand
        lower = bollinger_value.LowBand
        k = stochastic_value.K

        signal = 0
        if candle.LowPrice <= lower and k < Decimal(self._stoch_oversold.Value):
            signal = 1
        elif candle.HighPrice >= upper and k > Decimal(self._stoch_overbought.Value):
            signal = -1

        if signal == 0 or not self.IsFormedAndOnlineAndAllowTrading():
            return

        if signal > 0 and self.Position <= 0:
            self.BuyMarket(self.Volume + abs(self.Position))
        elif signal < 0 and self.Position >= 0:
            self.SellMarket(self.Volume + abs(self.Position))

    def CreateClone(self):
        return bollinger_stochastic_strategy()
