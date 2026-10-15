# Estratégia de Scalping Rampok
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Sistema de scalping que opera quando o preço rompe os envelopes da média móvel.
A estratégia entra comprada quando o preço cruza acima da banda inferior e
vendida quando o preço cruza abaixo da banda superior. As posições são protegidas
por parâmetros opcionais de take profit, stop loss e stop de rastreamento.

## Detalhes

- **Critérios de entrada**:
  - **Compra**: fechamento anterior abaixo da banda inferior e fechamento atual acima dela.
  - **Venda**: fechamento anterior acima da banda superior e fechamento atual abaixo dela.
- **Comprado/Vendido**: Ambos.
- **Critérios de saída**:
  - Take profit, stop loss ou stop de rastreamento.
- **Stops**: SL/TP e trailing configuráveis.
- **Filtros**: nenhum.
