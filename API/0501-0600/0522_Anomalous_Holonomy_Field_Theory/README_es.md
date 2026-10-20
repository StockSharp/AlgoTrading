# Anomalous Holonomy Field Theory

[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Señal de seis componentes: EMA20/EMA50 fijas, RSI14 Wilder, MACD(12,26,9), ATR Wilder y ROC normalizado por volatilidad. `C` es el cierre original, `A=ATR`, `clip(x,k)=max(-k,min(k,x))`. Las ventanas incluyen la vela terminada actual.

## Reglas de negociación

`T`: +1.5 sobre ambas EMA, +1 solo sobre EMA20, -1.5 bajo ambas, -1 solo bajo EMA20, cero en otro caso. VWAP de velas: `sum(((High+Low+Close)/3)*Volume)/sum(Volume)` durante `VwapPeriod`. `V=clip((C-VWAP)/A,2)`. Para `B=T+V`, RSI añade +1.5 bajo 30 o +0.5 bajo 40 si `B>0`; añade -1.5 sobre 70 o -0.5 sobre 60 si `B<0`; en otro caso `R=0`.

`M=clip((MACD-EMA9(MACD))/A,1)`; `Q=clip((C-C[hace RocPeriod velas])/A,1)`, equivalente a ROC/ATR% con la misma base histórica. `S=T+V+R+M+Q`. Sin posición, compra `Volume` con `S>=SignalThreshold`, vende con `S<=-SignalThreshold`. Sin preparación, ATR positivo y volumen, no entra. La separación de índices de entrada debe superar estrictamente `CooldownBars`.

Antes de cada entrada se fija `ATR*AtrStopMultiplier`. El stop local nativo a mercado se ancla a ejecuciones reales y reacciona a bid/ask entre velas; por defecto 2 ATR. La posición sale por protección, sin acumulación ni inversión por señal contraria. La espera de entrada no demora el stop. No hay objetivo de beneficio.

Las entradas requieren velas terminadas e indicadores preparados. La posición se gestiona hasta su salida; las órdenes de mercado pendientes impiden envíos duplicados.

## Parámetros

| Parameter | Default |
|---|---|
| `SignalThreshold` | `2` |
| `CooldownBars` | `50` |
| `AtrLength` | `14` |
| `AtrStopMultiplier` | `2` |
| `RocPeriod` | `10` |
| `VwapPeriod` | `20` |
| `CandleType` | `5m` |
