# Estratégia de janela Bull vs Medved
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A estratégia Bull vs Medved é uma conversão StockSharp do MetaTrader 4 especialista *Bull_vs_Medved.mq4*. O sistema tenta
entre em retrocessos dentro de um forte impulso de alta ou baixa, colocando ordens de limite pendentes durante seis períodos predefinidos de cinco minutos.
janelas espalhadas ao longo do dia de negociação. A versão StockSharp mantém a ideia de negociar apenas uma vez por janela, cancela obsoleto
ordens pendentes e usa o tamanho do corpo da vela de sinalização para derivar distâncias dinâmicas de stop-loss e take-profit.

## Lógica de negociação
1. Assine o fluxo de velas definido por `CandleType` e manipule apenas velas finalizadas.
2. Mantenha as duas últimas velas concluídas para que a vela atual (`shift1`), a vela anterior (`shift2`) e a vela
antes disso (`shift3`) replicar as referências `Close[1..3]` usadas em MetaTrader.
3. Durante cada janela de negociação (`EntryWindowMinutes` minutos começando em `StartTime0..5`) verifique os seguintes padrões:
   - **Bull**: `shift3` fecha acima da abertura de `shift2`, o corpo de `shift2` tem pelo menos 10 pontos de corretor e o corpo de
`shift1` tem pelo menos `CandleSizePoints` pontos. Se `IsBadBull` for falso (três corpos longos seguidos), coloque um limite de compra.
   - **Cool Bull**: `shift2` é um pullback mínimo de 20 pontos que fecha abaixo da abertura de `shift1`, que por sua vez fecha acima
o `shift2` abre com um corpo de pelo menos 40% do limite; coloque um limite de compra.
   - **Baixa**: o corpo de `shift1` tem pelo menos `CandleSizePoints` pontos, mas é baixista; coloque um limite de venda.
4. Os limites de compra são colocados em `ask - BuyIndentPoints * PriceStep`, os limites de venda em `bid + SellIndentPoints * PriceStep`. Apenas um
uma ordem ou posição pendente pode existir ao mesmo tempo, portanto a estratégia ignora novos sinais se uma negociação já estiver ativa dentro do
janela.
5. Stops e alvos estão ocultos dentro da estratégia. Quando uma ordem de entrada é preenchida, o corpo da vela de `shift1` é multiplicado por
`StopLossMultiplier` e `TakeProfitMultiplier`, normalizados para `PriceStep` e armazenados como preços de saída.
6. Em cada vela finalizada, a estratégia avalia se a máxima/mínima violou o stop ou alvo armazenado. Atingindo o nível
fecha a posição aberta com uma ordem de mercado e limpa as bandeiras de proteção.
7. Pedidos pendentes com mais de 230 minutos são cancelados para imitar a rotina de limpeza MetaTrader e `_orderPlacedInWindow` é
redefinido quando o preço sai da janela de negociação.

## Parâmetros
| Nome | Tipo | Padrão | Descrição |
| --- | --- | --- | --- |
| `OrderVolume` | `decimal` | `0.1` | Volume usado para cada ordem limite. |
| `CandleSizePoints` | `decimal` | `75` | Tamanho mínimo do corpo de alta/baixa (em pontos do corretor) para a vela de sinalização. |
| `StopLossMultiplier` | `decimal` | `0.8` | Multiplicador aplicado ao corpo da vela de sinal para construir a distância de parada. |
| `TakeProfitMultiplier` | `decimal` | `0.8` | Multiplicador aplicado ao corpo da vela de sinal para construir a distância alvo. |
| `BuyIndentPoints` | `decimal` | `16` | Número de pontos da corretora subtraídos do pedido ao colocar limites de compra. |
| `SellIndentPoints` | `decimal` | `20` | Número de pontos de corretor adicionados ao lance ao colocar limites de venda. |
| `EntryWindowMinutes` | `int` | `5` | Duração de cada sessão em minutos. |
| `CandleType` | `DataType` | Velas de 5 minutos | Série de velas processada pela estratégia. |
| `StartTime0..5` | `TimeSpan` | `00:05`, `04:05`, `08:05`, `12:05`, `16:05`, `20:05` | Hora de início de cada janela de negociação. |

## Diferenças do especialista original
- O especialista MetaTrader atribui stop-loss e take-profit à própria ordem pendente. A porta StockSharp simula isso
comportamento armazenando níveis ocultos e fechando a posição líquida com ordens de mercado quando as velas os quebram.
- Os limites de preço usam `Security.PriceStep` para que a conversão funcione em cotações forex de 4 e 5 dígitos sem custos adicionais
parâmetros.
- Apenas velas finalizadas são usadas para avaliar as regras de stop/target, enquanto MetaTrader stops podem ser acionados intrabar pelo
servidor de negociação.
- Alertas sonoros e campos de comentários do EA original são omitidos; os registros StockSharp fornecem feedback.

## Dicas de uso
- A estratégia é projetada para símbolos forex que usam preços de pip fracionários. Verifique `PriceStep` para confirmar que baseado em pontos
os filtros correspondem à distância pretendida do pip.
- Como o stop e o take-profit estão ocultos, considere executar a estratégia em um ambiente dedicado ou protegê-la com um
módulo de risco do lado da corretora caso a conexão caia.
- Ajuste os valores `StartTime` se a sessão do seu corretor for diferente da programação original baseada no GMT. Cada janela pode ser desativada por
definir os horários de início fora do seu dia de negociação.
- Anexe a estratégia a um gráfico para visualizar as ordens limitadas e confirme que apenas uma entrada é tentada em cada janela.
