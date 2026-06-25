# EMA Cruzamento de Concurso com Hedge
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
- Recria a estratégia do MetaTrader "EMA Cross Contest Hedged" usando a API de alto nível do StockSharp.
- Opera com um par de médias móveis exponenciais (EMA) e opcionalmente confirma com a linha principal do MACD.
- Constrói uma escada de ordens stop pendentes (níveis de "hedge") após cada entrada para escalar em tendências fortes.
- Aplica níveis estáticos de stop-loss/take-profit expressos em pips e um trailing stop que se ativa após um ganho mínimo.
- Permite escolher se os sinais devem usar a vela completa atual ou a vela fechada anterior.

## Indicadores e dados
- EMA curta com comprimento configurável (padrão 4).
- EMA longa com comprimento configurável (padrão 24); o período curto deve permanecer abaixo do período longo.
- MACD (4, 24, 12) linha principal usada como filtro de confirmação opcional.
- Funciona em qualquer período fornecido pelo parâmetro `CandleType` (padrão velas de 15 minutos).

## Lógica de entrada
1. Aguardar uma vela concluída do período configurado.
2. Calcular os valores de EMA rápida e lenta. Dependendo de `TradeBar`, determinar o cruzamento usando:
   - A última e a vela concluída anterior (`Current`).
   - A anterior e a vela ainda mais antiga (`Previous`, padrão).
3. Gerar um sinal comprado quando a EMA rápida cruzar acima da EMA lenta. Se `UseMacdFilter` estiver habilitado, o valor MACD para a mesma barra deve ser não negativo.
4. Gerar um sinal vendido quando a EMA rápida cruzar abaixo da EMA lenta. Com o filtro MACD habilitado, o valor MACD deve ser não positivo.
5. Abrir uma nova posição somente quando não houver exposição (todas as operações anteriores estão planas).
6. Executar ordens de mercado com tamanho `OrderVolume`. Após uma entrada, a estratégia:
   - Armazena os níveis de stop-loss e take-profit deslocados por `StopLossPips` e `TakeProfitPips` a partir do preço de execução.
   - Reinicia o estado do trailing stop.
   - Cria quatro ordens stop de hedge espaçadas por `HedgeLevelPips` na direção da operação. Cada ordem pendente herda a mesma distância de stop-loss/take-profit e expira após `PendingExpirationSeconds` segundos, a menos que o preço a alcance antes.

## Gestão de saída
- **Stop-loss / take-profit:** A estratégia monitora máximos e mínimos intrabarra. Se o preço tocar o stop ou o alvo armazenado, toda a posição é fechada.
- **Trailing stop:** Quando o lucro exceder `TrailingStopPips + TrailingStepPips`, o stop é seguido a `TrailingStopPips` atrás do último fechamento. Posições compradas seguem para cima, posições vendidas seguem para baixo.
- **Cruzamento oposto:** Quando `CloseOppositePositions` está habilitado, a posição é fechada assim que o cruzamento EMA oposto é detectado.
- **Escada pendente:** Cada ordem de hedge se torna uma ordem de mercado adicional quando o preço cruza o nível stop. Novas execuções ajustam o preço médio de entrada e apertam os níveis de proteção adequadamente.

## Parâmetros
| Nome | Padrão | Descrição |
| --- | --- | --- |
| `OrderVolume` | 0.1 | Tamanho da ordem para cada ordem de mercado ou stop. |
| `StopLossPips` | 140 | Distância do stop em pips. Defina como 0 para desabilitar. |
| `TakeProfitPips` | 120 | Distância do take-profit em pips. Defina como 0 para desabilitar. |
| `TrailingStopPips` | 30 | Distância do trailing stop em pips. Defina como 0 para desabilitar. |
| `TrailingStepPips` | 1 | Ganho adicional mínimo (em pips) antes do trailing stop ajustar novamente. |
| `HedgeLevelPips` | 6 | Distância entre as ordens stop de hedge escalonadas. |
| `CloseOppositePositions` | false | Fechar a posição ativa quando um cruzamento oposto aparecer. |
| `UseMacdFilter` | false | Exigir confirmação MACD (>= 0 para comprados, <= 0 para vendidos). |
| `PendingExpirationSeconds` | 65535 | Vida útil de cada ordem stop de hedge em segundos. |
| `ShortMaPeriod` | 4 | Comprimento da EMA curta. Deve ser menor que `LongMaPeriod`. |
| `LongMaPeriod` | 24 | Comprimento da EMA longa. |
| `TradeBar` | Previous | Determina qual par de barras é usado para detectar o cruzamento. |
| `CandleType` | 15 minutos | Período solicitado ao provedor de dados. |

## Notas adicionais
- Os pips são convertidos multiplicando `Security.PriceStep` e aplicando automaticamente um fator de 10 para instrumentos de 3 e 5 decimais para corresponder às convenções de pip do MetaTrader.
- As ordens de hedge pendentes são simuladas dentro da estratégia e executadas assim que o intervalo da vela toca seu nível.
- `StartProtection()` é invocado para ativar os serviços integrados de proteção de posição do StockSharp.
- A estratégia mantém lógica de trailing stop separada para posições compradas e vendidas para refletir a implementação com hedge original.
