# Estratégia TrendGuard Scalper SSL + Hama Candle com Zonas de Consolidação
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia combina um canal SSL simples com a direção das velas Hama. Uma posição comprada é aberta quando o fechamento está acima da média SSL, o fechamento Hama (EMA 20) está acima da linha Hama longa (EMA 100) e o preço permanece acima do fechamento Hama. Operações vendidas usam as condições opostas. O ATR é usado para marcar períodos de baixa volatilidade como possíveis zonas de consolidação.

## Detalhes
- **Entrada**: as tendências SSL e Hama estão alinhadas com confirmação do preço.
- **Saída**: percentuais fixos de take‑profit e stop‑loss.
- **Indicadores**: SMA, EMA, ATR.
- **Filtros**: detecção de consolidação apenas para análise.
