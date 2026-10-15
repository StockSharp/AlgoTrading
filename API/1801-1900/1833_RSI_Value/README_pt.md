# Estratégia RSI Value
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Estratégia que negocia com base no Índice de Força Relativa (RSI) cruzando um valor central.

A ideia é observar o RSI cruzando acima ou abaixo de um nível configurável (padrão 50). Quando o indicador se move de abaixo para acima deste nível, uma posição comprada é aberta. Quando cruza de volta abaixo do nível, uma posição vendida é aberta. As posições existentes são encerradas no cruzamento oposto. Stop-loss opcional, take-profit e trailing stop protegem a negociação.

## Detalhes

- **Critérios de entrada**: Comprar quando RSI cruza acima do nível. Vender quando RSI cruza abaixo.
- **Comprado/Vendido**: Ambas as direções.
- **Critérios de saída**: Cruzamento oposto ou trailing stop.
- **Stops**: Stop-loss fixo opcional, take-profit e trailing stop.
- **Valores padrão**:
  - `RsiPeriod` = 14
  - `RsiLevel` = 50
  - `StopLoss` = 100
  - `TakeProfit` = 200
  - `TrailingStop` = 0
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoria: Oscilador
  - Direção: Ambos
  - Indicadores: RSI
  - Stops: Sim
  - Complexidade: Básico
  - Período: Intradiário (5m)
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
