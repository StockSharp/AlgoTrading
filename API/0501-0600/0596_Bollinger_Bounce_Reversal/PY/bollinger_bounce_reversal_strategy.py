import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")
clr.AddReference("StockSharp.BusinessEntities")

from System import TimeSpan, Decimal
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes, Level1Fields
from StockSharp.BusinessEntities import Subscription
from StockSharp.Algo.Indicators import BollingerBands, MovingAverageConvergenceDivergenceSignal, SimpleMovingAverage, DecimalIndicatorValue
from StockSharp.Algo.Strategies import Strategy


class bollinger_bounce_reversal_strategy(Strategy):
    """
    Bollinger Bounce Reversal strategy.
    A close back above the lower Bollinger band after a close below it, with MACD above its signal line and volume at least
    VolumeFactor times its VolumePeriod average, goes long; a close back below the upper band after a close above it, with MACD
    below the signal and the same volume condition, goes short, reversing an opposite position. At most MaxTradesPerDay entries
    are made per UTC day, and percent stop loss and take profit close the position.
    """

    def __init__(self):
        super(bollinger_bounce_reversal_strategy, self).__init__()
        self._bollinger_period = self.Param("BollingerPeriod", 20).SetGreaterThanZero().SetDisplay("Bollinger Period", "Bollinger period", "Bollinger")
        self._bb_std_dev = self.Param("BbStdDev", 2.0).SetGreaterThanZero().SetDisplay("BB StdDev", "Bollinger standard deviation multiplier", "Bollinger")
        self._macd_fast_length = self.Param("MacdFastLength", 12).SetGreaterThanZero().SetDisplay("MACD Fast", "Fast EMA period of MACD", "MACD")
        self._macd_slow_length = self.Param("MacdSlowLength", 26).SetGreaterThanZero().SetDisplay("MACD Slow", "Slow EMA period of MACD", "MACD")
        self._macd_signal_length = self.Param("MacdSignalLength", 9).SetGreaterThanZero().SetDisplay("MACD Signal", "Signal line period of MACD", "MACD")
        self._volume_period = self.Param("VolumePeriod", 20).SetGreaterThanZero().SetDisplay("Volume Period", "Candles of the average volume", "Volume")
        self._volume_factor = self.Param("VolumeFactor", 1.0).SetNotNegative().SetDisplay("Volume Factor", "Multiple of the average volume the candle volume has to reach", "Volume")
        self._stop_loss_percent = self.Param("StopLossPercent", 2.0).SetNotNegative().SetDisplay("Stop Loss %", "Stop loss percentage from entry price", "Risk")
        self._take_profit_percent = self.Param("TakeProfitPercent", 4.0).SetNotNegative().SetDisplay("Take Profit %", "Take profit percentage from entry price", "Risk")
        self._max_trades_per_day = self.Param("MaxTradesPerDay", 5).SetGreaterThanZero().SetDisplay("Max Trades Per Day", "Maximum entries per UTC day", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._reset_state()

    @property
    def candle_type(self):
        return self._candle_type.Value

    def _reset_state(self):
        self._volume_average = None
        self._prev_close = None
        self._prev_upper = None
        self._prev_lower = None
        self._trade_day = None
        self._trades_today = 0

    def OnReseted(self):
        super(bollinger_bounce_reversal_strategy, self).OnReseted()
        self._reset_state()

    def OnStarted2(self, time):
        super(bollinger_bounce_reversal_strategy, self).OnStarted2(time)

        self._reset_state()

        self._volume_average = SimpleMovingAverage()
        self._volume_average.Length = self._volume_period.Value
        bollinger = BollingerBands()
        bollinger.Length = self._bollinger_period.Value
        bollinger.Width = Decimal(self._bb_std_dev.Value)
        macd = MovingAverageConvergenceDivergenceSignal()
        macd.Macd.ShortMa.Length = self._macd_fast_length.Value
        macd.Macd.LongMa.Length = self._macd_slow_length.Value
        macd.SignalMa.Length = self._macd_signal_length.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(bollinger, macd, self._process_candle).Start()

        self.StartProtection(Unit(Decimal(self._take_profit_percent.Value), UnitTypes.Percent), Unit(Decimal(self._stop_loss_percent.Value), UnitTypes.Percent), useMarketOrders=True, isLocalStop=True)

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
                self.DrawIndicator(oscillators, macd)

    def _observe_protection_quote(self, quote):
        # The high-level handler activates native protection before the callback, including between bars.
        pass

    def _process_candle(self, candle, bollinger_value, macd_value):
        if candle.State != CandleStates.Finished:
            return

        volume_input = DecimalIndicatorValue(self._volume_average, candle.TotalVolume, candle.OpenTime)
        volume_input.IsFinal = True
        volume_value = self._volume_average.Process(volume_input)

        if not bollinger_value.IsFormed:
            return

        upper = bollinger_value.UpBand
        lower = bollinger_value.LowBand
        if upper is None or lower is None:
            return

        close = candle.ClosePrice
        pc = self._prev_close
        pu = self._prev_upper
        pl = self._prev_lower
        self._prev_close = close
        self._prev_upper = upper
        self._prev_lower = lower

        if not macd_value.IsFormed or not volume_value.IsFormed or pc is None or pu is None or pl is None:
            return

        macd = macd_value.Macd
        signal = macd_value.Signal
        if macd is None or signal is None:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        day = candle.OpenTime.Date
        if day != self._trade_day:
            self._trade_day = day
            self._trades_today = 0

        if self._trades_today >= self._max_trades_per_day.Value:
            return

        volume_ok = candle.TotalVolume >= volume_value.GetValue[Decimal](None) * Decimal(self._volume_factor.Value)

        if pc < pl and close > lower and macd > signal and volume_ok and self.Position <= 0:
            self.BuyMarket(self.Volume + abs(self.Position))
            self._trades_today += 1
        elif pc > pu and close < upper and macd < signal and volume_ok and self.Position >= 0:
            self.SellMarket(self.Volume + abs(self.Position))
            self._trades_today += 1

    def CreateClone(self):
        return bollinger_bounce_reversal_strategy()
