# Estratégia de Cruzamento EMA MACD no Início de Sessão
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia entra comprado quando uma EMA rápida cruza acima de uma EMA lenta e o histograma do MACD é positivo. Entra vendido no cruzamento oposto com histograma negativo. Se essas condições já forem verdadeiras na primeira barra de uma sessão de trading, uma posição é aberta imediatamente. As posições são fechadas em um cruzamento oposto ou quando a sessão termina.

## Detalhes

- **Critérios de entrada**:
  - EMA rápida cruza acima da EMA lenta com histograma MACD positivo.
  - Ou na primeira barra de sessão quando a EMA rápida está acima da EMA lenta e o histograma MACD é positivo.
- **Critérios de saída**:
  - Cruzamento EMA oposto ou fim de sessão.
- **Indicadores**: EMA, MACD.
- **Tipo**: Seguidor de tendência.
- **Período**: 5 minutos (padrão).
