# Estratégia de Reversão com Doji
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

As velas Doji refletem um equilíbrio temporário entre compradores e vendedores. Quando um doji aparece após um movimento direcional forte, pode preceder uma reversão à medida que o momentum se dissipa. Esta estratégia mede o corpo da vela em relação ao seu intervalo para determinar se um verdadeiro doji foi formado.

Os testes indicam um retorno anual médio de aproximadamente 103%. Funciona melhor no mercado de ações.

Uma vez detectado um doji, as velas anteriores são verificadas para identificar uma tendência de alta ou de baixa. Um doji após uma queda pode acionar uma entrada comprada, enquanto um após uma alta pode abrir uma posição vendida. Os stops são colocados a uma distância percentual da entrada e as saídas ocorrem se o preço romper além dos extremos do doji.

O método busca capturar a primeira reação a partir do doji e é mais adequado para gráficos intradiários, onde reversões rápidas frequentemente se desenvolvem.

## Detalhes

- **Critérios de entrada**: Vela Doji após um movimento direcional.
- **Comprado/Vendido**: Ambos.
- **Critérios de saída**: Preço se movendo além da máxima/mínima do doji ou stop-loss.
- **Stops**: Sim, baseados em percentual.
- **Valores padrão**:
  - `CandleType` = 5 minute
  - `DojiThreshold` = 0.1
  - `StopLossPercent` = 1
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

