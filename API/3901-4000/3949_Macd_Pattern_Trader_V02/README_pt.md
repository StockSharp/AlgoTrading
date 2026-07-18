# Macd Pattern Trader v02 (StockSharp Porta)
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia é uma conversão StockSharp de alto nível API do especialista MetaTrader **MacdPatternTraderv02.mq4** (diretório `MQL/8194`). Ele reproduz a detecção de padrão MACD original e as regras de gerenciamento de posição ativa, ao mesmo tempo que expõe parâmetros convenientes para otimização adicional.

## Ideia Central

1. Calcule a linha principal MACD usando os períodos EMA rápidos e lentos (`FastEmaPeriod`, `SlowEmaPeriod`) com um comprimento de sinal de uma vela (correspondendo à versão MQL).
2. Monitore apenas velas concluídas. Quando o valor MACD pintar uma sequência específica de três barras ao redor da linha zero, arme o padrão curto ou longo:
   - **Padrão curto**: requer uma fase MACD positiva seguida por um retrocesso negativo acima de `MinThreshold` e depois uma inflexão descendente.
   - **Padrão longo**: requer uma fase MACD negativa seguida por um retrocesso positivo abaixo de `MaxThreshold` e depois uma inflexão ascendente.
3. Execute ordens de mercado usando `TradeVolume` assim que o padrão for confirmado.
4. Proteja cada posição com um stop-loss colocado além do extremo de oscilação recente (acima de `StopLossBars` velas) mais um deslocamento adicional em pontos (`OffsetPoints`).
5. Defina o nível de lucro examinando `TakeProfitBars` segmentos consecutivos e escolhendo o máximo/mínimo mais extremo alcançado enquanto a sequência continua imprimindo novos extremos.
6. Gerencie posições abertas com o gerenciador de posição ativo do especialista original: após atingir um lucro mínimo de cinco pontos, a estratégia fecha um terço do volume quando a vela anterior confirma a tendência (filtro `Ema2Period`) e outra metade quando o preço interage com a linha média de `SmaPeriod` e `Ema3Period`.

## Parâmetros

| Parâmetro | Descrição |
|-----------|-------------|
| `StopLossBars` | Número de velas concluídas inspecionadas ao calcular o extremo da oscilação do stop loss. |
| `TakeProfitBars` | Tamanho da janela (em velas) para a pesquisa sequencial de extremos que constrói a meta de lucro. |
| `OffsetPoints` | Compensação adicional, expressa em pontos de instrumento, adicionada ao stop loss. |
| `FastEmaPeriod` | Comprimento EMA rápido para a linha principal MACD. |
| `SlowEmaPeriod` | Comprimento EMA lento para a linha principal MACD. |
| `MaxThreshold` | Limite positivo MACD que encerra a preparação do padrão curto. |
| `MinThreshold` | Limite MACD negativo que encerra a longa preparação do padrão. |
| `Ema1Period` | Primeiro período EMA usado pelo bloco de gerenciamento de dinheiro original (mantido para fins de integridade). |
| `Ema2Period` | Segundo período EMA usado para validar lucro parcial para posições longas/curtas. |
| `SmaPeriod` | Período SMA usado no segundo gatilho de fechamento parcial. |
| `Ema3Period` | Período EMA lento emparelhado com SMA para detectar saídas de reversão à média. |
| `TradeVolume` | Volume de ordens de mercado (lotes). |
| `CandleType` | Tipo de dados Candle usado para alimentar todos os indicadores. |

## Lógica de negociação

- **Entrada curta**: acionada quando a sequência MACD `(prev3, prev2, prev1, current)` corresponde às condições originais (`macdPrev1 < macdPrev3`, `macdPrev1 > macdPrev2`, `current < prev1`, `current < 0` e verificação de magnitude). A exposição longa existente é achatada antes de abrir uma nova posição curta.
- **Entrada longa**: regras simétricas onde `current > 0`, os valores MACD formam o padrão de imagem espelhada e a verificação de magnitude é satisfeita. A exposição curta existente é achatada antes de abrir uma nova posição longa.
- **Stops e metas**: calculados imediatamente após cada entrada e atualizados somente quando uma nova negociação é executada.
- **Fechos parciais**: quando o lucro atingir cinco pontos (em relação ao tamanho dos pontos do instrumento), a estratégia fecha um terço do volume restante se a vela anterior fechar além de `EMA2`. A próxima etapa fecha metade do volume restante quando a vela anterior ultrapassa a média de `SMA` e `EMA3`.
- **Saída total**: qualquer toque de preço no nível de stop-loss ou take-profit fecha toda a posição. Após cada saída forçada, o estado interno é redefinido automaticamente.

## Notas

- O tamanho do ponto é derivado de `Security.PriceStep` ou, quando não disponível, dos decimais de segurança. Um valor padrão de `0.0001` é usado como um substituto seguro.
- O histórico de velas é armazenado (até 1.024 entradas) para replicar as funções auxiliares MQL `iHighest`, `iLowest` e a varredura extrema sequencial de `TakeProfit()`.
- Todos os comentários dentro da estratégia permanecem em inglês, conforme exigido pelas diretrizes do repositório.
- As portas Python são omitidas intencionalmente para esta tarefa.
