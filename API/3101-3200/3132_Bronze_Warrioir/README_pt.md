# Estratégia de Bronze Warrioir
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
- Conversão do especialista MetaTrader 5 *Bronze Warrioir.mq5* para a API de alto nível do StockSharp.
- Negocia um único símbolo usando velas finalizadas e combina CCI, Williams %R e um oscilador proprietário "DayImpuls".
- Focado em capturar explosões de momentum que ocorrem quando a inclinação do DayImpuls, os extremos do Williams %R e as leituras do CCI se alinham.

## Conjunto de indicadores
- **Commodity Channel Index (CCI)** – CCI clássico usando o `IndicatorPeriod` configurado. Sinais longos requerem o valor abaixo de `-CciLevel`; sinais curtos precisam que esteja acima de `CciLevel`.
- **Williams %R** – aplicado no mesmo período. Um valor acima de `WilliamsLevelUp` confirma território de sobrecompra, enquanto valores abaixo de `WilliamsLevelDown` confirmam níveis de sobrevenda.
- **Oscilador DayImpuls** – réplica do indicador personalizado incluído. Converte cada corpo de vela em pontos (fechamento menos abertura dividido pelo valor de ponto do instrumento) e aplica duas médias móveis exponenciais consecutivas com o mesmo período. Valores crescentes indicam pressão altista crescente; valores decrescentes indicam pressão baixista.

## Lógica de negociação
1. **Proteção de patrimônio** – antes de gerar quaisquer sinais, a estratégia acumula o PnL flutuante da exposição atual. Se subir acima de `ProfitTarget` ou cair abaixo de `LossTarget`, todas as posições abertas são fechadas imediatamente.
2. **Filtro de entrada** – velas finalizadas são obrigatórias. O algoritmo requer um valor DayImpuls armazenado da barra anterior para emular o look-back original usando `custom[1]`.
3. **Configuração curta** – acionada quando:
   - Não há exposição curta ativa.
   - DayImpuls está acima de `DayImpulsLevel` e maior que seu valor anterior (momentum positivo).
   - Williams %R está acima de `WilliamsLevelUp` (sobrecompra) e CCI é maior que `CciLevel`.
   - Ordens usam `TradeVolume` mais qualquer volume longo aberto para reverter em uma única transação dentro do modelo de netting do StockSharp.
4. **Configuração longa** – condições simétricas:
   - Sem exposição longa ativa.
   - DayImpuls está abaixo de `DayImpulsLevel` e menor que seu valor anterior (momentum decrescente).
   - Williams %R está abaixo de `WilliamsLevelDown` e CCI é menor que `-CciLevel`.
   - Usa `TradeVolume` mais qualquer volume curto pendente para uma reversão completa quando necessário.
5. **Reversões estilo hedge** – quando apenas uma exposição direcional está presente e o PnL flutuante sai da faixa `[-PredTarget / 2, PredTarget]`, o EA valida o passo martingale através do parâmetro `LotCoefficient`. No port StockSharp, a validação é preservada, mas a execução real realiza uma ordem de fechar-e-reverter porque a plataforma mantém posições líquidas em vez de tickets de hedge independentes.

## Gestão de risco
- `StopLossPips` e `TakeProfitPips` são convertidos em distâncias de preço usando o `PriceStep` do instrumento. Para símbolos forex de 3 ou 5 dígitos, um fator extra de 10 é aplicado para emular "pips" do MetaTrader.
- Ambos os valores são passados ao helper de alto nível `StartProtection`, que anexa níveis automáticos de stop-loss e take-profit à posição ativa.
- A estratégia mantém rastreamento interno de volume longo/curto para que `GetOpenPnL` corresponda ao cálculo do MetaTrader que soma `Commission + Swap + Profit` para cada ticket.

## Parâmetros
| Nome | Descrição | Padrão |
| --- | --- | --- |
| `TradeVolume` | Volume base da ordem em lotes. | `1` |
| `StopLossPips` | Stop protetor em pips convertido em distância de preço. | `50` |
| `TakeProfitPips` | Meta de lucro em pips convertida em distância de preço. | `50` |
| `IndicatorPeriod` | Período aplicado ao CCI, Williams %R e DayImpuls. | `14` |
| `CciLevel` | Limiar absoluto do CCI para negociações. | `150` |
| `WilliamsLevelUp` | Nível de sobrecompra do Williams %R (valor negativo). | `-15` |
| `WilliamsLevelDown` | Nível de sobrevenda do Williams %R (valor negativo). | `-85` |
| `DayImpulsLevel` | Limiar do DayImpuls que separa regimes altistas/baixistas. | `50` |
| `ProfitTarget` | Meta de lucro flutuante na moeda da conta. | `100` |
| `LossTarget` | Limite de perda flutuante na moeda da conta. | `-100` |
| `PredTarget` | Faixa usada para acionar reversões de médio. | `40` |
| `LotCoefficient` | Coeficiente de validação herdado do EA. | `2` |
| `CandleType` | Período usado para todos os indicadores. | Velas de `15m` |

## Notas de implementação
- O oscilador DayImpuls está embutido como classe de indicador interno e espelha a lógica original de suavização dupla EMA.
- Como as estratégias do StockSharp gerenciam posições líquidas, hedges simultâneos longo/curto da versão MQL são emulados combinando volumes de fechamento e abertura dentro da mesma ordem de mercado.
- A estratégia funciona apenas com velas finalizadas e usa `IsFormedAndOnlineAndAllowTrading()` para respeitar o ciclo de vida global da estratégia.
- Os preços médios longo/curto são rastreados através de `OnOwnTradeReceived` para que fechamentos parciais e reversões atualizem corretamente o PnL flutuante.
