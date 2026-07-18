# Estratégia MacdPatternTraderV01
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral

`MacdPatternTraderV01Strategy` é uma porta StockSharp fiel do consultor especialista FORTRADER "MacdPatternTraderv01" MetaTrader 4. O sistema procura padrões de gancho MACD que aparecem depois que o oscilador se estende até um nível extremo e depois retorna em direção à linha zero. Quando um gancho de baixa se forma após um pico de sobrecompra, a estratégia abre posições curtas, enquanto um gancho de alta após uma queda de sobrevenda aciona posições compradas. A versão StockSharp preserva o gerenciamento de risco original em várias camadas, incluindo níveis recursivos de stop-loss e take-profit, bem como escalonamento de posição em estágios.

A implementação C# usa a assinatura de vela de alto nível API com os indicadores `MACD`, `ExponentialMovingAverage` e `SimpleMovingAverage`. Todos os cálculos são realizados em velas finalizadas, espelhando as chamadas `iMACD` e `iMA` com mudanças de barra explícitas da versão MQL. A lógica auxiliar adicional rastreia manualmente os máximos e mínimos recentes para reproduzir as pesquisas recursivas de preços que o EA usa para ordens de proteção.

## Lógica de Sinais

1. **Condições de armamento**
   - Uma configuração *baixa* é armada quando a linha principal MACD excede `BearishThreshold`. O sinalizador de armar é apagado assim que MACD cruza abaixo de zero.
   - Uma configuração *otimista* é armada quando a linha principal MACD cai abaixo de `BullishThreshold`. O sinalizador é apagado quando MACD se torna positivo.
2. **Confirmação do gancho**
   - As entradas curtas exigem que `macd₀ < BearishThreshold`, `macd₀ < macd₁`, `macd₁ > macd₂`, a bandeira de baixa permaneça ativa e `macd₂ < BearishThreshold` enquanto `macd₀` permaneça acima de zero.
   - Entradas longas exigem que `macd₀ > BullishThreshold`, `macd₀ > macd₁`, `macd₁ < macd₂`, a bandeira de alta permaneça ativa e `macd₂ > BullishThreshold` enquanto `macd₀` permanece negativo.
3. **Execução de pedido**
   - Quando o gancho é concluído, a estratégia envia uma ordem de mercado com volume `OrderVolume`. Ele armazena simultaneamente os preços calculados de stop-loss e take-profit para monitoramento posterior.

## Gestão de risco

### Stop Loss

O stop-loss imita a função MQL `StopLoss(type)`:

- As negociações curtas procuram a máxima mais alta nas últimas `StopLossBars` velas **excluindo** a barra recém-fechada e, em seguida, adicionam `OffsetPoints * PriceStep` ao resultado.
- As negociações longas buscam o mínimo mais baixo nas últimas `StopLossBars` velas históricas, subtraindo o mesmo deslocamento.

Essa lógica é implementada com pesquisas manuais de extremos em um buffer de memória limitado (1.000 valores) para evitar a construção de grandes coleções personalizadas.

### Obter lucro

O take-profit reproduz a rotina recursiva `TakeProfit(type)` MQL:

1. Comece com o bloco mais recente de valores `TakeProfitBars`. Inclua a vela que acionou o sinal.
2. Calcule o extremo (baixo para posições vendidas, alto para posições compradas) dentro desse bloco.
3. Volte `TakeProfitBars` velas e repita enquanto o novo bloco produz um extremo mais favorável.
4. Pare no primeiro bloco que **não** melhora o extremo e use o último valor registrado como lucro.

### Gerenciamento de posição parcial

- Após a entrada, a estratégia registra o volume original e o preço de entrada.
- As saídas parciais são permitidas somente após o lucro flutuante expresso na moeda da conta exceder `ProfitThreshold`.
- Para posições longas:
  1. Feche um terço do volume inicial quando o fechamento da vela subir acima do meio EMA (`EmaMediumPeriod`).
  2. Feche metade da posição restante quando a alta da vela ultrapassar a média dos valores `SmaPeriod` e `EmaLongPeriod`.
- Para posições curtas, as regras são espelhadas com a vela fechada abaixo da média EMA e a vela baixa abaixo da média composta.

As ordens de proteção são verificadas antes do escalonamento para garantir que paradas bruscas ou alvos sempre tenham precedência.

## Parâmetros

| Parâmetro | Padrão | Descrição |
|-----------|---------|-------------|
| `StopLossBars` | 6 | Número de velas históricas para a busca de stop-loss. |
| `TakeProfitBars` | 20 | Tamanho do bloco usado pelo algoritmo recursivo de take-profit. |
| `OffsetPoints` | 10 | Pontos adicionais adicionados ao preço do stop loss. |
| `MacdFastPeriod` | 5 | Comprimento EMA rápido do indicador MACD. |
| `MacdSlowPeriod` | 13 | Comprimento lento de EMA do indicador MACD. |
| `MacdSignalPeriod` | 1 | Comprimento do sinal EMA do indicador MACD. |
| `BearishThreshold` | 0,0045 | Nível MACD positivo que arma configurações curtas. |
| `BullishThreshold` | -0,0045 | Nível MACD negativo que arma configurações longas. |
| `OrderVolume` | 1 | Volume por ordem de mercado. |
| `EmaShortPeriod` | 7 | EMA rápida usado na primeira saída parcial. |
| `EmaMediumPeriod` | 21 | Médio EMA usado em filtros e saídas parciais. |
| `SmaPeriod` | 98 | SMA usado dentro da média de saída composta. |
| `EmaLongPeriod` | 365 | Longo EMA combinado com o SMA para a segunda saída parcial. |
| `ProfitThreshold` | 5 | Lucro flutuante mínimo (em unidades monetárias) antes da expansão. |
| `CandleType` | Período de 1 hora | Série de velas processada pela estratégia. |

Todos os parâmetros são expostos por meio de `StrategyParam<T>` e oferecem suporte à otimização quando apropriado.

## Notas de implementação

- A estratégia depende exclusivamente de ligações `SubscribeCandles` de alto nível. Ele não envia indicadores para a coleção `Indicators`, seguindo as diretrizes do projeto.
- O histórico de MACD é armazenado usando um registrador de deslocamento compacto de três valores (`_macdPrev1..3`) para imitar o acesso de `iMACD(..., shift)`.
- Os níveis de preços protetores são rastreados como decimais; quando as velas atingem um stop ou alvo, a estratégia fecha toda a posição com ordens de mercado e reinicia a máquina de estado interna.
- O PnL flutuante é estimado usando `PriceStep`/`StepPrice` para que o limite de saída parcial permaneça consistente, independentemente da escala de preços do instrumento.
- Os buffers de velas para máximos e mínimos são limitados a 1.000 elementos, o que é suficiente para os parâmetros padrão, mas evita o crescimento descontrolado.

## Uso

1. Instancie `MacdPatternTraderV01Strategy`, atribua a segurança, o portfólio e o conector desejados.
2. Opcionalmente, ajuste parâmetros como `CandleType`, `StopLossBars` ou `OrderVolume` para se adequar ao instrumento negociado.
3. Inicie a estratégia; ele assinará a série de velas configurada, desenhará MACD e negociará marcadores no gráfico e gerenciará os pedidos automaticamente.

A estratégia contém extensos comentários embutidos que descrevem cada bloco traduzido para facilitar a manutenção e maior personalização.
