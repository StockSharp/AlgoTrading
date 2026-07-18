# Estratégia de Ciclo de Tendência ColorSchaff JJRSX
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia aplica o oscilador Schaff Trend Cycle baseado em médias JJRSX. Abre posições compradas ou vendidas quando o oscilador cruza níveis definidos pelo usuário.

## Detalhes

- **Critérios de entrada**:
  - Comprar quando o Schaff Trend Cycle cruza acima de `HighLevel`. Qualquer posição vendida é fechada primeiro.
  - Vender quando o Schaff Trend Cycle cruza abaixo de `LowLevel`. Qualquer posição comprada é fechada primeiro.
- **Comprado/Vendido**: Ambos.
- **Critérios de saída**: As posições fecham quando ocorre um sinal de entrada oposto.
- **Stops**: Nenhum.
- **Valores padrão**:
  - `Fast` = 23
  - `Slow` = 50
  - `Cycle` = 10
  - `HighLevel` = 60
  - `LowLevel` = -60
