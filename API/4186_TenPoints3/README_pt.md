# Estratégia Dez Pontos 3
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Resumo
- Converte o consultor especialista MetaTrader 4 **10p3v004 ("10pontos 3")** na estrutura de estratégia de alto nível StockSharp.
- Recria a lógica de entrada de grade baseada em inclinação MACD junto com escala martingale, proteção de trilha e saídas baseadas em equidade.
- Fornece documentação extensa de cada parâmetro para que o comportamento do EA original possa ser reproduzido ou ajustado com segurança.

## Lógica de negociação
1. **Detecção de sinal.** Em cada vela concluída do período de tempo configurado, a estratégia calcula um MACD com comprimentos de sinal rápido, lento e definidos pelo usuário. Quando o valor principal MACD aumenta em relação à barra anterior o sistema prepara uma grade longa; quando cai, uma pequena grade é preparada. A bandeira `ReverseSignals` inverte esta interpretação.
2. **Entradas de grade.** Somente uma grade direcional pode estar ativa por vez. A primeira ordem é colocada imediatamente após um sinal. Pedidos adicionais serão adicionados se:
   - A direção da grade ativa corresponde ao sinal atual e
   - O preço mudou em pelo menos `GridSpacingPoints * PriceStep` desde o preenchimento mais recente na direção da média favorável, e
   - O número de negociações de rede aberta não atingiu `MaxTrades`.
O tamanho do pedido é multiplicado por `2^n` para grades pequenas (até 12 entradas) ou `1.5^n` para grades maiores, reproduzindo a lógica martingale do código-fonte. O tamanho final é arredondado para a etapa de volume do instrumento e limitado pelos limites de segurança e pelo teto de segurança `MaxVolumeCap`.
3. **Gerenciamento de dinheiro.** Quando `UseMoneyManagement` está ativado, o tamanho do lote base é derivado do valor atual do portfólio e `RiskPerTenThousand`. O EA original usava regras separadas para contas padrão e mini; esta conversão mantém o mesmo comportamento através do parâmetro `IsStandardAccount`. Se a configuração estiver desativada, o `BaseVolume` fixo será usado.
4. **Regras de saída.**
   - A **parada inicial** opcional fecha toda a grade se a posição agregada se mover contra ela em `InitialStopPoints`.
   - **Take Profit** opcional fecha a grade quando o preço atinge `TakeProfitPoints` em favor da posição líquida.
   - O **trailing stop** opcional começa a seguir o preço depois que ele se move em `(TrailingStopPoints + GridSpacingPoints)` em relação ao preço médio de entrada e mantém um buffer móvel de `TrailingStopPoints`.
   - A **proteção patrimonial** opcional monitora o lucro não realizado medido em pontos vezes o volume. Quando `OrdersToProtect` ou mais posições estão abertas e o lucro atinge `SecureProfit`, a estratégia é encerrada imediatamente.

## Parâmetros
| Parâmetro | Descrição | Padrão |
|-----------|-------------|---------|
| `CandleType` | Período principal usado para cálculos de MACD e processamento de pedidos. | Velas de 30 minutos |
| `MacdFastPeriod` / `MacdSlowPeriod` / `MacdSignalPeriod` | Configuração MACD idêntica ao indicador MT4 (26/14/9 por padrão). | 14/26/9 |
| `BaseVolume` | Tamanho do lote inicial usado quando não existe posição na grade e o gerenciamento de dinheiro está desabilitado. | 0,01 |
| `GridSpacingPoints` | Distância mínima entre entradas consecutivas na grade, expressa em etapas de preço. | 15 |
| `TakeProfitPoints` | Distância da entrada média para acionar um lucro total. Defina como `0` para desativar. | 40 |
| `InitialStopPoints` | Distância adversa máxima tolerada antes do achatamento da grade. Defina como `0` para desativar. | 0 |
| `TrailingStopPoints` | Tamanho do buffer final. A trilha é ativada depois que o preço avança `GridSpacingPoints + TrailingStopPoints`. | 20 |
| `MaxTrades` | Número máximo de pedidos médios por direção. | 9 |
| `OrdersToProtect` | Número mínimo de negociações abertas necessárias antes da avaliação da verificação de proteção patrimonial. | 3 |
| `SecureProfit` | Meta de lucro não realizado (pontos × volume) que desencadeia a saída da proteção patrimonial. | 8 |
| `AccountProtectionEnabled` | Ativa ou desativa o bloqueio de proteção patrimonial. | `true` |
| `ReverseSignals` | Inverte a interpretação da inclinação MACD (útil para testes espelhados). | `false` |
| `UseMoneyManagement` | Ativa o cálculo de volume dinâmico usando `RiskPerTenThousand`. | `false` |
| `RiskPerTenThousand` | Montante de risco por 10.000 unidades de saldo utilizado quando a gestão de dinheiro está ativa. | 12 |
| `IsStandardAccount` | Replica as regras de arredondamento do lote original (`true` = lotes padrão, `false` = minilotes). | `true` |
| `MaxVolumeCap` | Tampa rígida aplicada após a escala martingale para manter o tamanho da posição sob controle. | 100 |

## Notas de conversão
- O especialista MQL manteve paradas separadas no nível do bilhete. Em StockSharp a grade é gerenciada como uma única posição agregada. Os níveis finais e de proteção são, portanto, recalculados a partir do preço médio de entrada ponderado pelo volume.
- O EA dependia do valor do tick do corretor para converter os lucros em moeda. Aqui, o limite de proteção patrimonial é medido em pontos multiplicados pelo volume, refletindo a comparação baseada em pip da fonte.
- `AccountFreeMarginCheck` e outras validações MT4 específicas da conta não têm um equivalente direto de StockSharp. Em vez disso, a estratégia respeita os limites de volume do instrumento e o opcional `MaxVolumeCap`.
- Comentários de pedidos, números mágicos e anotações gráficas do MT4 não são reproduzidos porque não possuem contraparte StockSharp.

## Uso
1. Adicione a estratégia ao seu projeto, defina `Security` e `Portfolio` como de costume para estratégias StockSharp.
2. Ajuste `CandleType` para corresponder ao período de tempo que deve ser analisado (a versão MT4 funcionou no período de tempo do gráfico atual).
3. Ajuste os parâmetros de risco: mantenha o `BaseVolume` fixo ou ative `UseMoneyManagement` com as opções `RiskPerTenThousand` e `IsStandardAccount` apropriadas.
4. Decida quais camadas de proteção ativar (stop inicial, take-profit, trailing stop, proteção de patrimônio) e defina os limites adequados à volatilidade do instrumento.
5. Inicie a estratégia; os auxiliares de gráfico integrados exibirão velas, valores MACD e negociações executadas.

## Ideias de Desenvolvimento Adicional
- Integre a lógica de espaçamento adaptativo (por exemplo, usando ATR) em vez do `GridSpacingPoints` fixo.
- Exponha parâmetros finais separados para grades longas e curtas ou permita grades assimétricas.
- Combine a inclinação MACD com filtros de tendência (médias móveis, confirmação de período de tempo mais alto) para reduzir o número de grades de contra-tendência.

> **Observação:** Nenhuma implementação Python é fornecida para esta estratégia, correspondendo à solicitação e à estrutura atual do projeto.
