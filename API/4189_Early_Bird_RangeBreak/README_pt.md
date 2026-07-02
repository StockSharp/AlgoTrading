# Estratégia de intervalo antecipado
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A **Estratégia de quebra de intervalo Early Bird** é uma versão C# do consultor especialista MetaTrader "earlyBird3". Tem como alvo rompimentos de intervalo que acontecem logo após a abertura do pregão europeu. O algoritmo observa um intervalo de consolidação no início da manhã, filtra possíveis rompimentos com um RSI de 14 períodos e insere até três ordens de mercado na direção do rompimento. Cada ordem utiliza níveis de take-profit predefinidos, um stop-loss partilhado e um mecanismo de trailing opcional que é ativado apenas quando a volatilidade se expande para além da sua média recente.

## Requisitos de dados
- Um fluxo de velas de período único (padrão: velas de 5 minutos) para o instrumento negociado.
- O instrumento deve fornecer um `PriceStep` válido porque todas as distâncias de stop-loss e take-profit são definidas em pontos.
- Os tempos de negociação são avaliados usando os carimbos de data e hora das velas recebidas (horário do servidor da fonte de dados).

## Sessão de negociação
1. **Construção de intervalo** – Entre `RangeStartHour` e `RangeEndHour` a estratégia registra a máxima mais alta e a mínima mais baixa.
2. **Janela de negociação** – Depois de `TradingStartHour:TradingStartMinute` e antes de `TradingEndHour` a lógica de breakout se torna ativa.
3. **Fechamento forçado** – Em `ClosingHour` todas as posições restantes são liquidadas independentemente de lucro ou prejuízo.
4. **Apenas durante a semana** – Os sinais são processados de segunda a sexta-feira.

## Lógica de entrada
1. Um nível de rompimento longo é definido em `range high + EntryBufferPoints`, enquanto um nível de rompimento curto é definido em `range low - EntryBufferPoints`. O buffer é expresso em faixas de preço.
2. O filtro RSI deve ser maior que 50 para uma configuração longa e menor ou igual a 50 para uma configuração curta.
3. Apenas um rompimento por direção é permitido em cada dia de negociação. Quando acionadas, três ordens de mercado (volume padrão `0.1`) são enviadas imediatamente.
4. Se uma posição oposta já estiver aberta e `HedgeTrading` estiver desativado, o novo sinal será ignorado. Quando `HedgeTrading` está ativado, a estratégia primeiro fecha a posição existente e depois entra na nova direção. Isso reflete a intenção do EA original, mas usa reversão de posição porque StockSharp contas são compensadas.

## Gerenciamento de saída
1. **Stop-loss** – Um stop-loss compartilhado (`StopLossPoints`) é aplicado à posição agregada. Se o preço ultrapassar o nível, o tamanho restante será fechado imediatamente.
2. **Escada de lucro** – Três alvos (`TakeProfit1Points`, `TakeProfit2Points`, `TakeProfit3Points`) fecham uma porção de posição cada. A parte restante permanece aberta até ser interrompida, arrastada ou fechada até o final da sessão.
3. **Trailing stop** – Quando resta apenas uma parte, o intervalo atual da vela deve exceder `ATR * TrailingRiskMultiplier`. Se o preço avançou pelo menos `TrailingStopPoints`, o stop loss é intensificado na direção comercial, preservando a distância inicial do stop.
4. **Fecho da sessão** – Qualquer exposição aberta é totalmente nivelada quando o horário atual atinge `ClosingHour`.

## Parâmetros
| Parâmetro | Descrição | Padrão |
|-----------|-------------|---------|
| `AutoTrading` | Ativa/desativa a execução de ordens. | `true` |
| `HedgeTrading` | Permite a reversão de posição em sinais opostos (implementado como flat-and-reverse). | `true` |
| `OrderType` | `0` – ambas as direções, `1` – apenas longo, `2` – apenas curto. | `0` |
| `TradeVolume` | Volume por ordem de mercado enviada. | `0.1` |
| `StopLossPoints` | Distância de stop-loss em faixas de preço. | `60` |
| `TakeProfit1Points` | Distância de lucro para a primeira parcela. | `10` |
| `TakeProfit2Points` | Distância de lucro para a segunda parcela. | `20` |
| `TakeProfit3Points` | Distância de lucro para a terceira parcela. | `30` |
| `TrailingStopPoints` | Movimento mínimo favorável antes que o trailing stop seja ativado. | `15` |
| `TrailingRiskMultiplier` | Multiplicador aplicado a ATR ao validar a expansão da volatilidade. | `1.0` |
| `EntryBufferPoints` | Distância extra adicionada aos níveis de fuga. | `2` |
| `RangeStartHour` | Hora em que o intervalo de referência começa. | `3` |
| `RangeEndHour` | Hora em que termina o intervalo de referência. | `7` |
| `TradingStartHour` | Hora em que entradas de breakout são permitidas. | `7` |
| `TradingStartMinute` | Minuto em que entradas de breakout são permitidas. | `15` |
| `TradingEndHour` | Hora após a qual nenhuma nova entrada é feita. | `15` |
| `ClosingHour` | Hora em que todas as negociações estão fechadas. | `17` |
| `RsiPeriod` | RSI lookback usado para filtragem. | `14` |
| `VolatilityPeriod` | ATR lookback para o portão de volatilidade. | `16` |
| `CandleType` | Série de velas usada para análise (padrão 5 minutos). | `TimeSpan.FromMinutes(5)` |

## Notas de implementação
- A estratégia assina velas por meio do StockSharp API de alto nível e vincula os indicadores RSI e ATR diretamente à assinatura.
- Os valores do indicador são consumidos dentro do retorno de chamada `ProcessCandle` sem chamar `GetValue` ou armazenar buffers personalizados, seguindo as diretrizes do projeto.
- Apenas velas prontas são processadas; atualizações parciais são ignoradas.
- Todas as distâncias de preços são convertidas de pontos em preços absolutos usando o instrumento `PriceStep`. Certifique-se de que a definição de segurança exponha o tamanho correto do tick.
- O consultor especialista original manteve MQL pedidos separados para hedge. StockSharp usa posições líquidas, portanto, esta porta executa uma operação de fechamento e reversão quando `HedgeTrading` está habilitado.

## Dicas de uso
- Alinhe o período da vela com o local de negociação usado no EA original (M5 a H1 em MetaTrader). Ajuste `RangeStartHour`, `RangeEndHour` e a janela de negociação para refletir a programação do mercado local do seu feed de dados.
- Ao otimizar, concentre-se no buffer de rompimento, na escada de lucro e no filtro de volatilidade, pois eles definem o equilíbrio entre rompimentos falsos e movimentos perdidos.
- O rastreamento é intencionalmente conservador – se você precisar de saídas mais estreitas, considere reduzir `TrailingRiskMultiplier` ou `StopLossPoints` para que os ajustes de rastreamento ocorram com mais frequência.
