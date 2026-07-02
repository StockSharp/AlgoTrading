# Estratégia WAMI Cloud X2
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia replica o comportamento de duplo período do especialista MetaTrader original "Exp_WAMI_Cloud_X2". Usa o Warren Momentum Indicator (WAMI) em um período superior para definir o viés dominante e uma segunda instância do mesmo indicador em um período inferior para cronometrar entradas e saídas. A linha principal WAMI é comparada contra sua linha de sinal interna em ambos os períodos, o que espelha a lógica da implementação MQL original.

## Conceito

- **Construção WAMI** – WAMI é construído a partir da primeira diferença dos preços de fechamento, suavizada por três médias móveis sequenciais com métodos individualmente selecionáveis (SMA, EMA, SMMA ou LWMA). Uma quarta média móvel produz a linha de sinal. O indicador personalizado na estratégia reproduz esta cadeia exatamente, de modo que tanto a linha principal quanto a de sinal estão disponíveis em um payload de valor.
- **Filtro de tendência (período superior)** – As velas padrão de seis horas impulsionam o WAMI de tendência. Quando a linha principal está acima da linha de sinal, a direção da tendência torna-se de alta; abaixo torna-se de baixa. Um estado neutro é mantido quando ambas as linhas são iguais ou o indicador ainda está se formando.
- **Motor de sinal (período inferior)** – As velas padrão de 30 minutos são usadas para buscar entradas. Para cada vela finalizada, a estratégia armazena os valores WAMI recentes e avalia a última barra fechada definida por `SignalBar`. Os cruzamentos são detectados comparando o valor mais recente (`SignalBar`) contra o anterior (`SignalBar + 1`).

## Regras de Trading

1. **Saídas**
   - Posições compradas são fechadas quando o período de sinal mostra baixa persistente (`previous.Main < previous.Signal`) se `CloseLongOnSignal` está habilitado.
   - Posições vendidas são fechadas analogamente quando `CloseShortOnSignal` está habilitado.
   - Quando o período superior muda de direção (`_trendDirection`), o flag respectivo `CloseLongOnTrendFlip` ou `CloseShortOnTrendFlip` força uma saída.
2. **Entradas**
   - Entradas vendidas são permitidas quando o período superior é de baixa e o WAMI de sinal cruza para cima (`current.Main >= current.Signal` com `previous.Main < previous.Signal`). Isso corresponde ao EA original que vende na primeira penetração ascendente da linha de sinal dentro de uma tendência de baixa.
   - Entradas compradas são a condição espelhada quando o período superior é de alta e o WAMI de sinal cruza para baixo (`current.Main <= current.Signal` com `previous.Main > previous.Signal`).
   - Alternâncias de entrada (`EnableBuyEntries`, `EnableSellEntries`) podem desabilitar qualquer lado. Quando uma posição oposta está aberta, a estratégia envia uma ordem a mercado compensatória para aplanar e reverter em um único comando, igual às funções auxiliares MQL.

## Parâmetros

- **WAMI de Tendência** – `TrendPeriod1/2/3`, `TrendMethod1/2/3`, `TrendSignalPeriod`, `TrendSignalMethod`, `TrendCandleType`.
- **WAMI de Sinal** – `SignalPeriod1/2/3`, `SignalMethod1/2/3`, `SignalSignalPeriod`, `SignalSignalMethod`, `SignalCandleType`.
- **Flags de Controle** – `SignalBar`, `EnableBuyEntries`, `EnableSellEntries`, `CloseLongOnTrendFlip`, `CloseShortOnTrendFlip`, `CloseLongOnSignal`, `CloseShortOnSignal`.
- **Tamanho de Trading** – `TradeVolume` define o tamanho da ordem a mercado usado para novas entradas. Reversões enviam o volume oposto mais o tamanho configurado.

Todos os parâmetros são expostos através de objetos `StrategyParam<T>`, portanto podem ser otimizados ou modificados na UI do StockSharp assim como as entradas do MetaTrader permitiam.

## Valores padrão

- **Período de tendência** – velas de 6 horas.
- **Período de sinal** – velas de 30 minutos.
- **Todos os métodos de média móvel** – Simples (SMA).
- **Comprimentos de média móvel** – 4 / 13 / 13 para as três etapas e 4 para a linha de sinal em ambos os períodos.
- **SignalBar** – 1 (usar a última vela fechada).
- **TradeVolume** – 1 contrato.
- **Todos os flags de permissão** – Habilitados (true).

## Notas Adicionais

- A estratégia não define ordens de stop-loss ou take-profit fixas. O gerenciamento de risco deve ser configurado externamente se necessário.
- Os helpers de gráfico desenham as velas do período de sinal, ambas as linhas WAMI e as operações executadas. O período de tendência é plotado em uma área separada para confirmação visual.
- A implementação evita o polling de valores de indicador (sem chamadas `GetValue`) e segue a API de assinatura de velas de alto nível, seguindo as diretrizes do projeto.
