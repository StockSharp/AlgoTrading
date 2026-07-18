# Estratégia de Rompimento de Swing PRO
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Estratégia de rompimento que opera quando o preço fecha além do último swing de alta ou baixa confirmado. A distância entre os últimos pontos de swing define os níveis de stop-loss e alvo.

## Detalhes

- **Comprado**: fechamento anterior acima do último swing de alta e máxima atual acima da máxima anterior.
- **Vendido**: fechamento anterior abaixo do último swing de baixa e mínima atual abaixo da mínima anterior.
- **Stops**: nível de swing oposto.
- **Alvos**: amplitude entre o último swing de alta e de baixa.
- **Indicadores**: cálculo interno de pivô.
