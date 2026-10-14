import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")
clr.AddReference("StockSharp.BusinessEntities")

from System import TimeSpan, Decimal
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes, Level1Fields
from StockSharp.BusinessEntities import Subscription
from StockSharp.Algo.Indicators import StochasticOscillator
from StockSharp.Algo.Strategies import Strategy

class vwap_stochastic_strategy(Strategy):
    """
    VWAP Stochastic strategy.
    The market trades around the clock, so the session VWAP restarts with each UTC day and weighs each candle's typical price by its volume.
    A close below VWAP with %K below OversoldLevel goes long and a close above VWAP with %K above OverboughtLevel goes short, reversing
    an opposite position; %K is the stochastic over StochPeriod candles smoothed over StochKPeriod candles. A long closes above VWAP
    and a short below it, and a percent stop limits the loss.
    """

    def __init__(self):
        super(vwap_stochastic_strategy, self).__init__()
        self._stoch_period = self.Param("StochPeriod", 14).SetGreaterThanZero().SetDisplay("Stochastic Period", "Lookback period of the raw stochastic", "Stochastic")
        self._stoch_k_period = self.Param("StochKPeriod", 3).SetGreaterThanZero().SetDisplay("Stochastic %K", "Smoothing period of %K", "Stochastic")
        self._overbought_level = self.Param("OverboughtLevel", 80.0).SetDisplay("Overbought Level", "%K level for shorts", "Stochastic")
        self._oversold_level = self.Param("OversoldLevel", 20.0).SetDisplay("Oversold Level", "%K level for longs", "Stochastic")
        self._stop_loss_percent = self.Param("StopLossPercent", 2.0).SetNotNegative().SetDisplay("Stop Loss %", "Stop loss percentage from entry price", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._reset_state()

    @property
    def candle_type(self):
        return self._candle_type.Value

    def _reset_state(self):
        self._day = None
        self._cumulative_price_volume = Decimal(0)
        self._cumulative_volume = Decimal(0)

    def OnReseted(self):
        super(vwap_stochastic_strategy, self).OnReseted()
        self._reset_state()

    def OnStarted2(self, time):
        super(vwap_stochastic_strategy, self).OnStarted2(time)

        self._reset_state()

        # The D line of the core oscillator is the smoothed %K.
        stochastic = StochasticOscillator()
        stochastic.K.Length = self._stoch_period.Value
        stochastic.D.Length = self._stoch_k_period.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(stochastic, self._process_candle).Start()

        self.StartProtection(Unit(), Unit(Decimal(self._stop_loss_percent.Value), UnitTypes.Percent), useMarketOrders=True, isLocalStop=True)

        # The stop has to see prices between candles, not only at their close.
        for field in (Level1Fields.BestBidPrice, Level1Fields.BestAskPrice):
            quotes = Subscription(DataType.Level1, self.Security)
            quotes.MarketData.BuildField = field
            self.SubscribeLevel1(quotes).Bind(self._observe_protection_quote).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)
            oscillators = self.CreateChartArea()
            if oscillators is not None:
                self.DrawIndicator(oscillators, stochastic)

    def _observe_protection_quote(self, quote):
        # The high-level handler activates native protection before the callback, including between bars.
        pass

    def _process_candle(self, candle, stochastic_value):
        if candle.State != CandleStates.Finished:
            return

        day = candle.OpenTime.Date
        if self._day is None or self._day != day:
            self._day = day
            self._cumulative_price_volume = Decimal(0)
            self._cumulative_volume = Decimal(0)

        typical_price = (candle.HighPrice + candle.LowPrice + candle.ClosePrice) / Decimal(3)
        self._cumulative_price_volume += typical_price * candle.TotalVolume
        self._cumulative_volume += candle.TotalVolume

        if not stochastic_value.IsFormed or self._cumulative_volume <= 0 or stochastic_value.D is None:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        vwap = self._cumulative_price_volume / self._cumulative_volume
        k = stochastic_value.D
        close = candle.ClosePrice

        if close < vwap and k < Decimal(self._oversold_level.Value) and self.Position <= 0:
            self.BuyMarket(self.Volume + abs(self.Position))
        elif close > vwap and k > Decimal(self._overbought_level.Value) and self.Position >= 0:
            self.SellMarket(self.Volume + abs(self.Position))
        elif self.Position > 0 and close > vwap:
            self.SellMarket(self.Position)
        elif self.Position < 0 and close < vwap:
            self.BuyMarket(-self.Position)

    def CreateClone(self):
        return vwap_stochastic_strategy()
