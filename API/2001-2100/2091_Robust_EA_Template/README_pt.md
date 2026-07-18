# Estratégia de Modelo Robust EA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Estratégia que implementa o Modelo Robust EA do MQL.
Usa o Commodity Channel Index (CCI) e o Relative Strength Index (RSI) para gerar sinais de entrada e aplica take profit e stop loss fixos.

## Lógica
- Comprar quando CCI está em -200..-150 ou -100..-50 e RSI está entre 0 e 25.
- Vender quando CCI está entre 50 e 150 e RSI está entre 80 e 100.
- O stop loss e o take profit são definidos em pips e convertidos em pontos de preço.

## Parâmetros
- `Candle Type` – série de dados de velas.
- `CCI Period` – período do indicador CCI.
- `RSI Period` – período do indicador RSI.
- `Take Profit (pips)` – distância para o alvo de lucro.
- `Stop Loss (pips)` – distância para o stop loss.
- `Volume` – volume da ordem.
