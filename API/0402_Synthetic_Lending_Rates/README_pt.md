# Estratégia de Taxas de Empréstimo Sintéticas
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia explora as diferenças entre as taxas de empréstimo sintéticas derivadas dos mercados de derivativos e os rendimentos de empréstimo on-chain. Ao tomar emprestado onde as taxas são baixas e emprestar onde as taxas são altas, captura o spread entre elas.

As posições são rebalanceadas regularmente para manter a neutralidade, e o risco é controlado por meio de limites de variação de taxas e filtros de liquidez.

## Detalhes

- **Dados**: Financiamento de swaps perpétuos e taxas de empréstimo DeFi.
- **Entrada**: Tomar emprestado no venue de taxa baixa e emprestar no de taxa alta quando o spread > limite.
- **Saída**: Fechar quando o spread reverter à média ou a liquidez se deteriorar.
- **Instrumentos**: Swaps perpétuos e plataformas DeFi.
- **Risco**: Limite de spread e stop de liquidez.

