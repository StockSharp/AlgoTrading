# Estratégia de Padrão de Velas Eugene
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Estratégia que opera um padrão de candles descrito por "Eugene". O algoritmo analisa as últimas quatro velas, verifica barras internas e formações especiais "pássaro", e calcula níveis de rompimento. As posições são abertas em rompimentos das extremidades do candle anterior quando condições adicionais de confirmação são atendidas. Níveis opcionais de stop loss e take profit são expressos em passos de preço.

## Detalhes

- **Critérios de entrada**:
  - Comprado: máxima atual acima da máxima anterior, mínima anterior abaixo da máxima anterior anterior, mínima atual acima da mínima anterior, e confirmação por nível zig ou filtro de tempo.
  - Vendido: mínima atual abaixo da mínima anterior, máxima anterior acima da mínima anterior anterior, máxima atual abaixo da máxima anterior, e confirmação por nível zig ou filtro de tempo.
- **Comprado/Vendido**: Ambos
- **Critérios de saída**:
  - Comprado: vender quando um sinal oposto aparece ou stop loss/take profit é atingido.
  - Vendido: comprar quando um sinal oposto aparece ou stop loss/take profit é atingido.
- **Stops**: distância fixa em passos de preço
- **Valores padrão**:
  - `Volume` = 1m
  - `StopLossPoints` = 0
  - `TakeProfitPoints` = 0
  - `InvertSignals` = false
  - `CandleType` = TimeSpan.FromMinutes(1).TimeFrame()
- **Filtros**:
  - Categoria: Padrão
  - Direção: Ambos
  - Indicadores: Nenhum
  - Stops: Opcional
  - Complexidade: Intermediário
  - Período: Curto prazo
  - Sazonalidade: Intradiário (filtro hora >= 8)
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
