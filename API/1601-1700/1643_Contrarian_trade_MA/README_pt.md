# Estratégia de Operação Contrarian com MA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Um sistema contrarian semanal que avalia máximas, mínimas anteriores e uma média móvel para abrir operações ao final de cada semana. A posição é mantida por uma semana independentemente da direção.

O método é projetado para os principais pares de moedas, mas pode ser aplicado a qualquer ativo líquido com dados semanais.

## Detalhes

- **Critérios de entrada**:
  - **Compra**: O fechamento da semana anterior está acima da máxima mais alta do período de análise, ou a média móvel está acima da abertura semanal.
  - **Venda**: O fechamento da semana anterior está abaixo da mínima mais baixa do período de análise, ou a média móvel está abaixo da abertura semanal.
- **Comprado/Vendido**: Ambos.
- **Critérios de saída**: A posição é fechada após ser mantida por uma semana.
- **Stops**: Nenhum.
- **Período**: Velas semanais.
