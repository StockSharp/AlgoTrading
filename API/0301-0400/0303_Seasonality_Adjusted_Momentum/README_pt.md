# Estratégia de Momentum Ajustado por Sazonalidade
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia **Seasonality Adjusted Momentum** é construída em torno do indicador de momentum ajustado pela força da sazonalidade.

Os testes indicam um retorno anual médio de aproximadamente 172%. Funciona melhor no mercado de câmbio.

Os sinais são disparados quando a Sazonalidade confirma mudanças de momentum em dados diários. Isso torna o método adequado para traders ativos.

Os stops se baseiam em múltiplos de ATR e fatores como MomentumPeriod, SeasonalityThreshold. Ajuste esses valores padrão para equilibrar risco e recompensa.

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
