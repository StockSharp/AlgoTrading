import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import RelativeStrengthIndex, Lowest
from StockSharp.Algo.Strategies import Strategy

RSI_LENGTH = 14


class mutanabby_ai_algo_pro_strategy(Strategy):
    """
    Mutanabby AI Algo Pro strategy.
    Long only. Enters on a bullish engulfing candle whose body is at least CandleStabilityIndex of its range, while RSI is below
    RsiIndex and the close is below the close CandleDeltaLength bars ago. Exits on a bearish engulfing candle or when the stop is hit.
    The stop is either EntryStopLossPercent below the entry price or StopLossBufferPercent below the lowest low of LookbackPeriod bars.
    StopLossMethod: EntryPriceBased or LowestLowBased.
    """

    def __init__(self):
        super(mutanabby_ai_algo_pro_strategy, self).__init__()
        self._candle_stability_index = self.Param("CandleStabilityIndex", 0.5).SetRange(0.0, 1.0).SetDisplay("Candle Stability Index", "Minimum body/true range ratio", "Technical")
        self._rsi_index = self.Param("RsiIndex", 50).SetRange(0, 100).SetDisplay("RSI Index", "RSI threshold for entries", "Technical")
        self._candle_delta_length = self.Param("CandleDeltaLength", 5).SetRange(1, 50).SetDisplay("Candle Delta Length", "Bars for price comparison", "Technical")
        self._disable_repeating_signals = self.Param("DisableRepeatingSignals", False).SetDisplay("Disable Repeating Signals", "Avoid consecutive identical signals", "Technical")
        self._enable_stop_loss = self.Param("EnableStopLoss", True).SetDisplay("Enable Stop Loss", "Activate stop loss", "Risk Management")
        self._stop_loss_method = self.Param("StopLossMethod", "EntryPriceBased").SetDisplay("Stop Loss Method", "Entry price or lowest low based", "Risk Management")
        self._entry_stop_loss_percent = self.Param("EntryStopLossPercent", 2.0).SetGreaterThanZero().SetDisplay("Entry Stop Loss %", "Stop loss percent from entry", "Risk Management")
        self._lookback_period = self.Param("LookbackPeriod", 10).SetGreaterThanZero().SetDisplay("Lookback Period", "Bars for lowest low stop", "Risk Management")
        self._stop_loss_buffer_percent = self.Param("StopLossBufferPercent", 0.5).SetDisplay("Stop Loss Buffer %", "Additional buffer below lowest low", "Risk Management")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(1))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._reset_state()

    @property
    def candle_type(self):
        return self._candle_type.Value

    def GetWorkingSecurities(self):
        return [(self.Security, self.candle_type)]

    def _reset_state(self):
        self._prev_open = None
        self._prev_close = None
        self._closes = []
        self._last_signal_buy = None
        self._stop_loss_price = 0.0

    def OnReseted(self):
        super(mutanabby_ai_algo_pro_strategy, self).OnReseted()
        self._reset_state()

    def OnStarted2(self, time):
        super(mutanabby_ai_algo_pro_strategy, self).OnStarted2(time)

        self._reset_state()

        rsi = RelativeStrengthIndex()
        rsi.Length = RSI_LENGTH
        lowest = Lowest()
        lowest.Length = self._lookback_period.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(rsi, lowest, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)
            oscillators = self.CreateChartArea()
            if oscillators is not None:
                self.DrawIndicator(oscillators, rsi)

    def _process_candle(self, candle, rsi_value, lowest_low):
        if candle.State != CandleStates.Finished:
            return

        delta_length = self._candle_delta_length.Value
        prev_open = self._prev_open
        prev_close = self._prev_close
        close_n = self._closes[0] if len(self._closes) == delta_length else None

        close = float(candle.ClosePrice)
        opn = float(candle.OpenPrice)
        self._prev_open = opn
        self._prev_close = close
        self._closes.append(close)
        while len(self._closes) > delta_length:
            self._closes.pop(0)

        if prev_open is None or prev_close is None or close_n is None:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        if self.Position > 0:
            stop_hit = self._enable_stop_loss.Value and self._stop_loss_price > 0 and float(candle.LowPrice) <= self._stop_loss_price
            bearish_engulfing = prev_close > prev_open and close < opn and close < prev_open
            if stop_hit or bearish_engulfing:
                self.SellMarket(self.Position)
                self._last_signal_buy = False
                self._stop_loss_price = 0.0
            return

        rng = float(candle.HighPrice) - float(candle.LowPrice)
        stable = rng > 0 and abs(close - opn) / rng >= float(self._candle_stability_index.Value)
        bullish_engulfing = prev_close < prev_open and close > opn and close > prev_open

        if not bullish_engulfing or not stable or float(rsi_value) >= self._rsi_index.Value or close >= close_n:
            return

        if self._disable_repeating_signals.Value and self._last_signal_buy is True:
            return

        self.BuyMarket(self.Volume + abs(self.Position))
        self._last_signal_buy = True

        if not self._enable_stop_loss.Value:
            self._stop_loss_price = 0.0
        elif str(self._stop_loss_method.Value) == "EntryPriceBased":
            self._stop_loss_price = close * (1.0 - float(self._entry_stop_loss_percent.Value) / 100.0)
        else:
            self._stop_loss_price = float(lowest_low) * (1.0 - float(self._stop_loss_buffer_percent.Value) / 100.0)

    def CreateClone(self):
        return mutanabby_ai_algo_pro_strategy()
