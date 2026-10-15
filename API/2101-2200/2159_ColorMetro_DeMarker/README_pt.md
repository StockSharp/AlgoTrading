# Estratégia ColorMetro DeMarker
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A **Estratégia ColorMetro DeMarker** é uma implementação em StockSharp do consultor especialista MQL5 `Exp_ColorMETRO_DeMarker`.
Usa o indicador DeMarker combinado com níveis escalonados para gerar sinais de trading.

## Parâmetros
- **DeMarker Period** – período do indicador DeMarker.
- **Fast Step** – tamanho do passo para construir o nível rápido (MPlus).
- **Slow Step** – tamanho do passo para construir o nível lento (MMinus).
- **Candle Type** – período das velas para análise.
- **Enable Buy Open** – permitir abertura de posições compradas.
- **Enable Sell Open** – permitir abertura de posições vendidas.
- **Enable Buy Close** – permitir fechamento de posições compradas.
- **Enable Sell Close** – permitir fechamento de posições vendidas.

## Lógica de Trading
1. O valor DeMarker é escalado para 0–100 e dois níveis dinâmicos (MPlus e MMinus) são calculados usando os tamanhos de passo rápido e lento.
2. Quando o nível rápido anterior estava acima do nível lento e o nível rápido atual cruza abaixo do lento, a estratégia compra e opcionalmente fecha posições vendidas.
3. Quando o nível rápido anterior estava abaixo do nível lento e o nível rápido atual cruza acima do lento, a estratégia vende e opcionalmente fecha posições compradas.
4. Todos os cálculos usam apenas velas concluídas.

Essa abordagem permite acompanhar as mudanças de tendência indicadas pelos níveis escalonados do DeMarker.
