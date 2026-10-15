# Estratégia de Direção por Tempo
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia baseada em tempo abre uma única posição comprada ou vendida durante uma janela predefinida e a fecha durante outra janela. A direção de entrada é configurável e o sistema monitora níveis opcionais de stop-loss e take-profit. A abordagem baseia-se exclusivamente em velas terminadas sem usar indicadores.

## Detalhes

- **Critérios de entrada**:
  - Quando o tempo da vela atual está dentro de `[OpenTime, OpenTime + TradeInterval)` e nenhuma posição está aberta, entrar na direção configurada.
- **Critérios de saída**:
  - Fechar a posição quando o tempo está dentro de `[CloseTime, CloseTime + TradeInterval)`.
  - Adicionalmente sair se os níveis de stop-loss ou take-profit forem atingidos.
- **Comprado/Vendido**: Configurável.
- **Stops**: Stop-loss e take-profit em unidades de preço relativas ao preço de entrada.
- **Valores padrão**:
  - `Trade` = Sell.
  - `OpenTime` = 1970-01-01 00:00.
  - `CloseTime` = 3000-01-01 00:00.
  - `TradeInterval` = 1 minuto.
  - `StopLoss` = 1000.
  - `TakeProfit` = 2000.
  - `Volume` = 0.1.
- **Filtros**:
  - Categoria: Baseado em tempo
  - Direção: Único
  - Indicadores: Nenhum
  - Stops: Sim
  - Complexidade: Simples
  - Período: Curto prazo
