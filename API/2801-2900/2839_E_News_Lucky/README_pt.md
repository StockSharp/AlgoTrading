# Estratégia E-News Lucky
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A **Estratégia E-News Lucky** é um port do StockSharp do consultor especialista MetaTrader `e-News-Lucky`. O sistema automatiza a abordagem clássica de rompimento por notícias:

- Em um `PlacementTime` configurável, envia tanto ordens de compra stop quanto de venda stop ao redor do preço atual, deslocadas por `DistancePips`.
- Quando qualquer ordem pendente é executada, a ordem oposta é cancelada imediatamente. Níveis iniciais de stop-loss e take-profit de proteção são anexados de acordo com os deslocamentos em pips configurados.
- Um trailing stop pode ser habilitado via `TrailingStopPips` e `TrailingStepPips` para travar lucros à medida que a negociação se move na direção favorável.
- No `CancelTime` configurado, todas as ordens pendentes restantes são removidas e quaisquer posições abertas são fechadas para evitar manter risco fora da janela de negociação.

A estratégia usa dados de candles (`CandleType`, 1 minuto por padrão) apenas para rastrear os horários agendados e atualizar o trailing stop. Ela não depende de cálculos de indicadores.

## Parâmetros
| Nome | Descrição |
| --- | --- |
| `Volume` | Volume de ordem para cada entrada pendente. A estratégia envia ordens simétricas de compra stop e venda stop com este volume. |
| `StopLossPips` | Distância entre o preço de entrada e o stop-loss de proteção, expressa em pips. Definir como zero para desabilitar o stop. |
| `TakeProfitPips` | Distância entre o preço de entrada e o alvo de lucro em pips. Definir como zero para desabilitar o alvo. |
| `TrailingStopPips` | Distância do trailing stop em pips. O motor de trailing fica ativo somente quando este valor é maior que zero. |
| `TrailingStepPips` | Ganho mínimo em pips necessário antes que o trailing stop seja movido novamente. Previne atualizações excessivas do stop em mercados laterais. |
| `DistancePips` | Deslocamento (em pips) do preço atual usado para colocar as ordens stop. |
| `PlacementTime` | Hora do dia (tempo do broker/servidor) quando as ordens pendentes são colocadas. Padrão: 10:30. |
| `CancelTime` | Hora do dia quando as ordens pendentes são canceladas e as posições abertas são fechadas. Padrão: 22:30. |
| `CandleType` | Série de candles usada para agendamento e trailing. Padrão: período de 1 minuto. |

## Notas de implementação
- O tamanho de pip segue a lógica do MetaTrader: se o símbolo tem 3 ou 5 dígitos, a estratégia multiplica o passo de preço por 10 para trabalhar em unidades de pip.
- Todos os preços são normalizados para o passo de preço do instrumento antes de as ordens serem enviadas.
- Os trailing stops comparam o último fechamento contra `PositionPrice` e apenas movem o stop de proteção quando o ganho excede tanto `TrailingStopPips` quanto `TrailingStepPips`.
- As ordens pendentes são recriadas cada dia de negociação quando o tempo de colocação é alcançado. As verificações do tempo de cancelamento garantem que toda a exposição esteja zerada ao final da janela.

## Dicas de uso
1. Anexe a estratégia a um instrumento líquido com spreads apertados; as distâncias de rompimento assumem comportamento de preço semelhante a notícias.
2. Defina `PlacementTime` e `CancelTime` de acordo com o calendário econômico de interesse.
3. Ajuste as distâncias em pips para corresponder à volatilidade do instrumento. Valores maiores reduzem a chance de falsos disparos, enquanto valores menores podem capturar movimentos mais cedo, mas aumentam o risco de whipsaw.
4. Desabilite o trailing mantendo `TrailingStopPips` em zero se stops fixos forem preferidos.
5. Monitore o slippage e o spread durante notícias de alto impacto para garantir que as ordens pendentes sejam executadas conforme esperado.
