# Estratégia de Fechamento vs Abertura Anterior
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia compara o fechamento da última vela concluída com a abertura da vela anterior.
Abre uma posição comprada quando o último fechamento está acima da abertura anterior e uma posição vendida quando o último fechamento está abaixo da abertura anterior.

## Regras de entrada
- **Long**: O fechamento da vela mais recente completada é maior do que a abertura da vela anterior.
- **Short**: O fechamento da vela mais recente completada é menor do que a abertura da vela anterior.

## Gestão de risco
- Stop loss e take profit opcionais medidos em pontos.
- Trailing do stop loss opcional.

## Parâmetros
- `Volume` – volume da ordem.
- `UseStopLoss` – habilitar stop loss.
- `StopLoss` – distância do stop loss em pontos.
- `UseTakeProfit` – habilitar take profit.
- `TakeProfit` – distância do take profit em pontos.
- `UseTrailingStop` – seguir o stop loss com o movimento do preço.
- `CandleType` – série de velas para cálculos.

## Notas
- Opera apenas em velas completamente formadas.
- Inverte a posição quando o sinal oposto aparece.
