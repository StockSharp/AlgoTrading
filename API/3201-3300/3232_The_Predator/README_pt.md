# Estratégia de The Predator
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral

Esta estratégia é uma tradução de alto nível do StockSharp do expert advisor MQL **"The Predator"**. O sistema original mistura filtros de direção de tendência com momentum, Bandas de Bollinger e osciladores estocásticos. Dois modelos de entrada independentes (Estratégia 1 e Estratégia 2) estão disponíveis, replicando os modos selecionáveis dentro da implementação MQL.

A conversão foca no processamento baseado em velas, usando assinaturas e vinculações de indicadores do StockSharp. Todos os cálculos são realizados em uma única série de velas configurável.

## Indicadores principais

- **Médias Móveis Linearmente Ponderadas (LWMA)** – estrutura rápida/lenta para confirmar a tendência de curto prazo.
- **Índice de Movimento Direcional + Índice Direcional Médio (DMI/ADX)** – força direcional e confirmação de tendência.
- **Momentum (período 14 por padrão)** – mede a distância do nível neutro 100 tanto para lógica de rompimento quanto de retração.
- **Bandas de Bollinger** – dois envelopes (estreito e largo) para detectar contexto e localização da vela anterior, especialmente para a Estratégia 2.
- **Oscilador Estocástico** – filtro adicional para a Estratégia 2 para exigir zonas de exaustão de momentum.
- **MACD** – confirmação de momentum de tendência comparando a linha MACD com seu sinal.

## Lógica de negociação

### Filtros comuns

1. Processar apenas velas completadas.
2. Exigir que os indicadores selecionados estejam formados antes de negociar (`IsFormedAndOnlineAndAllowTrading`).
3. ADX deve ser maior que o limiar configurado.
4. O histórico de desvio de Momentum é mantido para os últimos três valores para corresponder às verificações MQL sem chamar `GetValue` nos indicadores.

### Estratégia 1

- **Entradas compradas** quando:
  - ADX acima do limiar e +DI supera −DI.
  - LWMA rápida acima da LWMA lenta.
  - Desvio de Momentum acima do limiar de compra em qualquer um dos últimos três valores.
  - Linha MACD acima de sua linha de sinal.
- **Entradas vendidas** espelham o acima com os sinais invertidos.

### Estratégia 2

- **Entradas compradas** requerem adicionalmente:
  - Fechamento da vela anterior em ou acima do limite inferior de Bollinger de banda estreita anterior.
  - Linhas de sinal e principal estocásticas ambas acima do limiar superior.
  - Desvio de Momentum abaixo do limiar de compra em qualquer um dos últimos três valores (buscando retrações dentro de tendências).
- **Entradas vendidas** requerem:
  - Fechamento da vela anterior em ou abaixo do limite superior de Bollinger de banda estreita anterior.
  - Linha de sinal estocástica acima do limiar superior enquanto a linha principal está abaixo do limiar inferior.
  - Desvio de Momentum abaixo do limiar de venda em qualquer um dos últimos três valores.

### Tratamento de posições

- A estratégia cancela quaisquer ordens ativas pendentes antes de abrir uma nova operação.
- Quando ocorre um sinal de reversão, a estratégia fecha a exposição atual e abre uma nova posição na direção oposta usando uma ordem a mercado combinada.

## Gestão de risco

- `StartProtection` configura:
  - Distância inicial de stop-loss em pips.
  - Distância inicial de take-profit em pips.
  - Trailing stop opcional que segue um valor fixo de pips uma vez ativado.
- As distâncias de risco são convertidas em unidades de preço absolutas usando o passo de preço do instrumento.
- Os módulos de ponto de equilíbrio baseado em dinheiro e trailing do EA original são substituídos por essas proteções baseadas em pips (diferença documentada abaixo).

## Parâmetros

| Parâmetro | Descrição |
|-----------|-------------|
| `Mode` | Escolhe Estratégia 1 (rompimento de tendência) ou Estratégia 2 (retração com filtros estocásticos). |
| `FastMaLength`, `SlowMaLength` | Comprimentos LWMA usados para determinar a direção da tendência. |
| `DmiPeriod`, `AdxSmoothing` | Parâmetros do Índice de Movimento Direcional. |
| `MomentumPeriod` | Lookback usado pelo indicador de momentum. |
| `MomentumBuyThreshold`, `MomentumSellThreshold` | Desvio mínimo de 100 necessário para aceitar sinais. |
| `AdxThreshold` | Nível mínimo de ADX sinalizando uma tendência acionável. |
| `BollingerPeriod`, `TightBandWidth`, `WideBandWidth` | Configurações de Banda de Bollinger para os filtros de contexto. |
| `StochasticLength`, `StochasticSmooth`, `StochasticUpper`, `StochasticLower` | Parâmetros para o oscilador estocástico usado na Estratégia 2. |
| `TradeVolume` | Volume enviado com ordens a mercado. |
| `StopLossPips`, `TakeProfitPips`, `TrailingStopPips` | Distâncias de risco (convertidas em unidades de preço com o passo do instrumento). |
| `CandleType` | Série de dados usada pela estratégia. |

## Diferenças em relação à versão MQL

- Os valores de take-profit, stop-loss e trailing baseados em dinheiro são traduzidos em distâncias de pips tratadas por `StartProtection`.
- Ajustes de ponto de equilíbrio e mensagens de notificação por e-mail/push não são portados (não disponíveis na API de alto nível).
- O expert MQL chamava MACD e Momentum em períodos superiores. No StockSharp a lógica roda apenas na série de velas configurada; dados multitemporal podem ser adicionados através de assinaturas adicionais se necessário.
- Otimização de volume de ordens e dimensionamento estilo martingale não estão implementados; a versão StockSharp usa um parâmetro `TradeVolume` fixo.

## Uso

1. Criar um conector e portfólio como em outras amostras do StockSharp.
2. Instanciar `ThePredatorStrategy`, atribuir `Security`, `Portfolio` e os parâmetros desejados.
3. Iniciar a estratégia. A visualização é opcional, mas disponível quando uma área de gráfico é fornecida.

A tradução mantém a árvore de decisão fiel ao original enquanto adota as melhores práticas do StockSharp como vinculação de indicadores e `StartProtection` para risco. Ajuste os limiares ao instrumento e período escolhidos.
