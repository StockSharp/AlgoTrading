# Estratégia de bisturi ZigAndZag
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
ZigAndZagScalpelStrategy é uma porta StockSharp do kit de ferramentas MetaTrader 4 "ZigAndZag" (pasta 8304).
O pacote original combina um indicador personalizado e um consultor especializado. Duas janelas ZigZag são usadas:

* **KeelOver** – um detector de balanço longo que marca a tendência dominante.
* **Slalom** – um detector de oscilação de retrospectiva curta que define interrupções acionáveis.

Quando o ZigZag de longo prazo sobe, a estratégia procura o próximo mínimo do Slalom e espera pelo preço
para subir um número configurável de pontos acima desse pivô. Uma ordem de compra a mercado é emitida assim que o
a distância de fuga é atingida. Uma regra simétrica abre uma posição curta quando a tendência KeelOver muda
para baixo, o Slalom imprime uma nova máxima e o preço cai abaixo dela. As posições podem opcionalmente ser fechadas
assim que o pivô oposto do Slalom for confirmado, imitando a remoção da seta limite do indicador.

A implementação mantém o limitador diário de negociação do consultor especialista. Apenas um número configurável
de negociações é permitido por dia de negociação, zerando automaticamente à meia-noite (horário de câmbio). Isto
reproduz o sinalizador "novo dia" do código original.

## Como funciona
1. Assine o stream de vela principal definido por `CandleType`.
2. Alimente duas instâncias `ZigZagIndicator`:
   * Profundidade = `KeelOverLength` para o detector de tendências.
   * Profundidade = `SlalomLength` para sinais de entrada.
3. Acompanhe o pivô KeelOver mais recente para determinar se a tendência é de alta (o último pivô é baixo)
ou para baixo (o último pivô é alto).
4. Quando o indicador Slalom publicar um novo pivô, prepare um rompimento nessa direção.
5. Calcule o preço ponderado `(5×Close + 2×Open + High + Low) / 9`. Se o preço se mover mais do que
`BreakoutDistancePoints` (convertido em unidades de preço) longe do pivô enquanto a tendência suporta
o movimento, execute uma ordem de mercado.
6. Feche as posições existentes quando a tendência global mudar ou o pivô oposto do Slalom aparecer e
`CloseOnOppositePivot` está ativado.
7. Redefina o contador diário de negociações a cada mudança de dia do calendário.

Os parâmetros `DeviationPoints` e `Backstep` são compartilhados entre ambas as instâncias do ZigZag para que o
a estrutura swing corresponde aos buffers do indicador MetaTrader.

## Parâmetros
| Nome | Padrão | Descrição |
| ---- | ------- | ----------- |
| `CandleType` | `15m` | Período primário usado para construir ambas as escadas ZigZag. |
| `KeelOverLength` | `55` | Lookback ZigZag de longo prazo que define a tendência (original `KeelOver`). |
| `SlalomLength` | `17` | Lookback ZigZag de curto prazo usado para entradas (original `Slalom`). |
| `DeviationPoints` | `5` | Tamanho mínimo de swing em pontos antes que um novo pivô ZigZag seja confirmado. |
| `Backstep` | `3` | Distância necessária da barra entre pivôs consecutivos. |
| `BreakoutDistancePoints` | `2` | Distância de um pivô (em pontos) antes de disparar uma ordem. |
| `MaxTradesPerDay` | `1` | Número máximo de entradas por dia corrido. Espelha o sinalizador `newday` original. |
| `CloseOnOppositePivot` | `true` | Fechar posições abertas quando o Slalom ZigZag produzir o balanço oposto. |

Todos os parâmetros baseados em pontos são convertidos em unidades de preço usando `Security.PriceStep`. Se o instrumento
não tem nenhuma etapa de preço configurada, um valor de `1` é usado para manter a estratégia funcional durante o teste.

## Notas de uso
* A estratégia opera com ordens de mercado (`BuyMarket` / `SellMarket`). Anexe suas próprias regras de risco
ou ajudantes de stop-loss se for necessária uma gestão de risco mais rigorosa.
* Como ambos os indicadores ZigZag compartilham o mesmo fluxo de velas, certifique-se de que o `CandleType` escolhido seja
suportado pelo seu adaptador de dados.
* `MaxTradesPerDay = 1` reproduz o comportamento "uma negociação por dia". Aumente o valor se precisar
múltiplas entradas durante a mesma sessão.
* Defina `CloseOnOppositePivot = false` para manter as posições abertas até que a tendência global seja revertida em vez de
reagindo a cada oscilação de curto prazo.

## Diferenças versus o consultor especialista MT4
* A versão MetaTrader colocou setas de limite pendentes. A porta StockSharp executa breakouts com
ordens imediatas de mercado para permanecer dentro do nível superior API.
* A gestão de riscos, o dimensionamento de lotes e os fechamentos parciais são intencionalmente omitidos. Use a posição StockSharp
dimensionando ajudantes se você precisar de controle avançado de capital.
* Os buffers de indicadores 4/5/6 são substituídos por lógica de estratégia direta e anotações de gráfico via
`DrawIndicator` e `DrawOwnTrades`.

## Extensões recomendadas
* Adicione parâmetros de stop-loss e take-profit vinculados a ATR ou oscilações recentes do ZigZag.
* Sobreponha o indicador original com `BreakoutDistancePoints = 0` para visualizar a escada dinâmica bruta.
* Combine com um filtro de sessão (`IsFormedAndOnlineAndAllowTrading`) para limitar o horário de negociação.
