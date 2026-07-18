# Estratégia de Investidor Universal
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral

A **Estratégia de Investidor Universal** usa o cruzamento entre a Média Móvel Exponencial (EMA) e a Média Móvel Ponderada Linear (LWMA) para determinar a direção do mercado. Confirma a força da tendência verificando que ambas as médias se movem na mesma direção.

## Lógica

- **Entrada de compra**: LWMA está acima da EMA e ambas as médias estão subindo.
- **Entrada de venda**: LWMA está abaixo da EMA e ambas as médias estão caindo.
- **Saída de compra**: LWMA cruza abaixo da EMA.
- **Saída de venda**: LWMA cruza acima da EMA.

A estratégia reduz o tamanho da posição após operações perdedoras consecutivas quando o fator de redução está habilitado.

## Parâmetros

| Nome | Descrição |
| ---- | ----------- |
| `MovingPeriod` | Comprimento para cálculos de EMA e LWMA. |
| `DecreaseFactor` | Fator de redução de lotes após perdas (0 desabilita a redução). |
| `CandleType` | Tipo de dados de candles para cálculos. |
| `Volume` | Volume base de negociação das configurações da estratégia. |

## Notas

- Funciona apenas com candles fechados.
- Usa a API de alto nível do StockSharp com vinculação de indicadores.
- Não há versão em Python disponível.
