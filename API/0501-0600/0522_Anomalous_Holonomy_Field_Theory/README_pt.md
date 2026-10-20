# Anomalous Holonomy Field Theory

[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Sinal técnico de seis componentes: EMA20/EMA50 fixas, RSI14 Wilder, MACD(12,26,9), ATR Wilder e ROC normalizado pela volatilidade. `C` é o fechamento original, `A=ATR`, `clip(x,k)=max(-k,min(k,x))`. As janelas incluem a vela concluída atual.

## Regras de negociação

`T`: +1.5 acima de ambas EMA, +1 somente acima de EMA20, -1.5 abaixo de ambas, -1 somente abaixo de EMA20, zero nos demais casos. VWAP por velas: `sum(((High+Low+Close)/3)*Volume)/sum(Volume)` em `VwapPeriod`. `V=clip((C-VWAP)/A,2)`. Para `B=T+V`, RSI soma +1.5 abaixo de 30 ou +0.5 abaixo de 40 somente se `B>0`; soma -1.5 acima de 70 ou -0.5 acima de 60 somente se `B<0`; caso contrário `R=0`.

`M=clip((MACD-EMA9(MACD))/A,1)`; `Q=clip((C-C[há RocPeriod velas])/A,1)`, equivalente a ROC/ATR% com a mesma base histórica. `S=T+V+R+M+Q`. Sem posição, compra `Volume` se `S>=SignalThreshold` e vende se `S<=-SignalThreshold`. Sem aquecimento, ATR positivo e volume, não entra. O intervalo dos índices de entrada deve ser estritamente maior que `CooldownBars`.

Antes de cada entrada fixa-se `ATR*AtrStopMultiplier`. O stop local nativo a mercado parte das execuções reais e reage a bid/ask entre velas; padrão 2 ATR. A posição sai pela proteção, sem acumulação ou reversão por sinal oposto. A espera de entrada não atrasa o stop. Não há alvo de lucro.

As entradas exigem velas concluídas e indicadores aquecidos. A posição é acompanhada até a saída; ordens a mercado pendentes impedem envios duplicados.

## Parâmetros

| Parameter | Default |
|---|---|
| `SignalThreshold` | `2` |
| `CooldownBars` | `50` |
| `AtrLength` | `14` |
| `AtrStopMultiplier` | `2` |
| `RocPeriod` | `10` |
| `VwapPeriod` | `20` |
| `CandleType` | `5m` |
