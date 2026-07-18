# Estratégia de Equilíbrio de Barras
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia mede o equilíbrio entre os movimentos de alta e de baixa dentro de cada vela. Um equilíbrio positivo sugere que os compradores dominam a barra, enquanto um equilíbrio negativo aponta para pressão vendedora.

O sistema suaviza esse equilíbrio com uma média móvel. Quando tanto o equilíbrio atual quanto sua média estão acima de zero, a estratégia entra em uma posição comprada. Quando ambos caem abaixo de zero, entra vendida.

## Detalhes

- **Critérios de entrada**: equilíbrio > 0 e média > 0 para comprado; equilíbrio < 0 e média < 0 para vendido.
- **Critérios de saída**: o sinal oposto aciona a reversão da posição.
- **Indicadores**: bar balance personalizado, SMA.
- **Comprado/Vendido**: ambos.
- **Stop-loss**: nenhum.
