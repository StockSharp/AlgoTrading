# Estratégia de Rompimento do Candle das 2:45 AM
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia intradiária monitora o candle das 2:45 AM e opera rompimentos de sua máxima ou mínima dentro das próximas barras. Quando o preço supera a máxima do candle, entra em uma posição comprada; quando o preço cai abaixo da mínima do candle, abre uma posição vendida. As posições são fechadas no final da janela de observação se nenhum rompimento oposto ocorrer.

## Detalhes

- **Critérios de entrada**:
  - **Comprado**: O preço rompe acima da máxima do candle das 2:45 AM dentro dos próximos `LookForwardBars` candles.
  - **Vendido**: O preço rompe abaixo da mínima do candle das 2:45 AM dentro dos próximos `LookForwardBars` candles.
- **Comprado/Vendido**: Ambos.
- **Critérios de saída**:
  - Fim da janela de observação ou rompimento oposto.
- **Stops**: Nenhum.
- **Valores padrão**:
  - `TargetHour` = 2
  - `TargetMinute` = 45
  - `LookForwardBars` = 2
  - `CandleType` = candles de 45 minutos
- **Filtros**:
  - Categoria: Rompimento baseado em tempo
  - Direção: Ambos
  - Indicadores: Nenhum
  - Stops: Não
  - Complexidade: Baixo
  - Período: Intradiário
  - Sazonalidade: Sim
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
