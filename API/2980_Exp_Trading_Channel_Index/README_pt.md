# Estratégia Exp Índice do Canal de Trading
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
Esta estratégia é um port StockSharp do consultor especialista MQL5 `Exp_Trading_Channel_Index`. Ela segue o oscilador Trading Channel Index (TCI), um indicador de momentum ajustado por volatilidade que colore cada barra de acordo com sua posição em relação a dois níveis de canal. A estratégia reage quando a cor atribuída a uma barra histórica muda, imitando o comportamento do consultor especialista original.

A implementação assina uma série de candles configurável (padrão: H4) e processa apenas candles finalizados. Todas as decisões de gestão de negociação são tomadas na abertura da próxima barra após uma mudança de cor, assim como no script original.

## Indicador Trading Channel Index
O TCI é calculado em três estágios:

1. **Suavização primária** da fonte de preço escolhida via uma média móvel configurável (SMA, EMA, SMMA, WMA ou Jurik). Isso produz o valor de linha de base `XMA`.
2. **Estimativa de volatilidade** suavizando o desvio absoluto entre o preço e a linha de base.
3. **Normalização** do desvio pelo coeficiente configurado e um segundo estágio de suavização. O valor resultante é comparado com os limites `HighLevel` e `LowLevel` para atribuir um de cinco códigos de cor:
   - `0` (lima) – valor está acima de `HighLevel`.
   - `1` (verde-azulado) – valor é positivo mas abaixo de `HighLevel`.
   - `2` (cinza) – valor está próximo de zero.
   - `3` (laranja) – valor é negativo mas acima de `LowLevel`.
   - `4` (dourado) – valor está abaixo de `LowLevel`.

A versão StockSharp usa classes de indicadores nativas para as médias móveis. O Jurik MA respeita a entrada `Phase` enquanto outros métodos a ignoram, correspondendo ao comportamento original onde o parâmetro de fase só é significativo para JJMA.

## Critérios de entrada e saída
O algoritmo inspeciona a barra especificada por `SignalBar` (padrão 1, ou seja, o último candle fechado) e a barra anterior:

- **Abrir comprado**: há duas barras (`SignalBar + 1`) tinha cor `0` (positivo extremo) e a última barra (`SignalBar`) tem uma cor diferente. Uma posição vendida é fechada primeiro se existir, então um novo comprado de `TradeVolume` lotes é aberto.
- **Abrir vendido**: há duas barras tinha cor `4` (negativo extremo) e a última barra tem uma cor diferente. Uma posição comprada é fechada primeiro se existir, então um novo vendido é aberto.
- **Fechar comprado**: sempre que a barra mais antiga (há duas barras) estiver colorida com `4`, sinalizando exaustão baixista.
- **Fechar vendido**: sempre que a barra mais antiga estiver colorida com `0`, sinalizando exaustão altista.

A lógica reproduz o gerenciamento baseado em sinalizadores de `TradeAlgorithms.mqh`: saídas são avaliadas antes das entradas, e negociações opostas são liquidadas antes de abrir uma nova posição.

## Gestão de risco
Ordens de proteção opcionais são implementadas em unidades de passo de preço:

- `StopLossPoints` define a distância entre o preço de entrada e o nível de stop-loss. O stop é colocado abaixo das entradas compradas e acima das vendidas.
- `TakeProfitPoints` define a distância do alvo de lucro usando a mesma medida baseada em passos.

Os stops são verificados em cada candle finalizado. Se tanto o stop quanto o alvo forem acionados na mesma barra, a primeira condição que se tornar verdadeira fecha a posição.

## Parâmetros
- **Trade Volume** (`TradeVolume`): quantidade de ordem para cada nova posição.
- **Stop Loss (pts)** (`StopLossPoints`): distância de stop-loss em passos de preço.
- **Take Profit (pts)** (`TakeProfitPoints`): distância de take-profit em passos de preço.
- **Enable Long Entries/Exits** (`BuyPositionOpen`, `BuyPositionClose`): interruptores para sinais comprados.
- **Enable Short Entries/Exits** (`SellPositionOpen`, `SellPositionClose`): interruptores para sinais vendidos.
- **Signal Bar** (`SignalBar`): quantas barras atrás avaliar para a mudança de cor.
- **High Level / Low Level** (`HighLevel`, `LowLevel`): limites para atribuição de cor.
- **Primary / Secondary Method** (`Method1`, `Method2`): tipos de média móvel para ambos os estágios de suavização.
- **Length #1 / Length #2** (`Length1`, `Length2`): períodos usados pelas médias móveis.
- **Phase #1 / Phase #2** (`Phase1`, `Phase2`): configurações de fase Jurik (ignoradas por outros métodos).
- **Coefficient** (`Coefficient`): fator de normalização aplicado ao desvio.
- **Applied Price** (`AppliedPrice`): fonte de preço (close, open, high, low, median, typical, weighted, simple, quarter, trend-follow, trend-follow average, Demark).
- **Candle Type** (`CandleType`): período usado para cálculos do indicador.

## Notas
- O port Python é intencionalmente omitido conforme solicitado.
- A versão StockSharp mantém a diretriz de indentação baseada em tabulações e adiciona comentários em inglês em todo o código.
- O indicador não desenha histogramas de cor; no entanto, tanto o valor numérico quanto o índice de cor estão disponíveis via a classe personalizada `TradingChannelIndexValue` para visualização adicional, se desejado.
