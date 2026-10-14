import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")
clr.AddReference("StockSharp.BusinessEntities")

from System import TimeSpan, Decimal
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes, Level1Fields
from StockSharp.BusinessEntities import Subscription
from StockSharp.Algo.Indicators import StochasticK, RelativeStrengthIndex, MoneyFlowIndex, SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy
from indicator_extensions import *


class OscillatorChoice:
    """Oscillator choices."""
    Stochastic = 0
    Rsi = 1
    Mfi = 2


class chart_oscillator_strategy(Strategy):
    """
    Chart Oscillator strategy.
    Trades a selectable oscillator. With Stochastic, a %K cross above %D while %K is below Oversold buys and a %K cross below %D
    while %K is above Overbought sells. With RSI or MFI, a reading below Oversold buys and a reading above Overbought sells.
    An opposite signal reverses the position and a percent stop limits the loss.
    """

    def __init__(self):
        super(chart_oscillator_strategy, self).__init__()
        self._choice = self.Param("Choice", OscillatorChoice.Stochastic).SetDisplay("Oscillator", "Oscillator used for signals", "Indicators")
        self._length = self.Param("Length", 14).SetGreaterThanZero().SetDisplay("Length", "Period of RSI and MFI", "Indicators")
        self._k_period = self.Param("KPeriod", 14).SetGreaterThanZero().SetDisplay("%K Period", "Stochastic %K lookback", "Indicators")
        self._d_period = self.Param("DPeriod", 3).SetGreaterThanZero().SetDisplay("%D Period", "Stochastic %D smoothing", "Indicators")
        self._smooth_k = self.Param("SmoothK", 3).SetGreaterThanZero().SetDisplay("Smooth %K", "Stochastic %K smoothing", "Indicators")
        self._overbought = self.Param("Overbought", 80.0).SetDisplay("Overbought", "Overbought level", "Signals")
        self._oversold = self.Param("Oversold", 20.0).SetDisplay("Oversold", "Oversold level", "Signals")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._stop_loss_percent = self.Param("StopLossPercent", 2.0).SetNotNegative().SetDisplay("Stop Loss %", "Stop loss percentage from entry price", "Risk")
        self._k_smoother = None
        self._d_average = None
        self._reset_state()

    @property
    def candle_type(self):
        return self._candle_type.Value

    def _reset_state(self):
        self._prev_k = None
        self._prev_d = None

    def OnReseted(self):
        super(chart_oscillator_strategy, self).OnReseted()
        self._k_smoother = None
        self._d_average = None
        self._reset_state()

    def OnStarted2(self, time):
        super(chart_oscillator_strategy, self).OnStarted2(time)

        self._reset_state()

        raw_k = StochasticK()
        raw_k.Length = self._k_period.Value
        rsi = RelativeStrengthIndex()
        rsi.Length = self._length.Value
        mfi = MoneyFlowIndex()
        mfi.Length = self._length.Value
        self._k_smoother = SimpleMovingAverage()
        self._k_smoother.Length = self._smooth_k.Value
        self._d_average = SimpleMovingAverage()
        self._d_average.Length = self._d_period.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(raw_k, rsi, mfi, self._process_candle).Start()

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
                choice = self._choice.Value
                if choice == OscillatorChoice.Rsi:
                    self.DrawIndicator(oscillators, rsi)
                elif choice == OscillatorChoice.Mfi:
                    self.DrawIndicator(oscillators, mfi)
                else:
                    self.DrawIndicator(oscillators, raw_k)

    def _observe_protection_quote(self, quote):
        # The high-level handler activates native protection before the callback, including between bars.
        pass

    def _process_candle(self, candle, raw_k_value, rsi_value, mfi_value):
        if candle.State != CandleStates.Finished:
            return

        choice = self._choice.Value
        overbought = Decimal(self._overbought.Value)
        oversold = Decimal(self._oversold.Value)

        if choice == OscillatorChoice.Rsi or choice == OscillatorChoice.Mfi:
            value = rsi_value if choice == OscillatorChoice.Rsi else mfi_value
            if not value.IsFormed:
                return
            oscillator = value.GetValue[Decimal](None)
            buy = oscillator < oversold
            sell = oscillator > overbought
        else:
            if not raw_k_value.IsFormed:
                return
            k_result = process_value(self._k_smoother, raw_k_value.GetValue[Decimal](None), candle.OpenTime, True)
            if not k_result.IsFormed:
                return
            k = k_result.GetValue[Decimal](None)
            d_result = process_value(self._d_average, k, candle.OpenTime, True)
            if not d_result.IsFormed:
                return
            d = d_result.GetValue[Decimal](None)
            prev_k = self._prev_k
            prev_d = self._prev_d
            self._prev_k = k
            self._prev_d = d
            if prev_k is None or prev_d is None:
                return
            buy = prev_k <= prev_d and k > d and k < oversold
            sell = prev_k >= prev_d and k < d and k > overbought

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        if buy and self.Position <= 0:
            self.BuyMarket(self.Volume + abs(self.Position))
        elif sell and self.Position >= 0:
            self.SellMarket(self.Volume + abs(self.Position))

    def CreateClone(self):
        return chart_oscillator_strategy()
