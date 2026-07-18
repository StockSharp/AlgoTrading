# Estratégia Karacatica
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A estratégia Karacatica é uma abordagem de seguidor de tendência que combina ação do preço com o Índice Direcional Médio (ADX). Ela busca situações em que o preço de fechamento atual é maior ou menor do que o preço de fechamento um número especificado de candles atrás e confirma o movimento com a dominância da linha +DI ou -DI.

## Indicadores
- **Average Directional Index (ADX)** – mede a força da tendência e fornece os componentes +DI e -DI.
- **Comparação de preços** – verifica se o último fechamento está acima ou abaixo do fechamento de *Period* candles atrás.

## Parâmetros
- `Period` – número de candles utilizado tanto para o cálculo do ADX quanto para o lookback da comparação de preços. O padrão é 70.
- `TakeProfitPercent` – take-profit expresso como percentual do preço de entrada. O padrão é 2%.
- `StopLossPercent` – stop-loss expresso como percentual do preço de entrada. O padrão é 1%.
- `CandleType` – período dos candles a serem assinados. O padrão é 1 hora.

## Lógica de negociação
- **Entrada comprada**: `Close > Close[Period]` e `+DI > -DI` sem sinal comprado existente. Fecha posições vendidas e abre uma comprada.
- **Entrada vendida**: `Close < Close[Period]` e `-DI > +DI` sem sinal vendido existente. Fecha posições compradas e abre uma vendida.
- **Proteção de posição**: `StartProtection` aplica tanto os percentuais de take-profit quanto de stop-loss.

## Notas de uso
- Projetada para a API de alto nível do StockSharp; assina candles e vincula o indicador ADX.
- A estratégia fecha automaticamente as posições opostas quando um novo sinal aparece.
- Nenhuma implementação em Python é fornecida por enquanto.

## Aviso legal
Este exemplo é apenas para fins educacionais e não garante lucros. O trading envolve riscos significativos. Sempre teste as estratégias minuciosamente antes de implantá-las em mercados ao vivo.
