# Estratégia Pinbar Reversão
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Convertida do expert advisor MQL original `PINBAR.mq4` (pasta `MQL/22269`). A estratégia detecta reversões de pin bar no período primário e as confirma com filtros de momentum e MACD de períodos superiores. Reproduz o espírito do sistema fonte enquanto usa os recursos de API de alto nível do StockSharp.

## Lógica de negociação

- **Período primário** – tipo de candle configurável usado para identificar padrões de ação do preço.
- **Período superior** – tipo de candle configurável usado para confirmar o viés de momentum e tendência MACD.
- **Detecção de pin bar** – uma barra é aceita quando o corpo real é pequeno em relação ao intervalo completo e um pavio domina o candle (razões de corpo e pavio configuráveis).
- **Filtro de tendência** – a EMA rápida deve estar acima (ou abaixo) da EMA lenta para configurações compradas (ou vendidas), espelhando os filtros LWMA do código original.
- **Confirmação de momentum** – o momentum no período superior deve estar acima (comprado) ou abaixo (vendido) de um limiar configurável para pelo menos uma das últimas três barras do período superior.
- **Confirmação MACD** – o valor MACD deve estar acima de sua linha de sinal para operações compradas e abaixo da linha de sinal para vendidas, coincidindo com a confirmação MACD mensal usada no expert MQL.
- **Confirmação fractal** – a estratégia mantém uma janela deslizante de cinco barras e requer a presença do último fractal altista/baixista antes de aceitar uma nova operação, similar ao gate `FindFractals()` na fonte.
- **Gestão de risco** – stop-loss configurável, take-profit, gatilho de break-even/offset e lógica de trailing stop rastreiam a posição aberta. A operação é encerrada quando qualquer nível é tocado ou quando o nível de trailing é violado.

## Regras de entrada

### Configuração comprada
1. Último candle no período primário forma um pin bar altista (pavio inferior longo, corpo pequeno).
2. EMA rápida > EMA lenta.
3. Último momentum do período superior (ou um dos dois valores anteriores) está acima do limiar.
4. MACD do período superior está acima de sua linha de sinal.
5. Um fractal altista foi detectado recentemente e o preço não o invalidou.
6. A estratégia está flat ou vendida (as posições vendidas são revertidas).

### Configuração vendida
1. Último candle no período primário forma um pin bar baixista (pavio superior longo, corpo pequeno).
2. EMA rápida < EMA lenta.
3. Último momentum do período superior (ou um dos dois valores anteriores) está abaixo do limiar negativo.
4. MACD do período superior está abaixo de sua linha de sinal.
5. Um fractal baixista foi detectado recentemente e o preço não o invalidou.
6. A estratégia está flat ou comprada (as posições compradas são revertidas).

## Regras de saída

- Stop-loss e take-profit são expressos em porcentagem relativa ao preço de entrada.
- O break-even ativa-se uma vez que o preço se move pelo porcentual de gatilho; o stop é movido para entrada mais/menos um offset.
- O trailing stop ativa-se após o porcentual de ativação ser atingido e segue o preço na distância configurada.
- Sinais opostos também revertem a posição.

## Parâmetros

| Nome | Padrão | Descrição |
| --- | --- | --- |
| `CandleType` | Candles de 15 minutos | Período primário para detecção de padrões. |
| `TrendCandleType` | Candles de 1 hora | Período superior para filtros de momentum/MACD. |
| `FastMaLength` | 6 | Comprimento EMA rápida (substitui LWMA rápida). |
| `SlowMaLength` | 85 | Comprimento EMA lenta (substitui LWMA lenta). |
| `MomentumLength` | 14 | Comprimento do indicador de momentum no período superior. |
| `MomentumThreshold` | 0.1 | Valor mínimo absoluto de momentum para confirmação. |
| `MacdFastLength` | 12 | Comprimento EMA rápida MACD. |
| `MacdSlowLength` | 26 | Comprimento EMA lenta MACD. |
| `MacdSignalLength` | 9 | Comprimento EMA de sinal MACD. |
| `BodyToRangeRatio` | 0.3 | Tamanho máximo do corpo relativo ao intervalo do candle. |
| `WickRatio` | 0.6 | Razão mínima do pavio dominante que define um pin bar. |
| `StopLossPercent` | 2 | Tamanho do stop protetor em porcentagem. |
| `TakeProfitPercent` | 4 | Tamanho do alvo de lucro em porcentagem. |
| `BreakEvenTriggerPercent` | 1.5 | Lucro necessário para mover o stop para break-even. |
| `BreakEvenOffsetPercent` | 0.2 | Offset adicional adicionado ao stop de break-even. |
| `TrailingActivationPercent` | 2.5 | Limiar de lucro para habilitar o trailing stop. |
| `TrailingDistancePercent` | 1 | Distância do trailing stop uma vez ativado. |

## Notas

- O volume está fixado em 1 contrato por padrão; ajuste o volume da estratégia para diferentes tamanhos de posição.
- A detecção fractal reinicia quando o preço viola o nível fractal registrado, exigindo um novo padrão antes de uma nova operação.
- Os intervalos de otimização estão incluídos para os parâmetros principais para facilitar o backtesting e o ajuste no StockSharp Designer.
