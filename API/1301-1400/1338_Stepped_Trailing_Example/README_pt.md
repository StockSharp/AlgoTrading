# Exemplo de Estratégia de Trailing Escalonado
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Uma estratégia de exemplo demonstrando gerenciamento de operações em três etapas com stop trailing opcional.

A estratégia entra comprado quando a SMA de 14 períodos cruza acima da SMA de 28 períodos. O risco é controlado por um stop-loss e três alvos de lucro:
- Após o primeiro alvo, o stop se move para o ponto de equilíbrio.
- Após o segundo alvo, o stop se move para o primeiro alvo.
- Na terceira etapa, a posição sai no terceiro alvo ou inicia um stop trailing.

Este exemplo mostra como escalonar lucros e proteger posições à medida que avançam.
