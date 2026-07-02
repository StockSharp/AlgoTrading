# Estratégia Abrir Fechar (ID 3996)
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
Esta estratégia replica o MetaTrader 4 especialista `open_close.mq4`. Ele funciona em um único instrumento e compara a abertura e o fechamento da última vela com a anterior. Quando nenhuma posição está ativa, ele desvanece movimentos fortes de uma barra (padrões de intervalo e reversão). Durante uma negociação, ele fecha a posição quando o padrão é revertido ou quando um limite de proteção contra perdas flutuantes é violado.

## Lógica de negociação
### Regras de entrada
- Negocia somente quando a vela anterior tiver sido processada (a guarda `Volume[0] == 1` original).
- Entrada longa: a vela atual abre acima da abertura anterior **e** fecha abaixo do fechamento anterior. A estratégia compra o volume configurado no mercado.
- Entrada curta: a vela atual abre abaixo da abertura anterior **e** fecha acima do fechamento anterior. A estratégia vende a descoberto no mercado.

Apenas uma posição pode estar ativa por vez. Novos sinais são ignorados até que a posição aberta seja fechada.

### Regras de saída
1. **Proteção contra riscos:** o PnL flutuante é medido a partir do preço médio de entrada. Se a perda não realizada exceder `MaximumRisk × Portfolio.CurrentValue`, a estratégia fecha imediatamente a posição. A versão original do MQL usava `AccountMargin`, que é aproximado aqui com a melhor avaliação de portfólio disponível.
2. **Reversão de padrão:**
   - As posições longas fecham quando a próxima vela continua descendente (`open < previous open` e `close < previous close`).
   - As posições curtas fecham quando a próxima vela continua ascendente (`open > previous open` e `close > previous close`).

## Dimensionamento de posições
- O tamanho do pedido padrão é derivado de `MaximumRisk`. A estratégia multiplica o valor da conta disponível por `MaximumRisk` e divide o resultado por `1000`, imitando o cálculo de MetaTrader de `AccountFreeMargin * MaximumRisk / 1000`.
- Se as informações da conta não estiverem disponíveis, o parâmetro substituto `InitialVolume` será usado.
- Após mais de uma negociação consecutiva perdida, o tamanho do lote é reduzido em `volume × losses / DecreaseFactor`, reproduzindo o loop MetaTrader ao longo do histórico de negociações fechadas.
- Um volume negociável mínimo de `0.1` lotes é aplicado antes de alinhar a quantidade à etapa de volume do instrumento e aos limites de troca.

## Parâmetros
| Nome | Tipo | Padrão | Descrição |
| --- | --- | --- | --- |
| `InitialVolume` | `decimal` | `0.1` | Tamanho do lote substituto usado quando as informações sobre patrimônio não estão disponíveis. |
| `MaximumRisk` | `decimal` | `0.3` | Fração do valor da conta que controla o tamanho da posição e a perda flutuante máxima tolerada. |
| `DecreaseFactor` | `decimal` | `100` | Fator de redução aplicado após mais de uma negociação consecutiva com perdas. |
| `CandleType` | `DataType` | `15m` período de tempo | Série de velas usada para avaliar o padrão. |

## Notas de implementação
- A estratégia assina a série de velas selecionada e processa **apenas velas finalizadas**, correspondendo à condição `Volume[0] > 1` no especialista original.
- O PnL flutuante é estimado a partir da posição atual da estratégia e do último preço de fechamento porque StockSharp não expõe as métricas `AccountProfit` e `AccountMargin` de MetaTrader.
- As perdas consecutivas são rastreadas por meio de negociações preenchidas, permitindo que `DecreaseFactor` se comporte como o loop original ao longo do histórico de negociações.
- O alinhamento de volume respeita `Security.VolumeStep`, `MinVolume` e `MaxVolume` para permanecer compatível com os requisitos de troca.
- Os gráficos são preenchidos com velas e negociações próprias quando uma área do gráfico está disponível para depuração visual.

## Dicas de uso
- Escolha um intervalo de vela que corresponda ao usado em MetaTrader ao calibrar o especialista original.
- Ajuste `MaximumRisk` e `DecreaseFactor` para ajustar a agressividade da regra de dimensionamento de lote.
- Como a estratégia é contrária, ela tem melhor desempenho em instrumentos que apresentam freqüentes sobreextensões de barra única e movimentos de snap-back.
