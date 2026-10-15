# Estratégia RAVI + Awesome Oscillator
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão Geral
- Port do consultor especialista MetaTrader 5 "Ravi AO (edição de barabashkakvn)" para a API de alto nível do StockSharp.
- Combina o Range Action Verification Index (RAVI) com o Awesome Oscillator (AO) para detectar mudanças de impulso otimista e pessimista sincronizadas.
- Funciona em qualquer período e instrumento suportado pelo StockSharp; todos os ajustes numéricos são expressos em pips para se manter próximo à implementação original.

## Indicadores
- **RAVI** – calculado como `100 * (FastMA - SlowMA) / SlowMA` na série de preços selecionada. Você pode escolher o método de suavização (simples, exponencial, suavizado, ponderado), comprimentos e fonte de preço (fechamento, abertura, máximo, mínimo, mediano, típico, ponderado, simples, quarto, trend-follow, Demark).
- **Awesome Oscillator** – indicador de momentum de preço mediano com períodos curto e longo configuráveis. Os padrões correspondem aos valores MT5 (5 e 34).

## Parâmetros
| Nome | Descrição |
| --- | --- |
| `CandleType` | Período da vela ou tipo de dados para assinar. |
| `StopLossPips` | Distância de stop-loss de proteção em pips. `0` desabilita o stop. |
| `TakeProfitPips` | Distância de take-profit em pips. `0` desabilita o take profit. |
| `TrailingStopPips` | Distância base do trailing stop em pips. `0` desabilita o trailing. |
| `TrailingStepPips` | Lucro adicional mínimo (em pips) necessário antes que o trailing stop seja ajustado. Deve ser > 0 quando o trailing estiver habilitado. |
| `FastMethod` / `FastLength` | Método de suavização e comprimento da média móvel rápida do RAVI. |
| `SlowMethod` / `SlowLength` | Método de suavização e comprimento da média móvel lenta do RAVI. |
| `AppliedPrices` | Fórmula de preço usada por ambas as médias do RAVI (fechamento, abertura, máximo, mínimo, mediano, típico, ponderado, simples, quarto, trend-follow #1/#2, Demark). |
| `AoShortPeriod` / `AoLongPeriod` | Períodos rápido e lento do Awesome Oscillator. |

## Regras de Negociação
1. A estratégia atualiza os indicadores quando uma vela fecha (`CandleStates.Finished`).
2. Uma **entrada otimista** é acionada quando:
   - AO há duas barras `< 0` e AO há uma barra `> 0` (cruzamento positivo de zero), e
   - RAVI há duas barras `< 0` e RAVI há uma barra `> 0`.
3. Uma **entrada pessimista** é acionada quando:
   - AO há duas barras `> 0` e AO há uma barra `< 0`, e
   - RAVI há duas barras `> 0` e RAVI há uma barra `< 0`.
4. Apenas uma posição pode estar aberta de cada vez. Sinais são ignorados enquanto uma posição existe.

## Gestão de Saídas
- **Stop-loss**: calculado a partir de `StopLossPips` usando o passo de preço do instrumento (símbolos FX de 5 e 3 dígitos usam um multiplicador de 10×, correspondendo à definição de pip do MT5). Acionado quando as extremidades da vela tocam o nível do stop.
- **Take-profit**: alvo opcional calculado da mesma forma; desabilitado quando `TakeProfitPips = 0`.
- **Trailing stop**: quando habilitado, o stop é ajustado uma vez que o lucro flutuante excede `TrailingStopPips + TrailingStepPips`. Para comprados o stop se move para `ClosePrice - TrailingStopPips`; para vendidos para `ClosePrice + TrailingStopPips`.
- Todas as saídas fecham a posição completa com ordens de mercado.

## Notas de Implementação
- Sinais são avaliados no fechamento da barra; entradas reais ocorrem no mesmo fechamento da vela, enquanto a versão MT5 entra na abertura da próxima barra. Ajuste as configurações se precisar compensar esta diferença.
- Apenas médias móveis fornecidas pelo StockSharp são usadas; modos de suavização exóticos da biblioteca MT5 (JJMA, Jurik, T3, etc.) não estão disponíveis.
- O parâmetro visual `Shift` do indicador MT5 afeta apenas a plotagem; não tem impacto na negociação e, portanto, é omitido.
- As fórmulas de `AppliedPrices` seguem as definições MetaTrader, incluindo as opções TrendFollow e Demark.

## Dicas de Uso
- A estratégia é um seguidor de tendência; combine-a com filtros de período superior ou filtros de volatilidade para reduzir sinais falsos.
- Otimize comprimentos e distâncias em pips por instrumento, especialmente ao mudar entre FX, CFDs e futuros, porque o tamanho do pip é derivado de `Security.PriceStep`.
- Habilite `Strategy.StartProtection` externamente se quiser ordens de stop do lado do broker em vez de saídas dentro da estratégia.
