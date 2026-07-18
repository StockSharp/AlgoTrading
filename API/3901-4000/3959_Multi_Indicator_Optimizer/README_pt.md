# Estratégia de otimização de múltiplos indicadores
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia replica a lógica de votação do especialista MetaTrader **MultiIndicatorOptimizer** sobre o StockSharp API de alto nível. Cinco osciladores clássicos avaliam a vela finalizada e contribuem com um voto ponderado para o sentimento agregado. A pontuação resultante é então comparada com os limites definidos pelo usuário para decidir se a estratégia deve operar comprada, vendida ou nivelar uma posição existente.

## Lógica de negociação

1. **MACD bloco** – inspeciona o sinal do histograma e a relação entre as linhas principal e de sinal (ambas retiradas da barra finalizada anterior). A soma desses dois sinais é calculada e multiplicada por `MacdWeight`.
2. **Bloco oscilador incrível** – mede se o oscilador está acima ou abaixo da linha zero e se o momentum melhora em relação à barra anterior. A votação média é escalonada em `AoWeight`.
3. **Bloco OsMA** – verifica o sinal do histograma MACD da vela anterior e aplica `OsmaWeight`.
4. **Williams Bloco %R** – reage a cruzamentos de sobrevenda/sobrecompra definidos por `WilliamsLowerLevel` e `WilliamsUpperLevel`. Um cruzamento para cima a partir da banda inferior vota em alta, enquanto um cruzamento para baixo a partir da banda superior vota em baixa. O resultado é multiplicado por `WilliamsWeight`.
5. **Bloco Stochastic** – combina duas verificações: uma ultrapassagem de limite de %K vs. `StochasticLowerLevel`/`StochasticUpperLevel` e um relacionamento %K/%D. A média de ambos os subsinais é multiplicada por `StochasticWeight`.

A pontuação agregada é armazenada na coluna `Signal` dos logs e exposta por meio do campo `_lastSignal` dentro da estratégia. O mecanismo de negociação avalia a pontuação da seguinte forma:

- `signal >= EntryThreshold`: feche qualquer posição curta e abra/mantenha uma posição longa.
- `signal <= -EntryThreshold`: feche qualquer posição longa e abra/mantenha uma posição curta.
- `abs(signal) <= ExitThreshold`: alise a posição para evitar negociação em condições de mercado neutras.

Todos os cálculos funcionam na vela finalizada anterior para corresponder à implementação original do MT4 que usava valores de indicadores indexados (`shift = 1/2`).

## Parâmetros

| Parâmetro | Descrição | Padrão |
| --- | --- | --- |
| `CandleType` | Prazo principal para todos os cálculos dos indicadores. | Velas H1 |
| `MacdFast` / `MacdSlow` / `MacdSignal` | EMA comprimentos para o bloco MACD. | 26/12/9 |
| `MacdWeight` | Multiplicador de votos para o bloco MACD. Valores negativos invertem o voto. | 1 |
| `AoShortPeriod` / `AoLongPeriod` | Comprimentos de média móvel usados pelo Awesome Oscillator. | 5/34 |
| `AoWeight` | Multiplicador de votos para o bloco Awesome. | 1 |
| `OsmaFastPeriod` / `OsmaSlowPeriod` / `OsmaSignalPeriod` | Configurações MACD reutilizadas para construir o histograma OsMA. | 26/12/9 |
| `OsmaWeight` | Multiplicador de votos para o bloco OsMA. | 1 |
| `WilliamsPeriod` | Comprimento de lookback para Williams %R. | 14 |
| `WilliamsLowerLevel` / `WilliamsUpperLevel` | Limites de sobrevenda/sobrecompra (em porcentagem). | -80/-20 |
| `WilliamsWeight` | Multiplicador de votos para o bloco Williams. | 1 |
| `StochasticKPeriod` / `StochasticDPeriod` / `StochasticSlowing` | Períodos do oscilador Stochastic e sua suavização interna. | 5/3/3 |
| `StochasticLowerLevel` / `StochasticUpperLevel` | Limites de sobrevenda/sobrecompra para %K. | 20/80 |
| `StochasticWeight` | Multiplicador de votos para o bloco Stochastic. | 1 |
| `EntryThreshold` | Voto absoluto mínimo necessário para abrir ou reverter uma posição. | 0,5 |
| `ExitThreshold` | Largura da zona neutra. As posições são fechadas quando o valor absoluto do sinal cai abaixo deste valor. | 0,1 |

Todos os pesos podem ser negativos para suprimir ou inverter a contribuição de um bloco, o que é conveniente durante execuções de otimização.

## Notas

- A estratégia depende puramente de API de alto nível: `SubscribeCandles`, ligações de indicadores e ajudantes `BuyMarket`/`SellMarket`.
- Cada votação de indicador utiliza apenas velas completadas, garantindo que as decisões sejam baseadas em dados confirmados.
- O dimensionamento da posição é controlado pela propriedade base `Volume` de `Strategy`. As ordens de proteção (stop loss/takeprofit) podem ser adicionadas externamente via `StartProtection` se necessário.
- Comentários extensos são fornecidos em inglês, conforme solicitado, para simplificar a manutenção adicional.
