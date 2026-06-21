# Modelo de Precificação de Opções Binomial
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Este módulo calcula o preço teórico de uma opção usando uma árvore binomial de dois passos. Suporta estilos americano ou europeu e opções Call ou Put para diferentes classes de ativos. A volatilidade é estimada por meio do desvio padrão dos preços de fechamento.

Nenhum sinal de negociação é gerado; a estratégia registra o preço calculado da opção para cada candle finalizado.

## Detalhes
- **Função**: Precificação de opções (sem negociações)
- **Parâmetros**: Strike Price, Risk Free Rate, Dividend Yield, Asset Class, Option Style, Option Type, Minutes/Hours/Days to expiry, Timeframe
- **Indicadores**: Standard Deviation
- **Comprado/Vendido**: N/A
- **Stops**: Nenhum
