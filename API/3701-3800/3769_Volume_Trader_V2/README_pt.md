# Estratégia do Volume Trader V2
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
Volume Trader V2 é uma conversão direta do MetaTrader consultor especialista `Volume_trader_v2_www_forex-instruments_info.mq4`. O sistema original observa como o volume total das últimas velas evolui e utiliza esse fluxo de curto prazo para decidir se uma simples exposição longa ou curta deve estar ativa. A porta StockSharp mantém o comportamento de uma posição por vez, o filtro de hora do dia e o requisito de agir apenas uma vez por vela concluída.

A estratégia assina uma série de velas configuráveis e armazena em cache o volume das duas últimas velas finalizadas. Quando uma nova barra fecha, os volumes das duas barras anteriores (`Volume[1]` e `Volume[2]` de MetaTrader) são comparados e uma direção de negociação atualizada é produzida:

- `Volume[1] < Volume[2]` gera uma tendência **longa**.
- `Volume[1] > Volume[2]` gera uma tendência **curta**.
- Volumes iguais ou horários de negociação desativados removem qualquer exposição aberta.

Antes de enviar um novo pedido, a posição atual é nivelada se apontar na direção oposta, para que a implementação StockSharp corresponda ao ciclo de vida do pedido MetaTrader.

## Parâmetros
| Nome | Padrão | Descrição |
| --- | --- | --- |
| `CandleType` | Período de 5 minutos | Tipo de dados solicitado de `SubscribeCandles`. Defina-o para corresponder ao período do gráfico usado em MetaTrader. |
| `StartHour` | 8 | Primeira hora de negociação (inclusive). Os sinais fora da janela são ignorados e qualquer posição é fechada. |
| `EndHour` | 20 | Última hora de negociação (inclusive). Quando a vela atual começa após esta hora, a estratégia permanece estável. |
| `TradeVolume` | 0,1 | Tamanho do lote replicado de EA. O valor também é atribuído a `Strategy.Volume` para que os métodos auxiliares usem a mesma quantidade. |

Todos os parâmetros são instâncias `StrategyParam<T>` regulares para que possam ser otimizados ou expostos por meio da IU.

## Lógica de negociação
1. Lide apenas com velas finalizadas para garantir a paridade barra por barra com o EA.
2. Armazene em cache `Volume[1]` e `Volume[2]` equivalentes em `_previousVolume` e `_twoBarsAgoVolume` antes de qualquer avaliação de sinal.
3. Valide se o horário de início da vela está entre `StartHour` e `EndHour` (inclusive). Fora deste intervalo, qualquer posição ativa é fechada e nenhuma nova ordem é criada.
4. Calcule a direção desejada:
   - Longo quando o volume mais recente é inferior à barra anterior.
   - Venda quando o volume mais recente é superior à barra anterior.
   - Caso contrário, neutro.
5. Se a direção desejada for diferente da posição atual, feche primeiro a posição oposta (`BuyMarket(-Position)` ou `SellMarket(Position)`).
6. Insira a nova posição usando o `TradeVolume` configurado somente quando a estratégia estiver plana ou posicionada na direção oposta.
7. Atualize os volumes em cache para que o próximo ciclo ainda compare as duas últimas velas concluídas.

Este fluxo garante que nenhum pedido seja feito enquanto uma vela ainda está sendo construída e que a estratégia StockSharp reage exatamente uma vez por barra, assim como a implementação MetaTrader que dependia de `LastBarChecked`.

## Notas adicionais
- `StartProtection()` é chamado em `OnStarted` para reutilizar o auxiliar de proteção da estrutura que monitora a posição atual.
- A propriedade `Comment` espelha as mensagens de diagnóstico EA (`"Up trend"`, `"Down trend"`, `"No trend..."` ou `"Trading paused"`) para simplificar o monitoramento.
- A estratégia não mantém cobranças extras e aproveita a assinatura de vela de alto nível API de acordo com as diretrizes do projeto.
- Defina o tipo de vela, a segurança e o volume para corresponder ao instrumento e ao período usado originalmente em MetaTrader para obter resultados comparáveis.
