# Estratégia MA Shift Puria Method
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A estratégia MA Shift Puria Method é uma implementação do clássico Expert Advisor "Puria" adaptado para a API de alto nível do StockSharp. O algoritmo combina múltiplas médias móveis exponenciais (EMAs) com um filtro MACD e lógica de trailing opcional baseada em fractais. Os sinais são avaliados apenas em velas concluídas. O gerenciamento de posições inclui níveis fixos de stop-loss e take-profit, trailing stops configuráveis e um modo de trailing fractal opcional que garante lucros perto do alvo quando um ponto de swing confirmado aparece.

## Indicadores e cálculos
- **EMA rápida (padrão 14)** – captura o momentum de curto prazo e define a inclinação da média rápida.
- **EMA lenta (padrão 80)** – representa a direção mais ampla do mercado. A distância entre as EMAs rápida e lenta deve exceder um limiar em pips definido pelo usuário para validar sinais.
- **MACD (rápido 11, lento 102, sinal 9)** – confirma o momentum direcional exigindo que a linha principal cruze o eixo zero na direção da operação enquanto estava no lado oposto três barras atrás.
- **Janela fractal (5 barras)** – usada quando o trailing fractal está habilitado. A estratégia deriva máximos e mínimos de swing de um buffer de cinco barras rolante, correspondendo à definição fractal do MetaTrader (a barra central é o extremo local comparado com duas barras de cada lado).

## Lógica de entrada
Uma nova posição é aberta somente quando a estratégia tem permissão para operar e as seguintes condições são verdadeiras na vela concluída mais recente:

### Entrada comprada
1. EMA rápida está acima da EMA lenta.
2. EMA lenta está tendendo para cima em comparação com seu valor de três barras atrás.
3. EMA rápida tem inclinação ascendente (valor atual acima do valor anterior).
4. Linha principal MACD está acima de zero e estava abaixo de zero três barras atrás.
5. A EMA rápida aumentou mais do que o **Shift Minimum** configurado (em pips) entre as duas últimas barras, e ou continua acelerando ou o incremento anterior era não positivo.

### Entrada vendida
1. EMA rápida está abaixo da EMA lenta.
2. EMA lenta está tendendo para baixo em comparação com três barras atrás.
3. EMA rápida tem inclinação descendente (valor atual abaixo do valor anterior).
4. Linha principal MACD está abaixo de zero e estava acima de zero três barras atrás.
5. A EMA rápida diminuiu mais do que o limiar **Shift Minimum** e ou continua acelerando ou o incremento anterior era não negativo.

A estratégia abre posições em incrementos fixos (volume manual) ou unidades de tamanho dinâmico baseadas no risco do portfólio, dependendo do modo escolhido. Quando uma posição oposta está aberta, o algoritmo a fecha e abre uma nova na direção atual em uma única ordem a mercado.

## Saída e gestão de risco
- **Stop Loss** – definido em pips relativo ao preço de entrada. Se o mínimo/máximo da vela tocar o nível de proteção, a posição é fechada imediatamente.
- **Take Profit** – também expresso em pips. Atingir o alvo fecha toda a posição.
- **Trailing Stop** – quando habilitado, o nível de stop segue o preço pela distância configurada após os lucros excederem a distância de trailing mais o passo de trailing. A lógica espelha o expert MQL original, atualizando somente quando o stop pode se mover pelo menos o passo de trailing.
- **Trailing Fractal** – opcional. Uma vez que o preço cobre 95% da distância ao take-profit, o stop pode ser movido para o último mínimo de swing (comprado) ou máximo de swing (vendido) identificado pelo padrão fractal de cinco barras, apertando o risco enquanto deixa margem para um rompimento.
- **Dimensionamento baseado em risco** – se o volume manual está desabilitado, a estratégia arrisca uma porcentagem fixa do portfólio por operação. Divide o capital em risco pela distância monetária do stop e arredonda o resultado para o passo de volume permitido mais próximo dentro dos limites do exchange.

## Parâmetros
| Nome | Descrição | Padrão |
|------|-------------|---------|
| `UseManualVolume` | Alternar entre volume fixo e dimensionamento baseado em risco. | `true` |
| `ManualVolume` | Volume usado por operação quando o dimensionamento manual está ativo. | `0.1` |
| `RiskPercent` | Percentual do patrimônio arriscado por operação (usado quando `UseManualVolume` é false). | `9` |
| `StopLossPips` | Distância do stop-loss em pips. | `45` |
| `TakeProfitPips` | Distância do take-profit em pips. | `75` |
| `TrailingStopPips` | Distância do trailing stop em pips. | `15` |
| `TrailingStepPips` | Movimento mínimo em pips antes de atualizar o trailing stop. | `5` |
| `MaxPositions` | Número máximo de unidades de posição que podem ser acumuladas em uma direção. | `1` |
| `ShiftMinPips` | Inclinação mínima de EMA em pips necessária para um sinal válido. | `20` |
| `FastLength` | Comprimento da EMA rápida. | `14` |
| `SlowLength` | Comprimento da EMA lenta. | `80` |
| `MacdFast` | Período rápido do MACD. | `11` |
| `MacdSlow` | Período lento do MACD. | `102` |
| `UseFractalTrailing` | Habilitar/desabilitar ajustes de trailing fractal. | `false` |
| `CandleType` | Tipo de vela (período) usado para os cálculos. | `15 minutos` |

## Notas de implementação
- A estratégia assina um fluxo de velas e vincula indicadores EMA e MACD via `SubscribeCandles().Bind(...)`, garantindo que os valores dos indicadores sejam recebidos no manipulador de sinais sem consultas manuais ao buffer.
- O estado interno rastreia os últimos três valores de EMA e MACD para imitar o indexamento `shift` do MQL exigido pela lógica original.
- Os fractais são calculados localmente usando uma janela de cinco barras, correspondendo ao comportamento do MetaTrader sem chamar `GetValue` no indicador.
- O gerenciamento de stop e take-profit é realizado com saídas a mercado quando os níveis de preço são violados, espelhando o efeito das modificações de posição originais.
- A chamada `StartProtection()` habilita o monitoramento de posições integrado do StockSharp para resiliência durante desconexões inesperadas.

## Recomendações de uso
1. Selecione um tipo de vela apropriado (por ex., barras de 15 minutos para pares de moedas principais) para refletir a configuração original de Puria.
2. Ajuste os parâmetros baseados em pips para corresponder ao valor de ponto do instrumento. O helper escala automaticamente para cotações de cinco dígitos, mas instrumentos exóticos podem exigir ajuste personalizado.
3. Ao habilitar o dimensionamento baseado em risco, verifique a avaliação do portfólio e as restrições de passo de volume para garantir que o volume calculado seja negociável.
4. Combine com gestão de capital a nível de portfólio ou filtros de sessão se necessário; a estratégia foca estritamente na lógica de sinal e trailing do expert MQL original.
