# Estratégia GreenTrade
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A estratégia GreenTrade é uma conversão do expert advisor MQL5 original. Ela segue tendências de médio prazo combinando um filtro de inclinação de média móvel suavizada (SMMA) com confirmação de momentum do Índice de Força Relativa (RSI). Os sinais são calculados em velas concluídas do período configurado, e a estratégia pode fazer pirâmide até um número configurável de unidades de posição enquanto aplica controles de risco fixos e um stop trailing baseado em etapas.

## Lógica de negociação
1. **Preparação de indicadores**
   - A SMMA é calculada no preço mediano `((High + Low) / 2)` usando o parâmetro `MaPeriod`.
   - O RSI é calculado no preço de fechamento com o retrocesso `RsiPeriod`.
2. **Filtro de forma de tendência**
   - Quatro amostras históricas de SMMA são inspecionadas de acordo com os parâmetros de deslocamento de barra (`ShiftBar`, `ShiftBar1`, `ShiftBar2`, `ShiftBar3`).
   - Uma tendência de alta requer `SMMA(shift0) > SMMA(shift1) > SMMA(shift2) > SMMA(shift3)`.
   - Uma tendência de baixa requer `SMMA(shift0) < SMMA(shift1) < SMMA(shift2) < SMMA(shift3)`.
3. **Confirmação de momentum**
   - O RSI deve estar acima de `RsiBuyLevel` para entradas compradas e abaixo de `RsiSellLevel` para entradas vendidas. O valor do RSI é retirado `ShiftBar` barras atrás para espelhar a lógica MQL5 que ignora a vela em formação.
4. **Execução de ordens**
   - Quando um sinal é confirmado e o limite de posição não é excedido, a estratégia envia uma ordem a mercado por `TradeVolume`.
   - Se existir uma posição na direção oposta, a estratégia primeiro a neutraliza e depois abre uma nova posição com o volume configurado.
   - Se existir uma posição na mesma direção, o volume de negociação é adicionado à exposição líquida até `MaxPositions * TradeVolume`.

## Gestão de riscos
- **Stop Loss / Take Profit inicial**: Cada nova entrada define alvos de preço baseados em `StopLossPips` e `TakeProfitPips`. As distâncias de pip são convertidas em unidades de preço usando o `PriceStep` do instrumento. Instrumentos com passos fracionários (p. ex., símbolos Forex de cinco dígitos) recebem um fator adicional de 10, assim como o expert original.
- **Stop Trailing**: Quando o lucro excede `TrailingStopPips + TrailingStepPips`, o stop é movido para manter uma distância de `TrailingStopPips`. Movimentos adicionais requerem mais `TrailingStepPips` de melhoria de preço, reproduzindo o comportamento de trailing escalonado do código MQL.
- **Limite de posição**: O parâmetro `MaxPositions` limita o número máximo de unidades de volume. Sinais que excederiam esse limite são ignorados.

## Parâmetros
| Parâmetro | Descrição | Padrão |
|-----------|-----------|--------|
| `MaPeriod` | Comprimento da média móvel suavizada aplicada ao preço mediano. | 67 |
| `ShiftBar`, `ShiftBar1`, `ShiftBar2`, `ShiftBar3` | Deslocamentos (em barras) usados para acessar amostras históricas de SMMA para o filtro de forma de tendência. | 1, 1, 2, 3 |
| `RsiPeriod` | Período de retrocesso para o indicador RSI. | 57 |
| `RsiBuyLevel` | Limiar do RSI que confirma configurações de alta. | 60 |
| `RsiSellLevel` | Limiar do RSI que confirma configurações de baixa. | 36 |
| `TradeVolume` | Volume aplicado a cada entrada ou adição. | 0.1 |
| `StopLossPips` | Distância para o stop loss inicial em pips (0 desabilita). | 300 |
| `TakeProfitPips` | Distância para o take profit inicial em pips (0 desabilita). | 300 |
| `TrailingStopPips` | Distância entre o preço e o stop trailing uma vez ativado (0 desabilita o trailing). | 12 |
| `TrailingStepPips` | Progresso adicional necessário antes que o stop trailing se mova novamente. | 5 |
| `MaxPositions` | Número máximo de unidades de volume (múltiplos de `TradeVolume`) que podem estar ativas. | 7 |
| `CandleType` | Série de dados de velas usada para atualizações de indicadores. | Período de 1 hora |

## Notas
- Todos os cálculos são realizados apenas em velas concluídas; velas inacabadas são ignoradas para evitar sinais ruidosos.
- O estado da posição é rastreado internamente para que saídas por stop-loss, take-profit e trailing sejam tratadas mesmo quando ordens protetoras não são colocadas na corretora.
- A conversão mantém o comportamento original para conversão de pip e lógica de passo de trailing, enquanto aproveita a API de alto nível do StockSharp para subscrições e execução de ordens.
