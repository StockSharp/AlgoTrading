import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Strategies import Strategy


class e_turbo_fx_steps_strategy(Strategy):
    def __init__(self):
        super(e_turbo_fx_steps_strategy, self).__init__()

        self._depth_analysis = self.Param("DepthAnalysis", 3)
        self._take_profit_steps = self.Param("TakeProfitSteps", 120.0)
        self._stop_loss_steps = self.Param("StopLossSteps", 70.0)
        self._trade_volume = self.Param("TradeVolume", 0.1)
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(1)))

        self._bearish_sequence = 0
        self._bullish_sequence = 0
        self._previous_bearish_body = 0.0
        self._previous_bullish_body = 0.0

    @property
    def DepthAnalysis(self):
        return self._depth_analysis.Value

    @DepthAnalysis.setter
    def DepthAnalysis(self, value):
        self._depth_analysis.Value = value

    @property
    def TakeProfitSteps(self):
        return self._take_profit_steps.Value

    @TakeProfitSteps.setter
    def TakeProfitSteps(self, value):
        self._take_profit_steps.Value = value

    @property
    def StopLossSteps(self):
        return self._stop_loss_steps.Value

    @StopLossSteps.setter
    def StopLossSteps(self, value):
        self._stop_loss_steps.Value = value

    @property
    def TradeVolume(self):
        return self._trade_volume.Value

    @TradeVolume.setter
    def TradeVolume(self, value):
        self._trade_volume.Value = value

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    def OnStarted2(self, time):
        super(e_turbo_fx_steps_strategy, self).OnStarted2(time)

        self._bearish_sequence = 0
        self._bullish_sequence = 0
        self._previous_bearish_body = 0.0
        self._previous_bullish_body = 0.0

        tp_steps = float(self.TakeProfitSteps)
        sl_steps = float(self.StopLossSteps)

        if sl_steps > 0.0 or tp_steps > 0.0:
            sl_unit = Unit(sl_steps, UnitTypes.Absolute) if sl_steps > 0.0 else None
            tp_unit = Unit(tp_steps, UnitTypes.Absolute) if tp_steps > 0.0 else None
            self.StartProtection(sl_unit, tp_unit)

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(self.ProcessCandle).Start()

    def ProcessCandle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        if self.Position != 0:
            self._reset_state()
            return

        close = float(candle.ClosePrice)
        open_price = float(candle.OpenPrice)
        body_size = abs(close - open_price)

        if close < open_price:
            self._handle_bearish_candle(body_size)
        elif close > open_price:
            self._handle_bullish_candle(body_size)
        else:
            self._reset_state()

    def _handle_bearish_candle(self, body_size):
        self._reset_bullish_sequence()

        if body_size <= 0.0:
            self._reset_bearish_sequence()
            return

        if self._bearish_sequence == 0 or body_size > self._previous_bearish_body:
            self._bearish_sequence += 1
        else:
            self._bearish_sequence = 1

        self._previous_bearish_body = body_size

        if self._bearish_sequence >= int(self.DepthAnalysis):
            self.BuyMarket()
            self._reset_bearish_sequence()

    def _handle_bullish_candle(self, body_size):
        self._reset_bearish_sequence()

        if body_size <= 0.0:
            self._reset_bullish_sequence()
            return

        if self._bullish_sequence == 0 or body_size > self._previous_bullish_body:
            self._bullish_sequence += 1
        else:
            self._bullish_sequence = 1

        self._previous_bullish_body = body_size

        if self._bullish_sequence >= int(self.DepthAnalysis):
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

    def OnReseted(self):
        super(e_turbo_fx_steps_strategy, self).OnReseted()
        self._bearish_sequence = 0
        self._bullish_sequence = 0
        self._previous_bearish_body = 0.0
        self._previous_bullish_body = 0.0

    def CreateClone(self):
        return e_turbo_fx_steps_strategy()
