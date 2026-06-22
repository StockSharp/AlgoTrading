# Estratégia Expert AutoLot 20/200
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia abre no máximo uma posição por dia em uma hora definida pelo usuário. Ela compara o preço de abertura de duas barras passadas (T1 e T2). Se a barra anterior for mais alta que a posterior em DeltaShort pips, abre uma posição vendida. Se a barra posterior for mais alta em DeltaLong pips, abre uma posição comprada.

O volume da posição pode ser fixo ou calculado automaticamente a partir do saldo da conta. Quando o saldo diminui em relação à operação anterior, o lote é multiplicado por BigLotSize.

Cada operação usa seu próprio take-profit e stop-loss em pips. Além disso, um tempo máximo de retenção (MaxOpenTime) fecha a operação após o número de horas especificado.

## Parâmetros

- `CandleType` – período das velas processadas (padrão: 1 hora).
- `TradeHour` – hora do dia em que as condições de entrada são verificadas.
- `T1`, `T2` – deslocamentos de barras para comparar preços de abertura.
- `DeltaLong`, `DeltaShort` – diferença mínima de preço de abertura em pips.
- `TakeProfitLong`, `StopLossLong` – proteção para operações compradas em pips.
- `TakeProfitShort`, `StopLossShort` – proteção para operações vendidas em pips.
- `Lot` – volume de trading base.
- `AutoLot` – ativar o cálculo automático de lote.
- `BigLotSize` – multiplicador aplicado após perda.
- `MaxOpenTime` – tempo máximo em horas para manter uma posição.
