# Gerenciador de Stops Virtuais
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Estratégia convertida do advisor MetaTrader "VR---STEALS-3-EN". Implementa funcionalidades ocultas de gerenciamento de ordens: stop-loss, take-profit, trailing stop e ponto de equilíbrio. A estratégia abre uma posição comprada na primeira vela e gerencia os níveis de saída virtualmente sem colocar ordens de proteção visíveis na bolsa.

## Parâmetros
- **Volume**: volume de ordem.
- **Take Profit (points)**: distância em pontos para fechar a posição com lucro.
- **Stop Loss (points)**: distância em pontos para fechar a posição com perda.
- **Trailing Stop (points)**: distância do trailing stop a partir do preço mais alto.
- **Breakeven (points)**: lucro em pontos após o qual o stop-loss é movido para o preço de entrada.
- **Candle Type**: série de velas usada para processamento.
