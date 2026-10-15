# Estratégia de Divergência para Múltiplos Indicadores v4
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia detecta divergências entre o preço e múltiplos indicadores de momentum (MACD, RSI, Stochastic, CCI, Momentum, OBV, MFI).
Uma posição é aberta quando pelo menos um número especificado de indicadores mostra divergência na mesma direção.

## Detalhes
- **Critérios de entrada**: Entrar comprado quando o preço cai enquanto a maioria dos indicadores sobe (divergência positiva). Entrar vendido quando o preço sobe enquanto a maioria dos indicadores cai (divergência negativa).
- **Comprado/Vendido**: Ambos
- **Critérios de saída**: Divergência oposta ou proteção de posição
- **Stops**: Percentuais de take profit e stop loss configuráveis
- **Valores padrão**: Velas de 5m, 2 confirmações, 4% take profit, 2% stop loss
- **Filtros**: Usa vários indicadores de momentum para confirmação
