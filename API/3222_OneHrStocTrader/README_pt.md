# Estratégia de OneHrStocTrader
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral

A estratégia **OneHrStocTrader** replica o consultor especialista do MetaTrader 4 *OneHrStocTrader.mq4* dentro da API de alto nível do StockSharp. Ela opera um único instrumento em velas horárias e combina o oscilador estocástico com um filtro de largura das Bandas de Bollinger. Uma operação é aberta apenas quando a volatilidade (medida pela distância entre as Bandas de Bollinger) está dentro do intervalo configurado e o oscilador estocástico abandona uma zona extrema exatamente na hora configurada.

## Lógica de trading

1. **Dados**
   - Trabalha com velas horárias por padrão (configurável).
   - Usa os valores da vela *concluída* mais recente para corresponder ao comportamento do MetaTrader.
2. **Filtro das Bandas de Bollinger**
   - Calcula a diferença entre as bandas superior e inferior em pips.
   - Sinais de trading são ignorados quando a diferença cai fora do intervalo `[BollingerSpreadLower, BollingerSpreadUpper]`.
3. **Gatilho do oscilador estocástico**
   - Referencia as duas velas concluídas mais recentes da linha %K estocástica.
   - **Compra**: %K atual abaixo de `StochasticLower`, %K anterior subindo (`prev < current`) e a nova barra começa em `BuyHourStart`.
   - **Venda**: %K atual acima de `StochasticUpper`, %K anterior descendo (`prev > current`) e a nova barra começa em `SellHourStart`.
4. **Gestão de ordens**
   - Fecha uma posição oposta antes de abrir uma nova.
   - Limita entradas consecutivas na mesma direção via `MaxOrdersPerDirection`.
5. **Gestão de risco**
   - Distâncias fixas de take-profit e stop-loss definidas em pips.
   - Trailing stop opcional que se move em incrementos de pip assim que o preço avança além da distância configurada.
   - Níveis de proteção internos são monitorados em cada vela concluída; quando atingidos, a estratégia fecha a posição a mercado.

## Parâmetros

| Nome | Descrição | Padrão |
|------|-------------|---------|
| `TradeVolume` | Tamanho de ordem em lotes. | `0.01` |
| `CandleType` | Período usado para todos os cálculos. | `1h` |
| `BollingerPeriod` | Período de retrocesso das Bandas de Bollinger. | `20` |
| `BollingerSigma` | Multiplicador sigma das Bandas de Bollinger. | `2.0` |
| `BollingerSpreadLower` | Diferença mínima de banda em pips necessária para operar. | `56` |
| `BollingerSpreadUpper` | Diferença máxima de banda em pips permitida para operar. | `158` |
| `BuyHourStart` | Hora (0-23) quando entradas compradas são avaliadas. | `4` |
| `SellHourStart` | Hora (0-23) quando entradas vendidas são avaliadas. | `0` |
| `StochasticKPeriod` | Período %K estocástico. | `5` |
| `StochasticDPeriod` | Período %D estocástico. | `3` |
| `StochasticSlowing` | Fator de desaceleração estocástico. | `5` |
| `StochasticLower` | Limiar de sobrevenda. | `36` |
| `StochasticUpper` | Limiar de sobrecompra. | `70` |
| `TakeProfitPips` | Distância de take-profit em pips. | `200` |
| `StopLossPips` | Distância de stop-loss em pips. | `95` |
| `TrailingStopPips` | Distância de trailing stop em pips (0 = desabilitado). | `40` |
| `MaxOrdersPerDirection` | Máximo de entradas consecutivas por direção. | `1` |

## Gráficos

Quando uma superfície de gráfico está disponível, a estratégia desenha:
- Velas de preço.
- Bandas de Bollinger.
- Oscilador estocástico em um painel separado.
- Operações executadas para validação visual rápida.

## Notas

- O tamanho do pip é derivado do passo de preço do instrumento e da precisão decimal, refletindo a lógica do multiplicador do MetaTrader.
- Níveis de proteção são recalculados usando `Security.ShrinkPrice` para garantir arredondamento de preço conforme ao exchange.
- Ajustes do trailing stop imitam o EA original apertando o stop apenas quando o preço avança pelo menos um pip além do stop anterior.
- A implementação não cria ordens pendentes; todas as entradas e saídas usam ordens a mercado exatamente como o consultor especialista fonte.
