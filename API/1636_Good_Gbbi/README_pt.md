# Estratégia Good Gbbi
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia abre uma única posição a uma hora específica do dia com base na diferença entre preços de abertura históricos.

## Lógica

* Trabalha com velas horárias por padrão.
* Na hora `TradeTime` a estratégia compara o preço de abertura de `T1` barras atrás com o preço de abertura de `T2` barras atrás.
* Se a abertura mais antiga for maior que a recente em `DeltaShort` pontos, uma posição vendida é aberta.
* Se a abertura recente for maior que a mais antiga em `DeltaLong` pontos, uma posição comprada é aberta.
* Apenas uma operação por dia é permitida. O trading é habilitado novamente após a hora ser maior que `TradeTime`.
* Cada posição é protegida por níveis individuais de take-profit e stop-loss e pode ser fechada forçosamente após `MaxOpenTime` horas.

## Parâmetros

| Parâmetro | Descrição |
|-----------|-------------|
| `TakeProfitLong` | Distância de take profit em pontos para posições compradas. |
| `StopLossLong` | Distância de stop loss em pontos para posições compradas. |
| `TakeProfitShort` | Distância de take profit em pontos para posições vendidas. |
| `StopLossShort` | Distância de stop loss em pontos para posições vendidas. |
| `TradeTime` | Hora do dia em que as condições de entrada são verificadas. |
| `T1` | Número de barras para trás para o primeiro preço de abertura. |
| `T2` | Número de barras para trás para o segundo preço de abertura. |
| `DeltaLong` | Diferença necessária em pontos para abrir uma posição comprada. |
| `DeltaShort` | Diferença necessária em pontos para abrir uma posição vendida. |
| `MaxOpenTime` | Tempo máximo de manutenção de posição em horas; 0 desativa a verificação. |
| `CandleType` | Tipo de vela a processar. |

## Notas

A ideia original vem do consultor especializado do MetaTrader *GoodG@bi*. Esta portabilidade usa a API de alto nível do StockSharp e processa apenas velas concluídas. Certifique-se de que o `PriceStep` do instrumento esteja configurado corretamente para interpretar os valores em pontos.
