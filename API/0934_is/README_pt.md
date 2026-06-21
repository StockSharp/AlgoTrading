# Estratégia IS
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Uma estratégia simples que abre uma posição comprada quando a fonte selecionada é igual ao valor de ativação comprado e a fecha quando o valor muda para o oposto. Se a venda a descoberto estiver habilitada, a estratégia também abre uma posição vendida no sinal oposto. O take-profit e o stop-loss são especificados como percentuais do preço de entrada.

## Detalhes

- **Critérios de entrada**:
  - **Comprado**: A fonte é igual ao valor comprado e o valor anterior era diferente.
  - **Vendido**: A fonte é igual ao valor vendido e o valor anterior era diferente (se vendidos habilitados).
- **Critérios de saída**: Sinal inverso ou stop de proteção.
- **Stops**: Sim, take-profit e stop-loss como percentuais.
