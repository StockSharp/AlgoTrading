# Candle Volume Weighted MA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia constrói médias móvies ponderadas por volume (VWMA) para os preços de abertura e fechamento dos candles. A posição relativa dessas VWMAs define uma "cor" do candle.

## Lógica de Negociação
1. Um candle é **altista** quando VWMA(abertura) está abaixo de VWMA(fechamento).
2. Um candle é **baixista** quando VWMA(abertura) está acima de VWMA(fechamento).
3. Quando o candle anterior é altista e o atual se torna neutro ou baixista, a estratégia abre uma posição comprada e fecha qualquer posição vendida.
4. Quando o candle anterior é baixista e o atual se torna neutro ou altista, a estratégia abre uma posição vendida e fecha qualquer posição comprada.

## Parâmetros
- `VWMA Period` – comprimento utilizado para calcular ambas as médias móvies ponderadas por volume.
- `Candle Type` – período dos candles utilizados para os cálculos.

Um bloco de proteção está habilitado por padrão: take‑profit de 2% e stop‑loss de 1%.
