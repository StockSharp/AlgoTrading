# Estratégia de Analisador de Ordens de Bloco de Volume
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Estratégia simplificada baseada no script do TradingView **"Volume Block Order Analyzer"**. Mede como grandes picos de volume impactam a direção do preço e acumula esse efeito ao longo do tempo. Quando o impacto acumulado ultrapassa limites definidos pelo usuário, a estratégia entra em operações e as protege com um trailing stop.

## Detalhes

- **Entrada**: Impacto acumulado acima ou abaixo do limiar.
- **Saída**: Trailing stop baseado em percentagem a partir da entrada.
- **Comprado/Vendido**: Ambos.
- **Indicadores**: SMA.
- **Período**: Qualquer.

Este port foca na ideia central; muitas funcionalidades visuais do script original foram omitidas.
