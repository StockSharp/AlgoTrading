# Estratégia Roboti ADX Profit
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia converte o consultor especializado original **RobotiADXProfitwining.mq4** para a API do StockSharp. Ela se baseia no Índice de Movimento Direcional (DMI) para determinar a direção da tendência.

## Lógica de negociação

- Usa o indicador `DirectionalIndex` com um período padrão de 14.
- Trabalha com velas de uma hora por padrão, mas o período pode ser alterado.
- Abre uma posição **comprada** quando a linha `+DI` cruza acima da linha `-DI` e nenhuma posição comprada está aberta.
- Abre uma posição **vendida** quando a linha `-DI` cruza acima da linha `+DI` e nenhuma posição vendida está aberta.
- As posições são protegidas por um trailing stop expresso como uma porcentagem do preço.

## Parâmetros

| Nome | Descrição | Padrão |
| ---- | ----------- | ------- |
| `DmiPeriod` | Período para o cálculo do DMI. | 14 |
| `CandleType` | Tipo de vela e período usados pela estratégia. | 1 hora |
| `TrailingStopPercent` | Tamanho do trailing stop em porcentagem. | 1% |

## Notas

A estratégia usa a API de vinculação de alto nível do StockSharp e evita chamadas diretas a buffers de indicadores. Todos os comentários no código estão em inglês.
