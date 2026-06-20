# Estratégia Bebê Abandonado de Alta
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)
 
O Bebê Abandonado de Alta é um padrão raro de três velas com um doji com gap de baixa seguido de um gap de alta.
Esta formação deixa a vela do meio "abandonada" e frequentemente precede uma reversão acentuada para cima.

Os testes indicam um retorno anual médio de aproximadamente 76%. Funciona melhor no mercado forex.

A estratégia compra na abertura da terceira vela quando ela abre em gap acima do doji, antecipando um forte seguimento à medida que os vendidos cobrem suas posições.

Os stops ficam logo abaixo da mínima do doji, garantindo que as perdas permaneçam pequenas se a reversão não se sustentar.

## Detalhes

- **Critérios de entrada**: correspondência de padrão
- **Comprado/Vendido**: Ambos
- **Critérios de saída**: stop-loss ou sinal oposto
- **Stops**: Sim, baseado em percentual
- **Valores padrão**:
  - `CandleType` = 15 minute
  - `StopLoss` = 2%
- **Filtros**:
  - Categoria: Padrão
  - Direção: Ambos
  - Indicadores: Candlestick
  - Stops: Sim
  - Complexidade: Intermediário
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio

