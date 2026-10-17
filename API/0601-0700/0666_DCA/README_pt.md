# Estratégia DCA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia compra um valor fixo da moeda cotada em intervalos regulares, seja em um dia da semana escolhido ou após um número definido de candles. Opcionalmente fecha todas as posições após uma data de fim especificada.

### Agenda e volume executados

Padrões: `CandleType=5m`, `CapitalToInvest=200` **unidades da moeda cotada**, `StartDate=DateTime.MinValue`, `EndDate=DateTime.MaxValue`, `CloseAllOnLastCandle=false`, `UseDayOfWeek=false`, `DayOfWeek=Wednesday`, `EveryXCandles=72`.

Negocia somente candles concluídos no intervalo `StartDate <= data < EndDate`. No modo intervalo compra **a cada 72 candles elegíveis**; no modo semanal compra **no primeiro candle elegível de cada quarta-feira**, uma vez por data. `200 / Close` é arredondado **para baixo** a `Security.VolumeStep`, respeitando os limites do instrumento. Somente **compras a mercado** acumulativas, sem cruzamentos EMA nem short. Em EndDate param as compras; com `CloseAllOnLastCandle=true`, vende toda a **posição real restante** uma vez na primeira oportunidade elegível. Não existe stop de preço independente; o slippage pode alterar o valor executado. Os números padrão foram definidos nesta implementação porque o README original não os especificava.



## Parâmetros
- Tipo de candle
- Capital a investir por operação
- Data de início
- Data de fim
- Fechar tudo no último candle
- Usar modo de dia da semana
- Dia da semana
- A cada X candles
