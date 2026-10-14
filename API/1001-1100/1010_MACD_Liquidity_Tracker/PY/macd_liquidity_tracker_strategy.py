import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Decimal, DateTimeOffset
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import MovingAverageConvergenceDivergenceSignal
from StockSharp.Algo.Strategies import Strategy


class macd_liquidity_tracker_strategy(Strategy):
    """
    MACD Liquidity Tracker strategy.
    The MACD colour state produces the signals. SystemType selects the sensitivity: "Fast" follows the histogram direction,
    "Normal" follows MACD above or below its signal line, "Safe" also requires MACD on the same side of zero and "Crossover"
    signals only on the bar where MACD crosses its signal line. A bullish signal goes long, a bearish signal closes the long and,
    when AllowShortTrades is enabled, goes short. Trades are taken between StartDate and EndDate, with optional percent stop loss
    and take profit.
    """

    def __init__(self):
        super(macd_liquidity_tracker_strategy, self).__init__()
        self._fast_length = self.Param("FastLength", 25).SetGreaterThanZero().SetDisplay("Fast Length", "MACD fast EMA length", "MACD")
        self._slow_length = self.Param("SlowLength", 60).SetGreaterThanZero().SetDisplay("Slow Length", "MACD slow EMA length", "MACD")
        self._signal_length = self.Param("SignalLength", 220).SetGreaterThanZero().SetDisplay("Signal Length", "MACD signal line length", "MACD")
        self._allow_short_trades = self.Param("AllowShortTrades", False).SetDisplay("Allow Short Trades", "Allow short positions", "Trading")
        self._system_type = self.Param("SystemType", "Normal").SetDisplay("System Type", "Signal mode: Fast, Normal, Safe or Crossover", "Trading")
        self._use_stop_loss = self.Param("UseStopLoss", False).SetDisplay("Use Stop Loss", "Enable stop loss", "Risk")
        self._stop_loss_percent = self.Param("StopLossPercent", 3.0).SetNotNegative().SetDisplay("Stop Loss %", "Stop loss percentage", "Risk")
        self._use_take_profit = self.Param("UseTakeProfit", False).SetDisplay("Use Take Profit", "Enable take profit", "Risk")
        self._take_profit_percent = self.Param("TakeProfitPercent", 6.0).SetNotNegative().SetDisplay("Take Profit %", "Take profit percentage", "Risk")
        self._start_date = self.Param("StartDate", DateTimeOffset(2018, 1, 1, 0, 0, 0, TimeSpan.Zero)).SetDisplay("Start Date", "First date trades are allowed", "Time")
        self._end_date = self.Param("EndDate", DateTimeOffset(2069, 12, 31, 23, 59, 0, TimeSpan.Zero)).SetDisplay("End Date", "Last date trades are allowed", "Time")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._reset_state()

    @property
    def candle_type(self):
        return self._candle_type.Value

    def _reset_state(self):
        self._prev_macd = None
        self._prev_signal = None

    def OnReseted(self):
        super(macd_liquidity_tracker_strategy, self).OnReseted()
        self._reset_state()

    def OnStarted2(self, time):
        super(macd_liquidity_tracker_strategy, self).OnStarted2(time)

        self._reset_state()

        macd = MovingAverageConvergenceDivergenceSignal()
        macd.Macd.ShortMa.Length = self._fast_length.Value
        macd.Macd.LongMa.Length = self._slow_length.Value
        macd.SignalMa.Length = self._signal_length.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(macd, self._process_candle).Start()

        use_sl = self._use_stop_loss.Value
        use_tp = self._use_take_profit.Value
        if use_sl or use_tp:
            tp = Unit(Decimal(self._take_profit_percent.Value), UnitTypes.Percent) if use_tp else Unit()
            sl = Unit(Decimal(self._stop_loss_percent.Value), UnitTypes.Percent) if use_sl else Unit()
            self.StartProtection(tp, sl, useMarketOrders=True)

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)
            oscillators = self.CreateChartArea()
            if oscillators is not None:
                self.DrawIndicator(oscillators, macd)

    def _process_candle(self, candle, macd_value):
        if candle.State != CandleStates.Finished:
            return

        if not macd_value.IsFormed:
            return

        macd_line = macd_value.Macd
        signal_line = macd_value.Signal
        if macd_line is None or signal_line is None:
            return

        pm = self._prev_macd
        ps = self._prev_signal
        self._prev_macd = macd_line
        self._prev_signal = signal_line

        if pm is None or ps is None:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        if candle.OpenTime < self._start_date.Value.UtcDateTime or candle.OpenTime > self._end_date.Value.UtcDateTime:
            return

        zero = Decimal(0)
        histogram = macd_line - signal_line
        prev_histogram = pm - ps
        mode = str(self._system_type.Value or "").strip().lower()

        if mode == "fast":
            bullish = histogram > prev_histogram
            bearish = histogram < prev_histogram
        elif mode == "safe":
            bullish = macd_line > signal_line and macd_line > zero
            bearish = macd_line < signal_line and macd_line < zero
        elif mode == "crossover":
            bullish = pm <= ps and macd_line > signal_line
            bearish = pm >= ps and macd_line < signal_line
        else:
            bullish = macd_line > signal_line
            bearish = macd_line < signal_line

        if bullish and self.Position <= 0:
            self.BuyMarket(self.Volume + abs(self.Position))
        elif bearish:
            if self._allow_short_trades.Value and self.Position >= 0:
                self.SellMarket(self.Volume + self.Position)
            elif self.Position > 0:
                self.SellMarket(self.Position)

    def CreateClone(self):
        return macd_liquidity_tracker_strategy()
