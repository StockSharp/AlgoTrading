# Bot de Trailing TP
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia opera cruzamentos de SMA tanto em posições compradas quanto vendidas. Cada posição define um take profit e stop loss fixos. Após atingir o alvo de lucro, o stop pode seguir o preço para proteger os ganhos.

## Detalhes

- **Entrada**: SMA rápida cruza a SMA lenta.
- **Saída**: Stop loss, take profit ou trailing stop.
- **Indicadores**: SMA.
- **Direção**: Ambos.
- **Risco**: Stop loss fixo com trailing opcional.
