# Logistic RSI STOCH ROC AO
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia aplica um mapa logístico a um indicador selecionado (AO, ROC, RSI, Stochastic) e opera quando o desvio padrão com sinal cruza o zero.

## Detalhes

- **Critérios de entrada**: O desvio padrão com sinal cruza acima de zero.
- **Comprado/Vendido**: Ambas as direções.
- **Critérios de saída**: O desvio padrão com sinal cruza abaixo de zero.
- **Stops**: Nenhum.
- **Valores padrão**:
  - `Indicator` = LogisticDominance
  - `Length` = 13
  - `LenLd` = 5
  - `LenRoc` = 9
  - `LenRsi` = 14
  - `LenSto` = 14
  - `CandleType` = TimeSpan.FromMinutes(1)
- **Filtros**:
  - Categoria: Oscilador
  - Direção: Ambos
  - Indicadores: AwesomeOscillator, RateOfChange, RelativeStrengthIndex, StochasticOscillator, Highest
  - Stops: Não
  - Complexidade: Intermediário
  - Período: Intradiário (1m)
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
