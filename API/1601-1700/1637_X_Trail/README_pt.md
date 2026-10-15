# Estratégia X-Trail
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia gera operações quando uma média móvel simples rápida e uma lenta,
calculadas sobre o preço mediano, se cruzam. A lógica espelha o script MQL original
**X_trail.mq4**, que usava alertas nesses cruzamentos.

Uma posição comprada é aberta quando a MA rápida permanece acima da MA lenta na vela
atual e na anterior, enquanto estava abaixo duas velas atrás. O padrão oposto ativa
uma posição vendida. As posições são invertidas a cada novo sinal.

## Detalhes

- **Critérios de entrada**:
  - **Comprado**: MA rápida > MA lenta nas últimas duas velas concluídas e MA rápida estava abaixo da MA lenta duas velas antes.
  - **Vendido**: MA rápida < MA lenta nas últimas duas velas concluídas e MA rápida estava acima da MA lenta duas velas antes.
- **Comprado/Vendido**: Ambos.
- **Critérios de saída**:
  - Cruzamento oposto (inversão de posição).
- **Stops**: Nenhum.
- **Indicadores**:
  - Duas médias móveis simples calculadas a partir do preço mediano.
