# Estratégia de Rompimento de Canal de Regressão
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia implementa um sistema de trading baseado em canal de regressão a partir do script MQL `e-Regr`.
Ela constrói uma linha de regressão linear sobre um número configurável de velas recentes e
adiciona bandas superior e inferior a uma distância de desvio padrão especificada. Regras de trading:

- **Entrada comprada:** quando a mínima da vela toca ou rompe abaixo da banda inferior.
- **Entrada vendida:** quando a máxima da vela toca ou rompe acima da banda superior.
- **Saída:** quando o preço de fechamento cruza a linha de regressão na direção oposta.
- **Trailing Stop:** a lógica trailing opcional move o nível do stop após a operação
  ter alcançado um lucro configurado.

## Parâmetros

| Nome            | Descrição                                                       |
|-----------------|-----------------------------------------------------------------|
| `CandleType`    | Tipo de vela utilizado para os cálculos.                        |
| `Length`        | Número de velas para a regressão e o desvio padrão.             |
| `Deviation`     | Multiplicador de desvio padrão para a largura do canal.         |
| `UseTrailing`   | Ativa a lógica de trailing stop.                                |
| `TrailingStart` | Lucro necessário antes de o trailing começar.                   |
| `TrailingStep`  | Distância entre o preço e o trailing stop.                      |

A estratégia usa a API de alto nível do StockSharp através dos métodos `SubscribeCandles` e `Bind`
para receber dados de velas e valores de indicadores.
