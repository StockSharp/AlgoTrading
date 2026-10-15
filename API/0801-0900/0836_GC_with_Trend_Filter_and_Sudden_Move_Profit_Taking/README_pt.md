# Estratégia GC com Filtro de Tendência e Realização de Lucro em Movimentos Bruscos
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia usa um cruzamento de SMA 5/25 com um filtro de tendência de 75 períodos e confirmação ADX. As posições são encerradas quando o preço se move mais do que um percentual especificado em relação ao fechamento anterior, capturando movimentos bruscos.

## Detalhes
- **Entrada**: Comprado quando a SMA 5 cruza acima da SMA 25, preço acima da SMA 75 e ADX acima do limiar. Vendido nas condições opostas.
- **Saída**: Sinal oposto ou movimento brusco que excede o percentual configurado.
- **Indicadores**: SMA, Average Directional Index.
- **Mercados**: Qualquer.
