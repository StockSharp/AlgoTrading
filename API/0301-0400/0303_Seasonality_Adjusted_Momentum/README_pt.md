# Estratégia de Momentum Ajustado por Sazonalidade
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia **Seasonality Adjusted Momentum** é construída em torno do indicador de momentum ajustado pela força da sazonalidade.

Os testes indicam um retorno anual médio de aproximadamente 172%. Funciona melhor no mercado de câmbio.

Usa **candles concluídos de 5 minutos**, não dados diários. `M = Momentum(14)`, `A = SMA(14) de M`, `s = score[candle.OpenTime.Month]`. Sem posição, compra se `s >= 0.5` e `M > A`; vende se `s <= -0.5` e `M < A`. Fecha long se `s < 0.5` ou `M < A`; short se `s > -0.5` ou `M > A`. Espera de 120 candles após sinal. Proteção: **stop nativo de 2%**, não ATR.

Valores mensais de s (jan–dez): `0.8, 0.2, 0.5, 0.7, 0.3, -0.2, -0.3, -0.4, -0.7, 0.4, 0.6, 0.9`. BTC/TON contém apenas março (s=0.5), permitindo long e proibindo novos shorts. A rentabilidade de 172% **não está verificada**.

## Detalhes
- **Critérios de entrada**: ver implementação para as condições do indicador.
- **Comprado/Vendido**: Ambos.
- **Critérios de saída**: sinal oposto ou lógica de stop.
- **Stops**: Sim, usando cálculos baseados em indicadores.
- **Valores padrão**:
  - `MomentumPeriod = 14`
  - `SeasonalityThreshold = 0.5m`
  - `StopLossPercent = 2.0m`
  - `CandleType = TimeSpan.FromMinutes(5).TimeFrame()`
- **Filtros**:
  - Categoria: Seguidor de tendência
  - Direção: Ambos
  - Indicadores: Seasonality, Adjusted
  - Stops: Sim
  - Complexidade: Intermediário
  - Período: Diário
  - Sazonalidade: Sim
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
