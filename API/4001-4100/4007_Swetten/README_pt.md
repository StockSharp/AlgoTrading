# Estratégia Swetten Strategy
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
Swetten é uma estratégia de breakout orientada por rede neural que foi originalmente distribuída para MetaTrader 4. Ela avalia o spread entre uma média móvel simples de longo prazo de 233 períodos e dez médias móveis mais rápidas calculadas em velas de um minuto. Os spreads são alimentados em uma rede de base radial que produz um nível de ativação de alta ou baixa. Quando a ativação é positiva a estratégia entra comprada, quando é negativa entra curta.

## Mercado e Prazo
- Projetado para os principais pares de FX (o código original era direcionado ao EURUSD).
- A análise usa velas de um minuto e as decisões são tomadas apenas em velas concluídas.
- Os sinais são avaliados a cada duas horas no início da hora (00:00, 02:00,…, 22:00 horário de troca). Não são abertas negociações aos sábados ou domingos.

## Indicadores e recursos
- Médias móveis simples com períodos: 233 (linha de base), 144, 89, 55, 34, 21, 13, 8, 5, 3, 2.
- As entradas da rede neural são as diferenças entre a média de 233 períodos e cada média mais rápida.
- Antes de passar para a rede, as entradas são fixadas em faixas treinadas, normalizadas e escalonadas com os mesmos coeficientes usados na DLL original.
- A rede de base radial é replicada exatamente a partir da função `EURUSDn` exportada, consistindo em 38 recursos gaussianos cujas saídas são calculadas em média para obter a ativação final.

## Regras de negociação
1. Aguarde o fechamento de uma vela de um minuto que termina em uma hora par e cai em um dia da semana.
2. Calcule a ativação da rede neural a partir dos spreads da média móvel.
3. Se a ativação > 0 e a posição atual não for longa, envie uma compra de mercado para `TradeVolume + abs(current position)` lotes.
4. Se ativação < 0 e a posição atual não for curta, envie uma venda no mercado para `TradeVolume + abs(current position)` lotes.
5. As posições são protegidas por:
   - Um lucro fixo definido em etapas de preço (`TakeProfitPoints`).
   - Um stop loss fixo definido em etapas de preço (`StopLossPoints`).
   - Quando qualquer um dos níveis é tocado usando os extremos máximo/mínimo da vela, a posição é fechada por uma ordem de mercado.

## Parâmetros
| Nome | Descrição | Padrão |
| --- | --- | --- |
| `CandleType` | Série de velas usadas para cálculos. | Período de 1 minuto |
| `TradeVolume` | Volume base do pedido em lotes. | 0,1 |
| `SlowPeriod` | Período da média móvel simples da linha de base. | 233 |
| `TakeProfitPoints` | Distância alvo de lucro em etapas de preço. | 150 |
| `StopLossPoints` | Distância de stop-loss em etapas de preço. | 40 |

## Notas de conversão
- A rede neural baseada em DLL de MetaTrader foi totalmente portada para C# convertendo a função exportada em código gerenciado.
- As saídas de proteção imitam as condições originais `OrderClose`, verificando os máximos e mínimos das velas em relação aos limites dos níveis de preço.
- O gerenciamento de entradas acompanha o preço de preenchimento mais recente via `OnNewMyTrade` para alinhar as saídas com os preenchimentos reais.
- Todos os comentários foram reescritos em inglês e o código usa APIs StockSharp de alto nível (`SubscribeCandles`, `Bind`) conforme exigido pelas diretrizes de conversão.
