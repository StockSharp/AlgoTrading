# Estratégia MACD Stochastic 2
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia reproduz a lógica especialista MetaTrader "MACD Stochastic 2" com a API de alto nível do StockSharp. Combina um filtro de swing MACD de três barras com um oscilador estocástico para buscar reversões de momentum próximas a regiões de sobrevenda e sobrecompra. O risco é controlado por stops e take-profits específicos por direção, e um trailing stop opcional que opera em unidades de pips.

## Visão geral

- Funciona em qualquer instrumento e período fornecido pelo parâmetro `CandleType`.
- Usa a linha principal do MACD para confirmar mínimos/máximos locais, enquanto o histograma e a linha de sinal do MACD permanecem disponíveis para visualização.
- Confirma entradas com leitura de %K do estocástico abaixo de 20 para compras e acima de 80 para vendas.
- Adapta o tratamento de pips do MetaTrader derivando o tamanho do pip do step de preço do instrumento, multiplicando por 10 quando o símbolo tem 3 ou 5 casas decimais.

## Lógica de negociação

### Entrada comprada

1. Os valores da linha principal do MACD da vela atual e das duas anteriores completadas estão todos abaixo de zero.
2. O valor atual do MACD é maior que o anterior, enquanto o anterior é menor que o valor de duas barras atrás (mínimo local).
3. %K do estocástico está abaixo de 20 (sobrevendido).
4. Não há posição comprada existente aberta (`Position <= 0`). Qualquer posição vendida é zerada antes de entrar na nova compra.

### Entrada vendida

1. Os valores da linha principal do MACD da vela atual e das duas anteriores completadas estão todos acima de zero.
2. O valor atual do MACD é menor que o anterior, enquanto o anterior é maior que o valor de duas barras atrás (máximo local).
3. %K do estocástico está acima de 80 (sobrecomprado).
4. Não há posição vendida existente aberta (`Position >= 0`). Qualquer posição comprada é fechada antes de entrar na nova venda.

### Gerenciamento de risco e saídas

- **Stop Fixo / Take Profit:** Cada direção tem distâncias independentes de stop-loss e take-profit baseadas em pips. Os pips são convertidos em offsets de preço absolutos usando o tamanho de pip calculado.
- **Trailing Stop:** Quando habilitado, o trailing stop se ativa após o preço avançar além da distância de trailing. O stop é elevado/rebaixado apenas quando o movimento supera o passo de trailing configurado para evitar excessiva rotação de ordens.
- **Sinais opostos:** Ao entrar em um sinal oposto, primeiro zera a posição existente e então abre a nova com o volume de negociação configurado.

## Parâmetros

| Parâmetro | Padrão | Descrição |
|-----------|--------|-----------|
| `TradeVolume` | `1` | Volume de ordem enviado com cada nova negociação. |
| `StopLossBuyPips` | `50` | Distância em pips do stop-loss para compras. Defina como `0` para desativar. |
| `StopLossSellPips` | `50` | Distância em pips do stop-loss para vendas. Defina como `0` para desativar. |
| `TakeProfitBuyPips` | `50` | Distância em pips do take-profit para compras. Defina como `0` para desativar. |
| `TakeProfitSellPips` | `50` | Distância em pips do take-profit para vendas. Defina como `0` para desativar. |
| `TrailingStopPips` | `0` | Distância do trailing stop em pips. `0` desativa o trailing. |
| `TrailingStepPips` | `5` | Ganho mínimo em pips antes de atualizar o trailing stop. Deve permanecer positivo quando o trailing estiver ativo. |
| `MacdFastPeriod` | `12` | Comprimento da EMA rápida para MACD. |
| `MacdSlowPeriod` | `26` | Comprimento da EMA lenta para MACD. |
| `MacdSignalPeriod` | `9` | Comprimento do suavizamento do sinal para MACD. |
| `StochasticKPeriod` | `5` | Período de lookback para %K estocástico. |
| `StochasticDPeriod` | `3` | Período de suavizamento para %D estocástico. |
| `StochasticSlowing` | `3` | Suavizamento adicional aplicado ao %K estocástico. |
| `CandleType` | `período de 1h` | Tipo de vela (período) usado para cálculos de indicadores. |

## Notas

- O cálculo do tamanho do pip espelha o especialista MetaTrader original: `pip = PriceStep` e é multiplicado por 10 quando o instrumento é cotado com 3 ou 5 decimais.
- Os limites do estocástico (20/80) permanecem como constantes como no script original. Ajuste-os diretamente no código se níveis personalizados forem necessários.
- A estratégia opera apenas em velas completamente fechadas, garantindo consistência com a execução no fechamento de barra do MetaTrader.

## Uso

1. Configure o instrumento desejado, `CandleType` e volume antes de iniciar a estratégia.
2. Ajuste os parâmetros de stop, take-profit e trailing para corresponder à volatilidade do instrumento.
3. Opcionalmente otimize os comprimentos do MACD e estocástico usando o otimizador do StockSharp graças aos parâmetros expostos.
4. Monitore os objetos do gráfico (velas, MACD, estocástico, negociações próprias) adicionados automaticamente quando uma área de gráfico está disponível.
