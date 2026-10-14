import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")
clr.AddReference("StockSharp.BusinessEntities")

from System import TimeSpan, Decimal
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes, Level1Fields
from StockSharp.BusinessEntities import Subscription
from StockSharp.Algo.Indicators import (SimpleMovingAverage, AverageTrueRange, RelativeStrengthIndex,
    MovingAverageConvergenceDivergenceSignal, DecimalIndicatorValue)
from StockSharp.Algo.Strategies import Strategy


class bonk_long_volatility_strategy(Strategy):
    """
    BONK Long Volatility strategy.
    Long only: buys when SMA(MaFastLength) is above SMA(MaSlowLength), the candle range exceeds AtrMultiplier times ATR(AtrLength),
    RSI lies between RsiOversold and RsiOverbought, the MACD line is above its signal and above zero, volume exceeds VolumeThreshold
    times its VolumeSmaLength average, the close is above the fast SMA and the candle is no older than LookbackDays. The long closes on
    the percent take profit or stop loss, or when the close falls below a trailing stop AtrMultiplier ATRs under the highest close
    since entry.
    """

    def __init__(self):
        super(bonk_long_volatility_strategy, self).__init__()
        self._profit_target_percent = self.Param("ProfitTargetPercent", 5.0).SetNotNegative().SetDisplay("Profit Target %", "Take profit percentage from entry price", "Risk")
        self._stop_loss_percent = self.Param("StopLossPercent", 3.0).SetNotNegative().SetDisplay("Stop Loss %", "Stop loss percentage from entry price", "Risk")
        self._atr_length = self.Param("AtrLength", 10).SetGreaterThanZero().SetDisplay("ATR Length", "ATR period", "Volatility")
        self._atr_multiplier = self.Param("AtrMultiplier", 1.5).SetGreaterThanZero().SetDisplay("ATR Multiplier", "ATR multiple of the range filter and the trailing stop", "Volatility")
        self._rsi_length = self.Param("RsiLength", 14).SetGreaterThanZero().SetDisplay("RSI Length", "RSI period", "Momentum")
        self._rsi_overbought = self.Param("RsiOverbought", 65.0).SetDisplay("RSI Overbought", "Upper RSI bound for entries", "Momentum")
        self._rsi_oversold = self.Param("RsiOversold", 35.0).SetDisplay("RSI Oversold", "Lower RSI bound for entries", "Momentum")
        self._macd_fast = self.Param("MacdFast", 12).SetGreaterThanZero().SetDisplay("MACD Fast", "Fast EMA period of MACD", "Momentum")
        self._macd_slow = self.Param("MacdSlow", 26).SetGreaterThanZero().SetDisplay("MACD Slow", "Slow EMA period of MACD", "Momentum")
        self._macd_signal = self.Param("MacdSignal", 9).SetGreaterThanZero().SetDisplay("MACD Signal", "Signal line period of MACD", "Momentum")
        self._volume_sma_length = self.Param("VolumeSmaLength", 20).SetGreaterThanZero().SetDisplay("Volume SMA Length", "Candles of the average volume", "Volume")
        self._volume_threshold = self.Param("VolumeThreshold", 1.5).SetNotNegative().SetDisplay("Volume Threshold", "Multiple of the average volume the candle volume has to exceed", "Volume")
        self._ma_fast_length = self.Param("MaFastLength", 5).SetGreaterThanZero().SetDisplay("Fast MA Length", "Fast SMA period", "Trend")
        self._ma_slow_length = self.Param("MaSlowLength", 13).SetGreaterThanZero().SetDisplay("Slow MA Length", "Slow SMA period", "Trend")
        self._lookback_days = self.Param("LookbackDays", 30).SetGreaterThanZero().SetDisplay("Lookback Days", "Maximum age of a signal candle in days", "General")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(1))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._volume_sma = None
        self._highest_close = None

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(bonk_long_volatility_strategy, self).OnReseted()
        self._volume_sma = None
        self._highest_close = None

    def OnStarted2(self, time):
        super(bonk_long_volatility_strategy, self).OnStarted2(time)

        self._highest_close = None
        self._volume_sma = SimpleMovingAverage()
        self._volume_sma.Length = self._volume_sma_length.Value

        fast_ma = SimpleMovingAverage()
        fast_ma.Length = self._ma_fast_length.Value
        slow_ma = SimpleMovingAverage()
        slow_ma.Length = self._ma_slow_length.Value
        atr = AverageTrueRange()
        atr.Length = self._atr_length.Value
        rsi = RelativeStrengthIndex()
        rsi.Length = self._rsi_length.Value
        macd = MovingAverageConvergenceDivergenceSignal()
        macd.Macd.ShortMa.Length = self._macd_fast.Value
        macd.Macd.LongMa.Length = self._macd_slow.Value
        macd.SignalMa.Length = self._macd_signal.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(fast_ma, slow_ma, atr, rsi, macd, self._process_candle).Start()

        self.StartProtection(Unit(Decimal(self._profit_target_percent.Value), UnitTypes.Percent), Unit(Decimal(self._stop_loss_percent.Value), UnitTypes.Percent), useMarketOrders=True, isLocalStop=True)

        # The stop has to see prices between candles, not only at their close.
        for field in (Level1Fields.BestBidPrice, Level1Fields.BestAskPrice):
            quotes = Subscription(DataType.Level1, self.Security)
            quotes.MarketData.BuildField = field
            self.SubscribeLevel1(quotes).Bind(self._observe_protection_quote).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, fast_ma)
            self.DrawIndicator(area, slow_ma)
            self.DrawOwnTrades(area)
            oscillators = self.CreateChartArea()
            if oscillators is not None:
                self.DrawIndicator(oscillators, rsi)
                self.DrawIndicator(oscillators, macd)

    def _observe_protection_quote(self, quote):
        # The high-level handler activates native protection before the callback, including between bars.
        pass

    def _process_candle(self, candle, fast_value, slow_value, atr_value, rsi_value, macd_value):
        if candle.State != CandleStates.Finished:
            return

        volume_input = DecimalIndicatorValue(self._volume_sma, candle.TotalVolume, candle.OpenTime)
        volume_input.IsFinal = True
        volume_value = self._volume_sma.Process(volume_input)

        if not fast_value.IsFormed or not slow_value.IsFormed or not atr_value.IsFormed or not rsi_value.IsFormed or not macd_value.IsFormed or not volume_value.IsFormed:
            return

        macd = macd_value.Macd
        signal = macd_value.Signal
        if macd is None or signal is None:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        close = candle.ClosePrice
        atr = atr_value.GetValue[Decimal](None)
        multiplier = Decimal(self._atr_multiplier.Value)

        if self.Position > 0:
            if self._highest_close is None or close > self._highest_close:
                self._highest_close = close
            if close < self._highest_close - atr * multiplier:
                self.SellMarket(self.Position)
                self._highest_close = None
            return

        self._highest_close = None

        fast = fast_value.GetValue[Decimal](None)
        slow = slow_value.GetValue[Decimal](None)
        rsi = rsi_value.GetValue[Decimal](None)
        recent = candle.OpenTime >= self.CurrentTime.AddDays(-self._lookback_days.Value)

        if (self.Position == 0
                and fast > slow
                and candle.HighPrice - candle.LowPrice > atr * multiplier
                and rsi > Decimal(self._rsi_oversold.Value) and rsi < Decimal(self._rsi_overbought.Value)
                and macd > signal and macd > 0
                and candle.TotalVolume > volume_value.GetValue[Decimal](None) * Decimal(self._volume_threshold.Value)
                and close > fast
                and recent):
            self.BuyMarket(self.Volume)
            self._highest_close = close

    def CreateClone(self):
        return bonk_long_volatility_strategy()
