# Estratégia Kositbablo 10
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Estratégia multi-período para EURUSD usando sinais RSI e EMA.
Verifica indicadores diários e horários e abre ordens a mercado quando ambos os filtros de tendência se alinham.

## Parâmetros
- **Take Profit** – take profit em pontos.
- **Stop Loss** – stop loss em pontos.
- **Turbo Mode** – permitir novas operações mesmo se uma posição existir.

## Regras
- Comprar quando o RSI(11) diário < 60, o RSI(5) horário < 48 e EMA20 > EMA2.
- Vender quando o RSI(22) diário > 38, o RSI(20) horário > 60 e EMA23 > EMA12.
- As operações só ocorrem após o fechamento da vela horária.
