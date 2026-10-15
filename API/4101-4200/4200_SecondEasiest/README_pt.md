# Segunda estratégia mais fácil
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A segunda estratégia mais fácil é a porta StockSharp do especialista MetaTrader *Second_Easiest.mq4*. O robô original escaneia o
vela diária do pregão atual e abre uma única posição intradiária assim que o preço provar que está tendendo para longe do
o dia está aberto. Quando o mercado fecha o especialista liquida qualquer exposição, preparando-se para a próxima sessão. O StockSharp
A versão preserva esse comportamento de breakout intradiário enquanto aproveita as vantagens do API de alto nível da estrutura para velas
assinaturas, gerenciamento de pedidos e rastreamento de posição.

Ao contrário das estratégias de momentum que requerem múltiplos indicadores, o Second Easiest só precisa da abertura, alta e baixa do
dia atual. Isto o torna muito leve, ao mesmo tempo que reage aos primeiros sinais de convicção direcional. O código mantém
uma posição de cada vez e nunca reverte imediatamente; a nova negociação só poderá ser aberta após o encerramento da anterior.

## Lógica de negociação
1. Assine a série de velas intradiárias definidas por `CandleType`. O padrão é um período de um minuto, o que fornece uma previsão antecipada
visão dos extremos diários, permanecendo compatível com a lógica diária do EA original.
2. Para cada vela finalizada, atualize o registro na memória dos preços de abertura, máximo e mínimo da sessão. A primeira vela processada
num novo dia de negociação define todos os três valores; as velas subsequentes expandem apenas a máxima ou a mínima sempre que um novo extremo é alcançado.
3. Ignore novas configurações quando o relógio atingir `EntryCutoffHour`. O código MetaTrader para de abrir negociações às 16:00, horário do servidor e
a porta segue a mesma regra.
4. Uma posição longa é permitida somente quando o fechamento atual for negociado acima da abertura diária **e** da distância entre a abertura e o
o mínimo diário excede `RangePointsThreshold`. Isso reproduz as condições "Bid > open" e "open - low > 15 points" de MQL.
5. Uma posição curta é permitida somente quando o fechamento atual estiver abaixo da abertura diária **e** a distância entre a máxima diária e
a abertura excede o mesmo limite.
6. Sempre que aparecer um sinal de entrada e nenhuma posição estiver aberta, envie uma ordem de mercado usando `TradeVolume` lotes. Os métodos auxiliares de
a classe base `Strategy` se encarrega de selecionar o lado correto.
7. Depois que o mercado atingir `MarketCloseHour`, nivele qualquer exposição existente ligando para `ClosePosition()`. Nenhuma nova negociação é feita
após esse corte até o início da próxima sessão.

## Parâmetros
| Nome | Tipo | Padrão | Descrição |
| --- | --- | --- | --- |
| `CandleType` | `DataType` | Período de 1 minuto | Velas intradiárias primárias que orientam a lógica de entrada e saída. |
| `TradeVolume` | `decimal` | `1` | Tamanho do lote usado para cada ordem de mercado. |
| `EntryCutoffHour` | `int` | `16` | Hora (0-23) após a qual a estratégia se recusa a abrir novas posições. |
| `MarketCloseHour` | `int` | `20` | Hora (0-23) quando qualquer posição aberta é fechada à força. |
| `RangePointsThreshold` | `decimal` | `15` | Distância mínima, expressa em pontos de corretagem, entre a abertura diária e o extremo mais próximo. |

## Diferenças em relação à versão MetaTrader
- A versão StockSharp rastreia as posições de forma líquida. O comportamento é idêntico à lógica original de ordem única
porque apenas uma negociação pode ser aberta a qualquer momento e a posição é achatada antes que novas entradas sejam avaliadas.
- MetaTrader recupera a abertura, o máximo e o mínimo em execução por meio de chamadas `iOpen/iHigh/iLow` no período diário. O porto reconstrói
as mesmas informações das velas intradiárias, evitando chamadas de indicadores proibidas e garantindo que os dados permaneçam disponíveis mesmo quando
o feed da corretora não fornece barras diárias.
- O fechamento do pedido é realizado por meio de `ClosePosition()` em vez de percorrer os identificadores do ticket. O resultado final é o mesmo:
a exposição aberta é removida assim que a hora de fechamento configurada for atingida.
- Se o `PriceStep` do título não for fornecido, a conversão tratará o `RangePointsThreshold` como uma distância de preço absoluta.
Esta alternativa de segurança mantém o sistema operacional em instrumentos que reportam preços sem metadados de etapas.

## Notas de uso
- `Volume` está definido como `TradeVolume` em `OnStarted`, portanto, a alteração do parâmetro afeta imediatamente os pedidos subsequentes sem
modificando o resto do código.
- Ao escolher um `CandleType` diferente, certifique-se de que ele ainda forneça granularidade suficiente para rastrear a abertura/alta/baixa intradiária
com precisão. Por exemplo, velas de cinco minutos funcionam bem, mas barras horárias podem atrasar a detecção de extremos diários.
- Aumente `RangePointsThreshold` para filtrar sessões de baixa volatilidade. Diminuí-lo permite que a estratégia seja acionada mesmo quando
o intervalo inicial é pequeno.
- Como o algoritmo fecha todas as posições no final do dia, não requer margem overnight. Corretores que aplicam
as quebras de sessão também zerarão os contadores de intervalo internos automaticamente quando a negociação for retomada.
