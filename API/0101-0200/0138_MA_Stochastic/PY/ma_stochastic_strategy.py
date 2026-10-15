import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")
clr.AddReference("StockSharp.BusinessEntities")

from System import TimeSpan, Decimal
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes, Level1Fields
from StockSharp.BusinessEntities import Subscription
from StockSharp.Algo.Indicators import SimpleMovingAverage, StochasticOscillator
from StockSharp.Algo.Strategies import Strategy

class ma_stochastic_strategy(Strategy):
    """
    MA Stochastic strategy.
    Price above the MaPeriod SMA is an uptrend, below it a downtrend. Stochastic %K dipping below StochOversold in an uptrend
    prepares a long that is bought on the next upturn of %K while price stays above the SMA; %K reaching StochOverbought in a downtrend
    prepares a short sold on the next downturn. Leaving the trend cancels the setup. An opposite signal reverses the position,
    and a percent stop limits the loss.
    """

    def __init__(self):
        super(ma_stochastic_strategy, self).__init__()
        self._ma_period = self.Param("MaPeriod", 50).SetGreaterThanZero().SetDisplay("MA Period", "Period of the trend SMA", "Indicators")
        self._stoch_period = self.Param("StochPeriod", 14).SetGreaterThanZero().SetDisplay("Stochastic Period", "Lookback period of stochastic %K", "Indicators")
        self._stoch_d_period = self.Param("StochDPeriod", 3).SetGreaterThanZero().SetDisplay("Stochastic %D", "Smoothing period of stochastic %D", "Indicators")
        self._stoch_oversold = self.Param("StochOversold", 20.0).SetDisplay("Stochastic Oversold", "Level that prepares a long", "Indicators")
        self._stoch_overbought = self.Param("StochOverbought", 80.0).SetDisplay("Stochastic Overbought", "Level that prepares a short", "Indicators")
        self._stop_loss_percent = self.Param("StopLossPercent", 2.0).SetNotNegative().SetDisplay("Stop Loss %", "Stop loss percentage from entry price", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(15))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._reset_state()

    @property
    def candle_type(self):
        return self._candle_type.Value

    def _reset_state(self):
        self._prev_k = None
        # The prepared side: 1 long, -1 short, 0 none.
        self._setup = 0

    def OnReseted(self):
        super(ma_stochastic_strategy, self).OnReseted()
        self._reset_state()

    def OnStarted2(self, time):
        super(ma_stochastic_strategy, self).OnStarted2(time)

        self._reset_state()

        sma = SimpleMovingAverage()
        sma.Length = self._ma_period.Value
        stochastic = StochasticOscillator()
        stochastic.K.Length = self._stoch_period.Value
        stochastic.D.Length = self._stoch_d_period.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(sma, stochastic, self._process_candle).Start()

        self.StartProtection(Unit(), Unit(Decimal(self._stop_loss_percent.Value), UnitTypes.Percent), useMarketOrders=True, isLocalStop=True)

        # The stop has to see prices between candles, not only at their close.
        for field in (Level1Fields.BestBidPrice, Level1Fields.BestAskPrice):
            quotes = Subscription(DataType.Level1, self.Security)
            quotes.MarketData.BuildField = field
            self.SubscribeLevel1(quotes).Bind(self._observe_protection_quote).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, sma)
            self.DrawOwnTrades(area)
            oscillators = self.CreateChartArea()
            if oscillators is not None:
                self.DrawIndicator(oscillators, stochastic)

    def _observe_protection_quote(self, quote):
        # The high-level handler activates native protection before the callback, including between bars.
        pass

    def _process_candle(self, candle, sma_value, stochastic_value):
        if candle.State != CandleStates.Finished:
            return

        if not sma_value.IsFormed or not stochastic_value.IsFormed or stochastic_value.K is None:
            return

        ma = sma_value.GetValue[Decimal](None)
        k = stochastic_value.K
        close = candle.ClosePrice
        prev_k = self._prev_k
        self._prev_k = k

        uptrend = close > ma
        downtrend = close < ma
        signal = 0

        if prev_k is not None:
            if self._setup == 1 and uptrend and k > prev_k:
                signal = 1
            elif self._setup == -1 and downtrend and k < prev_k:
                signal = -1

        if signal != 0:
            self._setup = 0

        if uptrend and k < Decimal(self._stoch_oversold.Value):
            self._setup = 1
        elif downtrend and k > Decimal(self._stoch_overbought.Value):
            self._setup = -1
        elif (self._setup == 1 and not uptrend) or (self._setup == -1 and not downtrend):
            self._setup = 0

        if signal == 0 or not self.IsFormedAndOnlineAndAllowTrading():
            return

        if signal > 0 and self.Position <= 0:
            self.BuyMarket(self.Volume + abs(self.Position))
        elif signal < 0 and self.Position >= 0:
            self.SellMarket(self.Volume + abs(self.Position))

    def CreateClone(self):
        return ma_stochastic_strategy()
