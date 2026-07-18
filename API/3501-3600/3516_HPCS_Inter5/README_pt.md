# Estratégia HPCS Inter5
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral

A **Estratégia HPCS Inter5** é um script de impulso único convertido do MetaTrader 4 especialista `_HPCS_Inter5_MT4_EA_V01_WE`. Quando a estratégia é iniciada, ela inspeciona as últimas velas concluídas e, se o preço de fechamento de cinco barras atrás for superior ao fechamento mais recente, envia uma ordem de compra de mercado. As distâncias protetoras opcionais de stop-loss e take-profit emulam o comportamento baseado em pip do EA original.

## Lógica de negociação

1. Assine a série de velas configurada e mantenha os últimos seis fechamentos concluídos.
2. Depois que o buffer for preenchido, compare o fechamento de cinco barras atrás com o fechamento mais recente (`Close[5] > Close[1]` em termos de MetaTrader).
3. Se a condição for satisfeita e nenhuma negociação tiver sido realizada ainda, envie uma ordem de compra a mercado com o volume configurado.
4. As ordens de proteção são armadas uma vez na inicialização por meio de `StartProtection`, usando a conversão de pip no estilo MetaTrader: instrumentos com 3 ou 5 decimais multiplicam `PriceStep` por 10 para determinar o tamanho do pip, caso contrário, o `PriceStep` bruto é usado.

A estratégia não abre negociações adicionais e ignora todos os sinais subsequentes quando a primeira posição é preenchida.

## Parâmetros

| Nome | Padrão | Descrição |
| --- | --- | --- |
| `Candle Type` | Período de 1 minuto | Tipo de vela usado para coletar os preços de fechamento. Defina-o para o período de tempo que corresponde ao intervalo de sinal desejado. |
| `Stop Loss (pips)` | 10 | Distância para o stop loss de proteção em MetaTrader pips. Um valor de `0` desativa a parada. |
| `Take Profit (pips)` | 10 | Distância para o take-profit protetor em MetaTrader pips. Um valor de `0` desativa o lucro. |
| `Trade Volume` | 1 | Volume da ordem de mercado submetida quando a condição de entrada é acionada. |

## Notas de implementação

- A estratégia requer um `Security.PriceStep` configurado (ou `Security.Step`) para converter distâncias pip. Se esta informação estiver faltando, os deslocamentos de proteção permanecerão inativos, mas o sinal de entrada ainda funcionará.
- Somente velas finalizadas (`CandleStates.Finished`) são processadas para corresponder ao comportamento MetaTrader que depende de `Close[1]` e valores mais antigos.
- O buffer interno contém exatamente seis fechamentos sem usar o histórico do indicador, respeitando a natureza minimalista da fonte EA.
- `IsFormedAndOnlineAndAllowTrading()` é verificado antes de enviar o pedido para garantir que o ambiente esteja pronto para execução.

## Dicas de uso

1. Atribua um instrumento Forex com configurações adequadas de preço e volume.
2. Ajuste o `Candle Type` para corresponder ao período que você deseja analisar.
3. Deixe o stop-loss ou o take-profit em zero se preferir gerenciar as saídas manualmente.
4. Reinicie a estratégia sempre que desejar reavaliar a condição de entrada, pois ela é acionada apenas uma vez por sessão.
