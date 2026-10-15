# Estratégia de Cruzamento Kyrie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia opera cruzamentos de médias móveis exponenciais (EMA). Compra quando a EMA de curto prazo cruza acima da EMA de longo prazo e vende a descoberto quando a EMA de curto prazo cruza abaixo da EMA de longo prazo. O stop-loss é colocado a uma porcentagem configurável do preço de entrada.

## Parâmetros
- Tipo de vela
- Período da EMA curta
- Período da EMA longa
- Percentual de risco
