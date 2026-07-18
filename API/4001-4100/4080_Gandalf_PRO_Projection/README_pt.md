# Estratégia de projeção Gandalf PRO
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A estratégia Gandalf PRO é uma versão StockSharp do MetaTrader 4 consultor especialista *Gandalf_PRO*. O robô original constrói um
filtro de suavização adaptativo de uma média móvel ponderada e um componente de tendência recursiva. Quando o preço projetado se move em
pelo menos 15 pips além do preço de mercado atual, o EA entra nessa direção com um stop-loss distante e um take-profit no
nível projetado. A conversão StockSharp reproduz o mesmo filtro e lógica de decisão enquanto depende da vela de alto nível
API então cada cálculo é realizado em barras acabadas.

## Lógica de negociação
1. Assine o período selecionado por `CandleType` (padrão: velas de 1 hora) e processe apenas velas concluídas.
2. Mantenha um histórico contínuo de preços de fechamento grande o suficiente para cobrir o máximo de `CountBuy` e `CountSell` mais uma barra extra.
3. Recrie a função MetaTrader `Out()`: calcule médias móveis lineares ponderadas e simples (usando um deslocamento de uma barra), derive o
componentes recursivos `s` e `t` com os fatores de preço e tendência configurados e obtenha o preço projetado `s[1] + t[1]`.
4. Para configurações longas (`EnableBuy`):
   - Verifique se o preço projetado está pelo menos `15` pips acima do último fechamento (`Bid + 15*x*Point` no MT4).
   - Se nenhuma posição longa estiver aberta, compre o volume configurado (veja `BaseVolume` e `BuyRiskMultiplier`).
   - Armazene o preço projetado como take-profit e calcule o stop loss subtraindo `BuyStopLossPips` convertido em etapas de preço.
5. Para configurações curtas (`EnableSell`):
   - Exija que o preço projetado fique pelo menos `15` pips abaixo do último fechamento.
   - Se nenhuma posição curta estiver aberta, venda o volume configurado (revertendo uma posição longa existente, se necessário).
   - Salve o preço projetado como take-profit e defina os pips de stop loss `SellStopLossPips` acima do mercado.
6. Enquanto existir uma posição, monitore cada vela concluída:
   - Saia das posições compradas se a mínima da vela cruzar o stop armazenado ou a máxima atingir o take-profit.
   - Saia das posições vendidas se a máxima da vela cruzar o stop ou a mínima atingir o alvo.
   - As saídas usam `ClosePosition()` que nivela a exposição líquida em StockSharp.

## Parâmetros
| Nome | Tipo | Padrão | Descrição |
| --- | --- | --- | --- |
| `EnableBuy` | `bool` | `true` | Permita que a estratégia abra posições longas. |
| `CountBuy` | `int` | `24` | Comprimento do filtro de suavização usado para projeções longas. |
| `BuyPriceFactor` | `decimal` | `0.18` | Peso do fechamento da corrente no filtro recursivo longo. |
| `BuyTrendFactor` | `decimal` | `0.18` | Peso aplicado ao termo de tendência na construção da projeção longa. |
| `BuyStopLossPips` | `int` | `62` | Distância de stop-loss para posições longas, medida em pips. |
| `BuyRiskMultiplier` | `decimal` | `0` | Multiplicador aplicado a `BaseVolume` antes de enviar um pedido longo (0 mantém o volume base). |
| `EnableSell` | `bool` | `true` | Permita que a estratégia abra posições curtas. |
| `CountSell` | `int` | `24` | Comprimento do filtro de suavização usado para projeções curtas. |
| `SellPriceFactor` | `decimal` | `0.18` | Peso do fechamento da corrente no filtro recursivo curto. |
| `SellTrendFactor` | `decimal` | `0.18` | Peso aplicado ao termo de tendência na construção da projeção curta. |
| `SellStopLossPips` | `int` | `62` | Distância de stop-loss para posições curtas, medida em pips. |
| `SellRiskMultiplier` | `decimal` | `0` | Multiplicador aplicado a `BaseVolume` antes de enviar uma ordem curta (0 mantém o volume base). |
| `BaseVolume` | `decimal` | `1` | Tamanho base do pedido usado quando ambos os multiplicadores de risco são zero. |
| `CandleType` | `DataType` | Período de 1 hora | Série de velas processada pela estratégia. |

## Diferenças do original MetaTrader EA
- MetaTrader pode realizar compras e vendas independentes de ingressos simultaneamente. StockSharp usa posições líquidas, então a porta fecha ou
inverte uma posição existente antes de abrir o lado oposto.
- A função de lote MT4 usou margem livre de conta. A conversão expõe `BaseVolume` e dois multiplicadores de risco; quando eles são zero
o volume base é usado como está, caso contrário, o volume é simplesmente dimensionado (`BaseVolume * RiskMultiplier`).
- Os níveis de stop-loss e take-profit são executados monitorando velas concluídas. Os preenchimentos intrabarras podem, portanto, ser diferentes de MetaTrader
onde as ordens de proteção são gerenciadas pela corretora.
- O ajuste `Digits`/`Point` de cinco dígitos é emulado inspecionando `Security.Decimals` e `Security.PriceStep` para converter pip
distâncias em preços absolutos.
- Todos os cálculos dos indicadores são realizados em código gerenciado sem chamar `iMA`; o filtro recursivo é recriado em
`CalculateTarget` usando os mesmos coeficientes da função MQL.

## Notas de uso
- Atribua o instrumento desejado a `Strategy.Security` antes de começar. A estratégia lança uma exceção se nenhuma segurança estiver anexada.
- Configure `BaseVolume` para corresponder ao tamanho do contrato esperado pelo seu local; ajuste os multiplicadores de risco apenas se quiser escalar
a exposição relativa ao volume base.
- O histórico da vela deve conter pelo menos `max(CountBuy, CountSell) + 1` barras antes que qualquer negociação possa ser gerada. Forneça o suficiente
dados de aquecimento ou inicie a estratégia com velas históricas carregadas.
- O buffer de entrada de 15 pip é fixo (assim como no EA). Aumente `CountBuy`/`CountSell` para suavizar a projeção ou ajustar o
fatores de preço/tendência para corresponder ao comportamento observado em MetaTrader.
- Como as saídas dependem dos extremos das velas, ative um período de tempo adequado à sua latência de execução. Prazos mais baixos reagirão mais cedo
mas requerem mais dados históricos e podem gerar mais sinais.

## Detalhes de implementação
- Usa `SubscribeCandles()` com `Bind(ProcessCandle)` para que cada decisão seja baseada em velas finalizadas.
- Mantém uma lista compacta de fechamentos recentes e reconstrói o filtro recursivo `s`/`t` sob demanda, imitando a rotina `Out()`.
- Converte deslocamentos baseados em pip por meio do tamanho do tick do instrumento e da precisão decimal para replicar o dimensionamento MetaTrader `x * Point`.
- `ClosePosition()` é invocado quando os níveis de proteção são violados, garantindo que a posição líquida seja achatada antes que outra entrada seja
considerado.
