# Estratégia de Bollinger Bands Aprimorada
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Compra quando o preço cai abaixo da banda inferior de Bollinger enquanto o mercado permanece acima de uma EMA de 200 períodos.  
Um stop loss é colocado em `entrada - ATR * stop`, e depois que o preço sobe `ATR * trail` acima da entrada, a banda do meio se torna um alvo trailing.

## Detalhes

- **Critérios de entrada**: `Low > EMA` e `Low <= Banda inferior`.
- **Comprado/Vendido**: Somente comprado.
- **Critérios de saída**: Fechamento abaixo da banda do meio após ativação do trailing ou mínimo abaixo do stop.
- **Stops**: Stop loss baseado em ATR.
- **Valores padrão**:
  - Período de Bollinger = 20
  - Período de EMA = 200
  - Período de ATR = 14
  - Stop ATR = 1.75
  - Trail ATR = 2.25

