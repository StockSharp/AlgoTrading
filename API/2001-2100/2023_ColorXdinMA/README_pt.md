# Estratégia ColorXdinMA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A estratégia ColorXdinMA implementa o indicador XdinMA, calculado como `ma_main * 2 - ma_plus`, onde ambos os componentes são médias móveis simples com comprimentos diferentes. A estratégia monitora a inclinação dessa linha e abre posições quando a inclinação muda de direção.

## Lógica de trading
- Quando o indicador estava caindo e vira para cima, uma posição comprada é aberta. Posições vendidas existentes são fechadas.
- Quando o indicador estava subindo e vira para baixo, uma posição vendida é aberta. Posições compradas existentes são fechadas.

Apenas candles completados são processados. As ordens são executadas a mercado.

## Parâmetros
| Nome | Descrição | Padrão |
| --- | --- | --- |
| `MainLength` | Período da média móvel principal. | 10 |
| `PlusLength` | Período da média móvel adicional. | 20 |
| `CandleType` | Período das candles utilizadas para cálculo. | 6 horas |

## Notas
Esta implementação é uma estratégia StockSharp de alto nível e pode ser estendida com funcionalidades de gestão de risco ou visualização conforme necessário.
