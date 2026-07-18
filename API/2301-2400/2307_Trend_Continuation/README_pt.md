# Estratégia de Continuação de Tendência
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia identifica a continuação da tendência prevalecente usando um par de médias móveis exponenciais sobre os dados de preço. Uma posição comprada é aberta quando a EMA rápida cruza acima da EMA lenta, sinalizando continuação ascendente. Uma posição vendida é aberta quando a EMA rápida cruza abaixo da EMA lenta.

## Parâmetros
- **Fast EMA Length** – período para a EMA rápida (padrão: 20).
- **Candle Type** – período dos candles (padrão: 4 horas).
- **Stop Loss** – stop loss protetor aplicado via `StartProtection` (padrão: 1000).
- **Take Profit** – alvo de lucro aplicado via `StartProtection` (padrão: 2000).

## Como funciona
1. Ao iniciar, a estratégia subscreve a série de candles selecionada e cria dois indicadores EMA.
2. Cada candle concluído é processado para detectar cruzamentos entre as EMAs rápida e lenta.
3. Um cruzamento de baixo para cima abre uma posição comprada e fecha qualquer posição vendida. O cruzamento oposto abre uma posição vendida e fecha qualquer posição comprada.
4. O gerenciamento de risco é tratado pelos parâmetros integrados de stop loss e take profit.

Este exemplo é uma conversão simplificada do expert MQL original `Exp_TrendContinuation`.
