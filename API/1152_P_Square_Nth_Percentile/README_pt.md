# Estratégia P-Square do N-ésimo Percentil
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Estima o percentil selecionado da série de origem usando o algoritmo P-Square. Abre uma posição comprada quando o valor ultrapassa o percentil superior e uma posição vendida quando o valor cai abaixo do percentil inferior.

## Parâmetros
- `Percentile` – percentil a estimar.
- `UseReturns` – processar retornos em vez de preços.
- `CandleType` – tipo de dados de candle.
