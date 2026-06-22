# Estratégia Color Bears
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia constrói um oscilador Bears Power duplamente suavizado e opera com base nas mudanças de sua inclinação.

## Ideia
1. Calcular uma média móvel exponencial (MA1) dos preços de fechamento.
2. Calcular o Bears Power como a diferença entre a mínima da vela e MA1.
3. Suavizar o Bears Power com outra média móvel exponencial (MA2).
4. Rastrear se o valor suavizado sobe ou cai e reagir às reversões de inclinação.

## Regras de trading
- Quando o indicador muda de subindo para caindo (cor 0 → 2), fechar posições vendidas e abrir uma comprada.
- Quando o indicador muda de caindo para subindo (cor 2 → 0), fechar posições compradas e abrir uma vendida.
- Cada posição usa a propriedade `Volume` da estratégia como tamanho de ordem.

## Parâmetros
| Nome | Descrição |
|------|-----------|
| `Ma1Period` | Período do primeiro EMA utilizado para construir o Bears Power. |
| `Ma2Period` | Período do EMA de suavização. |
| `CandleType` | Período de velas para os cálculos. |

## Notas
Esta implementação em C# é adaptada do especialista MQL "ColorBears" (pasta `MQL/14314`).
O algoritmo depende de indicadores padrão do StockSharp e bindings de API de alto nível.
