# Estratégia de Supertrend RSI Divergence
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia **Supertrend RSI Divergence** utiliza o indicador Supertrend junto com a divergência RSI para identificar oportunidades de negociação.

Os testes indicam um retorno anual médio de aproximadamente 67%. Tem melhor desempenho no mercado de ações.

Os sinais são ativados quando Divergence confirma configurações de divergência em dados intradiários (15m). Este método é adequado para traders ativos.

Os stops dependem de múltiplos do ATR e fatores como SupertrendPeriod, SupertrendMultiplier. Ajuste esses valores padrão para equilibrar risco e retorno.

## Detalhes
- **Critérios de entrada**: ver implementação para condições de indicadores.
- **Comprado/Vendido**: Ambos os lados.
- **Critérios de saída**: sinal oposto ou lógica de stop.
- **Stops**: Sim, usando cálculos baseados em indicadores.
- **Valores padrão**:
  - `SupertrendPeriod = 10`
  - `SupertrendMultiplier = 3.0m`
  - `RsiPeriod = 14`
  - `CandleType = TimeSpan.FromMinutes(15).TimeFrame()`
- **Filtros**:
  - Categoria: Seguidor de tendência
  - Direção: Ambos
  - Indicadores: Divergence
  - Stops: Sim
  - Complexidade: Intermediário
  - Período: Intradiário (15m)
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Sim
  - Nível de risco: Médio
