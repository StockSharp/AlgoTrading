# Estratégia do Índice de Desvio Médio
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia usa o Índice de Desvio Médio (MDX) para negociar desvios de uma EMA filtrada por ATR.
Uma posição comprada é aberta quando o MDX sobe acima do nível especificado,
e uma posição vendida quando cai abaixo do nível negativo.

## Detalhes

- **Entrada**:
  - Comprado quando MDX > Level
  - Vendido quando MDX < -Level
- **Saída**: sinal oposto.
- **Indicadores**: EMA e ATR.
