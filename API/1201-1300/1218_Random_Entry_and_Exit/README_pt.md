# Estratégia de Entrada e Saída Aleatória
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia usa números aleatórios para entrar e sair de posições. Para cada candle concluído, um valor aleatório entre 0 e 1 é gerado. Se o valor estiver abaixo do limiar de entrada, uma operação é aberta. Outro valor aleatório controla as saídas. Operações compradas e vendidas podem ser habilitadas separadamente.

## Detalhes

- **Critérios de entrada**: valor aleatório < Limiar de entrada.
- **Critérios de saída**: valor aleatório < Limiar de saída.
- **Comprado/Vendido**: Ambos, configurável individualmente.
