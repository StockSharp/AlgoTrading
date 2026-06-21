# Estratégia Trend Trader Remastered
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia usa o indicador Parabolic SAR para seguir tendências. Uma ordem de compra é enviada quando o preço cruza acima do SAR e uma ordem de venda quando o preço cruza abaixo. Um cruzamento oposto fecha a posição atual.

## Detalhes

- **Critérios de entrada**:
  - **Comprado**: O preço cruza acima do PSAR.
  - **Vendido**: O preço cruza abaixo do PSAR.
- **Saídas**: Um cruzamento oposto do PSAR fecha a operação.
- **Stops**: Sem stops adicionais.
- **Valores padrão**:
  - `Start` = 0.02
  - `Increment` = 0.02
  - `Max` = 0.2
