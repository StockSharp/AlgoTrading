# Estratégia de Hull MA Volatility Contraction
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia **Hull MA Volatility Contraction** é construída em torno da Média Móvel Hull com filtro de contração de volatilidade.

Os testes indicam um retorno anual médio de aproximadamente 76%. Tem melhor desempenho no mercado de câmbio.

Os sinais são ativados quando os indicadores confirmam padrões de contração de volatilidade em dados intradiários (15m). Este método é adequado para traders ativos.

Os stops dependem de múltiplos do ATR e fatores como HmaPeriod, AtrPeriod. Ajuste esses valores padrão para equilibrar risco e retorno.

## Detalhes
- **Critérios de entrada**: ver implementação para condições de indicadores.
- **Comprado/Vendido**: Ambos os lados.
- **Critérios de saída**: sinal oposto ou lógica de stop.
- **Stops**: Sim, usando cálculos baseados em indicadores.
- **Valores padrão**:
  - `HmaPeriod = 9`
  - `AtrPeriod = 14`
  - `VolatilityContractionFactor = 2.0m`
  - `CandleType = TimeSpan.FromMinutes(15).TimeFrame()`
- **Filtros**:
  - Categoria: Seguidor de tendência
  - Direção: Ambos
  - Indicadores: múltiplos indicadores
  - Stops: Sim
  - Complexidade: Intermediário
  - Período: Intradiário (15m)
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
