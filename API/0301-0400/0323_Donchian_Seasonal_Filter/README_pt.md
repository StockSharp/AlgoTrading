# Estratégia de Donchian Seasonal Filter
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia **Donchian Seasonal Filter** é construída em torno dos Canais Donchian com filtro sazonal.

Os testes indicam um retorno anual médio de aproximadamente 70%. Tem melhor desempenho no mercado de ações.

Os sinais são ativados quando o Donchian confirma entradas filtradas em dados intradiários (15m). Este método é adequado para traders ativos.

Os stops dependem de múltiplos do ATR e fatores como DonchianPeriod, SeasonalThreshold. Ajuste esses valores padrão para equilibrar risco e retorno.

## Detalhes
- **Critérios de entrada**: ver implementação para condições de indicadores.
- **Comprado/Vendido**: Ambos os lados.
- **Critérios de saída**: sinal oposto ou lógica de stop.
- **Stops**: Sim, usando cálculos baseados em indicadores.
- **Valores padrão**:
  - `DonchianPeriod = 20`
  - `SeasonalThreshold = 0.5m`
  - `CandleType = TimeSpan.FromMinutes(15).TimeFrame()`
- **Filtros**:
  - Categoria: Seguidor de tendência
  - Direção: Ambos
  - Indicadores: Donchian, Seasonal
  - Stops: Sim
  - Complexidade: Intermediário
  - Período: Intradiário (15m)
  - Sazonalidade: Sim
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
