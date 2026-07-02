# Estratégia de pedidos pendentes às 9h em GBP
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia é uma conversão StockSharp do especialista original MetaTrader 4 localizado em `MQL/7687/Gbp9am.mq4`. Ele recria a rotina de rompimento das 9h de Londres, que arma duas ordens pendentes em torno do preço atual e mantém no máximo uma negociação ativa durante a sessão.

## Ideia de negociação

1. Nas *horas* e *minutos* configurados, a estratégia considera o último fechamento da vela como um instantâneo do preço.
2. Um stop de compra é colocado acima do preço instantâneo e um stop de venda é colocado abaixo dele. Ambas as ordens compartilham o mesmo volume e carregam níveis de stop loss individuais juntamente com uma distância de take-profit compartilhada.
3. Quando uma das ordens é atendida, a outra é cancelada imediatamente, de modo que apenas uma posição fica ativa.
4. A posição aberta é gerenciada com níveis sintéticos de stop-loss e take-profit que são verificados em cada vela concluída.
5. Uma hora de encerramento diária pode ser habilitada para nivelar qualquer exposição restante e remover ordens pendentes após a sessão de Londres.
6. Se ambas as ordens pendentes forem removidas sem negociação, ou se o horário do mercado se afastar da hora de observação, a estratégia será rearmada no dia seguinte exatamente como na versão MetaTrader.

As compensações de pip são aproximadas usando a etapa de preço do instrumento. Se o corretor fornecer pips fracionários (3 ou 5 dígitos decimais), a lógica será automaticamente dimensionada para incrementos típicos de 0,1 pip.

## Referência de parâmetro

| Parâmetro | Descrição |
|-----------|-------------|
| `Volume` | Volume de ordens (lotes) compartilhado por ambas as ordens pendentes. |
| `LookHour` | Horário de troca que representa 9h, horário de Londres. |
| `LookMinute` | Minuto dentro da hora de visualização em que o instantâneo é tirado. |
| `CloseHour` | Hora em que todas as posições e ordens pendentes são fechadas à força. |
| `UseCloseHour` | Habilita ou desabilita o procedimento de encerramento diário. |
| `TakeProfitPips` | Distância alvo em pips, aplicada simetricamente em ambas as direções. |
| `BuyDistancePips` | Compensação em pips entre o preço instantâneo e a entrada de stop de compra. |
| `SellDistancePips` | Compensação em pips entre o preço instantâneo e a entrada do stop de venda. |
| `BuyStopLossPips` | Distância de stop-loss em pips para negociações longas. |
| `SellStopLossPips` | Distância de stop-loss em pips para negociação a descoberto. |
| `CandleType` | Série de velas usada para gerenciamento de tempo e parada (padrão 1 minuto). |

## Notas comportamentais

- A estratégia ignora velas inacabadas para evitar múltiplos gatilhos dentro da mesma barra.
- Os preços dos pedidos são arredondados para o tick válido mais próximo usando a etapa do preço do título.
- O portão de rearme reflete o sinalizador `clear_to_send` do especialista MQL: uma vez que o straddle diário é colocado, nenhuma nova ordem é enviada até que ambas as ordens pendentes desapareçam enquanto o mercado estiver fora da hora de visualização ou o relógio alcance a hora anterior ao próximo sinal.
- Quando `UseCloseHour` está ativado, a estratégia sai de qualquer negociação aberta com uma ordem de mercado e limpa as ordens pendentes quando a hora de fechamento é atingida.
- Os cálculos de pip baseiam-se em velas históricas, portanto, as distâncias exatas de stop/alvo podem diferir ligeiramente do ambiente MetaTrader baseado em ticks, especialmente em símbolos com grandes spreads.

## Gestão de risco

A conversão mantém as paradas e metas estáticas originais. Não há parada móvel ou lógica de escala. A proteção de posição é ativada em `OnStarted` para que desconexões inesperadas não deixem a conta desprotegida.

## Uso

1. Configure os valores `Volume`, `LookHour` e `LookMinute` para corresponder ao fuso horário de troca do seu feed de dados.
2. Ajuste os parâmetros de distância para refletir a estrutura de spread da sua corretora.
3. Anexe a estratégia a um símbolo GBPUSD (ou outro par FX de sua escolha) e inicie-a antes da sessão de Londres.
4. Monitore as negociações resultantes no gráfico StockSharp que é desenhado automaticamente após o início.

A implementação segue as diretrizes de `AGENTS.md`: ela usa a assinatura de vela de alto nível API, emprega parâmetros de estratégia com metadados de UI e evita pesquisas de histórico de baixo nível.
