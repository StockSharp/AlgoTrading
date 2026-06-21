# Estratégia EMA SAR Bulls Bears
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia combina uma Média Móvel Exponencial (EMA) rápida e lenta, Parabolic SAR e indicadores Bulls/Bears Power. Opera apenas durante uma janela intradiária configurada e utiliza proteções simples de ganho e perda.

Uma posição vendida é aberta quando EMA3 está abaixo de EMA34, o Parabolic SAR está acima da máxima da vela e o Bears Power é negativo mas crescente. Uma posição comprada é aberta quando EMA3 está acima de EMA34, o SAR está abaixo da mínima da vela e o Bulls Power é positivo mas decrescente.

## Detalhes

- **Critérios de entrada**:
  - **Comprado**: EMA3 acima de EMA34, SAR abaixo da mínima da vela, Bulls Power > 0 e diminuindo.
  - **Vendido**: EMA3 abaixo de EMA34, SAR acima da máxima da vela, Bears Power < 0 e aumentando.
- **Comprado/Vendido**: Ambos.
- **Critérios de saída**: Sinal oposto ou stop/take acionado.
- **Stops**: Sim, take-profit absoluto (400 pontos) e stop-loss (2000 pontos).
- **Filtros**:
  - Opera apenas entre 09:00 e 17:00.
  - Funciona com velas de 15 minutos.
