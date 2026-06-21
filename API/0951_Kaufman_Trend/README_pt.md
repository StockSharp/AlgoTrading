# Estratégia de Tendência Kaufman
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A **Estratégia de Tendência Kaufman** usa um filtro de Kalman para estimar preço e momentum. A força da tendência é derivada da componente de velocidade do filtro e normalizada em uma janela recente. As entradas ocorrem quando condições de tendência forte se alinham com o preço acima ou abaixo do valor filtrado. Os stops são baseados em oscilações recentes mais ATR, e os lucros são realizados em etapas conforme o momentum enfraquece.

## Detalhes
- **Critérios de entrada**: limiar de força de tendência com preço acima/abaixo do valor filtrado.
- **Comprado/Vendido**: Ambas as direções.
- **Critérios de saída**: realização de lucros escalonada e enfraquecimento da tendência ou acionamento do stop.
- **Stops**: sim, mínimo/máximo de oscilação menos/mais ATR.
- **Valores padrão**:
  - `TakeProfit1Percent = 50`
  - `TakeProfit2Percent = 25`
  - `TakeProfit3Percent = 25`
  - `SwingLookback = 10`
  - `AtrPeriod = 14`
  - `TrendStrengthEntry = 60`
  - `TrendStrengthExit = 40`
  - `CandleType = TimeSpan.FromMinutes(15).TimeFrame()`
- **Filtros**:
  - Categoria: Seguidor de tendência
  - Direção: Ambos
  - Indicadores: Kalman
  - Stops: Sim
  - Complexidade: Intermediário
  - Período: Intradiário (15m)
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
