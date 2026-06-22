# Estratégia MACD com Filtro de Linha Zero e Take Profit
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão Geral
Esta estratégia replica o expert original do MetaTrader 5 "Robot_MACD" que opera cruzamentos da linha de sinal do MACD com filtros adicionais de linha zero. Opera em um único instrumento e busca reversões de momentum confirmadas pela posição da linha MACD em relação ao zero. Um take profit de distância fixa é anexado a cada ordem, espelhando o alvo de lucro baseado em pontos da implementação original.

## Dados e Indicadores
- **Dados primários**: subscrição de uma única vela (período padrão de 5 minutos). O período pode ser alterado com o parâmetro `CandleType` para se adaptar ao mercado negociado.
- **Indicadores**:
  - `MovingAverageConvergenceDivergenceSignal` (MACD + sinal + histograma). Os padrões são EMAs de 12/26 com uma linha de sinal de 9 períodos, correspondendo aos parâmetros MQL.

## Lógica de Negociação
1. Aguardar o cálculo do MACD fornecer os valores atual e anterior das linhas MACD e de sinal.
2. Identificar cruzamentos de alta e de baixa:
   - **Cruzamento de alta**: MACD anterior ≤ sinal anterior **e** MACD atual > sinal atual.
   - **Cruzamento de baixa**: MACD anterior ≥ sinal anterior **e** MACD atual < sinal atual.
3. **Gestão de posições**:
   - Fechar uma posição comprada quando um cruzamento de baixa aparece.
   - Fechar uma posição vendida quando um cruzamento de alta aparece.
4. **Critérios de entrada** (apenas quando não há posição aberta e há capital suficiente):
   - Entrar comprado em um cruzamento de alta **enquanto tanto o MACD quanto o sinal permanecerem abaixo de zero**.
   - Entrar vendido em um cruzamento de baixa **enquanto tanto o MACD quanto o sinal permanecerem acima de zero**.
5. Anexar um take profit fixo no momento do registro da ordem chamando `StartProtection` com uma distância absoluta medida em pontos de preço. A distância equivale ao valor de ponto configurado multiplicado pelo passo de preço do instrumento.

## Gestão de Risco
- Cada ordem tem um take profit anexado definido por `TakeProfitPoints`. Não há stop-loss na lógica base, preservando a paridade com o EA fonte.
- A estratégia verifica se o valor do portfólio é pelo menos `MinimumCapitalPerVolume * VolumePerTrade` antes de colocar uma nova ordem. Isso emula a proteção de margem livre (`FreeMargin() < 1000 * Lots`) da versão MQL.

## Parâmetros
| Parâmetro | Descrição | Valor padrão |
|-----------|-----------|--------------|
| `MacdFast` | Período EMA rápido para MACD. | 12 |
| `MacdSlow` | Período EMA lento para MACD. | 26 |
| `MacdSignal` | Período de suavização da linha de sinal. | 9 |
| `TakeProfitPoints` | Distância do take profit expressa em pontos de preço. | 300 |
| `VolumePerTrade` | Volume de negociação (lotes) utilizado para cada entrada. | 1 |
| `MinimumCapitalPerVolume` | Valor mínimo do portfólio exigido por lote negociado. | 1000 |
| `CandleType` | Tipo de vela (período) utilizado para alimentar o indicador MACD. | Velas de 5 minutos |

## Notas de Implementação
- As ordens são executadas com `BuyMarket`/`SellMarket`, idêntico ao EA que usava ordens de mercado via `CTrade`.
- Os filtros de linha zero impedem entradas na metade oposta do histograma MACD, assim como no script MQL.
- A verificação do valor do portfólio depende de `Portfolio.CurrentValue`. Se o ambiente de negociação não fornecer este valor, a proteção passa automaticamente, mantendo a estratégia utilizável para contas simuladas.
- A seção de desenho no gráfico plota velas, o indicador MACD e marcadores de negociação quando uma área de gráfico está disponível na aplicação host.
