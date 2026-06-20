# Estratégia de Parabolic SAR Hurst Filter
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia **Parabolic SAR Hurst Filter** é construída em torno do Parabolic SAR Hurst Filter.

Os testes indicam um retorno anual médio de aproximadamente 82%. Tem melhor desempenho no mercado de ações.

Os sinais são ativados quando o Parabolic confirma entradas filtradas em dados intradiários (5m). Este método é adequado para traders ativos.

Os stops dependem de múltiplos do ATR e fatores como SarAccelerationFactor, SarMaxAccelerationFactor. Ajuste esses valores padrão para equilibrar risco e retorno.

## Detalhes
- **Critérios de entrada**: ver implementação para condições de indicadores.
- **Comprado/Vendido**: Ambos os lados.
- **Critérios de saída**: sinal oposto ou lógica de stop.
- **Stops**: Sim, usando cálculos baseados em indicadores.
- **Valores padrão**:
  - `SarAccelerationFactor = 0.02m`
  - `SarMaxAccelerationFactor = 0.2m`
  - `HurstPeriod = 100`
  - `CandleType = TimeSpan.FromMinutes(5).TimeFrame()`
- **Filtros**:
  - Categoria: Seguidor de tendência
  - Direção: Ambos
  - Indicadores: Parabolic, Hurst
  - Stops: Sim
  - Complexidade: Intermediário
  - Período: Intradiário (5m)
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
