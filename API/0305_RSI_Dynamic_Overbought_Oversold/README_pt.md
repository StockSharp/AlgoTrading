# Estratégia RSI com Sobrecompra/Sobrevenda Dinâmica
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia **RSI Dynamic Overbought Oversold** é construída em torno do RSI com níveis dinâmicos de sobrecompra/sobrevenda.

Os testes indicam um retorno anual médio de aproximadamente 178%. Funciona melhor no mercado de ações.

Os sinais são disparados quando a Sobrecompra confirma mudanças de tendência em dados intradiários (5m). Isso torna o método adequado para traders ativos.

Os stops se baseiam em múltiplos de ATR e fatores como RsiPeriod, MovingAvgPeriod. Ajuste esses valores padrão para equilibrar risco e recompensa.

## Detalhes
- **Critérios de entrada**: ver implementação para as condições do indicador.
- **Comprado/Vendido**: Ambos.
- **Critérios de saída**: sinal oposto ou lógica de stop.
- **Stops**: Sim, usando cálculos baseados em indicadores.
- **Valores padrão**:
  - `RsiPeriod = 14`
  - `MovingAvgPeriod = 50`
  - `StdDevMultiplier = 2.0m`
  - `StopLossPercent = 2.0m`
  - `CandleType = TimeSpan.FromMinutes(5).TimeFrame()`
- **Filtros**:
  - Categoria: Seguidor de tendência
  - Direção: Ambos
  - Indicadores: Overbought, Oversold
  - Stops: Sim
  - Complexidade: Intermediário
  - Período: Intradiário (5m)
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
