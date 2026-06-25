# Estratégia de Rompimento II
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A Estratégia de Rompimento II é um sistema de rompimento de alta frequência originalmente escrito para MetaTrader 4. Combina um oscilador de timing proprietário com um indicador de pressão de volatilidade para entrar em movimentos direcionais fortes, gerenciando então as operações usando trailing stops adaptativos e piramidação. Esta conversão reproduz a lógica original sobre a API de alto nível do StockSharp e mantém as mesmas proteções para filtros de spread, volatilidade e calendário.

## Lógica de trading convertida
### Oscilador de timing
* Cada nova vela M1 contribui com um "preço típico" (média de high, low e close multiplicado por 100) que alimenta a cascata de suavização herdada.
* A cascata reconstrói a tubagem original de média móvel aninhada / diferença (buffers dtemp/atemp) para produzir um valor de timing de 0 a 100.
* Sinal de compra: o valor de timing cruza para cima sobre sua leitura anterior (buffer[0] > buffer[1] com buffer[1] ≤ buffer[2]).
* Sinal de venda: o valor de timing cruza para baixo (buffer[0] < buffer[1] com buffer[1] ≥ buffer[2]).

### Filtro de volatilidade
* Um desvio padrão de 10 períodos sobre preços de fechamento deve permanecer abaixo de `StdDevLimit`. Quando o limite é ultrapassado, nenhuma nova posição é permitida e opcionalmente um aviso é registrado.
* Uma pontuação de volatilidade personalizada replica a fórmula original de amplitude × densidade de ticks: usa a sobreposição entre a vela atual e a anterior e o número médio de ticks por segundo. A pontuação deve exceder o `VolatilityThreshold` configurável.

### Regras de entrada
* A estratégia trabalha com um único par símbolo/período fornecido através do parâmetro `CandleType` (padrão velas de 1 minuto).
* Quando nenhuma posição está aberta e o filtro de calendário permite o trading, o motor atualiza o tamanho do lote através de `CalculateOrderVolume()` e verifica o spread atual contra `SpreadThreshold` (usando dados bid/ask de nível 1).
* Uma posição comprada é aberta se o oscilador de timing emite um sinal de compra e a pontuação de volatilidade é válida. Uma posição vendida segue a condição espelhada. Na entrada, um stop estático é colocado duas vezes `TrailStopPoints` abaixo/acima do preço de execução.

### Piramidação e trailing
* O módulo de trailing se ativa assim que a posição agregada ganha pelo menos `TrailStopPoints + int(Commission) + SpreadThreshold` pontos de lucro não realizado.
* O stop é ajustado para `TrailStopPoints` atrás do último fechamento (rastreado separadamente para comprados e vendidos). Qualquer melhoria maior que um ponto atualiza o preço de trailing.
* Enquanto as condições de volatilidade, timing e spread permanecerem válidas, a estratégia pode piramidear novas ordens a cada `max(10, SpreadThreshold + 1)` pontos de lucro adicional. Novas ordens desativam o stop estático e dependem puramente da lógica de trailing.

### Gestão de risco e capital
* O tamanho da posição é recalculado antes de cada ordem: `saldo × MaximumRisk ÷ (500000 / AccountLeverage)` arredondado para o passo de volume do instrumento. Se as informações de saldo não estiverem disponíveis, usa `Volume` ou o lote mínimo.
* Uma verificação de margem simplificada aproxima a proteção original do MetaTrader (`volume × price / leverage × (1 + MaximumRisk × 190)`). Ordens são ignoradas se o valor da conta não puder cobrir essa quantia.
* Após a piramidação ser ativada, a estratégia monitora a perda flutuante. Quando a redução não realizada excede `TotalEquityRisk` por cento do valor da conta, todas as posições são liquidadas.

### Proteções de calendário e spread
* O trading para às sextas-feiras após as 23:00 no horário do servidor e durante os últimos dias de trading do ano (dia do ano 358, 359, 365 ou 366) após as 16:00.
* Cada entrada e adição verifica o spread bid/ask atual e omite a execução se ultrapassar o limite configurado.

## Parâmetros
| Parâmetro | Padrão | Descrição |
|-----------|--------|-----------|
| `Commission` | 4 | Comissão de lote completo em pontos usada ao calcular o deslocamento de ativação do trailing. |
| `SpreadThreshold` | 6 | Spread máximo (em pontos) permitido para novas entradas ou piramidação. |
| `TrailStopPoints` | 20 | Distância do trailing stop em pontos; o stop inicial é o dobro desse valor. |
| `TotalEquityRisk` | 0.5 | Porcentagem de perda de patrimônio da conta que aciona uma saída forçada após a piramidação. |
| `MaximumRisk` | 0.1 | Fração do saldo da conta comprometida com cada ordem ao dimensionar o volume. |
| `StdDevLimit` | 0.002 | Desvio padrão máximo de 10 períodos para aceitar novas operações. |
| `VolatilityThreshold` | 800 | Pontuação de volatilidade mínima (amplitude × densidade de ticks) necessária para operar. |
| `AccountLeverage` | 100 | Alavancagem da conta usada na aproximação de margem e dimensionamento de posição. |
| `WarningAlerts` | true | Habilita o registro quando o filtro de desvio padrão bloqueia entradas. |
| `CandleType` | 1 minuto | Tipo de vela usado para todos os cálculos. |

## Indicadores
* `StandardDeviation(Length = 10)` sobre preços de fechamento para o filtro de volatilidade.
* Oscilador de timing personalizado reproduzido do EA original (implementado inline sem objetos de indicador StockSharp).

## Notas de implementação
* O filtro de spread requer dados de nível 1 ao vivo (`Security.BestBid`/`BestAsk`). Quando o feed está ausente, a estratégia assume spread zero.
* As verificações de margem e patrimônio são aproximações porque o EA original dependia de propriedades de conta e tamanhos de contrato específicos do MetaTrader. Ajuste `AccountLeverage`, `MaximumRisk` ou `Volume` para se adequar ao modelo do corretor.
* A conversão usa a API de alto nível do StockSharp (assinaturas de velas com `Bind`) e mantém todos os comentários em inglês. Nenhum port Python é gerado para esta estratégia.
