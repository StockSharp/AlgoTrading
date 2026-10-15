import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Strategies import Strategy


class e_turbo_fx_momentum_strategy(Strategy):
    """Momentum reversal strategy that tracks consecutive candles with expanding bodies.
    Uses StartProtection for SL/TP."""

    def __init__(self):
        super(e_turbo_fx_momentum_strategy, self).__init__()

        self._depth_analysis = self.Param("DepthAnalysis", 3) \
            .SetGreaterThanZero() \
            .SetDisplay("Depth Analysis", "Number of finished candles used for pattern detection", "Trading Rules")
        self._take_profit_steps = self.Param("TakeProfitSteps", 120.0) \
            .SetDisplay("Take Profit (steps)", "Take profit distance in price steps (ticks)", "Risk Management")
        self._stop_loss_steps = self.Param("StopLossSteps", 70.0) \
            .SetDisplay("Stop Loss (steps)", "Stop loss distance in price steps (ticks)", "Risk Management")
        self._trade_volume = self.Param("TradeVolume", 0.1) \
            .SetGreaterThanZero() \
            .SetDisplay("Trade Volume", "Order volume used for entries", "Trading Rules")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(1))) \
            .SetDisplay("Candle Type", "Timeframe of the candles analysed by the strategy", "Market Data")

        self._bearish_sequence = 0
        self._bullish_sequence = 0
        self._previous_bearish_body = 0.0
        self._previous_bullish_body = 0.0

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    @property
    def DepthAnalysis(self):
        return self._depth_analysis.Value

    @property
    def TakeProfitSteps(self):
        return self._take_profit_steps.Value

    @property
    def StopLossSteps(self):
        return self._stop_loss_steps.Value

    @property
    def TradeVolume(self):
        return self._trade_volume.Value

    def OnReseted(self):
        super(e_turbo_fx_momentum_strategy, self).OnReseted()
        self._reset_state()

    def OnStarted2(self, time):
        super(e_turbo_fx_momentum_strategy, self).OnStarted2(time)

        self._reset_state()
        self.Volume = float(self.TradeVolume)

        tp_steps = float(self.TakeProfitSteps)
        sl_steps = float(self.StopLossSteps)

        tp_unit = Unit(tp_steps, UnitTypes.Absolute) if tp_steps > 0 else None
        sl_unit = Unit(sl_steps, UnitTypes.Absolute) if sl_steps > 0 else None

        if tp_unit is not None or sl_unit is not None:
            self.StartProtection(tp_unit, sl_unit)

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(self._process_candle).Start()

    def _process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        if self.Position != 0:
            self._reset_state()
            return

        body_size = abs(float(candle.ClosePrice) - float(candle.OpenPrice))

        if float(candle.ClosePrice) < float(candle.OpenPrice):
            self._handle_bearish_candle(body_size)
        elif float(candle.ClosePrice) > float(candle.OpenPrice):
            self._handle_bullish_candle(body_size)
        else:
            self._reset_state()

    def _handle_bearish_candle(self, body_size):
        self._reset_bullish_sequence()

        if body_size <= 0:
            self._reset_bearish_sequence()
            return

        if self._bearish_sequence == 0 or body_size > self._previous_bearish_body:
            self._bearish_sequence += 1
        else:
            self._bearish_sequence = 1

        self._previous_bearish_body = body_size

        if self._bearish_sequence >= self.DepthAnalysis:
            self.BuyMarket()
            self._reset_bearish_sequence()

    def _handle_bullish_candle(self, body_size):
        self._reset_bearish_sequence()

        if body_size <= 0:
            self._reset_bullish_sequence()
            return

        if self._bullish_sequence == 0 or body_size > self._previous_bullish_body:
            self._bullish_sequence += 1
        else:
            self._bullish_sequence = 1

        self._previous_bullish_body = body_size

        if self._bullish_sequence >= self.DepthAnalysis:
            self.SellMarket()
            self._reset_bullish_sequence()

    def _reset_bearish_sequence(self):
        self._bearish_sequence = 0
        self._previous_bearish_body = 0.0

    def _reset_bullish_sequence(self):
        self._bullish_sequence = 0
        self._previous_bullish_body = 0.0

    def _reset_state(self):
        self._reset_bearish_sequence()
        self._reset_bullish_sequence()

    def CreateClone(self):
        return e_turbo_fx_momentum_strategy()
