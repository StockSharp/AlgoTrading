# Scalper EMA Estratégia Simples
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral

A **Estratégia Simples do Scalper EMA** é uma conversão do consultor especialista MetaTrader `ScalperEMAEASimple`. Ele usa uma combinação de médias móveis exponenciais rápidas/lentas, um oscilador estocástico e um filtro de índice direcional médio (ADX) para identificar entradas de pullback de curta duração dentro de uma tendência existente. A estratégia foi projetada para scalping intradiário em pares de FX líquidos, mas pode ser aplicada a qualquer instrumento onde o gerenciamento de risco baseado em pip faça sentido.

A implementação segue o StockSharp API de alto nível e avalia apenas velas finalizadas. Todos os cálculos são realizados de forma incremental sem reprocessamento de dados históricos, tornando a lógica adequada para negociação em tempo real.

## Pilha de Indicadores

- **EMA rápida (`FastEmaPeriod`)** – detecta impulso de curto prazo.
- **EMA lenta (`SlowEmaPeriod`)** – define a direção da tendência predominante.
- **Stochastic Oscilador (`StochasticLength`, `StochasticKPeriod`, `StochasticDPeriod`)** – rastreia reversões de impulso perto dos limites de sobrevenda/sobrecompra.
- **Índice Direcional Médio** – rejeita negociações quando a tendência se torna excessivamente forte (ADX acima de `AdxThreshold`).

O oscilador estocástico dispara um sinal de confirmação sempre que a linha %K cruza novamente acima do nível de sobrevenda (configurações longas) ou abaixo do nível de sobrecompra (configurações curtas). O par EMA fornece o filtro direcional e o componente ADX garante que as entradas sejam restritas a retrações calmas em vez de tendências descontroladas.

## Lógica de entrada

1. A vela deve fechar no lado da tendência do EMA lenta e o EMA rápida deve concordar com essa direção (`fast > slow` para posições longas, `fast < slow` para posições curtas).
2. A distância entre a vela e o EMA lenta deve ser menor que o alcance da vela e mais estreita que as três distâncias anteriores. Este comportamento recria o loop de detecção de pullback do código MQL original.
3. Ou o corpo da vela cruza o EMA rápido ou o próprio EMA rápido cruza o EMA lento. Esta condição atua como o gatilho do rompimento.
4. O oscilador estocástico deve confirmar o impulso cruzando de volta a partir da zona extrema nas últimas `ConditionWindowBars` velas.
5. ADX deve permanecer abaixo de `AdxThreshold`, evitando negociações quando a volatilidade acelera acentuadamente.
6. Pelo menos `SignalCooldownBars` velas devem passar entre dois sinais consecutivos da mesma direção.

Quando todas as verificações forem aprovadas, a estratégia fecha qualquer exposição oposta e abre uma nova ordem de mercado na direção detectada.

## Lógica de saída e controles de risco

- Um stop-loss inicial é colocado em `StopLossPips` (convertido em preço usando o tamanho do pip do instrumento) a partir do preço de entrada.
- Um trailing stop mantém automaticamente uma distância de `TrailingDistancePips` quando o lucro não realizado atinge `TrailingActivationPips`.
- Sinais opostos forçam uma posição plana antes de estabelecer uma nova negociação.

Todas as ordens de proteção são gerenciadas pelo ajudante `SetStopLoss` de StockSharp para manter os controles de risco sincronizados com o volume de posição atual.

## Parâmetros

| Parâmetro | Descrição |
|-----------|-------------|
| `Volume` | Volume base de negociação para cada sinal. A estratégia adiciona automaticamente a exposição existente para garantir a reversão total ao mudar de direção. |
| `FastEmaPeriod` / `SlowEmaPeriod` | Durações dos períodos para as médias móveis exponenciais. |
| `StochasticLength`, `StochasticKPeriod`, `StochasticDPeriod` | Configuração do oscilador Stochastic espelhando os padrões originais EA. |
| `StochasticOversold` / `StochasticOverbought` | Níveis extremos que definem as zonas de retração. |
| `AdxThreshold` | Valor máximo de ADX permitido antes de rejeitar negociações. |
| `SignalCooldownBars` | Barras mínimas entre sinais sucessivos na mesma direção. |
| `ConditionWindowBars` | Número de barras durante as quais a retração, o rompimento EMA e a confirmação estocástica devem se alinhar. |
| `StopLossPips` | Distância inicial de stop-loss expressa em pips. |
| `TrailingDistancePips` | Distância mantida pelo trailing stop uma vez ativado. |
| `TrailingActivationPips` | Limite de lucro que arma o trailing stop. |
| `CandleType` | Série de velas usada para todos os indicadores. O padrão é um período de 5 minutos. |

## Notas de implementação

- As conversões de pip dependem do instrumento `PriceStep`. Para instrumentos de 3 ou 5 casas decimais, o fator pip é multiplicado por dez, correspondendo às convenções MetaTrader.
- A estratégia processa apenas velas finalizadas, portanto a execução ocorre após o fechamento de cada barra.
- Variáveis ​​de estado interno armazenam os últimos índices de retração, rompimento EMA e confirmações estocásticas para reproduzir as janelas de lookback usadas pelo consultor especialista original sem verificar todo o histórico.

## Uso

1. Anexe a estratégia a uma instância `Connector` ou `Trader` com segurança e portfólio configurados.
2. Certifique-se de que o título tenha um `PriceStep` válido para conversão de pip em preço.
3. Ajuste os parâmetros de acordo com a volatilidade do instrumento. EMA lenta o padrão é 740 para corresponder à origem EA, mas mercados mais rápidos podem se beneficiar de configurações mais curtas.
4. Comece a estratégia. As ordens de mercado e de proteção serão geradas automaticamente quando as condições descritas acima forem satisfeitas.

> **Isenção de responsabilidade**: esta estratégia foi portada para fins educacionais. Testes futuros completos e análises de risco são recomendados antes de negociar capital real.
