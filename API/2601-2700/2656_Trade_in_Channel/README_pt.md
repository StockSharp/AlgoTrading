# Estratégia de Operação em Canal
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Estratégia contrária de canal que opera contra os extremos do canal Donchian quando a largura de banda permanece inalterada. O sistema compara a última máxima/mínima contra os limites anteriores do canal e um pivô calculado a partir do fechamento anterior para decidir se opera contra o movimento. Os stops protetores dependem da distância ATR e um trailing stop opcional mantém os lucros uma vez que o preço avança a favor da posição.

## Detalhes

- **Critérios de entrada**:
  - Vendido: banda superior do canal inalterada e a última máxima do candle tocou a banda superior ou o fechamento anterior está entre o pivô e a banda superior.
  - Comprado: banda inferior do canal inalterada e a última mínima do candle tocou a banda inferior ou o fechamento anterior está entre o pivô e a banda inferior.
- **Comprado/Vendido**: Ambos.
- **Critérios de saída**:
  - Fechar comprado se a banda superior estiver plana e o preço a tocar, ou se o stop ATR ou trailing stop for acionado.
  - Fechar vendido se a banda inferior estiver plana e o preço a tocar, ou se o stop ATR ou trailing stop for acionado.
- **Stops**:
  - Stop inicial para comprados em `support - ATR` e para vendidos em `resistance + ATR`.
  - O trailing stop se move atrás do melhor preço uma vez que o lucro supera a distância `TrailingStopPips` (convertida em passos de preço).
- **Valores padrão**:
  - `ChannelPeriod` = 20 (lookback Donchian)
  - `AtrPeriod` = 4 (suavização ATR)
  - `Volume` = 1 contrato/lote
  - `TrailingStopPips` = 30 passos de preço
  - `CandleType` = período de 1 hora
- **Filtros**:
  - Categoria: Canal / Reversão à média
  - Direção: Comprado e Vendido
  - Indicadores: Donchian Channel, ATR
  - Stops: Stop fixo ATR + trailing stop
  - Complexidade: Intermediário
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio

## Notas

- O pivô é igual a `(banda superior + banda inferior + fechamento anterior) / 3`, coincidindo com a implementação MQL original.
- A estratégia mantém apenas uma posição líquida e muda de direção somente após o trade anterior ser totalmente fechado.
- A distância do trailing é especificada em passos de preço ("pips"); é multiplicada pelo `PriceStep` do instrumento para obter o offset de preço real.
