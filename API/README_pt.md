# Catálogo de estratégias da API StockSharp
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Este diretório contém exemplos de estratégias da API StockSharp implementados em C# e Python. As pastas são divididas em faixas numéricas (`0001-0100`, `0101-0200` e assim por diante), enquanto as páginas abaixo agrupam todas as estratégias pela ideia principal de trading.

Cada entrada do catálogo aponta diretamente para as duas pastas de implementação e usa um logotipo SVG transparente compatível com temas claro e escuro.

**Estratégias:** 3811

**Implementações:** C# e Python

## Tipos de estratégia

- [Arbitragem, pares e valor relativo (25)](StrategyTypes/arbitrage-pairs-relative-value_pt.md) — Estratégias que negociam relações de preços entre instrumentos, spreads ou ativos relacionados, em vez de depender de uma única previsão direcional.
- [Reversão à média e reversões (299)](StrategyTypes/mean-reversion-reversals_pt.md) — Sistemas contra a tendência que procuram preços estendidos, movimentos esgotados ou tendências falhas para negociar um retorno ao equilíbrio ou a um ponto de reversão.
- [Rompimentos e canais (319)](StrategyTypes/breakouts-channels_pt.md) — Estratégias construídas em torno da saída do preço de uma faixa, do cruzamento de suporte ou resistência ou da passagem por um limite calculado de canal.
- [Volume, VWAP e fluxo de ordens (63)](StrategyTypes/volume-vwap-order-flow_pt.md) — Sistemas que usam volume negociado, VWAP, liquidez, profundidade de mercado ou fluxo de ordens para identificar entradas e saídas.
- [Padrões de candles e preço (191)](StrategyTypes/candlestick-price-patterns_pt.md) — Estratégias que reconhecem formações de candles, estruturas gráficas, gaps, pivôs e outros padrões recorrentes diretamente na ação do preço.
- [Sazonalidade, sessões e eventos (92)](StrategyTypes/seasonal-session-event_pt.md) — Sistemas sensíveis ao tempo guiados por sessões, calendários, eventos programados, faixas de abertura ou comportamento sazonal.
- [Estatística, modelos adaptativos e IA (77)](StrategyTypes/statistical-adaptive-ai_pt.md) — Estratégias quantitativas que usam estimação estatística, modelos adaptativos, aprendizado de máquina, redes neurais ou classificação de sinais.
- [Fatores, portfólio e rotação (24)](StrategyTypes/factor-portfolio-rotation_pt.md) — Abordagens multiativos que classificam instrumentos, alocam capital por fatores, rebalanceiam portfólios ou fazem rotação entre mercados.
- [Grid, DCA e gestão de posições (143)](StrategyTypes/grid-dca-position-management_pt.md) — Estratégias focadas em escadas de ordens, preço médio, entradas em etapas, tamanho de posição, saídas e gestão contínua das operações.
- [Scalping e execução (133)](StrategyTypes/scalping-execution_pt.md) — Sistemas de curto prazo em que timing de entrada, spread, colocação de ordens e execução são centrais para a vantagem de trading.
- [Volatilidade e opções (78)](StrategyTypes/volatility-options_pt.md) — Estratégias baseadas em regimes de volatilidade, expansão ou contração da faixa, derivativos, precificação de opções e risco de volatilidade.
- [Médias móveis e cruzamentos (191)](StrategyTypes/moving-averages-crossovers_pt.md) — Sistemas de tendência centrados em direção, alinhamento, deslocamento e faixas de médias móveis, além de cruzamentos rápidos e lentos.
- [Indicadores direcionais de tendência (264)](StrategyTypes/directional-trend-indicators_pt.md) — Estratégias guiadas por ferramentas de tendência e força direcional como ADX/DMI, SuperTrend, Parabolic SAR, Ichimoku e Alligator.
- [Tendência por momentum e osciladores (206)](StrategyTypes/momentum-oscillator-trend_pt.md) — Estratégias direcionais confirmadas por momentum, MACD, RSI, CCI, estocástico, ROC, divergências e osciladores relacionados.
- [Rompimentos, pullbacks e ação do preço (95)](StrategyTypes/breakouts-pullbacks-price-action_pt.md) — Entradas de continuação de tendência por rompimentos, pullbacks, canais, swings, candles, retrações e estrutura de mercado.
- [Tendência adaptativa, multitemporal e especializada (277)](StrategyTypes/adaptive-multitimeframe-specialized-trend_pt.md) — Sistemas de tendência adaptativos, multitemporais, orientados por modelos, híbridos e especializados sem domínio de uma única família de indicadores.
- [Osciladores e sinais de indicadores (203)](StrategyTypes/oscillators-indicator-signals_pt.md) — Estratégias cujo gatilho principal vem de osciladores, limites, cruzamentos ou divergências de indicadores.
- [Ordens, risco e gestão de posições (194)](StrategyTypes/order-risk-position-management_pt.md) — Sistemas centrados em tratamento de ordens, dimensionamento, proteção, grids, recuperação, trailing e gestão de posições existentes.
- [Combinações de indicadores e lógica de sinais (319)](StrategyTypes/indicator-combinations-signal-logic_pt.md) — Regras de entrada compostas por concordância de indicadores, limites, cruzamentos, divergências e seleção de sinais.
- [Níveis de preço, padrões e estrutura de mercado (263)](StrategyTypes/price-levels-patterns-market-structure_pt.md) — Sistemas especializados baseados em níveis, faixas, pivôs, geometria de Fibonacci, ondas, candles e estrutura de mercado.
- [Estratégias quantitativas, adaptativas e experimentais (25)](StrategyTypes/quantitative-adaptive-experimental_pt.md) — Projetos matemáticos, estatísticos, de aprendizado de máquina, adaptativos, aleatórios e deliberadamente experimentais.
- [Ferramentas, painéis, alertas e modelos (74)](StrategyTypes/tools-panels-alerts-templates_pt.md) — Utilitários de trading, painéis de interface, alertas, modelos, ambientes de teste, auxiliares gráficos, bibliotecas e exemplos de integração.
- [Fundamental, macro e específico por ativo (22)](StrategyTypes/fundamental-macro-asset-specific_pt.md) — Lógica especializada ligada a fundamentos, dados macro, relatórios, classes de ativos ou instrumentos e mercados específicos.
- [Regras de tempo, sessão e eventos (13)](StrategyTypes/time-session-event-rules_pt.md) — Estratégias compostas cuja restrição distintiva é uma sessão, janela de tempo, evento de calendário ou programação recorrente.
- [Trading direcional e baseado em regras (111)](StrategyTypes/directional-rule-based-trading_pt.md) — Sistemas direcionais especializados expressos por regras explícitas de compra/venda, longo/curto, tendência, reversão, entrada ou saída.
- [Sistemas especialistas compostos (110)](StrategyTypes/composite-expert-systems_pt.md) — Sistemas multicomponentes, híbridos, de conjunto, robôs, traders e Expert Advisors que combinam vários mecanismos.

## Estrutura do repositório

Cada diretório numerado contém uma visão geral da estratégia e as pastas de implementação `CS` e `PY`. As páginas de tipos fornecem tabelas com descrições breves e links diretos pelos logotipos.

## Compatibilidade

Os exemplos foram projetados para a [API StockSharp](https://github.com/StockSharp/StockSharp) e podem ser adaptados aos fluxos do StockSharp Designer, Shell e Runner.
