# Estratégia de Indicador de Razão de Volume e Volatilidade WODI
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Estratégia simplificada derivada do script do TradingView **"Volume and Volatility Ratio Indicator - WODI"**. Monitora o produto do volume e da volatilidade do preço para identificar possíveis reversões. Quando o índice de volatilidade supera um limiar dinâmico e as velas recentes mostram uma mudança de direção, a estratégia abre uma posição com gestão de risco baseada em Fibonacci.

## Detalhes

- **Entrada**: Volume alto e volatilidade combinados com um padrão de reversão de velas.
- **Saída**: Stop loss e take profit calculados a partir do range da vela e multiplicadores de Fibonacci.
- **Comprado/Vendido**: Ambos.
- **Período**: Qualquer.
- **Indicadores**: SMA.

Esta é uma versão educacional simplificada. A lógica original do TradingView foi reduzida.
