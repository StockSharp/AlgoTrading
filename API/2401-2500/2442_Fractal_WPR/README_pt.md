# Estratégia Fractal WPR
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia usa o oscilador Williams %R para gerar sinais de trading com base em cruzamentos dos níveis de sobrecompra e sobrevenda. É adaptada de um expert advisor MQL5 e demonstra um sistema simples de reversão de momentum.

## Como funciona

1. Um indicador Williams %R com período configurável é calculado no período selecionado.
2. Dois níveis horizontais definem as zonas extremas:
   - `HighLevel` marca a zona de sobrecompra (padrão −30).
   - `LowLevel` marca a zona de sobrevenda (padrão −70).
3. Quando `Trend` está configurado como `Direct`:
   - Cruzar para baixo `LowLevel` abre uma posição comprada e fecha qualquer posição vendida.
   - Cruzar para cima `HighLevel` abre uma posição vendida e fecha qualquer posição comprada.
4. Quando `Trend` está configurado como `Against`, as reações aos cruzamentos são invertidas.
5. Parâmetros opcionais permitem habilitar ou desabilitar separadamente a abertura e o fechamento de posições compradas ou vendidas.
6. As distâncias de stop‑loss e take‑profit em ticks são aplicadas usando a API de proteção de alto nível.

Apenas velas completas são processadas para evitar reagir ao ruído intrabarra.

## Parâmetros

- `WprPeriod` – período de cálculo do Williams %R.
- `HighLevel` – limiar para a zona de sobrecompra.
- `LowLevel` – limiar para a zona de sobrevenda.
- `Trend` – modo de trading (`Direct` ou `Against`).
- `BuyPositionOpen` – permitir abertura de posições compradas.
- `SellPositionOpen` – permitir abertura de posições vendidas.
- `BuyPositionClose` – permitir fechamento de posições compradas.
- `SellPositionClose` – permitir fechamento de posições vendidas.
- `StopLossTicks` – distância do stop‑loss em ticks.
- `TakeProfitTicks` – distância do take‑profit em ticks.
- `CandleType` – período de velas usado para análise.

## Indicadores

- Williams %R
