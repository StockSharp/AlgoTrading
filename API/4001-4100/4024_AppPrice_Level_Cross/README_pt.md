# Estratégia cruzada de nível de preço do aplicativo
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
- Conversão do consultor especialista MetaTrader 4 **BT_v4** (`MQL/8543/BT_v4.mq4`).
- Reimplementado com a estratégia StockSharp de alto nível API (assinaturas de velas, processamento sem indicadores, proteções integradas).
- Focado em reagir ao preço de fechamento cruzando um nível horizontal definido pelo usuário (`AppPrice`).

## Lógica de negociação
1. Cada vela finalizada atualiza um buffer interno com o último preço de fechamento.
2. Quando o fechamento se move **acima** `AppPrice` enquanto o fechamento anterior estava **no nível ou abaixo** do nível, a estratégia
   - Negocia apenas se `BuyOnly = true` (espelha o padrão original EA).
   - Cancela quaisquer ordens pendentes, compensa uma posição vendida existente através do mesmo volume de ordem de mercado e estabelece uma posição longa do tamanho do lote calculado.
3. Quando o fechamento se move **abaixo** de `AppPrice` enquanto o fechamento anterior estava **no nível ou acima** do nível, a estratégia
   - Negociações somente se `BuyOnly = false` (modo somente venda do EA).
   - Cancela ordens pendentes, compensa qualquer posição longa existente e estabelece uma posição curta do tamanho do lote calculado.
4. Os sinais são avaliados estritamente nas velas concluídas; velas parcialmente formadas são ignoradas como no script MQL.

## Dimensionamento de posições
- `EnableMoneyManagement = false` → use `FixedVolume` (equivalente à entrada MQL `Lots`).
- `EnableMoneyManagement = true` → calcule o lote usando a fórmula original:

\[
\text{lote} = \text{round}_{\text{LotPrecision}} \left( \frac{\text{LotBalancePercent}}{100} \times \frac{\text{Balance}}{\text{divisor}} \right)
\]

  - `divisor = 1000` para lotes de uma casa decimal e `100` para lotes de duas casas decimais (mesma regra de `LotPrec` em MQL).
  - O resultado é fixado em [`MinLot`, `MaxLot`] e então alinhado com as restrições de segurança `VolumeStep`, `VolumeMin` e `VolumeMax`.
  - Se os dados de saldo do portfólio não estiverem disponíveis, a estratégia volta para `FixedVolume`.

## Gestão de risco
- `StopLossPoints` e `TakeProfitPoints` são medidos em preços de instrumentos (ticks).
- Se um dos valores for positivo, `StartProtection` será ativado com os deslocamentos convertidos por meio de `Security.PriceStep`.
- Definir uma distância para `0` desativa essa perna protetora específica, consistente com o comportamento original de EA.

## Parâmetros
| Nome | Descrição | Padrão |
| ---- | ----------- | ------- |
| `AppPrice` | Nível que aciona negociações quando o fechamento o ultrapassa. | `0` |
| `BuyOnly` | `true` = modo somente longo (padrão original), `false` = somente modo curto. | `true` |
| `FixedVolume` | Tamanho do lote quando MM está desabilitado. | `0.1` |
| `EnableMoneyManagement` | Ativa o dimensionamento percentual de saldo. | `false` |
| `LotBalancePercent` | Porcentagem do saldo usado quando MM está ativado. | `10` |
| `MinLot` / `MaxLot` | Limites para o tamanho do lote calculado. | `0.1` / `5` |
| `LotPrecision` | Número de casas decimais para arredondar o lote calculado. | `1` |
| `StopLossPoints` | Distância de stop-loss em pontos de preço (0 = desabilitado). | `140` |
| `TakeProfitPoints` | Distância de lucro em faixas de preço (0 = desativado). | `180` |
| `CandleType` | Período de vela usado para a detecção cruzada. | `1 Minute` |

## Notas de implementação
- Usa `SubscribeCandles(...).Bind(...)` então os indicadores são desnecessários; os preços de fechamento chegam diretamente no retorno de chamada.
- As ordens de mercado (`BuyMarket`/`SellMarket`) são dimensionadas para achatar a posição oposta antes de abrir uma nova, espelhando a lógica EA de fechar ordens opostas antes de entrar.
- `CancelActiveOrders()` é invocado antes de cada ordem de mercado para evitar ordens pendentes não intencionais.
- Parâmetros como `Magic`, `Slippage` e configurações de cores do arquivo MQL são omitidos porque não têm equivalente direto em StockSharp.
- Certifique-se de que os metadados `Security` (`PriceStep`, `VolumeStep`, `VolumeMin`, `VolumeMax`) sejam preenchidos para que os ajustes de preço/volume correspondam às regras do corretor.

## Dicas de uso
- Defina `AppPrice` para o nível horizontal que você deseja monitorar (por exemplo, preço psicológico, pivô diário, etc.).
- Desligue `BuyOnly` para replicar o modo "somente venda" original; deixe-o ativado para executar o comportamento padrão somente longo fornecido.
- Ao habilitar a gestão de dinheiro, verifique se a conexão da carteira fornece atualizações de saldo; caso contrário, a estratégia reverte para o volume fixo.
- Nenhuma porta Python é fornecida por solicitação; apenas a estratégia C# é gerada.
