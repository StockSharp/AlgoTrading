# Estratégia de MACD Cleaner
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A estratégia **MACD Cleaner** é uma conversão do assessor especialista "MACD Cleaner" do MetaTrader 5. Analisa velas concluídas de um único período e coloca operações quando a linha principal do MACD aumenta ou diminui de forma monótona durante três barras fechadas consecutivas. O sistema sempre mantém no máximo uma posição direcional e inverte quando o momentum se reverte.

## Lógica de negociação
- Em cada vela concluída a estratégia lê a linha MACD calculada com os períodos de rápido, lento e sinal configurados.
- Se os últimos três valores de MACD são não decrescentes, a estratégia prepara uma entrada comprada. Se uma posição vendida existir ela é fechada primeiro, então uma nova posição comprada é aberta.
- Se os últimos três valores de MACD são não crescentes, a estratégia prepara uma entrada vendida. As posições compradas existentes são niveladas antes de abrir a vendida.
- Os níveis protetores de stop-loss e take-profit são avaliados em máximas e mínimas de velas usando os deslocamentos baseados em pips.
- Quando os parâmetros de trailing estão habilitados, o stop é puxado na direção da operação assim que o preço avança pelo menos o passo de trailing configurado.
- Todas as ordens de saída são emitidas como ordens de mercado usando o volume de posição agregado para garantir que toda a posição seja fechada.

## Parâmetros
| Nome | Padrão | Descrição |
|------|---------|-------------|
| `CandleType` | Velas de 1 hora | Período usado para cálculo do MACD e avaliação de ordens. |
| `TradeVolume` | 1 | Volume base enviado para uma nova posição. Se o lado oposto estiver aberto, o volume de posição absoluto é adicionado para fechá-lo antes de reverter. |
| `StopLossPips` | 35 | Distância de stop-loss em pips do preço de entrada. Defina como zero para desativar o stop. |
| `TakeProfitPips` | 30 | Distância de take-profit em pips do preço de entrada. Defina como zero para desativar o objetivo. |
| `TrailingStopPips` | 0 | Distância do Trailing stop. Quando zero, a lógica de trailing está desativada. |
| `TrailingStepPips` | 5 | Movimento favorável mínimo (em pips) necessário antes de ajustar o Trailing stop. Ignorado quando o Trailing stop está desativado. |
| `MacdFastPeriod` | 15 | Comprimento da EMA rápida para o indicador MACD. |
| `MacdSlowPeriod` | 33 | Comprimento da EMA lenta para o indicador MACD. |
| `MacdSignalPeriod` | 11 | Comprimento da EMA de sinal para o indicador MACD. |

## Gerenciamento de ordens
- Saídas compradas: a estratégia emite uma ordem de venda de mercado quando o stop-loss, take-profit ou nível de trailing é atingido.
- Saídas vendidas: uma ordem de compra de mercado fecha a posição sob as mesmas condições, espelhadas para operações vendidas.
- Após o fechamento completo da posição, o estado do trailing é redefinido para que a próxima operação comece com níveis novos.

## Notas
- O tamanho do pip é derivado automaticamente do instrumento. Para símbolos com 3 ou 5 casas decimais o pip equivale a dez passos de preço mínimos, imitando a implementação original do MetaTrader.
- A lógica avalia apenas velas concluídas e não age em mudanças intrabarra.
- Para desativar o gerenciamento de risco defina as distâncias em pips correspondentes como zero. O trailing requer tanto `TrailingStopPips` quanto um `TrailingStepPips` positivo para funcionar.
