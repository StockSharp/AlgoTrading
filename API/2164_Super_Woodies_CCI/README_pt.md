# Estratégia Super Woodies CCI
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia é uma conversão do consultor especialista original MQL5 *Exp_SuperWoodiesCCI*. Opera com base na direção do Índice de Canal de Commodities (CCI) calculado em um período de tempo superior.

## Lógica

- Calcular o CCI com um período configurável.
- Quando o CCI cruza acima de zero:
  - Opcionalmente fechar posições vendidas.
  - Opcionalmente abrir uma posição comprada.
- Quando o CCI cruza abaixo de zero:
  - Opcionalmente fechar posições compradas.
  - Opcionalmente abrir uma posição vendida.

Apenas velas concluídas são processadas e a estratégia opera em um tipo de vela especificado.

## Parâmetros

- **CciPeriod** – período para o cálculo do CCI.
- **CandleType** – período de tempo das velas a analisar.
- **AllowLongEntry** – habilitar abertura de posições compradas.
- **AllowShortEntry** – habilitar abertura de posições vendidas.
- **AllowLongExit** – habilitar fechamento de posições compradas quando o CCI é negativo.
- **AllowShortExit** – habilitar fechamento de posições vendidas quando o CCI é positivo.

## Notas

A estratégia utiliza a API de alto nível do StockSharp com `SubscribeCandles` e ligação de indicadores. Os métodos de trading `BuyMarket` e `SellMarket` são usados para o gerenciamento de posições.
