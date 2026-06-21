# Estratégia Genie RSI
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia opera reversões de sobrecompra e sobrevenda usando o Índice de Força Relativa (RSI). Quando o RSI sobe acima de 80, a estratégia abre uma posição vendida; quando o RSI cai abaixo de 20, ela abre uma posição comprada. Níveis opcionais de take profit e stop trailing gerenciam o risco após a entrada.

A estratégia é projetada para mercados oscilantes onde o preço frequentemente se move entre suporte e resistência. Funciona em qualquer período, definido pelo parâmetro `CandleType`.

## Detalhes

- **Critérios de entrada**  
  - **Comprado**: O valor do RSI cruza abaixo de 20 em um candle finalizado e nenhuma posição está aberta.  
  - **Vendido**: O valor do RSI cruza acima de 80 em um candle finalizado e nenhuma posição está aberta.
- **Critérios de saída**  
  - **Comprado**: O RSI sobe acima de 80, o preço atinge a distância de take profit, ou o preço toca o nível de stop trailing.  
  - **Vendido**: O RSI cai abaixo de 20, o preço atinge a distância de take profit, ou o preço toca o nível de stop trailing.
- **Indicadores**: RSI.
- **Parâmetros**:  
  - `RSI Period` – comprimento do indicador RSI.  
  - `Take Profit` – distância em unidades de preço para o alvo de lucro.  
  - `Trailing Stop` – distância em unidades de preço para o stop trailing.  
  - `Candle Type` – período dos candles processados.
- **Gestão de posição**: Usa ordens a mercado para entradas e saídas. O stop trailing é recalculado a cada candle finalizado.
