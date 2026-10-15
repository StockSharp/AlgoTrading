# Estratégia ForexProfitBoost
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A estratégia **ForexProfitBoost** é um sistema de trading de reversão que combina uma Média Móvel Exponencial (EMA) rápida e uma Média Móvel Simples (SMA) lenta. A estratégia aguarda o cruzamento da EMA rápida com a SMA lenta e então opera contra a direção do cruzamento, esperando um recuo do preço. Níveis opcionais de stop-loss e take-profit em pontos de preço absolutos podem ser configurados para gestão de risco.

## Indicadores
- **EMA (rápida)**: período padrão de 7.
- **SMA (lenta)**: período padrão de 21.

## Regras de trading
1. Assinar o período de candles selecionado.
2. Calcular os valores de EMA e SMA em cada candle finalizado.
3. Quando a EMA rápida cruza **abaixo** da SMA lenta:
   - Fechar quaisquer posições vendidas.
   - Abrir uma nova posição comprada.
4. Quando a EMA rápida cruza **acima** da SMA lenta:
   - Fechar quaisquer posições compradas.
   - Abrir uma nova posição vendida.
5. Aplicar níveis de stop-loss e take-profit relativos ao preço de entrada, se especificados.

## Parâmetros
| Nome | Descrição | Padrão |
|------|-----------|--------|
| `FastPeriod` | Período para a EMA rápida. | 7 |
| `SlowPeriod` | Período para a SMA lenta. | 21 |
| `StopLoss` | Distância do stop-loss em pontos de preço. | 1000 |
| `TakeProfit` | Distância do take-profit em pontos de preço. | 2000 |
| `CandleType` | Período utilizado para os cálculos. | 1 hora |

## Notas
- A estratégia usa a API de alto nível do StockSharp e não armazena coleções históricas.
- As operações são executadas apenas com ordens a mercado após a finalização de um candle.
- Todos os comentários no código fonte estão escritos em inglês, conforme exigido.
