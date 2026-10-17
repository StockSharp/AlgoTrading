# Estratégia de Daytrading ES por Comprimento de Pavio
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia entra em uma posição comprada quando o comprimento total do pavio de um candle supera sua média móvel mais um deslocamento, e sai após manter a posição por um número fixo de barras.

### Contrato de sinais verificável (C# e Python)

Para cada **candle de 1 minuto concluído**, o pavio total é `W = max(0, High − max(Open, Close)) + max(0, min(Open, Close) − Low)`. As **20 velas anteriores**, excluindo a vela atual, calculam `VWMA(W) = sum(Wᵢ × Volumeᵢ) / sum(Volumeᵢ)`; se todos os volumes forem zero, usa-se a média simples. `MaType=Simple` seleciona a média sem pesos. `MaOffset=10` representa **10 ticks mínimos do instrumento**; o limiar é `VWMA(W) + 10 × Security.PriceStep`. Após 20 candles de aquecimento, apenas se **compra** sem posição quando `W > limiar`. Após **18 candles concluídos adicionais**, vende-se toda a posição restante. Sem short, stop-loss ou take-profit. Padrões: `MaLength=20`, `MaType=VolumeWeighted`, `MaOffset=10`, `HoldPeriods=18`, `CandleType=1 minuto`.



## Detalhes

- **Critérios de entrada**: Comprimento total do pavio maior que a média móvel com deslocamento.
- **Critérios de saída**: Posição fechada após manter `Hold periods` barras.
- **Comprado/Vendido**: Somente comprado.
- **Stops**: Nenhum.
- **Valores padrão**:
  - `MA length` = 20
  - `MA type` = VolumeWeighted
  - `MA offset` = 10
  - `Hold periods` = 18
  - `Candle type` = velas de 1 minuto
- **Filtros**:
  - Categoria: Volatilidade
  - Direção: Comprado
  - Indicadores: Moving Average, comprimento de pavio
  - Stops: Não
  - Complexidade: Simples
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
