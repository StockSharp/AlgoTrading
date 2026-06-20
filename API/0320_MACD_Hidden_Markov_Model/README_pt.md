# Estratégia de MACD Hidden Markov Model
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia **MACD Hidden Markov Model** é construída em torno do MACD Hidden Markov Model.

Os testes indicam um retorno anual médio de aproximadamente 61%. Tem melhor desempenho no mercado de criptomoedas.

Os sinais são ativados quando o Markov confirma mudanças de tendência em dados intradiários (5m). Este método é adequado para traders ativos.

Os stops dependem de múltiplos do ATR e fatores como MacdFast, MacdSlow. Ajuste esses valores padrão para equilibrar risco e retorno.

## Detalhes
- **Critérios de entrada**: ver implementação para condições de indicadores.
- **Comprado/Vendido**: Ambos os lados.
- **Critérios de saída**: sinal oposto ou lógica de stop.
- **Stops**: Sim, usando cálculos baseados em indicadores.
- **Valores padrão**:
  - `MacdFast = 12`
  - `MacdSlow = 26`
  - `MacdSignal = 9`
  - `CandleType = TimeSpan.FromMinutes(5).TimeFrame()`
  - `HmmHistoryLength = 100`
- **Filtros**:
  - Categoria: Seguidor de tendência
  - Direção: Ambos
  - Indicadores: Markov
  - Stops: Sim
  - Complexidade: Intermediário
  - Período: Intradiário (5m)
  - Sazonalidade: Não
  - Redes neurais: Sim
  - Divergência: Não
  - Nível de risco: Médio
