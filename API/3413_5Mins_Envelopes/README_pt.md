# Envelopes de 5 minutos
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia **5Mins Envelopes** reproduz o especialista MetaTrader que negocia velas de cinco minutos em torno de um envelope de média móvel ponderada linear.
Procura picos de preços que se estendem muito além das bandas e depois entra na direção de reversão à média.
Um filtro de spread, stop-loss estático, take-profit opcional e trailing stop refletem a gestão de dinheiro original.

## Lógica de negociação
- **Indicador**: Média Móvel Linear Ponderada (LWMA) calculada sobre o preço mediano (máximo+mínimo)/2 com período 3.
- **Largura do envelope**: desvio de 0,05% do valor LWMA (bandas superior e inferior).
- **Detecção de sinal** (avaliado na vela concluída anterior e no lance atual):
  - **Longo**: o mínimo da vela anterior fica mais de `DistancePoints` abaixo da faixa inferior **e** o lance atual também está além dessa distância.
  - **Curta**: a máxima da vela anterior fica mais de `DistancePoints` acima da faixa superior **e** o lance atual também está além dessa distância.
- **Filtros**:
  - Apenas uma posição por vez (novas entradas exigem que a posição atual seja plana).
  - Se `MaxSpreadPoints` for maior que zero, o spread de compra/venda deverá permanecer abaixo desse limite antes de enviar um novo pedido.

## Gestão de risco
- **Volume da ordem**: o parâmetro `TradeVolume` controla o tamanho da ordem de mercado.
- **Stop-loss**: `StopLossPoints` converte para a distância absoluta do preço usando o tamanho do tick do instrumento.
- **Realização de lucro**: Opcional `TakeProfitPoints`; definido como zero para desativar.
- **Parada final**: Opcional `TrailingStopPoints`; definido como zero para desativar.
- **Proteção**: O auxiliar `StartProtection` aplica todas as saídas com ordens de mercado, correspondendo ao comportamento MetaTrader.

## Parâmetros
- `TradeVolume = 1m`
- `DistancePoints = 140`
- `EnvelopePeriod = 3`
- `EnvelopeDeviationPercent = 0.05m`
- `StopLossPoints = 250`
- `TakeProfitPoints = 0`
- `TrailingStopPoints = 120`
- `MaxSpreadPoints = 25`
- `CandleType = TimeFrame(5 minutes)`

## Etiquetas
- Categoria: Reversão à Média
- Direção: Ambos
- Indicadores: WeightedMovingAverage
- Stops: Sim (fixo + final)
- Prazo: intradiário (M5)
- Complexidade: Iniciante
- Nível de risco: Médio
- Sazonalidade: Não
- Redes Neurais: Não
- Divergência: Não
