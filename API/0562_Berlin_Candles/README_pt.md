# Estratégia Berlin Candles
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Estratégia que utiliza velas Berlin personalizadas derivadas de valores Heikin Ashi suavizados. Uma posição comprada é aberta quando uma vela Berlin de alta fecha acima da linha de base Donchian. Uma posição vendida é aberta quando uma vela Berlin de baixa fecha abaixo da linha de base.

## Detalhes

- **Critérios de entrada**:
  - **Comprado**: fechamento Berlin > abertura Berlin e fechamento Berlin > linha de base.
  - **Vendido**: fechamento Berlin < abertura Berlin e fechamento Berlin < linha de base.
- **Comprado/Vendido**: Ambos
- **Stops**: Nenhum por padrão
- **Valores padrão**:
  - `Smoothing` = 1
  - `BaselinePeriod` = 26
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
