# Estratégia de Médio de Posição MA MACD
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia é uma conversão fiel do consultor especialista do MetaTrader **"MA MACD Position averaging"**. Combina um filtro de
média móvel ponderada com uma verificação da proporção MACD e adiciona um módulo de médio estilo martingale que aumenta o tamanho
da posição sempre que o preço se move adversamente por um número configurável de pips. Todos os parâmetros de risco são
configurados em unidades de pips e convertidos internamente em deslocamentos de preço usando os metadados do instrumento
fornecidos pelo StockSharp.

## Lógica de Trading

1. **Preparação de indicadores**
   - Uma média móvel configurável (`MaPeriod`, `MaMethod`, `MaAppliedPrice`) é amostrada em candles completadas. Os parâmetros
     `SignalBar` e `MaShift` emulam a capacidade do MetaTrader de olhar para trás um número específico de barras e plotar a
     média móvel com um deslocamento horizontal.
   - Um indicador MACD (`MacdFastPeriod`, `MacdSlowPeriod`, `MacdSignalPeriod`, `MacdAppliedPrice`) é processado nas mesmas
     candles. A estratégia armazena as linhas principal e de sinal do MACD em um pequeno buffer circular para que valores
     históricos possam ser acessados sem chamar APIs de indicadores diretamente.
2. **Condições de entrada**
   - **Comprado**: ambas as linhas MACD estão abaixo de zero, a proporção `MACDmain / MACDsignal` é maior ou igual a `MacdRatio`,
     o fechamento da candle está acima da média móvel amostrada e a distância entre o preço e a média é pelo menos `IndentPips`
     pips.
   - **Vendido**: ambas as linhas MACD estão acima de zero, a proporção está acima de `MacdRatio`, o fechamento da candle está
     abaixo da média móvel e a distância entre elas é pelo menos `IndentPips` pips.
   - Novas entradas só são permitidas quando a estratégia não tem exposição. Quando um ciclo de médio já está em andamento, a
     lógica de sinal é ignorada e apenas as regras de médio se aplicam.
3. **Módulo de médio**
   - Quando existe uma posição comprada e o preço cai pelo menos `StepLossingPips` da melhor entrada comprada (a mais baixa),
     a estratégia abre uma operação comprada adicional cujo volume é igual ao volume do último tramo multiplicado por
     `LotCoefficient` (arredondado pelo passo de volume do instrumento).
   - Quando existe uma posição vendida e o preço sobe pelo menos `StepLossingPips` da melhor entrada vendida (a mais alta),
     um novo tramo vendido é adicionado usando o mesmo multiplicador `LotCoefficient`.
   - Se for detectada exposição em ambas as direções (nunca deveria acontecer em condições normais), a estratégia fecha
     imediatamente todos os tramos para restaurar a consistência.
4. **Saídas de proteção**
   - Cada tramo armazena níveis individuais de stop-loss e take-profit expressos em unidades de preço (`StopLossPips`,
     `TakeProfitPips`). Em cada candle terminada, a estratégia verifica se o intervalo da candle cruzou algum dos níveis
     armazenados e, em caso afirmativo, fecha o tramo com uma ordem a mercado.
   - Um trailing stop (`TrailingStopPips`, `TrailingStepPips`) é opcional. Uma vez que o preço avança em favor de um tramo em
     `TrailingStopPips + TrailingStepPips`, o stop é movido para `TrailingStopPips` pips atrás do fechamento atual. O stop
     só se ajusta se o preço fizer um progresso adicional de pelo menos `TrailingStepPips` pips.
5. **Manutenção**
   - Os comandos de volume são alinhados ao passo de volume do instrumento e recortados ao mínimo/máximo permitido. A estratégia
     executa apenas em candles completamente formadas (`CandleStates.Finished`) para evitar o duplo processamento.

## Parâmetros

| Parâmetro | Tipo | Padrão | Descrição |
|-----------|------|--------|-----------|
| `CandleType` | `DataType` | `TimeSpan.FromHours(1).TimeFrame()` | Período usado para cálculos de indicadores. |
| `OrderVolume` | `decimal` | `0.1` | Tamanho de lote base para a entrada inicial. |
| `StopLossPips` | `int` | `50` | Distância do stop-loss em pips (0 desativa o stop). |
| `TakeProfitPips` | `int` | `50` | Distância do take-profit em pips (0 desativa o alvo). |
| `TrailingStopPips` | `int` | `5` | Offset do trailing stop em pips. Deve ser positivo para habilitar o trailing. |
| `TrailingStepPips` | `int` | `5` | Distância pip extra necessária antes que o trailing stop se mova novamente. |
| `StepLossingPips` | `int` | `30` | Recuo de preço em pips que aciona um novo tramo de médio. |
| `LotCoefficient` | `decimal` | `2.0` | Multiplicador aplicado ao volume do tramo anterior ao fazer médio. |
| `SignalBar` | `int` | `0` | Número de barras completadas para olhar para trás ao amostrar indicadores. |
| `MaPeriod` | `int` | `15` | Comprimento da média móvel em barras. |
| `MaShift` | `int` | `0` | Deslocamento horizontal (em barras) aplicado aos valores da média móvel. |
| `MaMethod` | `MovingAverageMethod` | `Weighted` | Algoritmo de suavização da média móvel (simples, exponencial, suavizado, ponderado). |
| `MaAppliedPrice` | `AppliedPriceType` | `Weighted` | Preço da candle usado como entrada para a média móvel. |
| `IndentPips` | `int` | `4` | Diferença mínima em pips necessária entre o preço e a média móvel antes de entrar. |
| `MacdFastPeriod` | `int` | `12` | Comprimento de EMA rápido do filtro MACD. |
| `MacdSlowPeriod` | `int` | `26` | Comprimento de EMA lento do filtro MACD. |
| `MacdSignalPeriod` | `int` | `9` | Comprimento da linha de sinal do filtro MACD. |
| `MacdAppliedPrice` | `AppliedPriceType` | `Weighted` | Preço aplicado usado para o cálculo do MACD. |
| `MacdRatio` | `decimal` | `0.9` | Proporção mínima MACD principal/sinal necessária para permitir trading. |

### Conversão de pips

Todas as configurações baseadas em pips (`StopLossPips`, `TakeProfitPips`, `TrailingStopPips`, `TrailingStepPips`,
`StepLossingPips`, `IndentPips`) são multiplicadas pelo `PriceStep` do instrumento. Quando o instrumento tem 3 ou 5 casas
decimais, o valor é multiplicado adicionalmente por 10 para reproduzir a definição de "pip" do MetaTrader para cotações
fracionárias. Se não há passo de preço disponível, um valor de fallback de `0.0001` é usado.

## Notas de Implementação

- A estratégia mantém uma lista interna de tramos de posição porque o StockSharp opera em modo netting. Cada tramo rastreia seu
  próprio preço de entrada, stop e níveis de take para que o médio se comporte como o EA original do MetaTrader.
- Ordens de proteção são simuladas em software: quando uma candle toca um nível de stop-loss ou take-profit, a posição é
  fechada com uma ordem a mercado nessa barra.
- O médio é desativado automaticamente quando `StepLossingPips` é zero. Caso contrário, cada tramo adicional usa o volume do
  tramo anterior multiplicado por `LotCoefficient` e arredondado para baixo ao passo de volume mais próximo.
- As atualizações do trailing stop usam o fechamento da candle como proxy do preço atual. O stop nunca se move na direção
  adversa e permanece inativo até que o progresso do preço exceda `TrailingStopPips + TrailingStepPips`.
- Os buffers de indicadores respeitam os deslocamentos `SignalBar` e `MaShift` para que a lógica de decisão veja exatamente
  os mesmos valores que o especialista do MetaTrader obteria de seus buffers de indicadores.
