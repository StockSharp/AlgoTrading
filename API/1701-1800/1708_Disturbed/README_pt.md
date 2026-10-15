# Estratégia Disturbed
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia de hedge abre simultaneamente ordens de mercado compradas e vendidas e as gerencia com base no spread atual. Assim que o preço se move um spread contra qualquer um dos lados, essa posição é fechada. A posição restante então visa um lucro ou perda igual a um múltiplo configurável do spread.

## Detalhes

- **Critérios de entrada**:
  - No início, são colocadas ordens de mercado de compra e venda simultaneamente.
- **Comprado/Vendido**: Ambos simultaneamente.
- **Critérios de saída**:
  - Fechar o lado que perde um spread.
  - Fechar o lado restante com lucro ou perda de `gainMultiplier * spread`.
- **Stops**: Implícitos através de níveis baseados no spread.
- **Filtros**: Nenhum.
