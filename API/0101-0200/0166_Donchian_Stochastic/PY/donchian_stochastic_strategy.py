import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")
clr.AddReference("StockSharp.BusinessEntities")

from System import TimeSpan, Decimal
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes, Level1Fields
from StockSharp.BusinessEntities import Subscription
from StockSharp.Algo.Indicators import DonchianChannels, StochasticOscillator
from StockSharp.Algo.Strategies import Strategy

class donchian_stochastic_strategy(Strategy):
    """
    Donchian Stochastic strategy.
    The channel spans the highest high and lowest low of the previous DonchianPeriod candles. A close above it with %K above StochOverbought
    confirms momentum and goes long, a close below it with %K below StochOversold goes short, reversing an opposite position;
    %K is the stochastic over StochPeriod candles smoothed over StochK candles.
    The breakout fails, closing the position, when price closes back beyond the broken level, and a percent stop limits the loss.
    """

    def __init__(self):
        super(donchian_stochastic_strategy, self).__init__()
        self._donchian_period = self.Param("DonchianPeriod", 20).SetGreaterThanZero().SetDisplay("Donchian Period", "Previous candles the channel spans", "Indicators")
        self._stoch_period = self.Param("StochPeriod", 14).SetGreaterThanZero().SetDisplay("Stochastic Period", "Lookback period of the raw stochastic", "Stochastic")
        self._stoch_k = self.Param("StochK", 3).SetGreaterThanZero().SetDisplay("Stochastic %K", "Smoothing period of %K", "Stochastic")
        self._stoch_overbought = self.Param("StochOverbought", 80.0).SetDisplay("Stochastic Overbought", "%K level that confirms an upside breakout", "Stochastic")
        self._stoch_oversold = self.Param("StochOversold", 20.0).SetDisplay("Stochastic Oversold", "%K level that confirms a downside breakout", "Stochastic")
        self._stop_loss_percent = self.Param("StopLossPercent", 2.0).SetNotNegative().SetDisplay("Stop Loss %", "Stop loss percentage from entry price", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._reset_state()

    @property
    def candle_type(self):
        return self._candle_type.Value

    def _reset_state(self):
        self._prev_upper = None
        self._prev_lower = None
        self._breakout_level = Decimal(0)

    def OnReseted(self):
        super(donchian_stochastic_strategy, self).OnReseted()
        self._reset_state()

    def OnStarted2(self, time):
        super(donchian_stochastic_strategy, self).OnStarted2(time)

        self._reset_state()

        donchian = DonchianChannels()
        donchian.Length = self._donchian_period.Value
        # The D line of the core oscillator is the smoothed %K.
        stochastic = StochasticOscillator()
        stochastic.K.Length = self._stoch_period.Value
        stochastic.D.Length = self._stoch_k.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(donchian, stochastic, self._process_candle).Start()

        self.StartProtection(Unit(), Unit(Decimal(self._stop_loss_percent.Value), UnitTypes.Percent), useMarketOrders=True, isLocalStop=True)

        # The stop has to see prices between candles, not only at their close.
        for field in (Level1Fields.BestBidPrice, Level1Fields.BestAskPrice):
            quotes = Subscription(DataType.Level1, self.Security)
            quotes.MarketData.BuildField = field
            self.SubscribeLevel1(quotes).Bind(self._observe_protection_quote).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, donchian)
            self.DrawOwnTrades(area)
            oscillators = self.CreateChartArea()
            if oscillators is not None:
                self.DrawIndicator(oscillators, stochastic)

    def _observe_protection_quote(self, quote):
        # The high-level handler activates native protection before the callback, including between bars.
        pass

    def _process_candle(self, candle, donchian_value, stochastic_value):
        if candle.State != CandleStates.Finished:
            return

        # The channel is measured on the candles before this one.
        upper = self._prev_upper
        lower = self._prev_lower

        if donchian_value.IsFormed and donchian_value.UpperBand is not None and donchian_value.LowerBand is not None:
            self._prev_upper = donchian_value.UpperBand
            self._prev_lower = donchian_value.LowerBand

        if not stochastic_value.IsFormed or stochastic_value.D is None or upper is None or lower is None:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        k = stochastic_value.D
        close = candle.ClosePrice

        if close > upper and k > Decimal(self._stoch_overbought.Value) and self.Position <= 0:
            self.BuyMarket(self.Volume + abs(self.Position))
            self._breakout_level = upper
        elif close < lower and k < Decimal(self._stoch_oversold.Value) and self.Position >= 0:
            self.SellMarket(self.Volume + abs(self.Position))
            self._breakout_level = lower
        elif self.Position > 0 and close < self._breakout_level:
            self.SellMarket(self.Position)
        elif self.Position < 0 and close > self._breakout_level:
            self.BuyMarket(-self.Position)

    def CreateClone(self):
        return donchian_stochastic_strategy()
