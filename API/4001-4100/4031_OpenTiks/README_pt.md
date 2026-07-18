# Estratégia OpenTiks
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A Estratégia OpenTiks transporta o clássico MetaTrader consultor especialista `OpenTiks.mq4` para o ecossistema StockSharp. O robô original
procurei uma escada de velas com máximos estritamente monótonos e aberturas para detectar rompimentos precoces. Uma vez que um sinal surgiu, ele
abriu uma ordem de mercado, opcionalmente anexou um stop de proteção e, em seguida, seguiu a posição enquanto realizava lucros progressivamente
reduzindo repetidamente a exposição pela metade. A versão StockSharp reflete essas ideias usando chamadas API de alto nível, assinaturas de velas,
e os auxiliares de ordem integrados para que a lógica seja executada dentro do Designer, do Runner ou de qualquer aplicativo S# personalizado.

## Detecção de padrões
Uma negociação pode ser lançada quando **quatro velas consecutivas** satisfazem um dos seguintes padrões:

- **Rompimento de alta** – para a vela atual e as três barras anteriores: cada `High` é estritamente superior à anterior
`High`, e cada `Open` é estritamente superior ao `Open` anterior.
- **Rompimento de baixa** – para a mesma janela de quatro barras: cada `High` é estritamente inferior ao `High` anterior, e cada `Open`
é estritamente inferior ao `Open` anterior.

Os sinais são avaliados em velas concluídas entregues pelo `CandleType` configurado. Quando a condição de rompimento for atendida, o
estratégia envia uma ordem de mercado com o volume configurado (normalizado para o título `VolumeStep` e limitado por `MinVolume`
e `MaxVolume`). O parâmetro `MaxOrders` limita quantas entradas simultâneas podem existir; um valor zero desativa a verificação,
enquanto qualquer número positivo bloqueia novas negociações quando a posição líquida absoluta dividida pelo volume de pedidos normalizado atinge esse valor
limite.

## Gestão de riscos e saídas
- **Stop loss** – se `StopLossPoints` for maior que zero, a estratégia monitora a última vela em busca de reversões de preços. Longo
as posições são liquidadas quando a mínima da vela penetra `entryPrice - StopLossPoints × PriceStep`. As posições curtas saem quando
os toques altos `entryPrice + StopLossPoints × PriceStep`.
- **Trailing stop** – quando o preço avança pelo menos `TrailingStopPoints × PriceStep` além da entrada, um trailing stop é armado
na mesma distância atrás (para posições compradas) ou à frente (para posições vendidas) do fechamento. Cada vez que o nível final melhora, o
a posição restante é opcionalmente reduzida.
- **Realização progressiva de lucros** – quando `UsePartialClose` está ativado, a estratégia sempre fecha metade da exposição atual
o trailing stop avança. Os volumes são arredondados para o `VolumeStep` do instrumento. Se o tamanho reduzido pela metade ficar abaixo
`MinVolume`, toda a posição é fechada, correspondendo ao comportamento do especialista MetaTrader.

Todos os cálculos de stop e trailing são realizados em velas finalizadas, então as saídas ocorrem no próximo fechamento da barra, em vez de em cada
carrapato de entrada. Isso mantém a implementação consistente com o API de alto nível de API, mantendo-se próximo do original
ideia de reagir a novas barras.

## Parâmetros
| Nome | Tipo | Padrão | Descrição |
| --- | --- | --- | --- |
| `OrderVolume` | `decimal` | `0.1` | Tamanho base do lote para cada entrada no mercado. A estratégia normaliza de acordo com a etapa e os limites do volume do título. |
| `StopLossPoints` | `decimal` | `0` | Distância de parada protetora expressa em faixas de preço (etapas de preço). Um valor zero desativa a parada. |
| `TrailingStopPoints` | `decimal` | `30` | Distância mantida pelo trailing stop quando a posição passa para o lucro, também em faixas de preço. |
| `MaxOrders` | `int` | `1` | Número máximo de entradas abertas simultaneamente. Zero remove a restrição. |
| `UsePartialClose` | `bool` | `true` | Ativa a lógica de redução pela metade que bloqueia os ganhos sempre que o trailing stop avança. |
| `CandleType` | `DataType` | `1 minute` período de tempo | Assinatura de vela primária usada para avaliação de sinal e verificações de rastreamento. |

## Notas de implementação
- StockSharp trabalha com **posições líquidas**, então todas as ordens para o título configurado acumulam em uma única compra ou venda
exposição. O parâmetro `MaxOrders`, portanto, atua na posição agregada e não em tickets MetaTrader individuais.
- O rastreamento baseado em velas significa que as verificações de parada acontecem uma vez por barra concluída. Os comerciantes que precisam de proteção no nível do tick podem reduzir o
tamanho da vela ou estender a lógica para assinar negociações.
- Os fechamentos parciais respeitam os metadados do instrumento (`VolumeStep`, `MinVolume`, `MaxVolume`) para evitar pedidos rejeitados.
- Os comentários em inglês embutidos destacam os principais pontos de decisão para que o arquivo funcione como material educacional ao adaptar a ideia
a outras experiências inovadoras ou de gestão de dinheiro.

## Dicas de uso
1. Selecione um tipo de vela que corresponda ao período de tempo usado na configuração original do MetaTrader (por exemplo, M1 ou M5).
2. Verifique as configurações de etapa e lote do instrumento; o padrão `OrderVolume` de `0.1` é adequado para contratos estilo Forex, mas pode ser
ajustado para futuros, ações ou símbolos criptográficos.
3. Experimente `TrailingStopPoints` e `UsePartialClose` para encontrar um equilíbrio entre bloqueio agressivo de lucro e permissão
os vencedores correm.
4. Combine a estratégia com gráficos StockSharp para confirmar visualmente o padrão de escada e observar as saídas parciais em tempo real
tempo.
