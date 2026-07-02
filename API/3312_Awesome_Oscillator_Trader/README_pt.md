# Estratégia Awesome Oscillator Trader
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia Awesome Oscillator Trader é uma conversão direta do expert advisor "AwesomeOscTrader" do MetaTrader. Ela combina o Awesome Oscillator de Bill Williams com filtros de largura das bandas de Bollinger e oscilador Estocástico para temporizar rompimentos após contrações profundas de momentum. O sistema foi projetado para negociação horária de um único símbolo em pares FX altamente líquidos, como EURUSD, espelhando a recomendação original.

A estratégia espera o spread das bandas de Bollinger entrar em um intervalo configurável, sinalizando que a volatilidade contraiu, mas não desapareceu. Durante esse squeeze, o histograma do Awesome Oscillator deve formar um padrão distinto de reversão de cinco barras: quatro barras consecutivas descendentes que permanecem abaixo de zero, seguidas por uma nova barra que vira para a cor ascendente enquanto ainda está negativa. Quando essa estrutura aparece e o Estocástico cruza de volta acima de um nível de sobrevenda, a estratégia abre uma posição comprada esperando que o squeeze se resolva para cima. O padrão inverso — quatro barras positivas ascendentes acima de zero e uma nova barra de cor descendente ainda positiva — combinado com o Estocástico caindo abaixo de um limiar superior, dispara uma entrada vendida.

As posições são protegidas por uma distância de stop baseada em ATR. A cada barra o sistema lê o Average True Range de 3 períodos, multiplica por um fator configurável e converte o resultado para pips com base no tamanho de tick do instrumento. Esse valor define tanto o stop-loss inicial quanto as metas de take-profit, reproduzindo a lógica simétrica de saída da versão MetaTrader. Um trailing stop opcional aperta o nível protetor quando o preço se move favoravelmente pelo número configurado de pips, enquanto `CloseOnReversal` fecha posições quando o padrão oposto do Awesome Oscillator ou mudança de cor aparece. Um filtro de lucro permite fechar apenas operações vencedoras, apenas perdedoras ou todas em sinais de reversão, replicando o comportamento `ProfitTypeClTrd` do EA.

## Regras de negociação

- **Timeframe:** candles de 1 hora por padrão (totalmente configurável).
- **Filtros:**
  - A largura das bandas de Bollinger deve estar entre `BollingerSpreadLower` e `BollingerSpreadUpper` pips.
  - Stochastic %K é comparado com `StochasticLowerLevel` para compras e `StochasticUpperLevel` para vendas.
  - O Awesome Oscillator deve construir a estrutura de reversão de cinco barras, com a barra mais recente mudando de cor enquanto permanece no lado oposto de zero, e sua magnitude normalizada deve exceder `AoStrengthLimit`.
- **Entradas:**
  - **Compra:** condições acima mais a barra atual dentro da janela de horário permitida.
  - **Venda:** condições espelhadas.
- **Saídas:**
  - Níveis de stop-loss e take-profit derivados do ATR definidos simetricamente na entrada.
  - Trailing stop (se `TrailingStopPips` &gt; 0) ajusta na direção do lucro.
  - Fechamento opcional em sinal oposto ou mudança de cor do oscilador, dependendo de `CloseOnReversal` e `ProfitFilter`.

## Parâmetros principais

| Parâmetro | Padrão | Descrição |
|-----------|---------|-------------|
| `CandleType` | 1 hora | Timeframe usado para todos os indicadores. |
| `BollingerPeriod` | 20 | Período do filtro de volatilidade das bandas de Bollinger. |
| `BollingerSigma` | 2.0 | Multiplicador do desvio padrão para as bandas de Bollinger. |
| `BollingerSpreadLower` | 24 pips | Spread mínimo das bandas exigido para operar. |
| `BollingerSpreadUpper` | 230 pips | Spread máximo das bandas permitido. |
| `AoFastPeriod` / `AoSlowPeriod` | 4 / 28 | Períodos rápido e lento do Awesome Oscillator. |
| `AoStrengthLimit` | 0.0 | Magnitude AO normalizada mínima para confirmar entradas. |
| `StochasticKPeriod` / `StochasticDPeriod` / `StochasticSlowing` | 1 / 4 / 1 | Comprimentos do Estocástico que reproduzem os padrões do MetaTrader. |
| `StochasticLowerLevel` / `StochasticUpperLevel` | 12 / 21 | Limiares de sobrevenda e sobrecompra para confirmar sinais. |
| `EntryHour` / `OpenHours` | 16 / 13 | Hora inicial e duração da janela de negociação. Trata a virada de meia-noite como o EA. |
| `RiskPercent` | 0.5% | Percentual de risco usado para dimensionamento quando dados da conta estão disponíveis. |
| `AtrMultiplier` | 4.5 | Multiplicador aplicado ao ATR de 3 períodos para calcular a distância do stop. |
| `TrailingStopPips` | 40 pips | Distância para o trailing stop opcional (0 desativa). |
| `ProfitFilter` | OnlyProfitable | Seleciona se saídas por reversão podem fechar qualquer operação, apenas lucrativas ou apenas perdedoras. |
| `MaxOpenOrders` | 1 | Número máximo de posições simultâneas (mantido em 1 para corresponder ao EA). |

## Notas de implementação

- Usa indicadores StockSharp `BollingerBands`, `StochasticOscillator`, `AwesomeOscillator`, `AverageTrueRange` e `Highest`; não há cálculos manuais de indicadores.
- Valores AO são normalizados nas últimas 100 barras para imitar os buffers do indicador MetaTrader e reproduzir a lógica de cor sem código personalizado.
- O dimensionamento respeita `Security.StepVolume`, `Security.MinVolume`, `Security.MaxVolume` e `Security.StepPrice` quando disponíveis, usando o volume padrão da estratégia caso contrário.
- Níveis protetores são gerenciados totalmente dentro da estratégia: verificações de stop e take-profit executam em cada candle concluído, correspondendo à gestão em nível de tick do EA sem exigir ordens do lado da corretora.
- Todos os comentários no código estão em inglês, e a indentação usa tabulações conforme as diretrizes do projeto.
