# Estratégia de Scalping EA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Um sistema de scalping simples que mantém constantemente duas ordens pendentes: um buy stop acima do mercado e um sell stop abaixo. Quando o preço de mercado se aproxima demais de uma ordem ou se afasta demais, a ordem é substituída para manter uma distância fixa do preço atual. Ordens executadas usam offsets fixos de take profit e stop loss.

A estratégia não depende de indicadores e reage apenas a mudanças de preço por tick.

## Detalhes

- **Critérios de entrada**:
  - Colocar buy stop 100 pontos acima do preço e sell stop 100 pontos abaixo.
  - Ordens são substituídas se a distância ao preço se tornar muito pequena ou muito grande.
- **Comprado/Vendido**: Ambos.
- **Critérios de saída**:
  - Cada ordem carrega take profit e stop loss fixos.
- **Stops**: Sim, distância fixa.
- **Filtros**: Nenhum.
