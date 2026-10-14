import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")
clr.AddReference("StockSharp.BusinessEntities")

from System import TimeSpan, Decimal
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes, Level1Fields
from StockSharp.BusinessEntities import Subscription
from StockSharp.Algo.Indicators import DonchianChannels, MovingAverageConvergenceDivergenceSignal
from StockSharp.Algo.Strategies import Strategy

class donchian_macd_strategy(Strategy):
    """
    Donchian MACD strategy.
    The channel spans the highest high and lowest low of the previous DonchianPeriod candles. A close above it with MACD above its signal
    line goes long and a close below it with MACD below the signal line goes short, reversing an opposite position. A long closes when MACD
    crosses below the signal line and a short when it crosses above it, and a percent stop limits the loss.
    """

    def __init__(self):
        super(donchian_macd_strategy, self).__init__()
        self._donchian_period = self.Param("DonchianPeriod", 20).SetGreaterThanZero().SetDisplay("Donchian Period", "Previous candles the channel spans", "Indicators")
        self._macd_fast = self.Param("MacdFast", 12).SetGreaterThanZero().SetDisplay("MACD Fast", "Fast EMA period of MACD", "MACD")
        self._macd_slow = self.Param("MacdSlow", 26).SetGreaterThanZero().SetDisplay("MACD Slow", "Slow EMA period of MACD", "MACD")
        self._macd_signal = self.Param("MacdSignal", 9).SetGreaterThanZero().SetDisplay("MACD Signal", "Signal line period of MACD", "MACD")
        self._stop_loss_percent = self.Param("StopLossPercent", 2.0).SetNotNegative().SetDisplay("Stop Loss %", "Stop loss percentage from entry price", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._reset_state()

    @property
    def candle_type(self):
        return self._candle_type.Value

    def _reset_state(self):
        self._prev_upper = None
        self._prev_lower = None

    def OnReseted(self):
        super(donchian_macd_strategy, self).OnReseted()
        self._reset_state()

    def OnStarted2(self, time):
        super(donchian_macd_strategy, self).OnStarted2(time)

        self._reset_state()

        donchian = DonchianChannels()
        donchian.Length = self._donchian_period.Value
        macd = MovingAverageConvergenceDivergenceSignal()
        macd.Macd.ShortMa.Length = self._macd_fast.Value
        macd.Macd.LongMa.Length = self._macd_slow.Value
        macd.SignalMa.Length = self._macd_signal.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(donchian, macd, self._process_candle).Start()

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
                self.DrawIndicator(oscillators, macd)

    def _observe_protection_quote(self, quote):
        # The high-level handler activates native protection before the callback, including between bars.
        pass

    def _process_candle(self, candle, donchian_value, macd_value):
        if candle.State != CandleStates.Finished:
            return

        # The channel is measured on the candles before this one.
        upper = self._prev_upper
        lower = self._prev_lower

        if donchian_value.IsFormed and donchian_value.UpperBand is not None and donchian_value.LowerBand is not None:
            self._prev_upper = donchian_value.UpperBand
            self._prev_lower = donchian_value.LowerBand

        if not macd_value.IsFormed or upper is None or lower is None:
            return
        if macd_value.Macd is None or macd_value.Signal is None:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        macd = macd_value.Macd
        signal = macd_value.Signal
        close = candle.ClosePrice

        if close > upper and macd > signal and self.Position <= 0:
            self.BuyMarket(self.Volume + abs(self.Position))
        elif close < lower and macd < signal and self.Position >= 0:
            self.SellMarket(self.Volume + abs(self.Position))
        elif self.Position > 0 and macd < signal:
            self.SellMarket(self.Position)
        elif self.Position < 0 and macd > signal:
            self.BuyMarket(-self.Position)

    def CreateClone(self):
        return donchian_macd_strategy()
