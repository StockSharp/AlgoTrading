# Martingale Estratégia de ruptura
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral

A **Martingale Estratégia de Breakout** é uma versão StockSharp do MetaTrader consultor especialista `MartinGaleBreakout.mq5`. O sistema
espera por velas de rompimento anormalmente grandes e coloca uma única ordem de mercado na direção do rompimento. Embora o EA original
rastreia um "número mágico" para gerenciar suas posições, a implementação do StockSharp depende do contexto da estratégia, portanto o comportamento
é efetivamente o mesmo quando a estratégia é executada isoladamente.

O algoritmo se concentra em duas ideias principais:

1. **Detecção de ruptura** – a estratégia examina o tamanho de cada vela finalizada e compara-a com o intervalo médio da
dez velas anteriores. Quando o intervalo atual é três vezes maior que a média e a vela fecha fortemente no
direção do rompimento, um sinal de negociação é produzido.
2. **Martingalerecuperação estilo** – a estratégia monitora lucros e perdas flutuantes. Sempre que o PnL não realizado atingir o
limite de perda configurado, ele fecha imediatamente todas as posições abertas e aumenta a próxima meta de lucro para que a negociação seguinte
tenta recuperar a perda. Assim que a meta aumentada for atingida, os limites serão redefinidos para os valores originais.

A porta mantém todos os parâmetros de gerenciamento de dinheiro do código MQL5, incluindo a porcentagem do saldo reservada para margem, o
metas de lucros e perdas baseadas em porcentagem e o multiplicador que expande a distância de obtenção de lucro durante a fase de recuperação.

## Lógica de negociação

1. Assine a série de velas configuradas e aguarde as velas finalizadas.
2. Calcule o intervalo da vela (`High - Low`) e mantenha um buffer de tamanho fixo com os dez intervalos anteriores para determinar o
média de referência usada para detecção de fuga.
3. Calcule o PnL flutuante acompanhando os preços médios de entrada para os lados comprado e vendido. Se o PnL não realizado exceder o
meta de lucro ou violar o limite de stop-loss, fechar imediatamente todas as posições e redefinir o estado de recuperação como no
consultor especialista original.
4. Ignore a colocação de ordens enquanto a estratégia já mantém uma posição ou quando a negociação não é permitida pelo estado da conexão.
5. Quando uma vela de alta aparecer, dimensione o pedido para que o lucro esperado corresponda à meta atual. O lucro
a distância nas etapas de preço é multiplicada durante a recuperação, exatamente como o parâmetro `TP_Points_Multiplier` do EA.
6. Valide o volume calculado em relação aos limites do instrumento (mínimo, máximo e passo) e certifique-se de que a margem necessária
não excede a alocação de saldo configurada ou os fundos livres disponíveis. Se as restrições forem respeitadas, envie um
ordem de compra de mercado.
7. Repita o mesmo processo para rompimentos de baixa, enviando uma ordem de venda a mercado.

A combinação dessas regras recria o comportamento do sistema MetaTrader original, incluindo a transição de entrada e saída
do modo de recuperação após um evento de stop-loss.

## Parâmetros

| Parâmetro | Descrição | Padrão |
|-----------|-------------|---------|
| `TakeProfitPoints` | Distância entre o preço de entrada e o preço de realização de lucro expresso em etapas de preço. | `50` |
| `BalancePercentAvailable` | Porcentagem máxima do saldo da conta que pode ser reservada para margem em uma única negociação. | `50` |
| `TakeProfitPercentOfBalance` | Lucro alvo expresso como uma percentagem do saldo atual. | `0.1` |
| `StopLossPercentOfBalance` | Tamanho do stop loss expresso como uma porcentagem do saldo atual. | `10` |
| `RecoveryStartFraction` | Fração do stop loss usada antes de mudar para o modo de recuperação. | `0.1` |
| `RecoveryPointsMultiplier` | Multiplicador aplicado à distância de lucro durante a recuperação. | `1` |
| `CandleType` | Fonte de dados de velas usada pela estratégia (período de tempo, velas de tick, etc.). | `15-minute time frame` |

## Notas adicionais

- O cálculo do volume replica o auxiliar MetaTrader `CalcLotWithTP`. Ele deriva o tamanho do lote necessário para atingir o atual
meta de lucro para um determinado movimento de preço e, em seguida, normaliza o resultado para a etapa de volume do instrumento.
- As verificações de margem são realizadas com o mesmo espírito de `CheckVolumeValue` e o filtro de porcentagem de saldo usado em MQL
versão. As ordens são rejeitadas quando a margem exigida excede a parcela permitida do saldo ou os fundos livres informados pelo
o portfólio.
- A estratégia cancela todos os pedidos ativos antes de nivelar as posições para que o comportamento corresponda ao auxiliar `CloseAllOrders` de
o consultor especialista original.
- O buffer de intervalo interno armazena apenas dez valores e é equivalente à iteração sobre `iHigh`/`iLow` na origem EA. Não
são necessários dados históricos além das últimas dez velas.
