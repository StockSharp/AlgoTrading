# ZScore
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Estratégia baseada no indicador Z-Score para trading de reversão à média

Os testes indicam um retorno anual médio de aproximadamente 121%. Funciona melhor no mercado de criptomoedas.

ZScore mede o desvio do preço de uma média móvel. Z-scores extremamente altos ou baixos sugerem sobreextensão e incentivam trades na direção oposta. O trade termina quando o Z-score se normaliza.

O Z-Score é um filtro flexível porque pode ser escalado para qualquer série temporal. Usar uma saída ajustada à volatilidade ajuda o sistema a se adaptar às condições de mercado em constante mudança.


## Detalhes

- **Critérios de entrada**: Sinais baseados em MA, ZScore.
- **Comprado/Vendido**: Ambos os direções.
- **Critérios de saída**: Sinal oposto ou stop.
- **Stops**: Sim.
- **Valores padrão**:
  - `ZScoreEntryThreshold` = 2.0m
  - `ZScoreExitThreshold` = 0.0m
  - `MAPeriod` = 20
  - `StdDevPeriod` = 20
  - `StopLossPercent` = 2.0m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoria: Reversão à média
  - Direção: Ambos
  - Indicadores: MA, ZScore
  - Stops: Sim
  - Complexidade: Básico
  - Período: Intradiário (5m)
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio

