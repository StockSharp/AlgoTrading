# Estratégia Larry Connors de 3 Dias de Máximas e Mínimas
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Implementa a abordagem de reversão à média de 3 dias de máximas/mínimas de Larry Connors.

## Lógica

- Comprar quando:
  - O fechamento está acima da média móvel longa.
  - O fechamento está abaixo da média móvel curta.
  - As máximas e mínimas foram mais baixas durante três velas consecutivas.
- Sair quando o preço fecha acima da média móvel curta.

## Parâmetros

- **Long MA Length** — período para a SMA longa (padrão 200)
- **Short MA Length** — período para a SMA curta (padrão 5)
- **Candle Type** — período utilizado para a análise
