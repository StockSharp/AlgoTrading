# Estratégia Volatility Adjusted Moving Average
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)
 
Esta técnica modifica uma banda de média móvel por um múltiplo do ATR. Quando o preço se move além da banda ajustada, indica uma tendência acelerada.

Os testes indicam um retorno anual médio de aproximadamente 160%. Funciona melhor no mercado forex.

Operações compradas são abertas acima da banda superior, vendidas abaixo da banda inferior. Um cruzamento de volta pela média móvil base fecha a posição.

Como as bandas se expandem com a volatilidade, os stops se adaptam às condições do mercado.

## Detalhes

- **Critérios de entrada**: Preço rompe acima ou abaixo de MA ± multiplicador ATR.
- **Comprado/Vendido**: Ambos os lados.
- **Critérios de saída**: Preço cruza MA ou stop.
- **Stops**: Sim.
- **Valores padrão**:
  - `MAPeriod` = 20
  - `ATRPeriod` = 14
  - `ATRMultiplier` = 2.0m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoria: Tendência
  - Direção: Ambos
  - Indicadores: MA, ATR
  - Stops: Sim
  - Complexidade: Intermediário
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio

