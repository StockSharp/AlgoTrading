# Lilith vai para a estratégia de Hollywood
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
Esta estratégia recria o comportamento do MetaTrader especialista "Lilith vai para Hollywood" dentro do StockSharp alto nível API. Implementa uma grade de hedge que pode operar em dois modos muito diferentes:

* **Modo automatizado** – Parabolic SAR aciona entradas imediatas no mercado sempre que o preço cruza o valor de stop e reversão.
* **Modo manual** – Ordens stop/limit pendentes são estacionadas em torno dos preços de referência definidos pelo usuário e deixadas para serem preenchidas.

Em ambos os casos, a estratégia monitoriza a exposição longa e curta separadamente, calcula o lucro líquido flutuante da rede aberta e utiliza essas informações para decidir quando implementar ordens de recuperação adicionais.

## Modos de operação
* **Automatizado** – Quando nenhuma posição está aberta, a estratégia se inscreve no indicador Parabolic SAR (fatores 0,02/0,2). Se o fechamento da vela estiver acima do indicador ela compra no mercado, se estiver abaixo ela vende. O preço executado se torna o novo **foco** e as paradas de recuperação são armadas a uma distância de âncora configurável ao seu redor.
* **Manual** – Quando nenhuma posição está aberta, a estratégia envia uma única ordem pendente por lado. Se o mercado negociar abaixo do nível de compra, será criado um stop de compra; caso contrário, será apresentado um limite de compra. O lado da venda reflete a mesma lógica em torno do nível `PriceDown`. Assim que um dos pedidos for atendido, o outro lado permanece ativo até ser cancelado manualmente ou pela estratégia.

## Lógica de gerenciamento de pedidos
* A grade continua executando totais de volumes longos/curtos preenchidos e ordens de compra/venda pendentes. Isto permite que a estratégia meça os desequilíbrios entre os dois lados do livro.
* Sempre que o lucro flutuante atinge a meta dinâmica (`account value / 1000`) a estratégia fecha todas as posições e cancela todas as ordens pendentes.
* Se o PnL flutuante cair abaixo de `-AccountValue * RiskPercent / 100`, um hedge de emergência é implantado através da abertura de ordens de mercado que cobrem o excesso líquido de curto ou longo prazo.
* As ordens de recuperação são expressas como ordens stop colocadas em torno do preço foco (modo automatizado) ou em torno dos preços manuais configurados. Seu tamanho é calculado como `(opposite exposure * XFactor) - current exposure`, imitando a lógica MT4 de superdimensionar o próximo pedido para reequilibrar a grade.

## Parâmetros
| Nome | Descrição |
| --- | --- |
| `Automated` | Permite entradas de mercado impulsionadas por Parabolic SAR. Desative para trabalhar no modo de ordem pendente manual. |
| `PriceUp` | Preço de referência usado para criar ordens stop/limit de compra em modo manual. |
| `PriceDown` | Preço de referência usado para criar ordens stop/limit de venda em modo manual. |
| `AnchorSteps` | Distância, expressa em etapas de preço, utilizada para compensar as ordens de recuperação do preço foco. |
| `ManualVolume` | Tamanho base do lote ao operar manualmente ou quando o dimensionamento da posição dinâmica produz zero. |
| `XFactor` | Multiplicador aplicado à exposição contrária no dimensionamento das ordens de recuperação. |
| `RiskPercent` | Perda flutuante máxima (porcentagem do valor da conta) tolerada antes que a estratégia implemente um hedge de emergência. |
| `CandleType` | Período usado para conduzir o Parabolic SAR e a lógica de gerenciamento geral. |

## Risk controls
* A realização de lucros é dinâmica e varia de acordo com o valor da conta, fornecendo uma maneira automática de aumentar a meta à medida que a conta cresce.
* A cobertura de emergência pode neutralizar rebaixamentos extremos, achatando o lado mais exposto da grade quando a perda flutuante excede o limite `RiskPercent`.
* Todas as ordens pendentes são arredondadas para o tamanho do tick do instrumento e os volumes são ajustados para respeitar os limites de câmbio, correspondendo às proteções típicas do especialista original MetaTrader.

## Notas de conversão
* MetaTrader ticks são substituídos por velas acabadas. O período padrão de um minuto mantém a estratégia reativa, mas pode ser ajustado por meio do parâmetro `CandleType`.
* A configuração `Anchor` da fonte MQL expressou a distância em pontos. Aqui ele é configurado como uma série de etapas de preço para que se adapte automaticamente ao tamanho do tick do instrumento.
* A saída original de "Comentário" foi convertida em mensagens de registro de estratégia (`LogInfo`) para que o diário da plataforma contenha o mesmo feedback sem depender de anotações de gráfico.
