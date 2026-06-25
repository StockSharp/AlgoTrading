# Estratégia Forex Fraus M1
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A estratégia Forex Fraus M1 replica o assessor especializado MetaTrader 5 "Forex Fraus M1" no framework StockSharp. É um sistema contrário que monitora um oscilador Williams %R de longo período (período 360) em velas de um minuto. Sempre que o oscilador toca valores extremos, a estratégia tenta desaparecer do movimento, visando uma rápida reversão ao ponto médio do intervalo recente. A implementação mantém o gerenciamento de dinheiro do especialista original, incluindo horas de trading opcionais, níveis estáticos de stop-loss e take-profit medidos em pips e um trailing stop baseado em pips.

## Lógica de trading
- **Indicador**: Williams %R com um período de 360.
- **Sinal de compra**: Quando Williams %R cai abaixo de `-99.9`, o mercado é considerado extremamente sobrevendido. A estratégia envia uma ordem de compra a mercado se não há posição comprada existente. Se `CloseOppositePositions` está habilitado, qualquer exposição vendida é fechada na mesma solicitação de ordem.
- **Sinal de venda**: Quando Williams %R sobe acima de `-0.1`, o mercado está extremamente sobrecomprado. A estratégia emite uma ordem de venda a mercado, opcionalmente fechando primeiro qualquer exposição comprada aberta.
- **Filtro de tempo**: Quando `UseTimeControl` está habilitado, a estratégia só avalia sinais entre `StartHour` (inclusive) e `EndHour` (exclusive). Se a sessão ultrapassa a meia-noite (`StartHour > EndHour`), o trading é permitido de `StartHour` a 23 e de 0 a `EndHour - 1`.

## Gestão de risco
- **Stop-loss**: Calculado como `StopLossPips * PipSize` abaixo (para comprados) ou acima (para vendidos) do preço de entrada. Quando o mínimo da vela toca o nível de stop, a posição é fechada a mercado.
- **Take-profit**: Calculado como `TakeProfitPips * PipSize` acima (para comprados) ou abaixo (para vendidos) do preço de entrada. Quando o máximo/mínimo da vela atinge este nível, a posição é fechada para garantir lucros.
- **Trailing stop**: Se tanto `TrailingStopPips` quanto `TrailingStepPips` são positivos, o stop é apertado quando o preço se move pelo menos `TrailingStopPips + TrailingStepPips` pips a favor da operação. Para comprados, o stop acompanha o fechamento menos `TrailingStopPips`; para vendidos, acompanha o fechamento mais `TrailingStopPips`.
- **Tamanho do pip**: `PipSize` define o valor monetário de um pip. Para símbolos Forex de cinco dígitos, defina `PipSize` como `0.0001`, para pares JPY de três dígitos use `0.01`, etc.

A estratégia verifica as condições de stop-loss e take-profit usando máximos/mínimos das velas. Quando ambos são tocados dentro da mesma vela, o stop de proteção tem prioridade, refletindo o comportamento conservador do especialista original.

## Parâmetros
| Nome | Padrão | Descrição |
| --- | --- | --- |
| `OrderVolume` | `0.1` | Volume de operação usado para novas posições. |
| `StopLossPips` | `50` | Distância do stop-loss em pips do preço de entrada. Defina como zero para desabilitar. |
| `TakeProfitPips` | `150` | Distância do take-profit em pips do preço de entrada. Defina como zero para desabilitar. |
| `TrailingStopPips` | `1` | Distância base do trailing stop em pips. Defina como zero para desabilitar o rastreamento. |
| `TrailingStepPips` | `1` | Ganho mínimo adicional em pips antes que o trailing stop se mova. |
| `UseTimeControl` | `true` | Habilita o filtro de sessão intradiária. |
| `StartHour` | `7` | Hora de início da sessão de trading (0-23). |
| `EndHour` | `17` | Hora de fim da sessão de trading (1-24, exclusive). |
| `CloseOppositePositions` | `true` | Se habilitado, reverte as posições existentes em uma única ordem. |
| `WilliamsPeriod` | `360` | Período de retrospectiva para o indicador Williams %R. |
| `CandleType` | `1 minute` | Tipo de vela usado para avaliar Williams %R e as regras de trading. |
| `PipSize` | `0.0001` | Valor de um único pip em unidades de preço. |

## Notas adicionais
- A estratégia usa a API de subscrição de velas de alto nível do StockSharp e a vinculação de indicadores para lógica concisa sem gerenciamento manual de buffers.
- Os cálculos de stop-loss, take-profit e rastreamento ocorrem em velas completadas para evitar agir sobre dados de preço incompletos.
- A implementação chama `StartProtection()` uma vez na inicialização para se alinhar com as diretrizes do projeto, enquanto o gerenciamento real de risco é feito dentro da lógica da estratégia.
- Ajuste o parâmetro `PipSize` para corresponder ao instrumento negociado para que as distâncias baseadas em pips sejam mapeadas corretamente para movimentos de preço.
