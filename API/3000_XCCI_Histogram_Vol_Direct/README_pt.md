# Estratégia XCCI Histogram Vol Direct
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A **Estratégia XCCI Histogram Vol Direct** é uma conversão do especialista MQL5 `Exp_XCCI_Histogram_Vol_Direct`. O sistema multiplica o Commodity Channel Index (CCI) pelo volume, suaviza ambas as séries com uma média móvel configurável e avalia a inclinação do oscilador suavizado. Quando a coloração direcional do histograma muda, a estratégia fecha posições contra o movimento e abre novas operações na direção emergente. A lógica funciona apenas em velas finalizadas e, portanto, comporta-se deterministicamente em dados históricos e ao vivo.

O consultor especialista original usava uma biblioteca de suavização proprietária com múltiplos algoritmos, bandas de limiar baseadas em volume e execução de ordens com deslocamento de tempo. O port para StockSharp mantém as entradas configuráveis, aproxima as escolhas de suavização com indicadores disponíveis e implementa a mesma sequência de abertura/fechamento usando a API de alto nível.

## Regime de mercado e vantagem
- Projetado para mercados onde a expansão do volume acompanha explosões de momentum.
- Prefere períodos com oscilações claras (padrão: velas de 2 horas), mas pode ser ajustado de intradiário a horizontes de swing.
- Os sinais reagem a uma mudança na inclinação do CCI*volume suavizado; portanto, comporta-se como um detector de reversão de momentum.

## Indicadores e pipeline de processamento
1. **Commodity Channel Index (CCI)** – calculado no tipo de vela selecionado com período `CciPeriod`.
2. **Fonte de volume** – `Tick` ou `Real` (ambos mapeados para o volume de vela porque as contagens de ticks não estão disponíveis nas velas do StockSharp).
3. **Oscilador ponderado** – multiplicar o CCI pelo fluxo de volume escolhido.
4. **Suavização** – aplicar a família de médias móveis selecionada tanto ao oscilador ponderado quanto ao volume bruto usando o comprimento `SmoothingLength`.
   - `Sma` → SimpleMovingAverage
   - `Ema` → ExponentialMovingAverage
   - `Smma` → SmoothedMovingAverage
   - `Lwma` → WeightedMovingAverage
   - `Jjma` → JurikMovingAverage
   - `Jurx` → ZeroLagExponentialMovingAverage
   - `Parabolic` → ArnaudLegouxMovingAverage (parâmetro de fase mapeado para offset ALMA)
   - `T3` → TripleExponentialMovingAverage
   - `Vidya` → ExponentialMovingAverage (melhor aproximação disponível)
   - `Ama` → KaufmanAdaptiveMovingAverage
5. **Cor direcional** – comparar o valor mais recente do oscilador suavizado com o anterior. Valores crescentes são coloridos `0` (altista), valores decrescentes `1` (baixista), e valores iguais herdam a cor anterior, assim como o buffer do indicador original.
6. **Memória de sinais** – armazenar as cores recentes para que a estratégia possa inspecionar a barra especificada por `SignalBar` e a barra anterior.

## Regras de negociação
### Gestão de posições compradas
- **Entrada**: Se a cor da barra de sinal for `1` (baixista) mas a barra anterior foi `0` (altista), abrir uma posição comprada desde que `AllowLongEntries = true` e a posição líquida atual não seja já comprada. O tamanho da ordem é `Volume + |Position|`, de modo que qualquer exposição vendida é encerrada primeiro.
- **Saída**: Sempre que a barra anterior à barra de sinal for altista (`0`) e `AllowShortExits = true`, fechar qualquer posição vendida aberta para evitar lutar contra o novo impulso ascendente.

### Gestão de posições vendidas
- **Entrada**: Se a cor da barra de sinal se tornar `0` após um `1` anterior, abrir uma posição vendida quando `AllowShortEntries = true` e a conta não estiver já líquida vendida. O tamanho da ordem espelha a lógica comprada.
- **Saída**: Quando a barra anterior à barra de sinal for baixista (`1`) e `AllowLongExits = true`, fechar a exposição comprada.

### Controles de risco
- `StopLossPoints` e `TakeProfitPoints` se traduzem em deslocamentos de preço usando o `PriceStep` do instrumento e são aplicados através de `StartProtection`.
- Ordens de proteção são ativadas para cada operação; defina ambos os valores para `0` para desabilitar um trecho individual.

## Referência de parâmetros
| Parâmetro | Descrição | Padrão |
|-----------|-----------|--------|
| `CciPeriod` | Comprimento do Commodity Channel Index. | `14` |
| `Smoothing` | Família de médias móveis usada para suavizar o oscilador e o volume. | `T3` |
| `SmoothingLength` | Período dos filtros de suavização. | `12` |
| `SmoothingPhase` | Valor de fase/offset mapeado para o offset ALMA; mantido por compatibilidade. | `15` |
| `HighLevel2`, `HighLevel1`, `LowLevel1`, `LowLevel2` | Multiplicadores de limiar preservados do indicador (úteis para diagnóstico/visualização). | `100`, `80`, `-80`, `-100` |
| `SignalBar` | Índice de retrocesso da barra que define o sinal (0 = última vela fechada). | `1` |
| `AllowLongEntries` / `AllowShortEntries` | Habilitar ou desabilitar a abertura de operações em uma direção. | `true` |
| `AllowLongExits` / `AllowShortExits` | Habilitar ou desabilitar o fechamento de operações em uma direção. | `true` |
| `StopLossPoints` | Distância do stop-loss em pontos de preço. | `1000` |
| `TakeProfitPoints` | Distância do take-profit em pontos de preço. | `2000` |
| `VolumeSource` | Fluxo de volume (`Tick` ou `Real`). Ambos usam volume de vela neste port. | `Tick` |
| `CandleType` | Período para análise. | `2h` |

## Fluxo de trabalho de processamento de velas
1. Aguardar uma vela finalizada do tipo configurado.
2. Calcular o valor do CCI e multiplicá-lo pelo fluxo de volume selecionado.
3. Alimentar o CCI ponderado e o volume bruto nos filtros de suavização.
4. Assim que ambos os suavizadores estiverem formados, determinar a nova cor e atualizar o buffer de histórico.
5. Inspecionar a cor em `SignalBar` e `SignalBar+1` para decidir se fecham posições opostas e/ou abrem uma nova operação.
6. Aplicar gestão de risco via stop-loss e take-profit pré-configurados.

## Notas de uso
- O `Strategy.Volume` base deve ser definido como um valor positivo; ele define o tamanho de cada entrada.
- Como as velas do StockSharp não expõem contagens de ticks, ambos os modos de volume `Tick` e `Real` usam `candle.TotalVolume`. Se dados em nível de tick forem necessários, alimente a estratégia com velas personalizadas que codifiquem o volume de ticks no campo `TotalVolume`.
- A fase de suavização afeta apenas o ALMA. Para outros filtros é ignorada, espelhando o comportamento do indicador MQL onde certos modos desconsideram a entrada de fase.
- Multiplicadores de limiar (`HighLevel*` e `LowLevel*`) são mantidos por completude. Podem ser visualizados plotando o volume suavizado e aplicando os multiplicadores externamente, se desejado.

## Limitações e diferenças em relação à versão MQL5
- O StockSharp atualmente carece de implementações diretas de VIDYA e Parabolic MA; EMA e ALMA são usados como substitutos mais próximos. Isso mantém as características de resposta similares, mas não idênticas à biblioteca personalizada original.
- A execução de ordens ocorre imediatamente no fechamento da vela de sinal. O especialista MQL agendava operações no início do próximo período via `TimeShiftSec`; esse comportamento é funcionalmente equivalente quando o corretor executa ordens de mercado quase instantaneamente.
- O volume de ticks é aproximado pelo volume total negociado porque as contagens individuais de ticks não são expostas em mensagens de velas padrão.

## Primeiros passos
1. Vincular a estratégia ao `Security` desejado e definir `Volume` para o número de lotes/contratos a negociar por sinal.
2. Escolher o período das velas através de `CandleType` (padrão: período de 2 horas).
3. Ajustar parâmetros de suavização e risco para corresponder ao perfil de volatilidade do mercado alvo.
4. Executar primeiro no modo papel, revisar o oscilador suavizado no gráfico e ajustar `SignalBar` se os sinais chegarem muito cedo ou tarde.

## Ideias de otimização
- Otimizar `SmoothingLength` junto com `CciPeriod` para alinhar a capacidade de resposta com o ativo alvo.
- Testar `SignalBar` em torno de `0` e `1` para reação mais rápida/lenta.
- Considerar ampliar ou reduzir `StopLossPoints` / `TakeProfitPoints` para adaptar ao ATR do instrumento.
- Executar a estratégia em múltiplos períodos e filtrar operações pela direção de tendência do período superior se confirmação adicional for necessária.

## Lista de verificação de segurança
- Confirmar que `Security.PriceStep` e `Volume` correspondem às especificações do contrato do instrumento antes da execução ao vivo.
- Monitorar o deslizamento e ajustar controles de risco externos se o mercado escolhido for ilíquido.
- Revisar regularmente os registros de operações para garantir que os filtros de direção (`Allow*`) estejam alinhados com a exposição pretendida.
