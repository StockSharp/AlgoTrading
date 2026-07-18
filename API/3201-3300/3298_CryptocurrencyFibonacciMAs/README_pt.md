# Estratégia Cryptocurrency Fibonacci MAs (StockSharp)
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
Esta estratégia porta o expert advisor do MetaTrader "Cryptocurrency Fibonacci MAs" para a API de alto nível do StockSharp. O sistema acompanha uma pilha de médias móveis exponenciais baseadas em Fibonacci (8/13/21/55), valida momentum em um período mais alto e confirma a tendência macro com um filtro MACD mensal antes de enviar ordens a mercado. Apenas candles concluídos são processados e todas as atualizações de indicadores são realizadas pelo pipeline `Bind`/`BindEx`.

Em comparação com a versão MetaTrader, os seguintes ajustes intencionais foram feitos:
- Take profit baseado em dinheiro, equity stop-out, trailing candle a candle e automação de break-even foram omitidos. A versão StockSharp usa stop-loss e take-profit clássicos baseados em pips via `StartProtection`.
- Piramidagem de ordens é limitada a uma posição líquida por direção. Reversões fecham primeiro a exposição oposta, espelhando o modelo de posição líquida do StockSharp.
- Dados multitemporais são fornecidos por assinaturas adicionais de candles em vez de solicitações ad hoc de indicadores sob demanda.

## Lógica de negociação
### Entrada comprada
1. Alinhamento EMA: 8 > 13 > 21 > 55 no período principal.
2. Momentum de período superior: o desvio absoluto do Momentum de 14 períodos a partir do nível neutro 100 está acima do limite de compra configurado em pelo menos um dos três últimos candles do período superior.
3. Filtro MACD mensal: a linha principal MACD está acima da linha de sinal.
4. Filtro de posição: a posição líquida atual deve estar zerada ou vendida e permanecer abaixo do volume máximo configurado.

### Entrada vendida
1. Alinhamento EMA: 8 < 13 < 21 < 55.
2. Desvio de momentum acima do limite de venda em pelo menos um dos três últimos candles do período superior.
3. Linha principal MACD abaixo de sua linha de sinal.
4. Exposição líquida deve estar zerada ou comprada e dentro do limite `MaxPositions`.

### Lógica de saída
- `StartProtection` coloca ordens de stop-loss e take-profit de proteção expressas em distâncias de pip. Nenhuma lógica adicional de trailing ou break-even é aplicada nesta versão.
- Sinais de reversão enviam o tamanho da ordem a mercado oposta, que primeiro compensa a posição existente antes de estabelecer a nova exposição.

## Mapeamento multitemporal
O período superior usado para o indicador de momentum espelha a tabela de coeficientes original:

| Período principal | Período de momentum |
| --- | --- |
| 1 minuto | 15 minutos |
| 5 minutos | 30 minutos |
| 15 minutos | 1 hora |
| 30 minutos | 4 horas |
| 1 hora | 1 dia |
| 4 horas | 1 semana |
| 1 dia | 1 mês |
| 1 semana | 1 mês |
| 1 mês | 1 mês |

A confirmação MACD sempre roda em uma aproximação mensal de 30 dias.

## Parâmetros
| Nome | Descrição | Padrão |
| --- | --- | --- |
| `TradeVolume` | Tamanho da ordem em lotes. | 0.1 |
| `StopLossPips` | Distância de stop-loss em pips. | 20 |
| `TakeProfitPips` | Distância de take-profit em pips. | 50 |
| `MomentumBuyThreshold` | Desvio absoluto mínimo de momentum a partir de 100 exigido para operações compradas. | 0.3 |
| `MomentumSellThreshold` | Desvio absoluto mínimo de momentum a partir de 100 exigido para operações vendidas. | 0.3 |
| `MaxPositions` | Volume líquido máximo por direção expresso como múltiplos de `TradeVolume`. | 1 |
| `CandleType` | Período primário para cálculos EMA. | Candles de 1 hora |

## Notas de uso
1. Anexe a estratégia a um símbolo e selecione um período apropriado por `CandleType`.
2. Garanta que a fonte de dados possa fornecer tanto o período principal quanto os períodos superiores derivados (momentum e mensal).
3. Ajuste parâmetros de risco baseados em pips para corresponder ao tamanho do tick do instrumento. O helper converte pips para passos do instrumento usando `Security.PriceStep`.
4. Backtesting e otimização podem ajustar os limites de momentum e as distâncias de stop usando os intervalos de parâmetros fornecidos.
