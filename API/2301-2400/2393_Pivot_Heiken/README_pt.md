# Estratégia Pivot Heiken
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Estratégia que combina pontos pivot diários com candles Heikin-Ashi e um trailing stop opcional. O pivot diário é calculado a partir da máxima, mínima e fechamento do dia anterior. O suavizamento Heikin-Ashi filtra o ruído do preço e destaca a direção da tendência.

## Lógica
- **Entrada comprada**: O candle Heikin-Ashi é altista e o fechamento está acima do pivot diário.
- **Entrada vendida**: O candle Heikin-Ashi é baixista e o fechamento está abaixo do pivot diário.
- **Saída**: A posição sai no nível de stop loss, take profit ou trailing stop.

## Parâmetros
- `CandleType` – série de candles de trabalho.
- `StopLossPips` – distância do stop loss em pips.
- `TakeProfitPips` – distância do take profit em pips.
- `TrailingStopPips` – distância do trailing stop em pips (0 desativa o trailing).

## Indicadores
- Heikin-Ashi (calculado internamente).
- Ponto pivot diário.

## Notas
- Usa a API de alto nível com assinaturas de candles e vinculação de indicadores.
- Adequada tanto para negociação comprada quanto vendida.
