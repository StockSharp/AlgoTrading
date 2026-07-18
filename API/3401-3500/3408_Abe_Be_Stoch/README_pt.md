# ABE BE Stochastic Estratégia envolvente
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia transporta o MetaTrader Expert Advisor **Expert_ABE_BE_Stoch** para o StockSharp API de alto nível. Ele combina análise de velas japonesas com confirmação de impulso para reversões de tempo em torno de zonas de sobrevenda e sobrecompra. O sinal primário procura uma vela envolvente de alta apoiada por um oscilador estocástico profundamente sobrevendido, ou uma vela envolvente de baixa confirmada por uma leitura do oscilador sobrecomprado. Uma vez aberta uma posição, a estratégia depende de cruzamentos de limites estocásticos para gerenciar as saídas, replicando a mecânica de “voto” do especialista original.

A tática é projetada para participação longa e curta. Ele avalia apenas velas concluídas e, portanto, permanece imune a ruídos intrabarras. O dimensionamento da negociação permanece sob o controle da propriedade `Volume` da estrutura, enquanto as proteções opcionais de stop-loss e take-profit convertem as configurações de risco originais baseadas em pontos em objetos StockSharp `Unit`.

## Como funciona

1. **Assinatura de dados** – A estratégia assina o tipo de vela configurado e constrói um `StochasticOscillator` com três parâmetros ajustáveis (`%K`, `%D` e o fator de lentidão).
2. **Detecção de padrão** – Em cada vela finalizada, o algoritmo verifica se a última barra engole o corpo da anterior. Dois métodos auxiliares reproduzem definições envolventes de alta e baixa usadas em MetaTrader.
3. **Confirmação de impulso** – A linha `%D` do estocástico serve como filtro de confirmação. Valores abaixo do limite de sobrevenda (padrão 30) são necessários para negociações envolventes de alta, enquanto valores acima do limite de sobrecompra (padrão 70) são necessários para sinais de baixa.
4. **Gerenciamento de posição** – O valor `%D` anterior é armazenado em cache. Se a nova leitura ultrapassar 20 ou 80, qualquer exposição curta será fechada. Por outro lado, cruzamentos descendentes através de 80 ou 20 liquidam a exposição longa. Esses limites refletem os votos de "fechamento" adicionais produzidos pela lógica MQL.
5. **Tratamento de risco** – Quando distâncias positivas de stop-loss ou take-profit (expressas em etapas de preço) são fornecidas, a estratégia as converte em `UnitTypes.Price` e habilita `StartProtection`. Caso contrário, a proteção padrão StockSharp será ativada com `StartProtection()`.

## Regras de negociação

- **Entrada longa**: a vela anterior é de baixa, a vela atual é de alta e o corpo da vela atual engole o corpo anterior. O valor estocástico `%D` deve estar abaixo de `EntryOversoldLevel` (padrão 30). Qualquer posição curta existente é fechada e uma nova posição longa é aberta via `BuyMarket`.
- **Entrada curta**: A vela anterior é de alta, a vela atual é de baixa e o corpo da vela atual engole o corpo anterior. O valor estocástico `%D` deve exceder `EntryOverboughtLevel` (padrão 70). Qualquer posição longa existente é fechada e uma nova posição curta é aberta via `SellMarket`.
- **Saída longa**: Com uma posição longa aberta, se `%D` cruzar para baixo através de `ExitUpperLevel` (padrão 80) ou `ExitLowerLevel` (padrão 20), a posição será fechada com `SellMarket`.
- **Saída curta**: Com uma posição curta aberta, se `%D` cruzar para cima através de `ExitLowerLevel` ou `ExitUpperLevel`, a posição será coberta usando `BuyMarket`.
- **Paradas/metas**: `StopLossPoints` e `TakeProfitPoints` opcionais convertem distâncias baseadas em pontos em compensações de preço absoluto quando o instrumento expõe um `PriceStep` diferente de zero.

## Parâmetros

| Nome | Tipo | Padrão | Descrição |
| --- | --- | --- | --- |
| `CandleType` | `DataType` | `TimeSpan.FromHours(1).TimeFrame()` | Fonte de vela usada para detecção de padrões. |
| `StochasticPeriodK` | `int` | `47` | Período de lookback para o cálculo rápido de `%K`. |
| `StochasticPeriodD` | `int` | `9` | Período de suavização para a linha de sinal `%D`. |
| `StochasticPeriodSlow` | `int` | `13` | Suavização adicional aplicada a `%K` antes de se tornar `%D`. |
| `EntryOversoldLevel` | `decimal` | `30` | Limite superior para `%D` que permite negociações envolventes de alta. |
| `EntryOverboughtLevel` | `decimal` | `70` | Limite inferior para `%D` que permite negociações envolventes de baixa. |
| `ExitLowerLevel` | `decimal` | `20` | Nível que, quando cruzado para cima, força saídas curtas; quando cruzado para baixo, fecha posições compradas. |
| `ExitUpperLevel` | `decimal` | `80` | Limite superior usado da mesma forma que o nível inferior, mas para território de sobrecompra. |
| `TakeProfitPoints` | `decimal` | `0` | Distância nas etapas de preço para a ordem take-profit (0 a desativa). |
| `StopLossPoints` | `decimal` | `0` | Distância em etapas de preço para a ordem stop-loss (0 a desativa). |

## Notas

- Funciona em qualquer instrumento que forneça OHLC velas; os padrões assumem barras horárias.
- Todos os cálculos dependem de velas fechadas para permanecerem alinhados com a lógica de prazo do especialista MQL.
- O tamanho da posição deve ser configurado por meio da propriedade da estratégia base `Volume` ou do gerenciamento de portfólio de nível superior.
