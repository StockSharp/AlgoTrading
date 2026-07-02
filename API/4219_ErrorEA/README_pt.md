# ErroEstratégia EA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A **Estratégia ErrorEA** é uma porta StockSharp do consultor MetaTrader `errorEA.mq4`. O especialista original comparou os componentes +DI e -DI do Índice Direcional Médio e continuou acumulando ordens de mercado na direção da tendência detectada enquanto aplicava um stop-loss de segurança muito grande e um take-profit de scalping rígido. Esta versão C# recria a mesma ideia com o API de alto nível do StockSharp, adiciona controles de parâmetros claros e documenta explicitamente o modelo de risco.

## Lógica de negociação
1. Assine o período configurado (`CandleType`) e alimente um indicador `AverageDirectionalIndex` com as velas recebidas.
2. Espere até que a vela esteja totalmente fechada e o ADX produza um valor final para aquela barra.
3. Compare as linhas +DI e -DI:
   - se +DI > -DI, a estratégia trata o mercado como altista;
   - se -DI > +DI, o mercado é considerado baixista;
   - valores iguais não geram novos sinais.
4. Em um sinal de alta:
   - nivelar uma posição líquida curta existente (StockSharp usa contas de compensação, então hedges opostos são fechados);
   - se o número de negociações de longo escalonamento ainda estiver abaixo de `MaxTrades`, envie mais uma ordem de compra de mercado com o volume retornado pelo bloco de controle de risco.
5. Em um sinal de baixa:
   - fechar uma posição longa existente;
   - se o número de tranches curtas for inferior a `MaxTrades`, envie uma ordem de venda a mercado com a mesma lógica de dimensionamento de posição.
6. As ordens de proteção são gerenciadas por `StartProtection`:
   - `StopLossPoints` é convertido em etapas de preço e funciona como um amplo stop fixo, assim como a entrada `StopLoss` em MetaTrader;
   - se `EnableTakeProfit` for verdadeiro, `TakeProfitPoints` replica o pequeno alvo de escalpelamento que o EA aplicou por meio de `OrderModify`.
7. Os contadores de posição (`_longTrades`/`_shortTrades`) são redefinidos sempre que a posição líquida retorna a zero ou muda para o lado oposto, garantindo que o limite de redução seja aplicado em interrupções e reversões.

## Gestão e dimensionamento de riscos
- `BaseVolume` espelha a entrada `MiniLots` de MetaTrader. Ele atua como o tamanho do lote inicial para cada negociação.
- Quando `EnableRiskControl` é verdadeiro, a estratégia reproduz a fórmula `PowerRisk` original: `volume = BaseVolume * max(1, PortfolioValue / RiskDivider)`. O divisor padrão (`10000`) corresponde à implementação de MQL.
- Depois que a fórmula é aplicada, o resultado é limitado por `MinVolume`, `MaxVolume`, os limites de troca (`Security.MinVolume`, `Security.MaxVolume`) e a etapa de volume (`Security.VolumeStep`). Isso evita que o EA solicite um tamanho que o local rejeitaria.
- O tamanho calculado é usado para cada nova ordem de redução, enquanto a direção correspondente permanece dentro do limite `MaxTrades`.

## Parâmetros
| Nome | Tipo | Padrão | MetaTrader contraparte | Descrição |
| --- | --- | --- | --- | --- |
| `AdxPeriod` | `int` | `14` | `iADX(..., 14, ...)` | Período de suavização do Índice Direcional Médio. |
| `CandleType` | `DataType` | Período de 15 minutos | prazo do gráfico | Série de velas usada para todos os cálculos. |
| `MaxTrades` | `int` | `9` | `MaxTrades` | Número máximo de pedidos de redução por direção. |
| `EnableRiskControl` | `bool` | `true` | `RiskControl` | Permite o cálculo dinâmico do lote com base no valor do portfólio. |
| `BaseVolume` | `decimal` | `0.15` | `MiniLots` | Tamanho base do lote antes de aplicar o multiplicador de risco. |
| `RiskDivider` | `decimal` | `10000` | implícito (divisor em `PowerRisk`) | Divisor aplicado ao valor da carteira quando o controle de risco está ativo. |
| `MaxVolume` | `decimal` | `3` | `MaxLot` | Limite para o volume calculado automaticamente (antes do arredondamento cambial). |
| `MinVolume` | `decimal` | `0.01` | `MarketInfo(..., MODE_MINLOT)` | Volume mínimo permitido no pedido final. |
| `StopLossPoints` | `int` | `1000` | `StopLoss` | Distância de stop-loss em etapas de preço. Defina como `0` para desativar a parada. |
| `EnableTakeProfit` | `bool` | `true` | `ScalpeControl` | Permite o lucro de scalping apertado. |
| `TakeProfitPoints` | `int` | `10` | `ScalpeProfit` | Distância de lucro em etapas de preço. |

## Diferenças do consultor especialista original
- A versão MetaTrader continha um bug que substituiu o valor +DI pelo valor -DI. A porta StockSharp compara os componentes corretos, refletindo o comportamento pretendido da estratégia.
- MetaTrader permite cobertura. StockSharp opera em um ambiente de compensação, então a porta fecha a exposição oposta antes de adicionar novas negociações na direção do sinal.
- A detecção de derrapagem (`GetSlippage`) e a saída de comentários foram removidas porque StockSharp lida com a derrapagem de pedidos internamente e as strings de risco eram puramente cosméticas.
- As modificações de pedido (`OrderModify`) são substituídas por uma única chamada `StartProtection`, que cobre distâncias de stop-loss e take-profit com arredondamento com reconhecimento de exchange.

## Dicas de uso
- Certifique-se de que a segurança tenha metadados `PriceStep`, `VolumeStep`, `MinVolume` e `MaxVolume` adequados para que o ajuste de volume integrado possa funcionar corretamente.
- Alinhe `BaseVolume`, `MinVolume` e `MaxVolume` com o instrumento que você negocia. O construtor também atribui o volume base ajustado a `Strategy.Volume`, o que torna as ações manuais na IU consistentes com pedidos automatizados.
- Aumente o intervalo de tempo ou ADX período quando os sinais +DI/-DI se tornam muito barulhentos; a lógica de redução tem melhor desempenho durante tendências constantes.
- Desative `EnableTakeProfit` se preferir deixar o stop loss sair da posição em vez de escalar pequenos lucros.
