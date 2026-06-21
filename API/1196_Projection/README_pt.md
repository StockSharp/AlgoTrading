# Estratégia de Projeção
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia calcula a variação percentual média das aberturas diárias recentes e projeta níveis de rompimento em torno da abertura do dia atual. Posições compradas são abertas quando o preço rompe acima da projeção superior, enquanto posições vendidas são abertas quando rompe abaixo da projeção inferior. Stops de proteção são colocados próximos ao lado oposto da projeção.

## Detalhes

- **Critérios de entrada**:
  - **Comprado**: o preço cruza acima de `open + threshold`.
  - **Vendido**: o preço cruza abaixo de `open - threshold`.
- **Critérios de saída**:
  - **Comprado**: o preço cai abaixo do stop comprado.
  - **Vendido**: o preço sobe acima do stop vendido.
- **Stops**: sim, baseados na variação média.
- **Parâmetros**:
  - `TargetMultiple` – multiplicador da variação média (padrão 0.2).
  - `Threshold` – percentual da variação média usado para formar os níveis de rompimento (padrão 1.0).
  - `CalculationPeriod` – número de dias no cálculo da média (padrão 5).
