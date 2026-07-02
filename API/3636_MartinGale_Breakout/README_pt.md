# Estratégia de ruptura da MartinGale
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A **Estratégia de Breakout MartinGale** é um sistema de acompanhamento de breakout convertido do consultor especialista MetaTrader 4 *MartinGaleBreakout*. O robô original entra em posições após detectar velas anormalmente grandes e aplica um mecanismo de recuperação estilo martingale para recuperar perdas anteriores. Esta porta StockSharp reproduz o comportamento usando a estratégia de alto nível API com assinaturas de velas e parâmetros de gerenciamento de dinheiro.

A estratégia monitora uma série de velas configuráveis, procurando velas cujo intervalo seja pelo menos três vezes maior que o intervalo médio das dez barras anteriores. Quando tal vela fecha fortemente em uma direção, a estratégia abre uma posição de mercado nessa direção. Se a posição for fechada com uma perda que exceda um limite configurável, o modo de recuperação aumenta a distância de obtenção de lucro para compensar o rebaixamento realizado.

## Lógica de negociação
1. Assine a série de velas selecionada (velas de 15 minutos por padrão).
2. Mantenha as 11 velas concluídas mais recentes para avaliar a volatilidade anormal.
3. Detecte um rompimento de alta quando:
   - A vela atual é três vezes maior que o intervalo médio das dez velas anteriores.
   - A vela fecha na metade superior do seu alcance.
4. Detecte um rompimento de baixa usando condições simétricas.
5. Abra uma posição de mercado na direção do rompimento se:
   - Nenhuma outra posição está aberta no momento.
   - A exposição de capital estimada está abaixo do percentual de saldo configurado.
6. Fechar posições e redefinir metas de lucros/perdas quando:
   - O lucro flutuante atinge o limite de lucro.
   - A perda flutuante atinge o limite de stop loss.
7. Quando ocorrer um stop loss, mude para o modo de recuperação:
   - Aumente a distância de lucro pelo multiplicador configurado.
   - Expanda o limite de stop loss para a porcentagem máxima permitida.
   - Continue negociando até que a próxima meta seja alcançada e, em seguida, redefina para a configuração básica.

## Parâmetros
| Nome | Descrição | Padrão |
| --- | --- | --- |
| `TakeProfitPoints` | Distância básica de take-profit expressa em pontos de instrumento. | `50` |
| `BalancePercentageAvailable` | Parcela máxima do saldo da conta que pode ser alocada para uma única negociação. | `50%` |
| `TakeProfitBalancePercent` | Lucro alvo como porcentagem do saldo da conta. | `0.1%` |
| `StopLossBalancePercent` | Rebaixamento máximo antes de acionar a recuperação. | `10%` |
| `StartRecoveryFactor` | Parte do stop loss usada antes de ativar o modo de recuperação. | `0.2` |
| `TakeProfitPointsMultiplier` | Multiplicador aplicado à distância de lucro durante a recuperação. | `1` |
| `CandleType` | Série de velas usada para cálculos de breakout. | `15-minute` |

## Dimensionamento de posição e controle de risco
- A estratégia calcula o volume necessário para atingir o take-profit monetário configurado usando o tamanho e o valor do tick do instrumento.
- Os volumes são normalizados para as restrições de troca (passo, mínimo, máximo).
- A exposição de capital estimada não deve exceder o percentual de saldo configurado.
- O modo de recuperação expande dinamicamente a meta de lucro após uma perda, emulando o comportamento original do martingale enquanto mantém as posições limitadas a uma única negociação aberta.

## Notas
- A estratégia depende de informações sobre o saldo da carteira; inicialize-o com uma conexão de portfólio antes de começar.
- O tratamento de comissões reflete o EA original, concentrando-se em lucros e perdas flutuantes derivados da posição atual.
- Não são utilizadas ordens pendentes – as entradas e saídas são realizadas apenas com ordens de mercado.
