# Estratégia Exp TSI CCI
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia calcula o True Strength Index (TSI) baseado no Commodity Channel Index (CCI) e opera em cruzamentos com uma linha de sinal.

## Lógica
- Calcular o CCI usando o período especificado.
- Inserir os valores do CCI no True Strength Index com comprimentos de suavização curto e longo.
- Suavizar o TSI resultante com um EMA para obter uma linha de sinal.
- Comprar quando o TSI cruza acima da linha de sinal.
- Vender quando o TSI cruza abaixo da linha de sinal.

## Parâmetros
- `Candle Type` – período de velas usado para análise.
- `CCI Period` – período para o Commodity Channel Index.
- `TSI Short Length` – comprimento de suavização curto do TSI.
- `TSI Long Length` – comprimento de suavização longo do TSI.
- `Signal Length` – comprimento EMA para a linha de sinal do TSI.

## Indicadores
- Commodity Channel Index
- True Strength Index
- Exponential Moving Average

## Aviso
Esta estratégia é fornecida apenas para fins educacionais e não constitui aconselhamento de investimento.
