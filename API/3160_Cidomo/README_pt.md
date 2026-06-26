# Estratégia de Cidomo
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Sistema de Rompimento convertido do consultor especialista MetaTrader 5 "Cidomo". A estratégia aguarda uma nova vela no período de tempo configurado, mede o intervalo de negociação recente e coloca ordens stop pareadas acima e abaixo desse intervalo. Gerencia o risco com níveis clássicos de stop-loss/take-profit, um trailing stop opcional e dois modos de gestão de capital (volume fixo ou risco percentual).

## Como funciona

1. A cada vela concluída de `CandleType`, coletar os últimos máximos e mínimos de `BarsCount` para definir o canal de curto prazo.
2. Colocar uma ordem buy stop em `highest + IndentPips` e uma sell stop em `lowest - IndentPips` (ambos os valores expressos em pips e convertidos para preços absolutos).
3. Quando uma ordem stop é acionada, a ordem pendente oposta é cancelada imediatamente.
4. Para uma posição aberta, a estratégia acompanha:
   - Stop-loss inicial (`StopLossPips`) e take-profit (`TakeProfitPips`).
   - Um trailing stop escalonado (`TrailingStopPips` / `TrailingStepPips`). O stop só é movido após o preço avançar pelo menos `TrailingStop + TrailingStep`, espelhando o EA original.
   - Saídas de mercado são usadas para emular as chamadas `PositionModify` do MetaTrader quando o stop ou take-profit é tocado.
5. Quando `UseTimeFilter` está habilitado, novas ordens são enviadas apenas dentro de ±30 segundos de `StartHour:StartMinute` (horário do servidor), replicando a janela de negociação estreita do script fonte.

## Gestão de capital

- **FixedVolume**: sempre negocia o `TradeVolume` exato especificado pelo usuário.
- **RiskPercent**: calcula o tamanho da ordem de modo que uma operação perdedora na distância de stop-loss configurada reduza o capital em `RiskPercent`. Os volumes são arredondados para o `VolumeStep` do instrumento e limitados entre `MinVolume` / `MaxVolume`.

## Controles de risco

- Os níveis iniciais de stop-loss e take-profit são armazenados localmente e executados via ordens de mercado quando o preço cruza o alvo durante a próxima vela.
- O trailing stop só se move em uma direção e respeita a distância de passo do EA original, evitando pequenos ajustes constantes.
- Se nenhum stop-loss estiver configurado, o dimensionamento de posição baseado em risco automaticamente volta ao `TradeVolume` fixo.

## Parâmetros

| Nome | Tipo | Padrão | Descrição |
| --- | --- | --- | --- |
| `CandleType` | `DataType` | `H4` | Período de tempo usado para construir o intervalo de rompimento. |
| `BarsCount` | `int` | `15` | Número de velas concluídas consideradas ao calcular o máximo mais alto e mínimo mais baixo. |
| `IndentPips` | `decimal` | `3` | Offset (em pips) adicionado acima/abaixo do intervalo antes de enviar ordens stop. |
| `StopLossPips` | `decimal` | `50` | Distância protetora do stop em pips. Um valor de `0` desabilita o stop. |
| `TakeProfitPips` | `decimal` | `50` | Distância da meta de lucro em pips. Um valor de `0` desabilita a meta. |
| `TrailingStopPips` | `decimal` | `35` | Distância do trailing stop em pips. Definir como `0` para desabilitar o trailing. |
| `TrailingStepPips` | `decimal` | `5` | Lucro extra mínimo necessário antes de apertar o trailing stop. |
| `MoneyManagement` | `CidomoMoneyManagementMode` | `RiskPercent` | Escolhe entre tamanho de posição fixo e dimensionamento baseado em risco. |
| `RiskPercent` | `decimal` | `1` | Percentual de capital arriscado por operação quando `MoneyManagement = RiskPercent`. |
| `TradeVolume` | `decimal` | `0.1` | Volume fixo de ordem usado no modo `FixedVolume` ou quando o dimensionamento baseado em risco não pode ser calculado. |
| `UseTimeFilter` | `bool` | `false` | Habilita o filtro de janela de tempo de ±30 segundos. |
| `StartHour` | `int` | `9` | Hora (0-23) do centro da janela de negociação. |
| `StartMinute` | `int` | `58` | Minuto (0-59) do centro da janela de negociação. |

## Notas

- Todos os parâmetros baseados em pips se adaptam automaticamente a cotações de 3 ou 5 dígitos multiplicando o `PriceStep` do instrumento por 10, exatamente como a implementação do MetaTrader.
- Como o StockSharp gerencia stops no lado do cliente neste port, certifique-se de que a estratégia permaneça conectada para que saídas de mercado possam ser emitidas quando os níveis de proteção forem violados.
