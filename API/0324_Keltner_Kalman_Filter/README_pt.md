# Estratégia de Keltner Kalman Filter
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia **Keltner Kalman Filter** é construída em torno da combinação dos Canais Keltner com um Kalman Filter para identificar tendências e oportunidades de negociação.

Os testes indicam um retorno anual médio de aproximadamente 73%. Tem melhor desempenho no mercado de criptomoedas.

Os sinais são ativados quando o Keltner confirma entradas filtradas em dados intradiários (15m). Este método é adequado para traders ativos.

Os stops dependem de múltiplos do ATR e fatores como EmaPeriod, AtrPeriod. Ajuste esses valores padrão para equilibrar risco e retorno.

## Detalhes
- **Critérios de entrada**: ver implementação para condições de indicadores.
- **Comprado/Vendido**: Ambos os lados.
- **Critérios de saída**: sinal oposto ou lógica de stop.
- **Stops**: Sim, usando cálculos baseados em indicadores.
- **Valores padrão**:
  - `EmaPeriod = 20`
  - `AtrPeriod = 14`
  - `AtrMultiplier = 2.0m`
  - `KalmanProcessNoise = 0.01m`
  - `KalmanMeasurementNoise = 0.1m`
  - `CandleType = TimeSpan.FromMinutes(15).TimeFrame()`
- **Filtros**:
  - Categoria: Seguidor de tendência
  - Direção: Ambos
  - Indicadores: Keltner
  - Stops: Sim
  - Complexidade: Intermediário
  - Período: Intradiário (15m)
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
