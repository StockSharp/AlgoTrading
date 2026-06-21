# Estratégia de Scalper de Canal
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Um sistema de scalping de rompimento de canal baseado em ATR. Para cada candle, o ponto médio é calculado como a média entre máxima e mínima. As bandas superior e inferior são construídas adicionando e subtraindo o Average True Range multiplicado por um fator. Quando o fechamento rompe acima da banda superior anterior, uma posição comprada é aberta. Um rompimento abaixo da banda inferior aciona uma posição vendida. As bandas seguem a direção da operação e servem como stops dinâmicos; um cruzamento da banda oposta reverte a posição.

## Detalhes

- **Critérios de entrada**:
  - **Compra**: O preço de fechamento cruza acima da banda superior anterior.
  - **Venda**: O preço de fechamento cruza abaixo da banda inferior anterior.
- **Comprado/Vendido**: Ambos os direções.
- **Critérios de saída**:
  - Sinal de reversão quando o preço cruza a banda oposta.
- **Stops**: As bandas do canal em trailing atuam como stops.
- **Filtros**: Nenhum.

## Parâmetros

- **ATR Period** – número de barras usadas para o cálculo do ATR.
- **ATR Multiplier** – fator aplicado ao ATR para a distância das bandas.
- **Candle Type** – período dos candles de entrada.
