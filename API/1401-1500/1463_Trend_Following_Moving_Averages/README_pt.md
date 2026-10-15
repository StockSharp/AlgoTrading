# Estratégia de Seguimento de Tendência com Médias Móveis
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Calcula uma média móvel e mede sua tendência dentro de um canal de preços dinâmico.
Posições compradas são abertas quando a pontuação de tendência é positiva e vendidas quando é negativa.

## Detalhes

- **Entrada**:
  - **Comprado**: pontuação de tendência > 0
  - **Vendido**: pontuação de tendência < 0
- **Saída**: sinal inverso
- **Indicadores**: SMA, Highest, Lowest
- **Período**: configurável
- **Tipo**: Seguidor de tendência
