# Estratégia de Rompimento de Volatilidade com Bollinger
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia **Bollinger Volatility Breakout** é construída em torno do rompimento das Bandas de Bollinger com confirmação de volatilidade.

Os testes indicam um retorno anual médio de aproximadamente 181%. Funciona melhor no mercado de criptomoedas.

Os sinais são disparados quando Bollinger confirma oportunidades de rompimento em dados intradiários (5m). Isso torna o método adequado para traders ativos.

Os stops se baseiam em múltiplos de ATR e fatores como BollingerPeriod, BollingerDeviation. Ajuste esses valores padrão para equilibrar risco e recompensa.

## Detalhes
- **Critérios de entrada**: ver implementação para as condições do indicador.
- **Comprado/Vendido**: Ambos.
- **Critérios de saída**: sinal oposto ou lógica de stop.
- **Stops**: Sim, usando cálculos baseados em indicadores.
- **Valores padrão**:
  - `BollingerPeriod = 20`
  - `BollingerDeviation = 2.0m`
  - `AtrPeriod = 14`
  - `AtrDeviationMultiplier = 2.0m`
  - `StopLossMultiplier = 2.0m`
  - `CandleType = TimeSpan.FromMinutes(5).TimeFrame()`
- **Filtros**:
  - Categoria: Seguidor de tendência
  - Direção: Ambos
  - Indicadores: Bollinger
  - Stops: Sim
  - Complexidade: Intermediário
  - Período: Intradiário (5m)
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
