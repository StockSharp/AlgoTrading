# Indicador de Divergência (Qualquer Oscilador)
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Detecta divergências regulares e ocultas entre o preço e o RSI. A estratégia compra em divergências de alta e vende em divergências de baixa.

## Parâmetros
- **Pivot Left** – barras à esquerda do pivô
- **Pivot Right** – barras à direita do pivô
- **Min Range** – barras mínimas entre pivôs
- **Max Range** – barras máximas entre pivôs
- **RSI Length** – período do RSI
- **Candle Type** – tipo de série de velas

## Indicador
- RSI

## Regras
- **Entrada**:
  - Comprar quando o preço faz uma mínima mais baixa enquanto o RSI faz uma mínima mais alta (divergência de alta)
  - Vender quando o preço faz uma máxima mais alta enquanto o RSI faz uma máxima mais baixa (divergência de baixa)
  - Divergências ocultas operam na direção oposta
