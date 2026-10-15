# Estratégia Snowieso
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia combina uma **Média Móvel Ponderada Linear (LWMA)** rápida e lenta com **MACD** e a **Média Móvel Adaptativa de Kaufman (KAMA)** para confirmar a direção da tendência.

## Como Funciona
1. Assinar candles do período escolhido.
2. Calcular os valores de Fast LWMA, Slow LWMA, MACD e KAMA.
3. **Entrada comprada**: ocorre quando a LWMA rápida cruza acima da LWMA lenta, o histograma do MACD é positivo e a KAMA está subindo.
4. **Entrada vendida**: ocorre quando a LWMA rápida cruza abaixo da LWMA lenta, o histograma do MACD é negativo e a KAMA está caindo.
5. Um stop loss e take profit fixos são aplicados via `StartProtection`.

A estratégia fecha posições opostas antes de abrir novas e visualiza indicadores e operações em um gráfico.

## Parâmetros
- `FastLength` – período da LWMA rápida.
- `SlowLength` – período da LWMA lenta.
- `MacdFast`, `MacdSlow`, `MacdSignal` – configuração do MACD.
- `KamaLength` – período de lookback para KAMA.
- `StopLossPoints` – stop loss absoluto em pontos de preço.
- `TakeProfitPoints` – take profit absoluto em pontos de preço.
- `CandleType` – período dos candles processados.

## Uso
Implante a estratégia no instrumento selecionado. O algoritmo assina automaticamente os candles e gerencia posições com base em sinais dos indicadores. A API de alto nível é usada para vinculação de dados e execução de ordens.
